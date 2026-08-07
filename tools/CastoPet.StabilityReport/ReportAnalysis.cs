using System.Globalization;
using System.Text.Json;

namespace CastoPet.StabilityReport;

public sealed record ReportSample(
    DateTimeOffset TimestampUtc,
    double ElapsedSeconds,
    string Role,
    int? ProcessId,
    bool Running,
    double? CpuPercent,
    long? WorkingSetBytes,
    long? PrivateBytes,
    long? VirtualBytes,
    int? HandleCount,
    int? ThreadCount,
    uint? GdiObjects,
    uint? UserObjects,
    ulong? ReadBytes,
    ulong? WriteBytes,
    bool? IsForeground,
    double? SystemCpuPercent,
    ulong? SystemAvailableMemoryBytes);

public sealed record ReportEvent(DateTimeOffset TimestampUtc, string Type, string Message, int? ProcessId);

public sealed record MetricSummary(double? Average, double? P95, double? Maximum);

public sealed record RoleSummary(
    string Role,
    int SampleCount,
    int RunningSampleCount,
    double RunningHours,
    MetricSummary Cpu,
    long? WorkingSetStartBytes,
    long? WorkingSetEndBytes,
    long? WorkingSetMaximumBytes,
    long? PrivateStartBytes,
    long? PrivateEndBytes,
    long? PrivateMaximumBytes,
    double? PrivateSteadySlopeBytesPerHour,
    double? PrivateFirstSteadyWindowMedianBytes,
    double? PrivateLastWindowMedianBytes,
    int? HandleStart,
    int? HandleEnd,
    int? HandleMaximum,
    int? ThreadStart,
    int? ThreadEnd,
    int? ThreadMaximum,
    uint? GdiStart,
    uint? GdiEnd,
    uint? GdiMaximum,
    uint? UserStart,
    uint? UserEnd,
    uint? UserMaximum,
    double? ForegroundPercent);

public sealed record SystemSummary(
    MetricSummary Cpu,
    ulong? AvailableMemoryMinimumBytes,
    ulong? AvailableMemoryAverageBytes,
    double? SampleGapP95Seconds,
    double? SampleGapMaximumSeconds);

public sealed record StabilityReportAnalysis(
    DateTimeOffset StartedUtc,
    DateTimeOffset EndedUtc,
    double DurationHours,
    RoleSummary Pet,
    RoleSummary Game,
    SystemSummary System,
    IReadOnlyList<ReportEvent> Events,
    string Status,
    IReadOnlyList<string> Findings);

public sealed record ChartPoint(double X, double Y);

public static class StabilityReportReader
{
    private const int ExpectedColumnCount = 18;

    public static (IReadOnlyList<ReportSample> Samples, IReadOnlyList<ReportEvent> Events) ReadSession(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        var samplesPath = Path.Combine(directory, "samples.csv");
        if (!File.Exists(samplesPath))
        {
            throw new FileNotFoundException("The stability session does not contain samples.csv.", samplesPath);
        }

        var samples = new List<ReportSample>();
        using (var reader = File.OpenText(samplesPath))
        {
            var header = reader.ReadLine();
            if (header is null || header.TrimStart('\uFEFF') !=
                "timestampUtc,elapsedSeconds,role,pid,running,cpuPercent,workingSetBytes,privateBytes,virtualBytes,handleCount,threadCount,gdiObjects,userObjects,readBytes,writeBytes,isForeground,systemCpuPercent,systemAvailableMemoryBytes")
            {
                throw new InvalidDataException("samples.csv has an unsupported header.");
            }

            var lineNumber = 1;
            while (reader.ReadLine() is { } line)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var columns = line.Split(',');
                if (columns.Length != ExpectedColumnCount)
                {
                    throw new InvalidDataException($"samples.csv line {lineNumber} has {columns.Length} columns; expected {ExpectedColumnCount}.");
                }

                samples.Add(ParseSample(columns, lineNumber));
            }
        }

