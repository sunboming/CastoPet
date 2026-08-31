namespace CastoPet.Tests;

internal static partial class TestSuite
{
    static void ReleaseSettingsExposeOnlyBasicOptions()
    {
        var settings = AppSettings.Default;
        var definitions = SettingCatalog.Create(settings, SettingActions.None);

        Assert.Equal(4, definitions.Count, "The 0.1 release should expose exactly four persistent boolean settings.");
        Assert.Equal(
            "topmost,click-through,show-in-taskbar,start-with-windows",
            string.Join(',', definitions.Select(definition => definition.Id)),
            "The release settings should stay within the approved basic scope.");
        Assert.True(definitions.All(definition => definition.ShowInDirectMenu), "Every basic setting should be available from the pet and tray menus.");
    }

    static void ReleaseUsesOnePublicProductIdentity()
    {
        var identity = CastoPetProductIdentity.Current;

        Assert.Equal("CastoPet", identity.ApplicationId, "The release should use the public application id.");
        Assert.Equal("CastoPet", identity.DisplayName, "The release should use one display name.");
        Assert.Equal("CastoPet", identity.DataDirectoryName, "The release should use one data directory.");
        Assert.Equal("CastoPet", identity.PackageId, "The release should use one installer identity.");
    }

    static void PortableDistributionKeepsUserDataBesideTheApplication()
    {
        var identity = CastoPetProductIdentity.Current;
        var portableRoot = System.IO.Path.Combine("D:\\Portable", "CastoPet");
        var localAppDataRoot = System.IO.Path.Combine("C:\\Users", "test", "AppData", "Local");

        var portable = AppPaths.ForDistribution(
            identity,
            isPortable: true,
            portableRoot,
            localAppDataRoot);
        var installed = AppPaths.ForDistribution(
            identity,
            isPortable: false,
            portableRoot,
            localAppDataRoot);

        Assert.Equal(System.IO.Path.Combine(portableRoot, "UserData"), portable.DataDirectory, "Portable data should stay beside the extracted application.");
        Assert.Equal(System.IO.Path.Combine(portableRoot, "UserData", "Crashes"), portable.CrashesDirectory, "Portable crash reports should stay in portable user data.");
        Assert.Equal(System.IO.Path.Combine(localAppDataRoot, "CastoPet"), installed.DataDirectory, "Installed data should remain under local app data.");
        Assert.False(portable.DataDirectory == installed.DataDirectory, "Portable and installed distributions must not share user data.");
    }

    static void BuiltInSkinProvidesIdleAndBlink()
    {
        var skin = BuiltInPetSkins.Castorice;

        Assert.True(skin.GetRequiredAction(PetActionKind.Idle).FramePaths.Count > 0, "The built-in skin should provide idle frames.");
        Assert.True(skin.GetRequiredAction(PetActionKind.Blink).FramePaths.Count > 0, "The built-in skin should provide blink frames.");
    }

    static void MaintenanceMenuCommandsUseSharedCallbacks()
    {
        var crashOpenCount = 0;
        var updateCheckCount = 0;
        var commands = new MenuCommandService(
            new FakePetCommandTarget(),
            AppSettings.Default,
            new FakeSettingsStore(),
            new FakeStartupRegistration(),
            new FakeApplicationLogger(),
            new FakeUserNotificationService(),
            new FakeApplicationShutdown(),
            "CastoPet.exe",
            () => crashOpenCount++,
            () => updateCheckCount++);

        commands.OpenCrashReports();
        commands.CheckForUpdates();

        Assert.Equal(1, crashOpenCount, "The maintenance menu should route crash reports through one shared command.");
        Assert.Equal(1, updateCheckCount, "The maintenance menu should route update checks through one shared command.");
    }

