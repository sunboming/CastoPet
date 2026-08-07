using System.Globalization;
using System.Text;
using System.Text.Json;

namespace CastoPet.StabilityRunner;

internal sealed record StabilitySample(
    DateTimeOffset TimestampUtc,
    TimeSpan Elapsed,
    string Role,
    ProcessMetricSample Process,
    SystemMetricSample System);

internal sealed record MetricAggregateSnapshot(
    long RunningSamples,
    int ObservedProcessCount,
    double? AverageCpuPercent,
    double? PeakCpuPercent,
    long PeakWorkingSetBytes,
    MemoryTrendSnapshot CurrentProcessPrivateMemoryTrend,
    MemoryTrendSnapshot CurrentProcessSteadyStatePrivateMemoryTrend,
    int PeakHandleCount,
    int PeakThreadCount);

internal sealed record StabilityRunSummary(
    DateTimeOffset StartedUtc,
    DateTimeOffset EndedUtc,
    double ElapsedSeconds,
    long SampleCycles,
    int RestartCount,
    string PetExecutablePath,
    string? GameProcessName,
    MetricAggregateSnapshot Pet,
    MetricAggregateSnapshot Game);

internal sealed class MetricAggregate
{
    private static readonly TimeSpan SteadyStateDelay = TimeSpan.FromMinutes(5);
    private MemoryTrendAccumulator _privateMemory = new();
    private MemoryTrendAccumulator _steadyStatePrivateMemory = new();
    private long _runningSamples;
    private int _observedProcessCount;
    private int? _activeProcessId;
    private TimeSpan _activeProcessStartedAt;
    private long _cpuSamples;
    private double _cpuTotal;
    private double? _peakCpu;
    private long _peakWorkingSet;
    private int _peakHandles;
    private int _peakThreads;

    public void Add(TimeSpan elapsed, ProcessMetricSample sample)
    {
        if (!sample.Running)
        {
            return;
        }

        if (sample.ProcessId is int processId && processId != _activeProcessId)
        {
            _activeProcessId = processId;
            _activeProcessStartedAt = elapsed;
            _observedProcessCount++;
            _privateMemory = new MemoryTrendAccumulator();
            _steadyStatePrivateMemory = new MemoryTrendAccumulator();
        }

        _runningSamples++;
        if (sample.CpuPercent is double cpu)
        {
            _cpuSamples++;
            _cpuTotal += cpu;
            _peakCpu = Math.Max(_peakCpu ?? cpu, cpu);
        }

        if (sample.WorkingSetBytes is long workingSet)
        {
            _peakWorkingSet = Math.Max(_peakWorkingSet, workingSet);
        }

        if (sample.PrivateBytes is long privateBytes)
        {
            var processElapsed = elapsed - _activeProcessStartedAt;
            _privateMemory.Add(processElapsed, privateBytes);
            if (processElapsed >= SteadyStateDelay)
            {
                _steadyStatePrivateMemory.Add(processElapsed - SteadyStateDelay, privateBytes);
            }
        }

        if (sample.HandleCount is int handles)
        {
            _peakHandles = Math.Max(_peakHandles, handles);
        }

        if (sample.ThreadCount is int threads)
        {
            _peakThreads = Math.Max(_peakThreads, threads);
        }
    }

    public MetricAggregateSnapshot Snapshot() => new(
        _runningSamples,
        _observedProcessCount,
        _cpuSamples == 0 ? null : _cpuTotal / _cpuSamples,
        _peakCpu,
        _peakWorkingSet,
        _privateMemory.Snapshot(),
        _steadyStatePrivateMemory.Snapshot(),
        _peakHandles,
        _peakThreads);
}

internal sealed class StabilitySessionOutput : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _directory;
    private readonly StreamWriter _samples;
    private readonly StreamWriter _events;

    public StabilitySessionOutput(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(directory);
        _samples = CreateWriter(Path.Combine(directory, "samples.csv"));
        _events = CreateWriter(Path.Combine(directory, "events.jsonl"));
        _samples.WriteLine(
            "timestampUtc,elapsedSeconds,role,pid,running,cpuPercent,workingSetBytes,privateBytes,virtualBytes," +
            "handleCount,threadCount,gdiObjects,userObjects,readBytes,writeBytes,isForeground," +
            "systemCpuPercent,systemAvailableMemoryBytes");
    }

    public void WriteSample(StabilitySample sample)
    {
        var process = sample.Process;
        string[] values =
        [
            sample.TimestampUtc.ToString("O", CultureInfo.InvariantCulture),
            Format(sample.Elapsed.TotalSeconds),
            sample.Role,
            Format(process.ProcessId),
            process.Running ? "true" : "false",
            Format(process.CpuPercent),
            Format(process.WorkingSetBytes),
            Format(process.PrivateBytes),
            Format(process.VirtualBytes),
            Format(process.HandleCount),
            Format(process.ThreadCount),
            Format(process.GdiObjects),
            Format(process.UserObjects),
            Format(process.ReadBytes),
            Format(process.WriteBytes),
            Format(process.IsForeground),
            Format(sample.System.CpuPercent),
            sample.System.AvailableMemoryBytes.ToString(CultureInfo.InvariantCulture),
        ];
        _samples.WriteLine(string.Join(',', values));
    }

    public void WriteEvent(string type, string message, int? processId = null)
    {
        var json = JsonSerializer.Serialize(new
        {
            timestampUtc = DateTimeOffset.UtcNow,
            type,
            message,
            processId,
        });
        _events.WriteLine(json);
    }

    public void WriteSummary(StabilityRunSummary summary)
    {
        var path = Path.Combine(_directory, "summary.json");
        var temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(summary, JsonOptions), new UTF8Encoding(false));
        File.Move(temporaryPath, path, overwrite: true);
    }

    public void Dispose()
    {
        _samples.Dispose();
        _events.Dispose();
    }

    private static StreamWriter CreateWriter(string path) => new(
        new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read),
        new UTF8Encoding(false))
    {
        AutoFlush = true,
    };

    private static string Format(double? value) => value?.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Format(long? value) => value?.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Format(int? value) => value?.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Format(uint? value) => value?.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Format(ulong? value) => value?.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Format(bool? value) => value is null ? string.Empty : value.Value ? "true" : "false";
}
