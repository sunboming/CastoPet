# CastoPet MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the CastoPet Windows desktop pet MVP as a low-resource WPF app that shows `Castorice.png` near the bottom-right of the desktop with tray and right-click controls.

**Architecture:** Use one WPF app plus a dependency-free console test project. Keep the WPF window focused on presentation, put settings/logging/startup/single-instance/window-state behavior into small services, and coordinate menu commands through one command service used by both tray and right-click menus.

**Tech Stack:** C# 14, .NET 10 SDK, WPF, Windows Forms notify icon, Windows Registry current-user startup key, named mutex, named pipe, file-based JSON settings, dependency-free console test harness.

---

## Prerequisites

Current environment check on 2026-06-26 showed .NET SDK 10.0.301 is installed. Use `net10.0-windows` for the WPF app and test project.

Recommended check:

```powershell
dotnet --info
```

Expected: `10.0.301` or another `10.0.x` SDK appears under `.NET SDKs installed`.

## File Structure

Create this structure:

```text
CastoPet.sln
src/CastoPet/CastoPet.csproj
src/CastoPet/App.xaml
src/CastoPet/App.xaml.cs
src/CastoPet/PetWindow.xaml
src/CastoPet/PetWindow.xaml.cs
src/CastoPet/Assets/Castorice.png
src/CastoPet/Core/AppPaths.cs
src/CastoPet/Core/AppSettings.cs
src/CastoPet/Core/AssetService.cs
src/CastoPet/Core/ClickThroughService.cs
src/CastoPet/Core/LoggingService.cs
src/CastoPet/Core/MenuCommandService.cs
src/CastoPet/Core/SettingsService.cs
src/CastoPet/Core/SingleInstanceService.cs
src/CastoPet/Core/StartupService.cs
src/CastoPet/Core/TrayService.cs
src/CastoPet/Core/WindowPlacementService.cs
tests/CastoPet.Tests/CastoPet.Tests.csproj
tests/CastoPet.Tests/Program.cs
```

Responsibilities:

- `PetWindow`: transparent borderless WPF character window and right-click menu.
- `TrayService`: system tray icon and tray menu.
- `MenuCommandService`: shared behavior behind tray and right-click menu items.
- `SettingsService`: JSON read/write under `%LocalAppData%\CastoPet`.
- `StartupService`: current-user Windows startup registration.
- `SingleInstanceService`: named mutex and named pipe restore signal.
- `WindowPlacementService`: bottom-right placement math.
- `ClickThroughService`: WPF window extended style update for click-through mode.
- `LoggingService`: local text logs.
- `AssetService`: built-in `Castorice.png` resource loading.
- `CastoPet.Tests`: dependency-free console tests for non-UI services.

## Task 1: Scaffold Solution And Projects

**Files:**
- Create: `CastoPet.sln`
- Create: `src/CastoPet/CastoPet.csproj`
- Create: `tests/CastoPet.Tests/CastoPet.Tests.csproj`
- Move/copy: `Castorice.png` to `src/CastoPet/Assets/Castorice.png`

- [ ] **Step 1: Verify SDK**

Run:

```powershell
dotnet --info
```

Expected: `.NET SDKs installed:` lists a `10.0.x` SDK. If it says `No SDKs were found`, install .NET 10 SDK first.

- [ ] **Step 2: Create solution and projects**

Run:

```powershell
dotnet new sln -n CastoPet
dotnet new wpf -n CastoPet -o src/CastoPet -f net10.0-windows
dotnet new console -n CastoPet.Tests -o tests/CastoPet.Tests -f net10.0-windows
dotnet sln CastoPet.sln add src/CastoPet/CastoPet.csproj
dotnet sln CastoPet.sln add tests/CastoPet.Tests/CastoPet.Tests.csproj
dotnet add tests/CastoPet.Tests/CastoPet.Tests.csproj reference src/CastoPet/CastoPet.csproj
New-Item -ItemType Directory -Force -Path src/CastoPet/Assets
Copy-Item -LiteralPath Castorice.png -Destination src/CastoPet/Assets/Castorice.png -Force
```

Expected: solution, WPF project, console test project, and asset file exist.

- [ ] **Step 3: Replace WPF project file**

Write `src/CastoPet/CastoPet.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>
    <ApplicationIcon />
  </PropertyGroup>

  <ItemGroup>
    <Resource Include="Assets\Castorice.png" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Replace test project file**

Write `tests/CastoPet.Tests/CastoPet.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\CastoPet\CastoPet.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 5: Build scaffold**