    static void CurrentUpdateMessageIncludesInstalledVersion()
    {
        var workspace = FindWorkspaceRoot();
        var app = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "App.xaml.cs"));

        Assert.Contains(
            app,
            "$\"当前已是最新版本。\\n当前版本：{result.CurrentVersion}\"",
            "The current-version update result should tell the user which version is installed.");
    }

    static void PetWindowContainsOnlyBasicInteractionEntryPoints()
    {
        var workspace = FindWorkspaceRoot();
        var windowRoot = System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows");
        var markup = File.ReadAllText(System.IO.Path.Combine(windowRoot, "PetWindow.xaml"));
        var source = File.ReadAllText(System.IO.Path.Combine(windowRoot, "PetWindow.xaml.cs"));

        Assert.Contains(source, "StartIdleAnimation", "The release pet should play idle animation.");
        Assert.Contains(source, "ScheduleNextBlink", "The release pet should schedule random blinks.");
        Assert.Contains(source, "DragMove();", "The release pet should support left-button dragging.");
        Assert.Contains(source, "StopPassiveAnimations();", "Dragging should pause idle and blink animation.");
        Assert.Contains(source, "ContextMenu", "The release pet should retain the traditional right-click menu.");
        Assert.False(source.Contains("GetDraggingCharacter", StringComparison.Ordinal), "Dragging should keep the current character frame instead of loading another image.");
        foreach (var excludedFeature in new[] { "RadialWheel", "Shortcut", "Petting", "ActiveMovement", "PushCursor", "Expression" })
        {
            Assert.False(markup.Contains(excludedFeature, StringComparison.OrdinalIgnoreCase), $"Pet window markup should not contain {excludedFeature}.");
            Assert.False(source.Contains(excludedFeature, StringComparison.OrdinalIgnoreCase), $"Pet window code should not contain {excludedFeature}.");
        }
    }

    static void CrashReportsDoNotExposeObsoleteEdition()
    {
        var report = CrashReportFormatter.Format(
            new CrashReportContext(
                DateTimeOffset.UnixEpoch,
                "0.1.0",
                "Windows",
                "X64",
                "C:\\Users\\test",
                "test",
                "abc123"),
            new InvalidOperationException("failure"),
            []);

        Assert.Contains(report, "CastoPet version: 0.1.0", "Crash reports should include the release version.");
        Assert.Contains(report, "Source commit: abc123", "Crash reports should retain source traceability.");
        Assert.False(report.Contains("edition", StringComparison.OrdinalIgnoreCase), "Crash reports should not describe a removed product edition.");
    }

    static void ReleaseSettingsPersistAndRecoverFromCorruption()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var service = new SettingsService(paths, new LoggingService(paths));

        Assert.True(service.Save(new AppSettings { Topmost = true, ClickThrough = false }), "The initial settings save should succeed.");
        Assert.True(service.Save(new AppSettings
        {
            Topmost = false,
            ClickThrough = true,
            ShowInTaskbar = true,
            StartWithWindows = true,
        }), "The replacement settings save should succeed.");

        var loaded = service.Load();
        Assert.False(loaded.Topmost, "Topmost should round trip.");
        Assert.True(loaded.ClickThrough, "Click-through should round trip.");
        Assert.True(loaded.ShowInTaskbar, "Taskbar visibility should round trip.");
        Assert.True(loaded.StartWithWindows, "Startup registration preference should round trip.");
        Assert.True(File.Exists(paths.SettingsBackupFile), "Replacing settings should retain a valid backup.");

        File.WriteAllText(paths.SettingsFile, "{broken json");
        var recovered = service.Load();
        Assert.True(recovered.Topmost, "A damaged current file should recover the previous valid settings.");
        Assert.False(recovered.ClickThrough, "Recovery should use the previous valid backup values.");
        Assert.True(Directory.EnumerateFiles(paths.DataDirectory, "settings.invalid-*.json").Any(), "The damaged file should be retained for diagnosis.");
    }

    static void ReleaseCrashReportsSanitizeAndRetainBoundedHistory()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var timestamp = new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero);
        var sequence = -1;
        var service = new CrashReportService(
            paths,
            new LoggingService(paths),
            maxReports: 2,
            nowProvider: () => timestamp.AddMilliseconds(Interlocked.Increment(ref sequence)));

        for (var index = 0; index < 3; index++)
        {
            Assert.True(service.TryWriteReport(new InvalidOperationException($"failure-{index} at {Environment.UserName}"), out _), "Crash reports should be written locally.");
        }

        var reports = Directory.EnumerateFiles(paths.CrashesDirectory, "crash-*.txt").Order().ToArray();
        Assert.Equal(2, reports.Length, "Crash report retention should enforce its configured bound.");
        var newest = File.ReadAllText(reports[^1]);
        Assert.Contains(newest, "failure-2", "The newest report should be retained.");
        Assert.False(newest.Contains(Environment.UserName, StringComparison.OrdinalIgnoreCase), "Crash reports should remove the local user name.");
        Assert.Contains(newest, "%USERNAME%", "Sanitized reports should use a neutral user-name placeholder.");
    }

    static void ReleaseLoggingRotatesBoundedArchives()
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
        Assert.True(files.Length <= 3, "Logging should retain only the current file and two archives.");
        Assert.Contains(File.ReadAllText(paths.LogFile), "entry-7", "The current log should contain the newest entry.");
    }

    static void ReleaseSingleInstanceRejectsASecondOwner()
    {
        using var temp = TempDirectory.Create();
        var logger = new LoggingService(new AppPaths(temp.Path));
        var scope = "CastoPet.Release.Tests." + Guid.NewGuid().ToString("N");

        using var first = new SingleInstanceService(logger, scope);
        using var second = new SingleInstanceService(logger, scope);

        Assert.True(first.IsPrimaryInstance, "The first process should own the instance mutex.");
        Assert.False(second.IsPrimaryInstance, "A second process should not own the same instance mutex.");
    }

    static void ReleaseStartupRegistrationMatchesTheCurrentExecutable()
    {
        Assert.True(
            StartupService.MatchesExecutablePath("\"C:\\Apps\\CastoPet\\CastoPet.exe\"", "C:\\Apps\\CastoPet\\CastoPet.exe"),
            "A quoted registration should match the current executable path.");
        Assert.False(
            StartupService.MatchesExecutablePath("\"C:\\Old\\CastoPet.exe\"", "C:\\Apps\\CastoPet\\CastoPet.exe"),
            "A stale registration should not be treated as enabled.");
    }
}
