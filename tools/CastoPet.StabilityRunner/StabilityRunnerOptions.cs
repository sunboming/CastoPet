using System.Globalization;

namespace CastoPet.StabilityRunner;

public sealed record StabilityRunnerOptions(
    string PetExecutablePath,
    string OutputDirectory,
    TimeSpan Duration,
    TimeSpan SampleInterval,
    string? GameProcessName,
    int MaxRestarts,
    TimeSpan RestartDelay,
    bool StopPetOnExit)
{
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromHours(8);
    public static readonly TimeSpan DefaultSampleInterval = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan DefaultRestartDelay = TimeSpan.FromSeconds(2);

    public static StabilityRunnerOptions Parse(
        IReadOnlyList<string> args,
        string defaultPetExecutablePath,
        string defaultOutputDirectory)
    {
        ArgumentNullException.ThrowIfNull(args);

        var petExecutablePath = defaultPetExecutablePath;
        var outputDirectory = defaultOutputDirectory;
        var duration = DefaultDuration;
        var sampleInterval = DefaultSampleInterval;
        string? gameProcessName = null;
        var maxRestarts = 10;
        var restartDelay = DefaultRestartDelay;
        var stopPetOnExit = false;

        for (var index = 0; index < args.Count; index++)
        {
            var option = args[index];
            switch (option)
            {
                case "--pet-exe":
                    petExecutablePath = ReadValue(args, ref index, option);
                    break;
                case "--output":
                    outputDirectory = ReadValue(args, ref index, option);
                    break;
                case "--duration":
                    duration = ParseDuration(ReadValue(args, ref index, option), option);
                    break;
                case "--interval-seconds":
                    sampleInterval = TimeSpan.FromSeconds(ParseDouble(ReadValue(args, ref index, option), option));
                    break;
                case "--game-process":
                    gameProcessName = NormalizeProcessName(ReadValue(args, ref index, option));
                    break;
                case "--max-restarts":
                    maxRestarts = ParseInt32(ReadValue(args, ref index, option), option);
                    break;
                case "--restart-delay-seconds":
                    restartDelay = TimeSpan.FromSeconds(ParseDouble(ReadValue(args, ref index, option), option));
                    break;
                case "--stop-pet-on-exit":
                    stopPetOnExit = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown option: {option}");
            }
        }

        if (string.IsNullOrWhiteSpace(petExecutablePath))
        {
            throw new ArgumentException("Pet executable path is required.");
        }

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("Output directory is required.");
        }

        if (duration < TimeSpan.Zero || duration > TimeSpan.FromDays(30))
        {
            throw new ArgumentException("Duration must be between zero and 30 days; zero runs until canceled.");
        }

        if (sampleInterval < TimeSpan.FromMilliseconds(250) || sampleInterval > TimeSpan.FromMinutes(1))
        {
            throw new ArgumentException("Sample interval must be between 0.25 and 60 seconds.");
        }

        if (maxRestarts is < 0 or > 1000)
        {
            throw new ArgumentException("Maximum restarts must be between zero and 1000.");
        }

        if (restartDelay < TimeSpan.Zero || restartDelay > TimeSpan.FromMinutes(5))
        {
            throw new ArgumentException("Restart delay must be between zero and five minutes.");
        }

        return new StabilityRunnerOptions(
            Path.GetFullPath(petExecutablePath),
            Path.GetFullPath(outputDirectory),
            duration,
            sampleInterval,
            gameProcessName,
            maxRestarts,
            restartDelay,
            stopPetOnExit);
    }

    private static string ReadValue(IReadOnlyList<string> args, ref int index, string option)
    {
        if (++index >= args.Count || string.IsNullOrWhiteSpace(args[index]))
        {
            throw new ArgumentException($"Option {option} requires a value.");
        }

        return args[index];
    }

    private static TimeSpan ParseDuration(string value, string option)
    {
        if (!TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var result))
        {
            throw new ArgumentException($"Option {option} requires a duration such as 08:00:00.");
        }

        return result;
    }

    private static double ParseDouble(string value, string option)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ||
            !double.IsFinite(result))
        {
            throw new ArgumentException($"Option {option} requires a finite number.");
        }

        return result;
    }

    private static int ParseInt32(string value, string option)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            throw new ArgumentException($"Option {option} requires an integer.");
        }

        return result;
    }

    private static string NormalizeProcessName(string value)
    {
        var processName = Path.GetFileName(value.Trim());
        if (processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            processName = processName[..^4];
        }

        return string.IsNullOrWhiteSpace(processName)
            ? throw new ArgumentException("Game process name is required.")
            : processName;
    }
}
