using System.IO.Pipes;
using System.Text;

namespace CastoPet.Core;

public sealed class SingleInstanceService : IDisposable
{
    private const string DefaultInstanceName = "CastoPet";
    private readonly LoggingService _logger;
    private readonly Mutex _mutex;
    private readonly string _pipeName;
    private readonly CancellationTokenSource _cancellation = new();
    private Task? _serverTask;
    private bool _disposed;

    public SingleInstanceService(LoggingService logger, string instanceName = DefaultInstanceName)
    {
        _logger = logger;
        _pipeName = $"{instanceName}.SingleInstance.Restore";
        _mutex = new Mutex(initiallyOwned: true, $"Local\\{instanceName}.SingleInstance", out var ownsMutex);
        IsPrimaryInstance = ownsMutex;
    }

    public bool IsPrimaryInstance { get; }

    public void StartRestoreServer(Action restore)
    {
        if (!IsPrimaryInstance || _serverTask is not null)
        {
            return;
        }

        _serverTask = Task.Run(() => RunRestoreServerAsync(restore, _cancellation.Token));
    }

    public async Task<bool> SignalRestoreAsync()
    {
        try
        {
            await using var pipe = new NamedPipeClientStream(
                ".",
                _pipeName,
                PipeDirection.Out,
                PipeOptions.Asynchronous);

            await pipe.ConnectAsync(500);
            var bytes = Encoding.UTF8.GetBytes("restore");
            await pipe.WriteAsync(bytes);
            await pipe.FlushAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to signal existing CastoPet instance.", ex);
            return false;
        }
    }

    private async Task RunRestoreServerAsync(Action restore, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var pipe = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.In,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await pipe.WaitForConnectionAsync(cancellationToken);
                var buffer = new byte[16];
                await pipe.ReadAtLeastAsync(buffer, minimumBytes: 1, throwOnEndOfStream: false, cancellationToken);
                restore();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.Error("Single-instance restore server failed.", ex);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cancellation.Cancel();
        _cancellation.Dispose();

        if (IsPrimaryInstance)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
    }
}
