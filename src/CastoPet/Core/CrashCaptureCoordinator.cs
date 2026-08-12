namespace CastoPet.Core;

public sealed class CrashCaptureCoordinator
{
    private readonly Func<Exception, CrashReportKind, bool> _writeReport;
    private int _fatalReportState;

    public CrashCaptureCoordinator(Func<Exception, CrashReportKind, bool> writeReport)
    {
        _writeReport = writeReport ?? throw new ArgumentNullException(nameof(writeReport));
    }

    public bool TryRecordFatal(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (Interlocked.CompareExchange(ref _fatalReportState, 1, 0) != 0)
        {
            return false;
        }

        var written = TryWrite(exception, CrashReportKind.Fatal);
        Volatile.Write(ref _fatalReportState, written ? 2 : 0);
        return written;
    }

    public void HandleUnobservedTaskException(UnobservedTaskExceptionEventArgs eventArgs)
    {
        ArgumentNullException.ThrowIfNull(eventArgs);
        try
        {
            _ = TryWrite(eventArgs.Exception, CrashReportKind.UnobservedTask);
        }
        finally
        {
            eventArgs.SetObserved();
        }
    }

    private bool TryWrite(Exception exception, CrashReportKind kind)
    {
        try
        {
            return _writeReport(exception, kind);
        }
        catch
        {
            return false;
        }
    }
}
