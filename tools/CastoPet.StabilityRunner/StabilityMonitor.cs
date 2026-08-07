using System.Diagnostics;

namespace CastoPet.StabilityRunner;

internal sealed class StabilityMonitor
{
    private readonly StabilityRunnerOptions _options;
    private readonly SystemMetricsSampler _systemMetrics = new();
    private readonly ProcessRestartPolicy _restartPolicy;
    private readonly MetricAggregate _petAggregate = new();
    private readonly MetricAggregate _gameAggregate = new();
    private bool _restartLimitReached;

    public StabilityMonitor(StabilityRunnerOptions options)
    {
        _options = options;
        _restartPolicy = new ProcessRestartPolicy(options.MaxRestarts, options.RestartDelay);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_options.PetExecutablePath))
        {
            throw new FileNotFoundException("CastoPet executable was not found.", _options.PetExecutablePath);
        }

        var startedUtc = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        long sampleCycles = 0;
        ProcessMetricsSampler? pet = null;
        ProcessMetricsSampler? game = null;

        using var output = new StabilitySessionOutput(_options.OutputDirectory);
        try
        {
            pet = AttachToTargetPet();
            if (pet is null)
            {
                pet = StartPet();
                output.WriteEvent("pet-started", "Started CastoPet for the stability session.", pet.Process.Id);
            }
            else
            {
                output.WriteEvent("pet-attached", "Attached to an existing CastoPet process.", pet.Process.Id);
            }

            output.WriteEvent("session-started", "Stability monitoring started.", pet.Process.Id);
            while (!cancellationToken.IsCancellationRequested &&
                   (_options.Duration == TimeSpan.Zero || stopwatch.Elapsed < _options.Duration))
            {
                if (pet is not null && !IsRunning(pet.Process))
                {
                    output.WriteEvent("pet-exited", ReadExitMessage(pet.Process), pet.Process.Id);
                    pet.Dispose();
                    pet = await RestartPetAsync(output, cancellationToken);
                }
                else if (pet is null && !_restartLimitReached)
                {
                    pet = await RestartPetAsync(output, cancellationToken);
                }

                game = RefreshGameSampler(game, output);
                var timestamp = DateTimeOffset.UtcNow;
                var elapsed = stopwatch.Elapsed;
                var systemSample = _systemMetrics.Capture();
                var petSample = CaptureOrMissing(pet, output, "pet-sample-failed");
                var gameSample = CaptureOrMissing(game, output, "game-sample-failed");

                output.WriteSample(new StabilitySample(timestamp, elapsed, "pet", petSample, systemSample));
                if (_options.GameProcessName is not null)
                {
                    output.WriteSample(new StabilitySample(timestamp, elapsed, "game", gameSample, systemSample));
                }

                _petAggregate.Add(elapsed, petSample);
                _gameAggregate.Add(elapsed, gameSample);
                sampleCycles++;

                await Task.Delay(_options.SampleInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            output.WriteEvent("session-canceled", "Stability monitoring was canceled.");
        }
        finally
        {
            if (_options.StopPetOnExit && pet is not null && IsRunning(pet.Process))
            {
                StopProcess(pet.Process, output);
            }

            pet?.Dispose();
            game?.Dispose();
            stopwatch.Stop();
            output.WriteSummary(new StabilityRunSummary(
                startedUtc,
                DateTimeOffset.UtcNow,
                stopwatch.Elapsed.TotalSeconds,
                sampleCycles,
                _restartPolicy.RestartCount,
                _options.PetExecutablePath,
                _options.GameProcessName,
                _petAggregate.Snapshot(),
                _gameAggregate.Snapshot()));
            output.WriteEvent("session-ended", "Stability monitoring ended.");
        }
    }

    private async Task<ProcessMetricsSampler?> RestartPetAsync(
        StabilitySessionOutput output,
        CancellationToken cancellationToken)
    {
        if (!_restartPolicy.TryScheduleRestart(out var delay))
        {
            _restartLimitReached = true;
            output.WriteEvent("restart-limit-reached", "CastoPet restart limit was reached.");
            return null;
        }

        output.WriteEvent("pet-restart-scheduled", $"Restarting CastoPet after {delay.TotalSeconds:F1} seconds.");
        await Task.Delay(delay, cancellationToken);
        try
        {
            var sampler = StartPet();
            output.WriteEvent("pet-restarted", "CastoPet restarted.", sampler.Process.Id);
            return sampler;
        }
        catch (Exception ex)
        {
            output.WriteEvent("pet-restart-failed", ex.Message);
            return null;
        }
    }

    private ProcessMetricsSampler StartPet()
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = _options.PetExecutablePath,
            WorkingDirectory = Path.GetDirectoryName(_options.PetExecutablePath),
            UseShellExecute = false,
        }) ?? throw new InvalidOperationException("CastoPet process could not be started.");
        return new ProcessMetricsSampler(process);
    }

    private ProcessMetricsSampler? AttachToTargetPet()
    {
        var targetPath = Path.GetFullPath(_options.PetExecutablePath);
        var processName = Path.GetFileNameWithoutExtension(targetPath);
        foreach (var process in Process.GetProcessesByName(processName))
        {
            try
            {
                var processPath = process.MainModule?.FileName;
                if (processPath is not null && string.Equals(
                    Path.GetFullPath(processPath),
                    targetPath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return new ProcessMetricsSampler(process);
                }
            }
            catch
            {
            }

            process.Dispose();
        }

        return null;
    }

    private ProcessMetricsSampler? RefreshGameSampler(
        ProcessMetricsSampler? current,
        StabilitySessionOutput output)
    {
        if (_options.GameProcessName is null)
        {
            return null;
        }

        if (current is not null && IsRunning(current.Process))
        {
            return current;
        }

        if (current is not null)
        {
            output.WriteEvent("game-exited", "The observed game process exited.", current.Process.Id);
            current.Dispose();
        }

        var process = FindLargestProcess(_options.GameProcessName);
        if (process is null)
        {
            return null;
        }

        output.WriteEvent("game-attached", $"Attached to {_options.GameProcessName}.", process.Id);
        return new ProcessMetricsSampler(process);
    }

    private static Process? FindLargestProcess(string processName)
    {
        Process? selected = null;
        long selectedWorkingSet = -1;
        foreach (var process in Process.GetProcessesByName(processName))
        {
            try
            {
                process.Refresh();
                if (!process.HasExited && process.WorkingSet64 > selectedWorkingSet)
                {
                    selected?.Dispose();
                    selected = process;
                    selectedWorkingSet = process.WorkingSet64;
                    continue;
                }
            }
            catch
            {
            }

            process.Dispose();
        }

        return selected;
    }

    private static ProcessMetricSample CaptureOrMissing(
        ProcessMetricsSampler? sampler,
        StabilitySessionOutput output,
        string failureEvent)
    {
        if (sampler is null)
        {
            return ProcessMetricsSampler.Missing();
        }

        try
        {
            return sampler.Capture(DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            output.WriteEvent(failureEvent, ex.Message, TryGetProcessId(sampler.Process));
            return ProcessMetricsSampler.Missing();
        }
    }

    private static bool IsRunning(Process process)
    {
        try
        {
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    }

    private static string ReadExitMessage(Process process)
    {
        try
        {
            return $"CastoPet exited with code {process.ExitCode}.";
        }
        catch
        {
            return "CastoPet exited.";
        }
    }

    private static int? TryGetProcessId(Process process)
    {
        try
        {
            return process.Id;
        }
        catch
        {
            return null;
        }
    }

    private static void StopProcess(Process process, StabilitySessionOutput output)
    {
        try
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(5000);
            output.WriteEvent("pet-stopped", "Stopped CastoPet at the end of the session.", process.Id);
        }
        catch (Exception ex)
        {
            output.WriteEvent("pet-stop-failed", ex.Message, TryGetProcessId(process));
        }
    }
}
