using System.Text;
using CastoPet.StabilityReport;

if (args.Any(argument => argument is "--help" or "-h"))
{
    PrintHelp();
    return 0;
}

try
{
    var (sessionDirectory, outputPath) = ParseArguments(args);
    var (samples, events) = StabilityReportReader.ReadSession(sessionDirectory);
    var analysis = StabilityReportAnalyzer.Analyze(samples, events);
    var html = StabilityReportHtml.Render(analysis, samples);
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    File.WriteAllText(outputPath, html, new UTF8Encoding(false));

    Console.WriteLine($"Session: {sessionDirectory}");
    Console.WriteLine($"Samples: {samples.Count:N0}");
    Console.WriteLine($"Status: {analysis.Status}");
    Console.WriteLine($"Report: {outputPath}");
    return 0;
}
catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine("Run with --help for usage.");
    return 2;
}

static (string SessionDirectory, string OutputPath) ParseArguments(string[] arguments)
{
    string? session = null;
    string? output = null;
    for (var index = 0; index < arguments.Length; index++)
    {
        switch (arguments[index])
        {
            case "--session":
                session = ReadValue(arguments, ref index, "--session");
                break;
            case "--output":
                output = ReadValue(arguments, ref index, "--output");
                break;
            default:
                if (arguments[index].StartsWith('-'))
                {
                    throw new ArgumentException($"Unknown option: {arguments[index]}");
                }

                if (session is not null)
                {
                    throw new ArgumentException("Only one session directory can be specified.");
                }

                session = arguments[index];
                break;
        }
    }

    if (string.IsNullOrWhiteSpace(session))
    {
        throw new ArgumentException("A session directory is required. Use --session <directory>.");
    }

    var sessionPath = Path.GetFullPath(session);
    var outputPath = string.IsNullOrWhiteSpace(output)
        ? Path.Combine(sessionPath, "report.html")
        : Path.GetFullPath(output);
    return (sessionPath, outputPath);
}

static string ReadValue(string[] arguments, ref int index, string option)
{
    if (++index >= arguments.Length || string.IsNullOrWhiteSpace(arguments[index]))
    {
        throw new ArgumentException($"{option} requires a value.");
    }

    return arguments[index];
}

static void PrintHelp()
{
    Console.WriteLine("""
        CastoPet StabilityReport

        Generates a self-contained offline HTML report from a StabilityRunner session.

        Usage:
          dotnet run --project tools/CastoPet.StabilityReport -- --session <directory>

        Options:
          --session <directory>  Directory containing samples.csv and events.jsonl
          --output <path>        Output HTML path (default: <session>/report.html)
          --help, -h             Show this help
        """);
}
