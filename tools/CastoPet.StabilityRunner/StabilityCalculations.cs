namespace CastoPet.StabilityRunner;

public static class ProcessCpuCalculator
{
    public static double CalculatePercent(
        TimeSpan previousProcessorTime,
        TimeSpan currentProcessorTime,
        TimeSpan elapsed,
        int processorCount)
    {
        if (elapsed <= TimeSpan.Zero || processorCount <= 0 || currentProcessorTime <= previousProcessorTime)
        {
            return 0;
        }

        var usage = (currentProcessorTime - previousProcessorTime).TotalSeconds /
            elapsed.TotalSeconds /
            processorCount * 100;
        return Math.Clamp(usage, 0, 100);
    }
}

public sealed record MemoryTrendSnapshot(
    long SampleCount,
    long GrowthBytes,
    long PeakBytes,
    double SlopeBytesPerHour);

public sealed class MemoryTrendAccumulator
{
    private long _sampleCount;
    private double _sumHours;
    private double _sumBytes;
    private double _sumHoursSquared;
    private double _sumHoursBytes;
    private long _firstBytes;
    private long _latestBytes;
    private long _peakBytes;

    public void Add(TimeSpan elapsed, long privateBytes)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(privateBytes);

        if (_sampleCount == 0)
        {
            _firstBytes = privateBytes;
        }

        var hours = elapsed.TotalHours;
        _sampleCount++;
        _sumHours += hours;
        _sumBytes += privateBytes;
        _sumHoursSquared += hours * hours;
        _sumHoursBytes += hours * privateBytes;
        _latestBytes = privateBytes;
        _peakBytes = Math.Max(_peakBytes, privateBytes);
    }

    public MemoryTrendSnapshot Snapshot()
    {
        var denominator = _sampleCount * _sumHoursSquared - _sumHours * _sumHours;
        var slope = _sampleCount < 2 || Math.Abs(denominator) < double.Epsilon
            ? 0
            : (_sampleCount * _sumHoursBytes - _sumHours * _sumBytes) / denominator;
        return new MemoryTrendSnapshot(
            _sampleCount,
            _sampleCount == 0 ? 0 : _latestBytes - _firstBytes,
            _peakBytes,
            slope);
    }
}

public sealed class ProcessRestartPolicy
{
    private readonly int _maxRestarts;
    private readonly TimeSpan _restartDelay;

    public ProcessRestartPolicy(int maxRestarts, TimeSpan restartDelay)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxRestarts);
        if (restartDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(restartDelay));
        }
        _maxRestarts = maxRestarts;
        _restartDelay = restartDelay;
    }

    public int RestartCount { get; private set; }

    public bool TryScheduleRestart(out TimeSpan delay)
    {
        if (RestartCount >= _maxRestarts)
        {
            delay = TimeSpan.Zero;
            return false;
        }

        RestartCount++;
        delay = _restartDelay;
        return true;
    }
}