        var events = ReadEvents(Path.Combine(directory, "events.jsonl"));
        return (samples, events);
    }

    private static ReportSample ParseSample(string[] columns, int lineNumber)
    {
        try
        {
            return new ReportSample(
                DateTimeOffset.Parse(columns[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                ParseRequiredDouble(columns[1]),
                columns[2],
                ParseInt(columns[3]),
                bool.Parse(columns[4]),
                ParseDouble(columns[5]),
                ParseLong(columns[6]),
                ParseLong(columns[7]),
                ParseLong(columns[8]),
                ParseInt(columns[9]),
                ParseInt(columns[10]),
                ParseUInt(columns[11]),
                ParseUInt(columns[12]),
                ParseULong(columns[13]),
                ParseULong(columns[14]),
                ParseBool(columns[15]),
                ParseDouble(columns[16]),
                ParseULong(columns[17]));
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            throw new InvalidDataException($"samples.csv line {lineNumber} contains an invalid value.", ex);
        }
    }

    private static IReadOnlyList<ReportEvent> ReadEvents(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        var events = new List<ReportEvent>();
        var lineNumber = 0;
        foreach (var line in File.ReadLines(path))
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(line);
                var root = document.RootElement;
                events.Add(new ReportEvent(
                    root.GetProperty("timestampUtc").GetDateTimeOffset(),
                    root.GetProperty("type").GetString() ?? "unknown",
                    root.GetProperty("message").GetString() ?? string.Empty,
                    root.TryGetProperty("processId", out var processId) && processId.ValueKind == JsonValueKind.Number
                        ? processId.GetInt32()
                        : null));
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
            {
                throw new InvalidDataException($"events.jsonl line {lineNumber} is invalid.", ex);
            }
        }

        return events;
    }

    private static double ParseRequiredDouble(string value) => double.Parse(value, CultureInfo.InvariantCulture);
    private static double? ParseDouble(string value) => string.IsNullOrWhiteSpace(value) ? null : double.Parse(value, CultureInfo.InvariantCulture);
    private static long? ParseLong(string value) => string.IsNullOrWhiteSpace(value) ? null : long.Parse(value, CultureInfo.InvariantCulture);
    private static int? ParseInt(string value) => string.IsNullOrWhiteSpace(value) ? null : int.Parse(value, CultureInfo.InvariantCulture);
    private static uint? ParseUInt(string value) => string.IsNullOrWhiteSpace(value) ? null : uint.Parse(value, CultureInfo.InvariantCulture);
    private static ulong? ParseULong(string value) => string.IsNullOrWhiteSpace(value) ? null : ulong.Parse(value, CultureInfo.InvariantCulture);
    private static bool? ParseBool(string value) => string.IsNullOrWhiteSpace(value) ? null : bool.Parse(value);
}

public static class StabilityReportAnalyzer
{
    private const double SteadyStateSeconds = 5 * 60;
    private const double WindowSeconds = 10 * 60;

    public static StabilityReportAnalysis Analyze(
        IReadOnlyList<ReportSample> samples,
        IReadOnlyList<ReportEvent> events)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentNullException.ThrowIfNull(events);
        if (samples.Count == 0)
        {
            throw new ArgumentException("At least one sample is required.", nameof(samples));
        }

        var ordered = samples.OrderBy(sample => sample.ElapsedSeconds).ThenBy(sample => sample.Role).ToArray();
        var petRows = ordered.Where(sample => sample.Role == "pet").ToArray();
        var gameRows = ordered.Where(sample => sample.Role == "game").ToArray();
        var systemRows = petRows.Length > 0
            ? petRows
            : ordered.GroupBy(sample => sample.TimestampUtc).Select(group => group.First()).ToArray();

        var pet = SummarizeRole("pet", petRows);
        var game = SummarizeRole("game", gameRows);
        var system = SummarizeSystem(systemRows);
        var findings = BuildFindings(pet, game, system, events);
        var status = ClassifyStatus(pet, system, events);

