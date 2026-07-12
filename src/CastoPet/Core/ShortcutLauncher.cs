using System.Diagnostics;
using System.IO;

namespace CastoPet.Core;

public sealed record ShortcutLaunchResult(
    bool Succeeded,
    string? Error = null);

public sealed class ShortcutLauncher
{
    private static readonly HashSet<string> UnsupportedFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bat", ".cmd", ".ps1", ".vbs", ".js", ".jse",
        ".wsf", ".wsh", ".hta", ".com", ".scr", ".msi", ".exe",
    };

    private readonly LoggingService _logger;
    private readonly Func<ProcessStartInfo, Process?> _startProcess;

    public ShortcutLauncher(
        LoggingService logger,
        Func<ProcessStartInfo, Process?>? startProcess = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _startProcess = startProcess ?? Process.Start;
    }

    public ProcessStartInfo CreateStartInfo(ShortcutDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Validate(definition);

        var startInfo = new ProcessStartInfo
        {
            FileName = definition.Target,
            Arguments = definition.Arguments ?? "",
            UseShellExecute = true,
        };

        if (!string.IsNullOrWhiteSpace(definition.WorkingDirectory))
        {
            startInfo.WorkingDirectory = definition.WorkingDirectory;
        }

        return startInfo;
    }

    public ShortcutLaunchResult Launch(ShortcutDefinition definition)
    {
        try
        {
            _startProcess(CreateStartInfo(definition));
            return new ShortcutLaunchResult(true);
        }
        catch (Exception ex)
        {
            var target = definition?.Target ?? "<missing>";
            TryLogFailure(target, ex);
            return new ShortcutLaunchResult(false, ex.Message);
        }
    }

    private static void Validate(ShortcutDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.Target))
        {
            throw new InvalidOperationException("Shortcut target is required.");
        }

        switch (definition.Type)
        {
            case ShortcutType.Program:
                if (!File.Exists(definition.Target) ||
                    !Path.GetExtension(definition.Target).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Program target must be an existing .exe file.");
                }

                break;

            case ShortcutType.File:
                if (!File.Exists(definition.Target))
                {
                    throw new InvalidOperationException("File target does not exist.");
                }

                if (UnsupportedFileExtensions.Contains(Path.GetExtension(definition.Target)))
                {
                    throw new InvalidOperationException("Executable scripts and installers cannot be launched as files.");
                }

                break;

            case ShortcutType.Folder:
                if (!Directory.Exists(definition.Target))
                {
                    throw new InvalidOperationException("Folder target does not exist.");
                }

                break;

            case ShortcutType.WindowsShortcut:
                if (!File.Exists(definition.Target) ||
                    !Path.GetExtension(definition.Target).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Windows shortcut target must be an existing .lnk file.");
                }

                break;

            case ShortcutType.WebUrl:
                if (!TryGetSafeWebUri(definition.Target, out _))
                {
                    throw new InvalidOperationException("Web target must be an absolute HTTP or HTTPS URL with a host.");
                }

                break;

            default:
                throw new InvalidOperationException($"Unsupported shortcut type: {definition.Type}.");
        }
    }

    private static bool TryGetSafeWebUri(string target, out Uri? uri)
    {
        if (!Uri.TryCreate(target, UriKind.Absolute, out uri) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            return false;
        }

        return uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
               uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }

    private void TryLogFailure(string target, Exception exception)
    {
        try
        {
            _logger.Error($"Failed to launch shortcut target '{target}'.", exception);
        }
        catch
        {
            // A logging failure must not turn a contained launch failure into an application error.
        }
    }
}
