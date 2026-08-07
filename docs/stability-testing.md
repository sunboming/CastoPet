# Stability Testing

`CastoPet.StabilityRunner` starts or attaches to the requested CastoPet executable and writes
long-running process metrics without retaining the complete sample history in memory.

## Start a Session

Build the Release executable first, then run an eight-hour session:

```powershell
dotnet run --project tools/CastoPet.StabilityRunner/CastoPet.StabilityRunner.csproj -c Release -- `
  --duration 08:00:00
```

Observe a game process at the same time:

```powershell
dotnet run --project tools/CastoPet.StabilityRunner/CastoPet.StabilityRunner.csproj -c Release -- `
  --duration 04:00:00 `
  --game-process GameExecutableName
```

Use `--duration 00:00:00` to run until `Ctrl+C`. The default executable is:

```text
src/CastoPet/bin/Release/net10.0-windows/CastoPet.exe
```

Run with `--help` for all options. Close other Debug, installed, or differently located
CastoPet instances before a controlled test so the single-instance guard does not redirect the
new process.

## Output

Each session writes to `artifacts/stability-tests/<timestamp>/` by default:

- `samples.csv`: one flushed row per observed process and sample interval.
- `events.jsonl`: process starts, exits, restarts, attachment changes, and sampling failures.
- `summary.json`: CPU averages and peaks, current-process memory growth and slope,
  handle/thread peaks, observed process count, and restart count.

The private-memory summary contains the current process segment and a steady-state trend that
starts five minutes after that process starts. A restart begins a new segment so a lower initial
allocation cannot hide growth from the new process. The CSV and events retain all prior process
IDs. A positive slope is evidence to investigate, not proof of a managed memory leak; compare
multiple sessions with the same workload and inspect handle, GDI, USER, and private-memory
growth together.

The optional game observation records process CPU, memory, handles, threads, I/O, and whether
the game owns the foreground window. It does not record FPS or frame times. Reliable frame-time
comparison requires a later PresentMon integration and controlled pet-on/pet-off test phases.

No keyboard contents, window titles, screenshots, or network data are collected.

## Generate An Offline Report

Create a self-contained report after a session finishes or is manually stopped:

```powershell
dotnet run --project tools/CastoPet.StabilityReport/CastoPet.StabilityReport.csproj -c Release -- `
  --session artifacts/stability-tests/<timestamp>
```

The command writes `report.html` into the session directory by default. It embeds the analyzed
summary, lifecycle events, and spike-preserving downsampled chart data, so the report can be
opened directly without a local server or network connection. Exact cards and tables use every
raw sample; only chart rendering is downsampled.

The report grades CastoPet resource stability and highlights sampling failures. Game metrics are
observational only: without a controlled pet-off baseline and frame-time collection, they cannot
establish whether CastoPet affected game performance.