Run:

```powershell
dotnet build CastoPet.sln
```

Expected: `Build succeeded.`

- [ ] **Step 6: Commit**

If the repository has valid git metadata, run:

```powershell
git add CastoPet.sln src/CastoPet tests/CastoPet.Tests
git commit -m "chore: scaffold CastoPet WPF solution"
```

Expected: commit succeeds. If `git status` reports `fatal: not a git repository`, skip commits until the repository is initialized or repaired.

## Task 2: Add Settings, Paths, Logging, And Tests

**Files:**
- Create: `src/CastoPet/Core/AppPaths.cs`
- Create: `src/CastoPet/Core/AppSettings.cs`
- Create: `src/CastoPet/Core/LoggingService.cs`
- Create: `src/CastoPet/Core/SettingsService.cs`
- Replace: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Write failing console tests**

Write `tests/CastoPet.Tests/Program.cs`:

```csharp
using CastoPet.Core;

var tests = new (string Name, Action Test)[]
{
    ("Default settings match MVP defaults", DefaultSettingsMatchMvpDefaults),
    ("Settings round trip as JSON", SettingsRoundTripAsJson),
    ("Invalid settings file falls back to defaults", InvalidSettingsFallsBackToDefaults),
    ("Logging writes a dated log file", LoggingWritesDatedLogFile),
};

var failures = 0;
foreach (var test in tests)
{
    try
    {
        test.Test();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.WriteLine($"FAIL {test.Name}: {ex.Message}");
    }
}

return failures;

static void DefaultSettingsMatchMvpDefaults()
{
    var settings = AppSettings.Default;
    Assert.True(settings.Topmost, "Topmost should default to true.");
    Assert.False(settings.ClickThrough, "ClickThrough should default to false.");
    Assert.False(settings.ShowInTaskbar, "ShowInTaskbar should default to false.");
    Assert.False(settings.StartWithWindows, "StartWithWindows should default to false.");
}

static void SettingsRoundTripAsJson()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var logger = new LoggingService(paths);
    var service = new SettingsService(paths, logger);

    var settings = new AppSettings
    {
        Topmost = false,
        ClickThrough = true,
        ShowInTaskbar = true,
        StartWithWindows = true,
    };

    service.Save(settings);
    var loaded = service.Load();

    Assert.False(loaded.Topmost, "Topmost should round trip.");
    Assert.True(loaded.ClickThrough, "ClickThrough should round trip.");
    Assert.True(loaded.ShowInTaskbar, "ShowInTaskbar should round trip.");
    Assert.True(loaded.StartWithWindows, "StartWithWindows should round trip.");
}

static void InvalidSettingsFallsBackToDefaults()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    Directory.CreateDirectory(paths.DataDirectory);
    File.WriteAllText(paths.SettingsFile, "{not valid json");

    var logger = new LoggingService(paths);
    var service = new SettingsService(paths, logger);
    var loaded = service.Load();

    Assert.True(loaded.Topmost, "Invalid settings should return defaults.");
    Assert.False(loaded.ClickThrough, "Invalid settings should return defaults.");
    Assert.True(File.Exists(paths.LogFile), "Invalid settings should be logged.");
}

static void LoggingWritesDatedLogFile()
{
    using var temp = TempDirectory.Create();
    var paths = new AppPaths(temp.Path);
    var logger = new LoggingService(paths);

    logger.Info("hello");

    Assert.True(File.Exists(paths.LogFile), "Log file should exist.");
    var text = File.ReadAllText(paths.LogFile);
    Assert.Contains(text, "hello", "Log file should include message.");
}

static class Assert
{
    public static void True(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    public static void False(bool value, string message)
    {
        if (value) throw new InvalidOperationException(message);
    }

    public static void Contains(string text, string expected, string message)
    {
        if (!text.Contains(expected, StringComparison.Ordinal)) throw new InvalidOperationException(message);
    }
}

sealed class TempDirectory : IDisposable
{
    public string Path { get; }

    private TempDirectory(string path)
    {
        Path = path;
    }

    public static TempDirectory Create()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CastoPet.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return new TempDirectory(path);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
```

- [ ] **Step 2: Run tests to verify failure**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
```

Expected: build fails because `CastoPet.Core` types do not exist.

- [ ] **Step 3: Implement paths**

Create `src/CastoPet/Core/AppPaths.cs`:

```csharp
namespace CastoPet.Core;

