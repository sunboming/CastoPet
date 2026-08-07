using CastoPet.StabilityRunner;

if (args.Any(argument => argument is "--help" or "-h"))
{
    PrintHelp();
    return 0;
}

var workspace = FindWorkspaceRoot(Environment.CurrentDirectory) ??
    FindWorkspaceRoot(AppContext.BaseDirectory) ??
    Environment.CurrentDirectory;
var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
var defaultPetExecutable = Path.Combine(
    workspace,
    "src",
    "CastoPet",
    "bin",
    "Release",
    "net10.0-windows",
    "CastoPet.exe");
var defaultOutput = Path.Combine(workspace, "artifacts", "stability-tests", timestamp);

try
{
    var options = StabilityRunnerOptions.Parse(args, defaultPetExecutable, defaultOutput);
    using var cancellation = new CancellationTokenSource();
    Console.CancelKeyPress += (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        cancellation.Cancel();
    };

    Console.WriteLine($"CastoPet executable: {options.PetExecutablePath}");
    Console.WriteLine($"Output directory: {options.OutputDirectory}");
    Console.WriteLine(options.Duration == TimeSpan.Zero
        ? "Duration: until Ctrl+C"
        : $"Duration: {options.Duration}");
    Console.WriteLine("Press Ctrl+C to stop and write the final summary.");

    await new StabilityMonitor(options).RunAsync(cancellation.Token);
    Console.WriteLine($"Stability session completed: {options.OutputDirectory}");
    return 0;
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine("Run with --help for usage.");
    return 2;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}

static string? FindWorkspaceRoot(string startPath)
{
    var directory = new DirectoryInfo(startPath);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "CastoPet.sln")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    return null;
}

static void PrintHelp()
{
    Console.WriteLine("""
        CastoPet StabilityRunner

        Automatically starts or attaches to CastoPet and records long-running process metrics.

        Options:
          --pet-exe <path>               CastoPet.exe to monitor (default: Release build)
          --output <directory>           Session output directory
          --duration <hh:mm:ss>          Run duration; 00:00:00 means until Ctrl+C (default: 08:00:00)
          --interval-seconds <number>     Sampling interval from 0.25 to 60 seconds (default: 1)
          --game-process <name>           Optional game process name, with or without .exe
          --max-restarts <number>         CastoPet restart limit from 0 to 1000 (default: 10)
          --restart-delay-seconds <n>     Delay before restart, up to 300 seconds (default: 2)
          --stop-pet-on-exit              Stop the monitored CastoPet when the session ends
          --help, -h                      Show this help
        """);
}
