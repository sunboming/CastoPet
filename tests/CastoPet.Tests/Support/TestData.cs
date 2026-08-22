namespace CastoPet.Tests;

internal static partial class TestSuite
{
    static IReadOnlyList<IdleFrameDiagnostic> ReadIdleFrameDiagnostics()
    {
        var workspace = FindWorkspaceRoot();
        var idleRoot = System.IO.Path.Combine(workspace, "src", "CastoPet", "Assets", "Runtime", "Castorice", "States", "Idle");
        var frames = Directory
            .EnumerateFiles(idleRoot, "Castorice.Idle.*.png", SearchOption.TopDirectoryOnly)
            .OrderBy(System.IO.Path.GetFileName, StringComparer.Ordinal)
            .ToArray();

        var diagnostics = new List<IdleFrameDiagnostic>();
        for (var index = 0; index < frames.Length; index++)
        {
            using var bitmap = new Bitmap(frames[index]);
            var bounds = FindVisibleBounds(bitmap);
            diagnostics.Add(new IdleFrameDiagnostic(
                Name: System.IO.Path.GetFileName(frames[index]),
                Width: bitmap.Width,
                Height: bitmap.Height,
                Bounds: bounds,
                CenterX: bounds.Left + bounds.Width / 2d,
                AdjacentAverageDelta: 0));
        }

        for (var index = 0; index < diagnostics.Count; index++)
        {
            var current = frames[index];
            var next = frames[(index + 1) % frames.Length];
            using var currentBitmap = new Bitmap(current);
            using var nextBitmap = new Bitmap(next);
            diagnostics[index] = diagnostics[index] with
            {
                AdjacentAverageDelta = CalculateAverageRgbaDelta(currentBitmap, nextBitmap),
            };
        }

        return diagnostics;
    }

    static Rectangle FindVisibleBounds(Bitmap bitmap)
    {
        var minX = bitmap.Width;
        var minY = bitmap.Height;
        var maxX = -1;
        var maxY = -1;

        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).A <= 8)
                {
                    continue;
                }

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        if (maxX < minX || maxY < minY)
        {
            return Rectangle.Empty;
        }

        return Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
    }

    static double CalculateAverageRgbaDelta(Bitmap current, Bitmap next)
    {
        if (current.Width != next.Width || current.Height != next.Height)
        {
            throw new InvalidOperationException("Idle frames must have matching dimensions.");
        }

        long total = 0;
        long samples = 0;
        for (var y = 0; y < current.Height; y += 2)
        {
            for (var x = 0; x < current.Width; x += 2)
            {
                var a = current.GetPixel(x, y);
                var b = next.GetPixel(x, y);
                total += Math.Abs(a.R - b.R);
                total += Math.Abs(a.G - b.G);
                total += Math.Abs(a.B - b.B);
                total += Math.Abs(a.A - b.A);
                samples++;
            }
        }

        return total / (samples * 4d);
    }

    static string FindWorkspaceRoot()
    {
        var current = new DirectoryInfo(Environment.CurrentDirectory);
        while (current is not null)
        {
            if (File.Exists(System.IO.Path.Combine(current.FullName, "CastoPet.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not find workspace root.");
    }

    static void CreateExternalSkinResources(string manifestDirectory)
    {
        var root = System.IO.Path.Combine(manifestDirectory, "Resources");
        WriteTestPng(System.IO.Path.Combine(root, "Default.png"));
        WriteTestPng(System.IO.Path.Combine(root, "Idle", "00.png"));
        WriteTestPng(System.IO.Path.Combine(root, "Move", "00.png"));
        WriteTestPng(System.IO.Path.Combine(root, "Blink", "00.png"));
    }

    static string WriteExternalSkinManifest(
        string manifestDirectory,
        string idleFrame = "Idle/00.png",
        string defaultCharacter = "Default.png",
        string resourceRoot = "Resources")
    {
        Directory.CreateDirectory(manifestDirectory);
        var manifestPath = System.IO.Path.Combine(manifestDirectory, "skin.json");
        var frameList = System.Text.Json.JsonSerializer.Serialize(idleFrame);
        File.WriteAllText(manifestPath, CreateExternalSkinJson(frameList, defaultCharacter, resourceRoot));
        return manifestPath;
    }

    static string CreateExternalSkinJson(
        string idleFrameList,
        string defaultCharacter = "Default.png",
        string resourceRoot = "Resources")
    {
        var encodedDefault = System.Text.Json.JsonSerializer.Serialize(defaultCharacter);
        var encodedResourceRoot = System.Text.Json.JsonSerializer.Serialize(resourceRoot);
        return $$"""
            {
              "schemaVersion": 2,
              "id": "external-test",
              "displayName": "External Test",
              "resourceRoot": {{encodedResourceRoot}},
              "defaultCharacter": {{encodedDefault}},
              "actions": [
                { "id": "idle", "kind": "idle", "frames": [{{idleFrameList}}] },
                { "id": "move", "kind": "move", "frames": ["Move/00.png"] },
                { "id": "blink", "kind": "blink", "frames": ["Blink/00.png"] }
              ]
            }
            """;
    }

    static void WriteTestPng(string path)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("Test PNG path must have a directory."));
        using var bitmap = new Bitmap(2, 2);
        bitmap.SetPixel(0, 0, Color.White);
        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    }

    static void WritePngHeader(string path, int width, int height)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("Test PNG path must have a directory."));
        Span<byte> header = stackalloc byte[24];
        new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }.CopyTo(header);
        header[11] = 13;
        header[12] = (byte)'I';
        header[13] = (byte)'H';
        header[14] = (byte)'D';
        header[15] = (byte)'R';
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header[16..20], width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header[20..24], height);
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        stream.Write(header);
    }

    static (int Width, int Height) ReadPngSize(string path)
    {
        Span<byte> header = stackalloc byte[24];
        using var stream = File.OpenRead(path);
        if (stream.Read(header) != header.Length)
        {
            throw new InvalidOperationException($"{path} is not a valid PNG.");
        }

        var width = ReadBigEndianInt32(header[16..20]);
        var height = ReadBigEndianInt32(header[20..24]);
        return (width, height);
    }

    static int ReadBigEndianInt32(ReadOnlySpan<byte> bytes)
    {
        return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
    }
}