public sealed class AppPaths
{
    public AppPaths(string? baseDirectory = null)
    {
        DataDirectory = baseDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CastoPet");
        LogsDirectory = Path.Combine(DataDirectory, "logs");
        SettingsFile = Path.Combine(DataDirectory, "settings.json");
        LogFile = Path.Combine(LogsDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");
    }

    public string DataDirectory { get; }
    public string LogsDirectory { get; }
    public string SettingsFile { get; }
    public string LogFile { get; }
}
```

- [ ] **Step 4: Implement settings model**

Create `src/CastoPet/Core/AppSettings.cs`:

```csharp
namespace CastoPet.Core;

public sealed class AppSettings
{
    public bool Topmost { get; set; } = true;
    public bool ClickThrough { get; set; }
    public bool ShowInTaskbar { get; set; }
    public bool StartWithWindows { get; set; }

    public static AppSettings Default => new();

    public AppSettings Clone()
    {
        return new AppSettings
        {
            Topmost = Topmost,
            ClickThrough = ClickThrough,
            ShowInTaskbar = ShowInTaskbar,
            StartWithWindows = StartWithWindows,
        };
    }
}
```

- [ ] **Step 5: Implement logging**

Create `src/CastoPet/Core/LoggingService.cs`:

```csharp
namespace CastoPet.Core;

public sealed class LoggingService
{
    private readonly AppPaths _paths;
    private readonly object _gate = new();

    public LoggingService(AppPaths paths)
    {
        _paths = paths;
    }

    public void Info(string message)
    {
        Write("INFO", message, null);
    }

    public void Error(string message, Exception? exception = null)
    {
        Write("ERROR", message, exception);
    }

    private void Write(string level, string message, Exception? exception)
    {
        lock (_gate)
        {
            Directory.CreateDirectory(_paths.LogsDirectory);
            var line = $"{DateTime.Now:O} [{level}] {message}";
            if (exception is not null)
            {
                line += Environment.NewLine + exception;
            }

            File.AppendAllText(_paths.LogFile, line + Environment.NewLine);
        }
    }
}
```

- [ ] **Step 6: Implement settings service**

Create `src/CastoPet/Core/SettingsService.cs`:

```csharp
using System.Text.Json;

namespace CastoPet.Core;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly AppPaths _paths;
    private readonly LoggingService _logger;

    public SettingsService(AppPaths paths, LoggingService logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_paths.SettingsFile))
            {
                return AppSettings.Default;
            }

            var json = File.ReadAllText(_paths.SettingsFile);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? AppSettings.Default;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load settings. Defaults will be used.", ex);
            return AppSettings.Default;
        }
    }

    public bool Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(_paths.DataDirectory);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_paths.SettingsFile, json);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to save settings.", ex);
            return false;
        }
    }
}
```

- [ ] **Step 7: Run tests**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
```

Expected:

```text
PASS Default settings match MVP defaults
PASS Settings round trip as JSON
PASS Invalid settings file falls back to defaults
PASS Logging writes a dated log file
```

- [ ] **Step 8: Commit**

```powershell
git add src/CastoPet/Core/AppPaths.cs src/CastoPet/Core/AppSettings.cs src/CastoPet/Core/LoggingService.cs src/CastoPet/Core/SettingsService.cs tests/CastoPet.Tests/Program.cs
git commit -m "feat: add settings and logging services"
```

Skip this commit if git metadata is invalid.

## Task 3: Add Placement, Startup, And Single-Instance Services

**Files:**
- Create: `src/CastoPet/Core/WindowPlacementService.cs`
- Create: `src/CastoPet/Core/StartupService.cs`
- Create: `src/CastoPet/Core/SingleInstanceService.cs`
- Modify: `tests/CastoPet.Tests/Program.cs`

- [ ] **Step 1: Extend tests**

Add these entries to the `tests` array in `tests/CastoPet.Tests/Program.cs`:

```csharp
("Bottom-right placement uses work area margin", BottomRightPlacementUsesWorkAreaMargin),
("Startup value name is CastoPet", StartupValueNameIsCastoPet),
```

Add these test methods before `Assert`:

```csharp
static void BottomRightPlacementUsesWorkAreaMargin()
{
    var bounds = WindowPlacementService.CalculateBottomRight(
        workAreaLeft: 0,
        workAreaTop: 0,
        workAreaWidth: 1920,
        workAreaHeight: 1080,
        windowWidth: 320,
        windowHeight: 420,
        margin: 24);

    Assert.Equal(1576, (int)bounds.Left, "Left should place window near the right edge.");
    Assert.Equal(636, (int)bounds.Top, "Top should place window near the bottom edge.");
}

static void StartupValueNameIsCastoPet()
{
    Assert.Equal("CastoPet", StartupService.ValueName, "Startup registry value should use app name.");
}
```

