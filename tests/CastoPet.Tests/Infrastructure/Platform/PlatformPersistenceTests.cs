namespace CastoPet.Tests;

internal static partial class TestSuite
{
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
        Assert.False(loaded.ActiveMovement, "Invalid settings should return defaults.");
        Assert.False(loaded.PushCursor, "Invalid settings should return defaults.");
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

    static void LoggingRotatesBoundedArchiveFiles()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var logger = new LoggingService(paths, maxLogFileBytes: 180, maxArchiveFiles: 2);

        for (var index = 0; index < 8; index++)
        {
            logger.Info($"entry-{index}-{new string('x', 150)}");
        }

        var logName = System.IO.Path.GetFileName(paths.LogFile);
        var files = Directory.EnumerateFiles(paths.LogsDirectory, $"{logName}*").ToArray();
        Assert.True(files.Length <= 3, "Logging should keep the current file and at most two archives.");
        Assert.True(File.Exists(paths.LogFile + ".1"), "Rotation should create the newest archive.");
        Assert.Contains(File.ReadAllText(paths.LogFile), "entry-7", "The current log should contain the newest entry.");
    }

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

    static void StartupServiceAcceptsProductRegistrationIdentity()
    {
        using var temp = TempDirectory.Create();
        var service = new StartupService(
            new LoggingService(new AppPaths(temp.Path)),
            CastoPetProductIdentity.Preview.StartupValueName);

        Assert.Equal("CastoPet Preview", service.RegistrationValueName, "Preview should use its own Windows startup value.");
    }

    static void StartupRegistrationMatchesCurrentExecutablePath()
    {
        Assert.True(
            StartupService.MatchesExecutablePath(
                "\"C:\\Apps\\CastoPet\\CastoPet.exe\"",
                "C:\\Apps\\CastoPet\\CastoPet.exe"),
            "Quoted registry path should match the executable path.");
        Assert.True(
            StartupService.MatchesExecutablePath(
                "C:\\Apps\\CastoPet\\CastoPet.exe",
                "C:\\Apps\\CastoPet\\CastoPet.exe"),
            "Unquoted registry path should match the executable path.");
        Assert.False(
            StartupService.MatchesExecutablePath(
                "\"C:\\Old\\CastoPet.exe\"",
                "C:\\Apps\\CastoPet\\CastoPet.exe"),
            "Different registry path should not count as enabled for this executable.");
    }

    static void ProjectDoesNotKeepTemplateMainWindow()
    {
        var workspace = FindWorkspaceRoot();

        Assert.False(
            File.Exists(System.IO.Path.Combine(workspace, "src", "CastoPet", "MainWindow.xaml")),
            "Template MainWindow.xaml should not be kept in the tray-only pet app.");
        Assert.False(
            File.Exists(System.IO.Path.Combine(workspace, "src", "CastoPet", "MainWindow.xaml.cs")),
            "Template MainWindow.xaml.cs should not be kept in the tray-only pet app.");
    }

    static void SingleInstanceRejectsSecondOwner()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var logger = new LoggingService(paths);
        var scope = "CastoPet.Tests." + Guid.NewGuid().ToString("N");

        using var first = new SingleInstanceService(logger, scope);
        using var second = new SingleInstanceService(logger, scope);

        Assert.True(first.IsPrimaryInstance, "First service should own the instance mutex.");
        Assert.False(second.IsPrimaryInstance, "Second service should not own the same instance mutex.");
    }

    static void ApplicationComposesTheCurrentProductIdentity()
    {
        var workspace = FindWorkspaceRoot();
        var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "App.xaml.cs"));

        Assert.Contains(source, "CastoPetProductIdentity.Current", "App startup should select one centralized product identity.");
        Assert.Contains(source, "AppPaths.ForProduct(_identity)", "Application data should follow the product identity.");
        Assert.Contains(source, "_identity.InstanceName", "Single-instance ownership should be edition-specific.");
        Assert.Contains(source, "_identity.StartupValueName", "Startup registration should be edition-specific.");
        Assert.Contains(source, "_identity.UpdatesEnabled", "Update composition should explicitly enforce the edition policy.");
    }

    static void SingleInstanceRestoreSignalReachesPrimary()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var logger = new LoggingService(paths);
        var scope = "CastoPet.Tests." + Guid.NewGuid().ToString("N");
        using var first = new SingleInstanceService(logger, scope);
        using var second = new SingleInstanceService(logger, scope);
        using var restored = new ManualResetEventSlim(false);

        first.StartRestoreServer(() => restored.Set());

        var signaled = second.SignalRestoreAsync().GetAwaiter().GetResult();

        Assert.True(signaled, "Second instance should signal primary without pipe errors.");
        Assert.True(restored.Wait(TimeSpan.FromSeconds(2)), "Primary should receive restore signal.");
    }

    static void RuntimePositionStartsAtDefault()
    {
        var state = new PetRuntimeState();

        Assert.False(state.HasRuntimePosition, "New runtime state should not have a dragged position.");
    }

    static void RuntimePositionTracksDragForCurrentRunOnly()
    {
        var state = new PetRuntimeState();

        state.SetRuntimePosition(120, 240);

        Assert.True(state.HasRuntimePosition, "Dragged position should be tracked during this run.");
        Assert.Equal(120d, state.Left, "Dragged left should be stored.");
        Assert.Equal(240d, state.Top, "Dragged top should be stored.");
    }

    static void ShowRestoreKeepsHiddenPositionButResetsVisiblePosition()
    {
        var state = new PetRuntimeState();
        state.SetRuntimePosition(120, 240);

        var hiddenAction = state.GetShowRestoreAction(isVisible: false);
        var visibleAction = state.GetShowRestoreAction(isVisible: true);

        Assert.Equal(PetShowRestoreAction.ShowAtRuntimePosition, hiddenAction, "Hidden pet should reappear at current runtime position.");
        Assert.Equal(PetShowRestoreAction.RestoreDefaultPosition, visibleAction, "Visible pet should restore to default position.");
        Assert.False(state.HasRuntimePosition, "Restoring visible pet to default should clear runtime position.");
    }
}