        return new StabilityReportAnalysis(
            ordered.Min(sample => sample.TimestampUtc),
            ordered.Max(sample => sample.TimestampUtc),
            Math.Max(0, ordered.Max(sample => sample.ElapsedSeconds) - ordered.Min(sample => sample.ElapsedSeconds)) / 3600,
            pet,
            game,
            system,
            events.OrderBy(item => item.TimestampUtc).ToArray(),
            status,
            findings);
    }

    private static RoleSummary SummarizeRole(string role, IReadOnlyList<ReportSample> rows)
    {
        var running = rows.Where(sample => sample.Running).OrderBy(sample => sample.ElapsedSeconds).ToArray();
        if (running.Length == 0)
        {
            return new RoleSummary(
                Role: role,
                SampleCount: rows.Count,
                RunningSampleCount: 0,
                RunningHours: 0,
                Cpu: EmptyMetric(),
                WorkingSetStartBytes: null,
                WorkingSetEndBytes: null,
                WorkingSetMaximumBytes: null,
                PrivateStartBytes: null,
                PrivateEndBytes: null,
                PrivateMaximumBytes: null,
                PrivateSteadySlopeBytesPerHour: null,
                PrivateFirstSteadyWindowMedianBytes: null,
                PrivateLastWindowMedianBytes: null,
                HandleStart: null,
                HandleEnd: null,
                HandleMaximum: null,
                ThreadStart: null,
                ThreadEnd: null,
                ThreadMaximum: null,
                GdiStart: null,
                GdiEnd: null,
                GdiMaximum: null,
                UserStart: null,
                UserEnd: null,
                UserMaximum: null,
                ForegroundPercent: null);
        }

        var start = running[0].ElapsedSeconds;
        var end = running[^1].ElapsedSeconds;
        var steady = running.Where(sample => sample.ElapsedSeconds >= start + SteadyStateSeconds && sample.PrivateBytes is not null).ToArray();
        var firstSteadyWindow = steady.Where(sample => sample.ElapsedSeconds < start + SteadyStateSeconds + WindowSeconds).ToArray();
        var lastWindow = running.Where(sample => sample.ElapsedSeconds >= end - WindowSeconds && sample.PrivateBytes is not null).ToArray();

        return new RoleSummary(
            role,
            rows.Count,
            running.Length,
            Math.Max(0, end - start) / 3600,
            SummarizeMetric(running.Select(sample => sample.CpuPercent)),
            First(running, sample => sample.WorkingSetBytes),
            Last(running, sample => sample.WorkingSetBytes),
            Maximum(running, sample => sample.WorkingSetBytes),
            First(running, sample => sample.PrivateBytes),
            Last(running, sample => sample.PrivateBytes),
            Maximum(running, sample => sample.PrivateBytes),
            CalculateSlope(steady),
            Median(firstSteadyWindow.Select(sample => sample.PrivateBytes is long value ? (double?)value : null)),
            Median(lastWindow.Select(sample => sample.PrivateBytes is long value ? (double?)value : null)),
            First(running, sample => sample.HandleCount),
            Last(running, sample => sample.HandleCount),
            Maximum(running, sample => sample.HandleCount),
            First(running, sample => sample.ThreadCount),
            Last(running, sample => sample.ThreadCount),
            Maximum(running, sample => sample.ThreadCount),
            First(running, sample => sample.GdiObjects),
            Last(running, sample => sample.GdiObjects),
            Maximum(running, sample => sample.GdiObjects),
            First(running, sample => sample.UserObjects),
            Last(running, sample => sample.UserObjects),
            Maximum(running, sample => sample.UserObjects),
            Percentage(running.Select(sample => sample.IsForeground)));
    }

    private static SystemSummary SummarizeSystem(IReadOnlyList<ReportSample> rows)
    {
        var ordered = rows.OrderBy(sample => sample.ElapsedSeconds).ToArray();
        var gaps = ordered.Zip(ordered.Skip(1), (left, right) => (double?)(right.ElapsedSeconds - left.ElapsedSeconds));
        var available = ordered.Where(sample => sample.SystemAvailableMemoryBytes is not null)
            .Select(sample => sample.SystemAvailableMemoryBytes!.Value).ToArray();
        return new SystemSummary(
            SummarizeMetric(ordered.Select(sample => sample.SystemCpuPercent)),
            available.Length == 0 ? null : available.Min(),
            available.Length == 0 ? null : (ulong?)Math.Round(available.Average(value => (double)value)),
            Percentile(gaps, 0.95),
            Maximum(gaps));
    }

    private static IReadOnlyList<string> BuildFindings(
        RoleSummary pet,
        RoleSummary game,
        SystemSummary system,
        IReadOnlyList<ReportEvent> events)
    {
        var findings = new List<string>();
        if (pet.PrivateSteadySlopeBytesPerHour is double slope &&
            pet.PrivateFirstSteadyWindowMedianBytes is double first &&
            pet.PrivateLastWindowMedianBytes is double last)
        {
            findings.Add($"桌宠稳态私有内存趋势为 {FormatSignedMiB(slope)}/小时，首尾稳态窗口中位数变化 {FormatSignedMiB(last - first)}。");
        }

        if (pet.HandleStart is int handleStart && pet.HandleEnd is int handleEnd)
        {
            findings.Add($"桌宠句柄 {handleStart} → {handleEnd}，线程 {pet.ThreadStart ?? 0} → {pet.ThreadEnd ?? 0}，GDI {pet.GdiStart ?? 0} → {pet.GdiEnd ?? 0}。");
        }

        if (system.SampleGapP95Seconds is double p95 && system.SampleGapMaximumSeconds is double maximum)
        {
            findings.Add($"采样间隔 P95 为 {p95:F3} 秒，最大 {maximum:F3} 秒；共记录 {events.Count} 个生命周期事件。");
        }

        if (game.RunningSampleCount > 0 && game.PrivateFirstSteadyWindowMedianBytes is double gameFirst &&
            game.PrivateLastWindowMedianBytes is double gameLast)
        {
            findings.Add($"游戏私有内存稳态窗口变化 {FormatSignedMiB(gameLast - gameFirst)}；未设置无桌宠基线，不能据此归因于 CastoPet。");
        }

        return findings;
    }

    private static string ClassifyStatus(RoleSummary pet, SystemSummary system, IReadOnlyList<ReportEvent> events)
    {
        var failed = events.Any(item => item.Type.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
            item.Type is "pet-exited" or "pet-restart-limit-reached");
        var memoryGrowth = pet.PrivateFirstSteadyWindowMedianBytes is double first &&
            pet.PrivateLastWindowMedianBytes is double last ? last - first : 0;
        var handleGrowth = (pet.HandleEnd ?? 0) - (pet.HandleStart ?? 0);
        var objectGrowth = (long)(pet.GdiEnd ?? 0) - (pet.GdiStart ?? 0) +
            (long)(pet.UserEnd ?? 0) - (pet.UserStart ?? 0);

        if (failed || system.SampleGapMaximumSeconds > 10 || handleGrowth > 250 || objectGrowth > 100 ||
            pet.PrivateSteadySlopeBytesPerHour > 10 * 1024 * 1024 && memoryGrowth > 30 * 1024 * 1024)
        {
            return "需要处理";
        }

        if (system.SampleGapMaximumSeconds > 3 || handleGrowth > 100 || objectGrowth > 40 ||
            pet.PrivateSteadySlopeBytesPerHour > 3 * 1024 * 1024 && memoryGrowth > 10 * 1024 * 1024)
        {
            return "需要观察";
        }

        return "稳定";
    }

    private static string FormatSignedMiB(double bytes) => $"{bytes / 1024 / 1024:+0.00;-0.00;0.00} MiB";

    private static MetricSummary EmptyMetric() => new(null, null, null);

    private static MetricSummary SummarizeMetric(IEnumerable<double?> source)
    {
        var values = source.Where(value => value is not null).Select(value => value!.Value).Order().ToArray();
        return values.Length == 0
            ? EmptyMetric()
            : new MetricSummary(values.Average(), Percentile(values.Select(value => (double?)value), 0.95), values[^1]);
    }

    private static double? CalculateSlope(IReadOnlyList<ReportSample> samples)
    {
        if (samples.Count < 2)
        {
            return null;
        }

        var origin = samples[0].ElapsedSeconds;
        var points = samples.Select(sample => new
        {
            X = (sample.ElapsedSeconds - origin) / 3600,
            Y = (double)sample.PrivateBytes!.Value,
        }).ToArray();
        var meanX = points.Average(point => point.X);
        var meanY = points.Average(point => point.Y);
        var numerator = points.Sum(point => (point.X - meanX) * (point.Y - meanY));
        var denominator = points.Sum(point => Math.Pow(point.X - meanX, 2));
        return denominator == 0 ? null : numerator / denominator;
    }

    private static double? Percentage(IEnumerable<bool?> source)
    {
        var values = source.Where(value => value is not null).Select(value => value!.Value).ToArray();
        return values.Length == 0 ? null : values.Count(value => value) * 100d / values.Length;
    }

    private static double? Median(IEnumerable<double?> source) => Percentile(source, 0.5);

    private static double? Percentile(IEnumerable<double?> source, double percentile)
    {
        var values = source.Where(value => value is not null).Select(value => value!.Value).Order().ToArray();
        return values.Length == 0 ? null : values[(int)Math.Floor((values.Length - 1) * percentile)];
    }

    private static T? First<T>(IEnumerable<ReportSample> rows, Func<ReportSample, T?> selector) where T : struct =>
        rows.Select(selector).FirstOrDefault(value => value is not null);

    private static T? Last<T>(IEnumerable<ReportSample> rows, Func<ReportSample, T?> selector) where T : struct =>
        rows.Select(selector).LastOrDefault(value => value is not null);

    private static T? Maximum<T>(IEnumerable<ReportSample> rows, Func<ReportSample, T?> selector) where T : struct, IComparable<T>
    {
        var values = rows.Select(selector).Where(value => value is not null).Select(value => value!.Value).ToArray();
        return values.Length == 0 ? null : values.Max();
    }

    private static double? Maximum(IEnumerable<double?> values)
    {
        var present = values.Where(value => value is not null).Select(value => value!.Value).ToArray();
        return present.Length == 0 ? null : present.Max();
    }
}

