using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace CastoPet.StabilityRunner;

internal sealed record ProcessMetricSample(
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
    bool? IsForeground);

internal sealed class ProcessMetricsSampler : IDisposable
{
    private readonly Process _process;
    private TimeSpan? _previousProcessorTime;
    private DateTimeOffset? _previousTimestamp;

    public ProcessMetricsSampler(Process process)
    {
        _process = process;
    }

    public Process Process => _process;

    public ProcessMetricSample Capture(DateTimeOffset timestamp)
    {
        _process.Refresh();
        if (_process.HasExited)
        {
            return Missing();
        }

        var processorTime = _process.TotalProcessorTime;
        double? cpuPercent = null;
        if (_previousProcessorTime is TimeSpan previousProcessorTime &&
            _previousTimestamp is DateTimeOffset previousTimestamp)
        {
            cpuPercent = ProcessCpuCalculator.CalculatePercent(
                previousProcessorTime,
                processorTime,
                timestamp - previousTimestamp,
                Environment.ProcessorCount);
        }

        _previousProcessorTime = processorTime;
        _previousTimestamp = timestamp;
        var io = NativeMethods.TryGetIoCounters(_process.SafeHandle);
        return new ProcessMetricSample(
            _process.Id,
            true,
            cpuPercent,
            _process.WorkingSet64,
            _process.PrivateMemorySize64,
            _process.VirtualMemorySize64,
            _process.HandleCount,
            _process.Threads.Count,
            NativeMethods.GetGuiResources(_process.Handle, 0),
            NativeMethods.GetGuiResources(_process.Handle, 1),
            io?.ReadTransferCount,
            io?.WriteTransferCount,
            NativeMethods.IsForegroundProcess(_process.Id));
    }

    public static ProcessMetricSample Missing() => new(
        null, false, null, null, null, null, null, null, null, null, null, null, null);

    public void Dispose() => _process.Dispose();
}

internal sealed record SystemMetricSample(double? CpuPercent, ulong AvailableMemoryBytes);

internal sealed class SystemMetricsSampler
{
    private ulong? _previousIdle;
    private ulong? _previousKernel;
    private ulong? _previousUser;

    public SystemMetricSample Capture()
    {
        double? cpuPercent = null;
        if (NativeMethods.GetSystemTimes(out var idle, out var kernel, out var user))
        {
            var currentIdle = idle.ToUInt64();
            var currentKernel = kernel.ToUInt64();
            var currentUser = user.ToUInt64();
            if (_previousIdle is ulong previousIdle &&
                _previousKernel is ulong previousKernel &&
                _previousUser is ulong previousUser)
            {
                var idleDelta = currentIdle - previousIdle;
                var totalDelta = currentKernel - previousKernel + currentUser - previousUser;
                cpuPercent = totalDelta == 0
                    ? 0
                    : Math.Clamp((totalDelta - idleDelta) * 100d / totalDelta, 0, 100);
            }

            _previousIdle = currentIdle;
            _previousKernel = currentKernel;
            _previousUser = currentUser;
        }

        var memory = new NativeMethods.MemoryStatusEx();
        var availableMemory = NativeMethods.GlobalMemoryStatusEx(memory)
            ? memory.AvailablePhysical
            : 0;
        return new SystemMetricSample(cpuPercent, availableMemory);
    }
}

internal static class NativeMethods
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct FileTime
    {
        public uint Low;
        public uint High;

        public readonly ulong ToUInt64() => ((ulong)High << 32) | Low;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class MemoryStatusEx
    {
        public uint Length = (uint)Marshal.SizeOf<MemoryStatusEx>();
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IoCounters
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);

    [DllImport("kernel32.dll", EntryPoint = "GlobalMemoryStatusEx")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx buffer);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessIoCounters(SafeProcessHandle processHandle, out IoCounters counters);

    [DllImport("user32.dll")]
    internal static extern uint GetGuiResources(nint process, uint flags);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint window, out uint processId);

    internal static IoCounters? TryGetIoCounters(SafeProcessHandle processHandle) =>
        GetProcessIoCounters(processHandle, out var counters) ? counters : null;

    internal static bool IsForegroundProcess(int processId)
    {
        var window = GetForegroundWindow();
        return window != 0 && GetWindowThreadProcessId(window, out var foregroundProcessId) != 0 &&
            foregroundProcessId == processId;
    }
}