Add this assertion method to `Assert`:

```csharp
public static void Equal<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message} Expected {expected}, got {actual}.");
    }
}
```

- [ ] **Step 2: Run tests to verify failure**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
```

Expected: build fails because `WindowPlacementService` and `StartupService` do not exist.

- [ ] **Step 3: Implement placement service**

Create `src/CastoPet/Core/WindowPlacementService.cs`:

```csharp
using System.Windows;

namespace CastoPet.Core;

public static class WindowPlacementService
{
    public static Rect CalculateBottomRight(
        double workAreaLeft,
        double workAreaTop,
        double workAreaWidth,
        double workAreaHeight,
        double windowWidth,
        double windowHeight,
        double margin)
    {
        var left = workAreaLeft + Math.Max(margin, workAreaWidth - windowWidth - margin);
        var top = workAreaTop + Math.Max(margin, workAreaHeight - windowHeight - margin);
        return new Rect(left, top, windowWidth, windowHeight);
    }

    public static void MoveToBottomRight(Window window, double margin = 24)
    {
        var workArea = SystemParameters.WorkArea;
        var width = window.Width > 0 ? window.Width : window.ActualWidth;
        var height = window.Height > 0 ? window.Height : window.ActualHeight;
        var target = CalculateBottomRight(workArea.Left, workArea.Top, workArea.Width, workArea.Height, width, height, margin);
        window.Left = target.Left;
        window.Top = target.Top;
    }
}
```

- [ ] **Step 4: Implement startup service**

Create `src/CastoPet/Core/StartupService.cs`:

```csharp
using Microsoft.Win32;

namespace CastoPet.Core;

public sealed class StartupService
{
    public const string ValueName = "CastoPet";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private readonly LoggingService _logger;

    public StartupService(LoggingService logger)
    {
        _logger = logger;
    }

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            return key?.GetValue(ValueName) is string value && !string.IsNullOrWhiteSpace(value);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to read startup registration.", ex);
            return false;
        }
    }

    public bool SetEnabled(bool enabled, string executablePath)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(RunKey, writable: true);

            if (enabled)
            {
                key.SetValue(ValueName, $"\"{executablePath}\"");
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to update startup registration.", ex);
            return false;
        }
    }
}
```

- [ ] **Step 5: Implement single-instance service**

Create `src/CastoPet/Core/SingleInstanceService.cs`:

```csharp
using System.IO.Pipes;
using System.Text;

namespace CastoPet.Core;

public sealed class SingleInstanceService : IDisposable
{
    private const string MutexName = "Local\\CastoPet.SingleInstance";
    private const string PipeName = "CastoPet.SingleInstance.Restore";
    private readonly LoggingService _logger;
    private readonly Mutex _mutex;
    private CancellationTokenSource? _serverCancellation;

    public SingleInstanceService(LoggingService logger)
    {
        _logger = logger;
        _mutex = new Mutex(initiallyOwned: true, MutexName, out IsPrimaryInstance);
    }

    public bool IsPrimaryInstance { get; }

    public void StartRestoreServer(Action restore)
    {
        if (!IsPrimaryInstance)
        {
            return;
        }

        _serverCancellation = new CancellationTokenSource();
        _ = Task.Run(() => RunServerAsync(restore, _serverCancellation.Token));
    }

    public async Task SignalRestoreAsync()
    {
        try
        {
            await using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            await client.ConnectAsync(1000);
            var payload = Encoding.UTF8.GetBytes("restore");
            await client.WriteAsync(payload);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to signal existing CastoPet instance.", ex);
        }
    }

    private async Task RunServerAsync(Action restore, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(cancellationToken);
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
        _serverCancellation?.Cancel();
        _serverCancellation?.Dispose();
        if (IsPrimaryInstance)
        {
            _mutex.ReleaseMutex();
        }
        _mutex.Dispose();
    }
}
```

- [ ] **Step 6: Run tests**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
```

Expected: all tests print `PASS`.

- [ ] **Step 7: Commit**

```powershell
git add src/CastoPet/Core/WindowPlacementService.cs src/CastoPet/Core/StartupService.cs src/CastoPet/Core/SingleInstanceService.cs tests/CastoPet.Tests/Program.cs
git commit -m "feat: add placement startup and single-instance services"
```

Skip this commit if git metadata is invalid.

## Task 4: Build Pet Window And Asset Loading