public static class ChartDownsampler
{
    public static IReadOnlyList<ChartPoint> MinMax(IReadOnlyList<ChartPoint> points, int maximumPoints)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (maximumPoints < 4)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumPoints), "At least four points are required.");
        }

        if (points.Count <= maximumPoints)
        {
            return points.ToArray();
        }

        var bucketCount = Math.Max(1, (maximumPoints - 2) / 2);
        var interiorCount = points.Count - 2;
        var result = new List<ChartPoint>(maximumPoints) { points[0] };
        for (var bucket = 0; bucket < bucketCount; bucket++)
        {
            var start = 1 + (int)Math.Floor(bucket * interiorCount / (double)bucketCount);
            var end = 1 + (int)Math.Floor((bucket + 1) * interiorCount / (double)bucketCount);
            var segment = points.Skip(start).Take(Math.Max(1, end - start)).ToArray();
            var minimum = segment.MinBy(point => point.Y)!;
            var maximum = segment.MaxBy(point => point.Y)!;
            if (minimum.X <= maximum.X)
            {
                result.Add(minimum);
                if (maximum != minimum) result.Add(maximum);
            }
            else
            {
                result.Add(maximum);
                if (maximum != minimum) result.Add(minimum);
            }
        }

        result.Add(points[^1]);
        return result;
    }
}
