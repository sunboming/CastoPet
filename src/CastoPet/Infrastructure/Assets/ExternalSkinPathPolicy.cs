using System.Buffers.Binary;
using System.IO;

using CastoPet.Core.Skins;

namespace CastoPet.Infrastructure.Assets;

internal static class ExternalSkinPathPolicy
{
    private static ReadOnlySpan<byte> PngSignature =>
        [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];

    public static string ResolveResourceRoot(string manifestDirectory, string? resourceRoot)
    {
        var rootValue = resourceRoot?.Trim() ?? string.Empty;
        EnsureRelativePath(rootValue, "resourceRoot", allowEmpty: true);
        var fullManifestDirectory = GetFullPath(manifestDirectory, "manifest directory");
        RejectUncPath(fullManifestDirectory, "manifest directory");
        var fullRoot = GetFullPath(Path.Combine(fullManifestDirectory, rootValue), "resourceRoot");
        EnsureContained(fullManifestDirectory, fullRoot, "resourceRoot");
        RejectUncPath(fullRoot, "resourceRoot");
        if (ContainsReparsePoint(fullManifestDirectory, fullRoot))
        {
            throw new InvalidDataException("External skin resourceRoot cannot contain a symbolic link or junction.");
        }

        if (!Directory.Exists(fullRoot))
        {
            throw new InvalidDataException($"External skin resourceRoot does not exist: {fullRoot}.");
        }

        return fullRoot;
    }

    public static string ResolvePng(string resourceRoot, string relativePath)
    {
        EnsureRelativePath(relativePath, "image path", allowEmpty: false);
        var fullPath = GetFullPath(Path.Combine(resourceRoot, relativePath), "image path");
        EnsureContained(resourceRoot, fullPath, "image path");
        RejectUncPath(fullPath, "image path");
        if (!Path.GetExtension(fullPath).Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("External skin images must use the PNG file format.");
        }

        if (ContainsReparsePoint(resourceRoot, fullPath))
        {
            throw new InvalidDataException("External skin image paths cannot contain a symbolic link or junction.");
        }

        if (!File.Exists(fullPath))
        {
            throw new InvalidDataException($"External skin PNG does not exist: {fullPath}.");
        }

        ValidatePng(fullPath);
        return fullPath;
    }

    public static bool IsUncPath(string path) =>
        path.StartsWith("\\\\", StringComparison.Ordinal);

    internal static bool ContainsReparsePoint(
        string containmentRoot,
        string candidate,
        Func<string, FileAttributes>? attributeReader = null)
    {
        var root = Path.GetFullPath(containmentRoot);
        var fullCandidate = Path.GetFullPath(candidate);
        EnsureContained(root, fullCandidate, "candidate path");
        attributeReader ??= File.GetAttributes;

        var current = root;
        if (HasReparsePoint(current, attributeReader))
        {
            return true;
        }

        var relative = Path.GetRelativePath(root, fullCandidate);
        if (relative == ".")
        {
            return false;
        }

        foreach (var segment in relative.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            try
            {
                if (HasReparsePoint(current, attributeReader))
                {
                    return true;
                }
            }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
            {
                return false;
            }
        }

        return false;
    }

    private static bool HasReparsePoint(
        string path,
        Func<string, FileAttributes> attributeReader) =>
        (attributeReader(path) & FileAttributes.ReparsePoint) != 0;

    private static void ValidatePng(string path)
    {
        var file = new FileInfo(path);
        if (file.Length <= 0 || file.Length > ExternalSkinResourceLimits.MaxImageFileBytes)
        {
            throw new InvalidDataException(
                $"External skin PNG must contain 1 to {ExternalSkinResourceLimits.MaxImageFileBytes} bytes: {path}.");
        }

        Span<byte> header = stackalloc byte[24];
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Read(header) != header.Length
            || !header[..8].SequenceEqual(PngSignature)
            || !header[12..16].SequenceEqual("IHDR"u8))
        {
            throw new InvalidDataException($"External skin image is not a valid PNG: {path}.");
        }

        var width = BinaryPrimitives.ReadInt32BigEndian(header[16..20]);
        var height = BinaryPrimitives.ReadInt32BigEndian(header[20..24]);
        var pixels = (long)width * height;
        var aspectRatio = width > 0 && height > 0
            ? Math.Max((double)width / height, (double)height / width)
            : double.PositiveInfinity;
        if (width <= 0 || height <= 0
            || width > ExternalSkinResourceLimits.MaxImageDimension
            || height > ExternalSkinResourceLimits.MaxImageDimension
            || pixels > ExternalSkinResourceLimits.MaxImagePixels
            || aspectRatio > ExternalSkinResourceLimits.MaxImageAspectRatio)
        {
            throw new InvalidDataException(
                $"External skin PNG dimensions are outside the supported budget: {width}x{height} at {path}.");
        }
    }

    private static void EnsureRelativePath(string path, string name, bool allowEmpty)
    {
        if (!allowEmpty && string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidDataException($"External skin {name} is required.");
        }

        if (path.Length > ExternalSkinResourceLimits.MaxPathCharacters)
        {
            throw new InvalidDataException($"External skin {name} is too long.");
        }

        if (Path.IsPathRooted(path)
            || Path.IsPathFullyQualified(path)
            || IsUncPath(path)
            || Uri.TryCreate(path, UriKind.Absolute, out _))
        {
            throw new InvalidDataException($"External skin {name} must be a relative local path.");
        }
    }

    private static string GetFullPath(string path, string name)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidDataException($"External skin {name} is invalid.", ex);
        }
    }

    private static void EnsureContained(string root, string candidate, string name)
    {
        var relative = Path.GetRelativePath(root, candidate);
        if (relative == ".."
            || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal)
            || Path.IsPathRooted(relative))
        {
            throw new InvalidDataException($"External skin {name} must stay inside the skin resource root.");
        }
    }

    private static void RejectUncPath(string path, string name)
    {
        if (IsUncPath(path))
        {
            throw new InvalidDataException($"External skin {name} cannot use a UNC path.");
        }
    }
}