**Files:**
- Create: `src/CastoPet/Core/AssetService.cs`
- Replace: `src/CastoPet/PetWindow.xaml`
- Replace: `src/CastoPet/PetWindow.xaml.cs`

- [ ] **Step 1: Implement asset service**

Create `src/CastoPet/Core/AssetService.cs`:

```csharp
using System.Windows.Media.Imaging;

namespace CastoPet.Core;

public sealed class AssetService
{
    private readonly LoggingService _logger;

    public AssetService(LoggingService logger)
    {
        _logger = logger;
    }

    public BitmapImage LoadDefaultCharacter()
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri("pack://application:,,,/Assets/Castorice.png", UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load built-in Castorice.png.", ex);
            throw;
        }
    }
}
```

- [ ] **Step 2: Replace pet window XAML**

Write `src/CastoPet/PetWindow.xaml`:

```xml
<Window x:Class="CastoPet.PetWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="CastoPet"
        Width="320"
        Height="420"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        ResizeMode="NoResize"
        ShowInTaskbar="False"
        Topmost="True"
        SizeToContent="Manual">
    <Grid Background="Transparent">
        <Image x:Name="CharacterImage"
               Stretch="Uniform"
               SnapsToDevicePixels="True"
               RenderOptions.BitmapScalingMode="HighQuality" />
    </Grid>
</Window>
```

- [ ] **Step 3: Replace pet window code-behind**

Write `src/CastoPet/PetWindow.xaml.cs`:

```csharp
using System.Windows;
using CastoPet.Core;

namespace CastoPet;

public partial class PetWindow : Window
{
    private readonly LoggingService _logger;

    public PetWindow(AssetService assets, LoggingService logger)
    {
        InitializeComponent();
        _logger = logger;

        try
        {
            CharacterImage.Source = assets.LoadDefaultCharacter();
        }
        catch
        {
            MessageBox.Show(
                "CastoPet could not load the built-in character image Castorice.png.",
                "CastoPet",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        Loaded += (_, _) => WindowPlacementService.MoveToBottomRight(this);
    }

    public void ApplySettings(AppSettings settings)
    {
        Topmost = settings.Topmost;
        ShowInTaskbar = settings.ShowInTaskbar;
    }

    public void ShowOrRestore()
    {
        if (!IsVisible)
        {
            Show();
        }

        WindowState = WindowState.Normal;
        Activate();
        WindowPlacementService.MoveToBottomRight(this);
        _logger.Info("Pet window shown or restored.");
    }
}
```

- [ ] **Step 4: Build**

Run:

```powershell
dotnet build CastoPet.sln
```

Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```powershell
git add src/CastoPet/Core/AssetService.cs src/CastoPet/PetWindow.xaml src/CastoPet/PetWindow.xaml.cs
git commit -m "feat: add static pet window"
```

Skip this commit if git metadata is invalid.

## Task 5: Add Click-Through, Menus, Tray, And Commands

**Files:**
- Create: `src/CastoPet/Core/ClickThroughService.cs`
- Create: `src/CastoPet/Core/MenuCommandService.cs`
- Create: `src/CastoPet/Core/TrayService.cs`
- Modify: `src/CastoPet/PetWindow.xaml.cs`

- [ ] **Step 1: Implement click-through service**

Create `src/CastoPet/Core/ClickThroughService.cs`:

```csharp
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CastoPet.Core;

public static class ClickThroughService
{
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;

    public static void Apply(Window window, bool clickThrough, bool showInTaskbar)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var style = GetWindowLong(handle, GwlExStyle);

        if (clickThrough)
        {
            style |= WsExTransparent;
        }
        else
        {
            style &= ~WsExTransparent;
        }

        if (showInTaskbar)
        {
            style &= ~WsExToolWindow;
        }
        else
        {
            style |= WsExToolWindow;
        }

        SetWindowLong(handle, GwlExStyle, style);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
```

- [ ] **Step 2: Implement command service**

Create `src/CastoPet/Core/MenuCommandService.cs`:

```csharp
using System.Windows;

namespace CastoPet.Core;

public sealed class MenuCommandService
{
    private readonly PetWindow _window;
    private readonly SettingsService _settingsService;
    private readonly StartupService _startupService;
    private readonly LoggingService _logger;
    private readonly string _executablePath;

    public MenuCommandService(
        PetWindow window,
        AppSettings settings,
        SettingsService settingsService,
        StartupService startupService,
        LoggingService logger,
        string executablePath)
    {
        _window = window;
        Settings = settings;
        _settingsService = settingsService;
        _startupService = startupService;
        _logger = logger;
        _executablePath = executablePath;
    }

    public AppSettings Settings { get; }
    public event Action? SettingsChanged;

    public void ShowOrRestore()
    {
        _window.ShowOrRestore();
    }

    public void ToggleTopmost()
    {
        Settings.Topmost = !Settings.Topmost;
        ApplyAndSave("Always on top setting changed.");
    }

    public void ToggleClickThrough()
    {
        Settings.ClickThrough = !Settings.ClickThrough;
        ApplyAndSave("Mouse click-through setting changed.");
    }

    public void ToggleShowInTaskbar()
    {
        Settings.ShowInTaskbar = !Settings.ShowInTaskbar;
        ApplyAndSave("Taskbar visibility setting changed.");
    }

    public void ToggleStartWithWindows()
    {
        var target = !Settings.StartWithWindows;
        if (!_startupService.SetEnabled(target, _executablePath))
        {
            MessageBox.Show(
                "CastoPet could not update the Start with Windows setting.",
                "CastoPet",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        Settings.StartWithWindows = target;
        ApplyAndSave("Start with Windows setting changed.");
    }

    public void Exit()
    {
        _logger.Info("CastoPet exiting.");
        Application.Current.Shutdown();
    }

    private void ApplyAndSave(string logMessage)
    {
        _window.ApplySettings(Settings);
        _settingsService.Save(Settings);
        _logger.Info(logMessage);
        SettingsChanged?.Invoke();
    }
}
```

- [ ] **Step 3: Implement tray service**

Create `src/CastoPet/Core/TrayService.cs`:

```csharp
using Forms = System.Windows.Forms;

namespace CastoPet.Core;

public sealed class TrayService : IDisposable
{
    private readonly MenuCommandService _commands;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _topmostItem;
    private readonly Forms.ToolStripMenuItem _clickThroughItem;
    private readonly Forms.ToolStripMenuItem _taskbarItem;
    private readonly Forms.ToolStripMenuItem _startupItem;

    public TrayService(MenuCommandService commands)
    {
        _commands = commands;
        _topmostItem = CreateCheckedItem("Always on top", _commands.ToggleTopmost);
        _clickThroughItem = CreateCheckedItem("Mouse click-through", _commands.ToggleClickThrough);
        _taskbarItem = CreateCheckedItem("Show taskbar icon", _commands.ToggleShowInTaskbar);
        _startupItem = CreateCheckedItem("Start with Windows", _commands.ToggleStartWithWindows);

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Show/restore", null, (_, _) => _commands.ShowOrRestore());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(_topmostItem);
        menu.Items.Add(_clickThroughItem);
        menu.Items.Add(_taskbarItem);
        menu.Items.Add(_startupItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => _commands.Exit());

        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "CastoPet",
            Icon = Forms.SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true,
        };

        _notifyIcon.DoubleClick += (_, _) => _commands.ShowOrRestore();
        _commands.SettingsChanged += RefreshChecks;
        RefreshChecks();
    }

    private static Forms.ToolStripMenuItem CreateCheckedItem(string text, Action action)
    {
        var item = new Forms.ToolStripMenuItem(text);
        item.Click += (_, _) => action();
        return item;
    }

    private void RefreshChecks()
    {
        _topmostItem.Checked = _commands.Settings.Topmost;
        _clickThroughItem.Checked = _commands.Settings.ClickThrough;
        _taskbarItem.Checked = _commands.Settings.ShowInTaskbar;
        _startupItem.Checked = _commands.Settings.StartWithWindows;
    }

    public void Dispose()
    {
        _commands.SettingsChanged -= RefreshChecks;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
```

- [ ] **Step 4: Add right-click menu to pet window**

Modify `src/CastoPet/PetWindow.xaml.cs` so the class contains this method:

```csharp
public void AttachContextMenu(MenuCommandService commands)
{
    var menu = new System.Windows.Controls.ContextMenu();

    menu.Items.Add(CreateMenuItem("Show/restore", commands.ShowOrRestore));
    menu.Items.Add(new System.Windows.Controls.Separator());
    menu.Items.Add(CreateCheckedMenuItem("Always on top", () => commands.Settings.Topmost, commands.ToggleTopmost));
    menu.Items.Add(CreateCheckedMenuItem("Mouse click-through", () => commands.Settings.ClickThrough, commands.ToggleClickThrough));
    menu.Items.Add(CreateCheckedMenuItem("Show taskbar icon", () => commands.Settings.ShowInTaskbar, commands.ToggleShowInTaskbar));
    menu.Items.Add(CreateCheckedMenuItem("Start with Windows", () => commands.Settings.StartWithWindows, commands.ToggleStartWithWindows));
    menu.Items.Add(new System.Windows.Controls.Separator());
    menu.Items.Add(CreateMenuItem("Exit", commands.Exit));

    menu.Opened += (_, _) => RefreshContextMenuChecks(menu, commands);
    ContextMenu = menu;
    commands.SettingsChanged += () => RefreshContextMenuChecks(menu, commands);
}

private static System.Windows.Controls.MenuItem CreateMenuItem(string header, Action action)
{
    var item = new System.Windows.Controls.MenuItem { Header = header };
    item.Click += (_, _) => action();
    return item;
}

private static System.Windows.Controls.MenuItem CreateCheckedMenuItem(string header, Func<bool> isChecked, Action action)
{
    var item = new System.Windows.Controls.MenuItem
    {
        Header = header,
        IsCheckable = true,
        IsChecked = isChecked(),
    };
    item.SubmenuOpened += (_, _) => item.IsChecked = isChecked();
    item.Click += (_, _) => action();
    return item;
}

private static void RefreshContextMenuChecks(System.Windows.Controls.ContextMenu menu, MenuCommandService commands)
{
    foreach (var item in menu.Items.OfType<System.Windows.Controls.MenuItem>())
    {
        var header = item.Header as string;
        item.IsChecked = header switch
        {
            "Always on top" => commands.Settings.Topmost,
            "Mouse click-through" => commands.Settings.ClickThrough,
            "Show taskbar icon" => commands.Settings.ShowInTaskbar,
            "Start with Windows" => commands.Settings.StartWithWindows,
            _ => item.IsChecked,
        };
    }
}
```

Modify `ApplySettings` in the same file:

```csharp
public void ApplySettings(AppSettings settings)
{
    Topmost = settings.Topmost;
    ShowInTaskbar = settings.ShowInTaskbar;
    if (new System.Windows.Interop.WindowInteropHelper(this).Handle == IntPtr.Zero)
    {
        SourceInitialized += (_, _) => ClickThroughService.Apply(this, settings.ClickThrough, settings.ShowInTaskbar);
    }
    else
    {
        ClickThroughService.Apply(this, settings.ClickThrough, settings.ShowInTaskbar);
    }
}
```

- [ ] **Step 5: Build**

Run:

```powershell
dotnet build CastoPet.sln
```

Expected: `Build succeeded.`

- [ ] **Step 6: Commit**

```powershell
git add src/CastoPet/Core/ClickThroughService.cs src/CastoPet/Core/MenuCommandService.cs src/CastoPet/Core/TrayService.cs src/CastoPet/PetWindow.xaml.cs
git commit -m "feat: add tray and menu controls"
```

Skip this commit if git metadata is invalid.

## Task 6: Wire Application Startup And Shutdown

**Files:**
- Replace: `src/CastoPet/App.xaml`
- Replace: `src/CastoPet/App.xaml.cs`

- [ ] **Step 1: Replace app XAML**

Write `src/CastoPet/App.xaml`:

```xml
<Application x:Class="CastoPet.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             ShutdownMode="OnExplicitShutdown">
    <Application.Resources />
</Application>
```

- [ ] **Step 2: Replace app startup code**

Write `src/CastoPet/App.xaml.cs`:

```csharp
using System.Reflection;
using System.Windows;
using CastoPet.Core;

namespace CastoPet;

public partial class App : Application
{
    private LoggingService? _logger;
    private SingleInstanceService? _singleInstance;
    private TrayService? _tray;
    private PetWindow? _window;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var paths = new AppPaths();
        _logger = new LoggingService(paths);
        _logger.Info("CastoPet starting.");

        _singleInstance = new SingleInstanceService(_logger);
        if (!_singleInstance.IsPrimaryInstance)
        {
            await _singleInstance.SignalRestoreAsync();
            Shutdown();
            return;
        }

        var settingsService = new SettingsService(paths, _logger);
        var settings = settingsService.Load();
        var startupService = new StartupService(_logger);
        settings.StartWithWindows = startupService.IsEnabled();

        var assets = new AssetService(_logger);
        _window = new PetWindow(assets, _logger);

        var executablePath = Environment.ProcessPath
            ?? Assembly.GetExecutingAssembly().Location;
        var commands = new MenuCommandService(_window, settings, settingsService, startupService, _logger, executablePath);

        _window.AttachContextMenu(commands);
        _tray = new TrayService(commands);
        _singleInstance.StartRestoreServer(() => Dispatcher.Invoke(commands.ShowOrRestore));

        _window.Show();
        _window.ApplySettings(settings);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.Info("CastoPet shutdown.");
        _tray?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
```

- [ ] **Step 3: Build**

Run:

```powershell
dotnet build CastoPet.sln
```

Expected: `Build succeeded.`

- [ ] **Step 4: Run app manually**

Run:

```powershell
dotnet run --project src/CastoPet/CastoPet.csproj
```

Expected:

- One CastoPet window appears near the bottom-right.
- The window background is transparent.
- The character image displays.
- A tray icon appears.
- Closing from tray exits the process.

- [ ] **Step 5: Commit**

```powershell
git add src/CastoPet/App.xaml src/CastoPet/App.xaml.cs
git commit -m "feat: wire CastoPet application startup"
```

Skip this commit if git metadata is invalid.

## Task 7: Verify MVP Behavior

**Files:**
- Modify only files required to fix failed checks.

- [ ] **Step 1: Run automated service tests**

Run:

```powershell
dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj
```

Expected: every test prints `PASS` and the process exits with code `0`.

- [ ] **Step 2: Build release configuration**

Run:

```powershell
dotnet build CastoPet.sln -c Release
```

Expected: `Build succeeded.`

- [ ] **Step 3: Manual window verification**

Run:

```powershell
dotnet run --project src/CastoPet/CastoPet.csproj -c Release
```

Expected:

- Pet appears near bottom-right.
- Pet has no normal window chrome.
- Pet is topmost by default.
- Taskbar icon is hidden by default.
- Tray icon is visible.

- [ ] **Step 4: Manual menu verification**

Use the tray menu and right-click menu.

Expected:

- `Show/restore` shows a hidden pet.
- `Show/restore` moves a visible pet back near bottom-right.
- `Always on top` toggles immediately and persists after restart.
- `Mouse click-through` toggles immediately and persists after restart.
- With click-through enabled, the tray menu can disable it.
- `Show taskbar icon` toggles immediately and persists after restart.
- `Start with Windows` toggles current-user startup registration.
- `Exit` removes the tray icon and exits.

- [ ] **Step 5: Manual single-instance verification**

Start the app, then run the same command again:

```powershell
dotnet run --project src/CastoPet/CastoPet.csproj -c Release
```

Expected:

- No second pet window remains.
- Existing pet shows/restores near bottom-right.

- [ ] **Step 6: Manual settings corruption verification**

Close CastoPet, corrupt settings, and restart:

```powershell
$settings = Join-Path $env:LOCALAPPDATA 'CastoPet\settings.json'
Set-Content -LiteralPath $settings -Value '{bad json'
dotnet run --project src/CastoPet/CastoPet.csproj -c Release
```

Expected:

- App still starts.
- Default settings are used.
- `%LocalAppData%\CastoPet\logs` contains an error entry.

- [ ] **Step 7: Commit final fixes**

```powershell
git status --short
git add src tests CastoPet.sln
git commit -m "test: verify CastoPet MVP behavior"
```

Skip this commit if git metadata is invalid.

## Spec Coverage Review

Covered by this plan:

- Transparent borderless WPF window: Task 4.
- Built-in `Castorice.png`: Tasks 1 and 4.
- Fixed bottom-right placement: Tasks 3, 4, 6, and 7.
- Topmost toggle: Tasks 2, 5, 6, and 7.
- Mouse click-through toggle and tray recovery path: Tasks 5, 6, and 7.
- Tray and right-click menus: Task 5.
- Show/restore behavior: Tasks 4, 5, 6, and 7.
- Start with Windows: Tasks 3, 5, 6, and 7.
- Taskbar icon toggle: Tasks 2, 5, 6, and 7.
- Single-instance behavior: Tasks 3, 6, and 7.
- Basic persisted settings: Task 2.
- Lightweight logs and key error prompts: Tasks 2, 4, 5, and 7.
- Exclusions such as dragging, click feedback, settings window, auto-update, and external asset packs: maintained by not adding those tasks.

Implementation risk to watch:

- WPF `ShowInTaskbar` and extended window styles can interact subtly. Verify taskbar visibility and click-through manually after Task 5.
- Startup registration should use the actual executable path. Debug `dotnet run` paths may differ from published exe paths; final packaging can revisit this.
- The current workspace previously had invalid git metadata. Commit steps are included, but must be skipped until `git status` works.
