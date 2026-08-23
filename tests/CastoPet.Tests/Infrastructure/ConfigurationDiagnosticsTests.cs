namespace CastoPet.Tests;

internal static partial class TestSuite
{
    static void DefaultSettingsMatchMvpDefaults()
    {
        var settings = AppSettings.Default;
        Assert.True(settings.Topmost, "Topmost should default to true.");
        Assert.False(settings.ClickThrough, "ClickThrough should default to false.");
        Assert.False(settings.ShowInTaskbar, "ShowInTaskbar should default to false.");
        Assert.False(settings.StartWithWindows, "StartWithWindows should default to false.");
        Assert.False(settings.ActiveMovement, "ActiveMovement should default to false.");
        Assert.False(settings.PushCursor, "PushCursor should default to false.");
    }

    static void DefaultActiveMovementIsDisabled()
    {
        var settings = AppSettings.Default;

        Assert.False(settings.ActiveMovement, "Active movement should default to false.");
    }

    static void DefaultPushCursorIsDisabled()
    {
        var settings = AppSettings.Default;

        Assert.False(settings.PushCursor, "Push cursor should default to false.");
    }

    static void DefaultInputReactiveModeIsDisabled()
    {
        var settings = AppSettings.Default;

        Assert.False(settings.InputReactiveMode, "Input reactive mode should default to false.");
    }

    static void DefaultThemeFollowsTheSystem()
    {
        Assert.Equal(AppThemeMode.System, AppSettings.Default.ThemeMode, "Existing users should follow the Windows app theme by default.");
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
            ActiveMovement = true,
            PushCursor = true,
        };

        service.Save(settings);
        var loaded = service.Load();

        Assert.False(loaded.Topmost, "Topmost should round trip.");
        Assert.True(loaded.ClickThrough, "ClickThrough should round trip.");
        Assert.True(loaded.ShowInTaskbar, "ShowInTaskbar should round trip.");
        Assert.True(loaded.StartWithWindows, "StartWithWindows should round trip.");
        Assert.True(loaded.ActiveMovement, "ActiveMovement should round trip.");
        Assert.True(loaded.PushCursor, "PushCursor should round trip.");
    }

    static void SettingsRoundTripIncludesActiveMovement()
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
            ActiveMovement = true,
        };

        service.Save(settings);
        var loaded = service.Load();

        Assert.True(loaded.ActiveMovement, "ActiveMovement should round trip.");
    }

    static void SettingsRoundTripIncludesPushCursor()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var logger = new LoggingService(paths);
        var service = new SettingsService(paths, logger);

        var settings = new AppSettings
        {
            PushCursor = true,
        };

        service.Save(settings);
        var loaded = service.Load();

        Assert.True(loaded.PushCursor, "PushCursor should round trip.");
    }

    static void SettingsRoundTripIncludesInputReactiveMode()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var logger = new LoggingService(paths);
        var service = new SettingsService(paths, logger);

        var settings = new AppSettings
        {
            InputReactiveMode = true,
        };

        service.Save(settings);
        var loaded = service.Load();

        Assert.True(loaded.InputReactiveMode, "InputReactiveMode should round trip.");
    }

    static void SettingsRoundTripIncludesSkinManifestPath()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var logger = new LoggingService(paths);
        var service = new SettingsService(paths, logger);

        var settings = new AppSettings
        {
            SkinManifestPath = @"D:\Skins\Custom\skin.json",
        };

        service.Save(settings);
        var loaded = service.Load();

        Assert.Equal(@"D:\Skins\Custom\skin.json", loaded.SkinManifestPath, "Skin manifest path should round trip.");
    }

    static void SettingsRoundTripIncludesThemeMode()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var service = new SettingsService(paths, new LoggingService(paths));
        var settings = new AppSettings { ThemeMode = AppThemeMode.Dark };

        service.Save(settings);
        var loaded = service.Load();

        Assert.Equal(AppThemeMode.Dark, loaded.ThemeMode, "Theme mode should round trip.");
    }

    static void SettingsSaveIsAtomicAndVersioned()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var service = new SettingsService(paths, new LoggingService(paths));

        Assert.True(service.Save(new AppSettings { Topmost = true }), "Initial settings save should succeed.");
        Assert.True(service.Save(new AppSettings { Topmost = false }), "Replacement settings save should succeed.");

        Assert.True(File.Exists(paths.SettingsFile), "Atomic save should leave the current settings file.");
        Assert.True(File.Exists(paths.SettingsBackupFile), "Atomic replacement should retain the previous valid settings file.");
        Assert.False(File.Exists(paths.SettingsTemporaryFile), "Atomic save should not leave a temporary file behind.");

        using var current = System.Text.Json.JsonDocument.Parse(File.ReadAllText(paths.SettingsFile));
        using var backup = System.Text.Json.JsonDocument.Parse(File.ReadAllText(paths.SettingsBackupFile));
        Assert.Equal(AppSettings.CurrentSchemaVersion, current.RootElement.GetProperty("SchemaVersion").GetInt32(), "Saved settings should declare the current schema.");
        Assert.False(current.RootElement.GetProperty("Topmost").GetBoolean(), "Current settings should contain the replacement value.");
        Assert.True(backup.RootElement.GetProperty("Topmost").GetBoolean(), "Backup settings should contain the previous valid value.");
    }

    static void SettingsLoadRestoresTheLastValidBackup()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var service = new SettingsService(paths, new LoggingService(paths));
        service.Save(new AppSettings { ClickThrough = false });
        service.Save(new AppSettings { ClickThrough = true });
        File.WriteAllText(paths.SettingsFile, "{broken json");

        var loaded = service.Load();

        Assert.False(loaded.ClickThrough, "A damaged current file should recover the previous valid settings.");
        Assert.True(Directory.EnumerateFiles(paths.DataDirectory, "settings.invalid-*.json").Any(), "The damaged file should be preserved for diagnosis.");
        Assert.Equal(AppSettings.CurrentSchemaVersion, loaded.SchemaVersion, "Recovered settings should use the current schema.");
        Assert.False(new SettingsService(paths, new LoggingService(paths)).Load().ClickThrough, "The recovered backup should replace the damaged current file.");
    }

    static void SettingsLoadMigratesLegacySchema()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        Directory.CreateDirectory(paths.DataDirectory);
        File.WriteAllText(paths.SettingsFile, """
            {
              "Topmost": false,
              "ThemeMode": 2
            }
            """);

        var loaded = new SettingsService(paths, new LoggingService(paths)).Load();

        Assert.Equal(AppSettings.CurrentSchemaVersion, loaded.SchemaVersion, "A legacy file without schemaVersion should migrate in memory.");
        Assert.False(loaded.Topmost, "Legacy values should survive migration.");
        Assert.Equal(AppThemeMode.Dark, loaded.ThemeMode, "Legacy theme values should survive migration.");
    }

    static void SettingsTransactionRollsBackFailedPersistence()
    {
        var settings = new AppSettings { Topmost = true, ThemeMode = AppThemeMode.Light };

        var saved = SettingsTransaction.TryApply(
            settings,
            candidate =>
            {
                candidate.Topmost = false;
                candidate.ThemeMode = AppThemeMode.Dark;
            },
            _ => false);

        Assert.False(saved, "A failed save should report failure.");
        Assert.True(settings.Topmost, "A failed save should restore the original boolean value.");
        Assert.Equal(AppThemeMode.Light, settings.ThemeMode, "A failed save should restore the original theme.");
    }

    static void AppPathsIncludeLocalCrashReports()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);

        Assert.Equal(
            System.IO.Path.Combine(temp.Path, "Crashes"),
            paths.CrashesDirectory,
            "Crash reports should live beside settings and logs in the application data directory.");
    }

    static void ProductIdentitiesIsolateStableAndPreview()
    {
        var stable = CastoPetProductIdentity.Stable;
        var preview = CastoPetProductIdentity.Preview;

        Assert.Equal("CastoPet", stable.ApplicationId, "Stable should retain the public application identity.");
        Assert.Equal("CastoPet", stable.DataDirectoryName, "Stable should retain the public data directory.");
        Assert.Equal("CastoPet", stable.PackageId, "Stable should retain the public package id.");
        Assert.True(stable.UpdatesEnabled, "Stable installed builds should use the public update feed.");

        Assert.Equal("CastoPet.Preview", preview.ApplicationId, "Preview should have a distinct application identity.");
        Assert.Equal("CastoPet-Preview", preview.DataDirectoryName, "Preview should have a distinct data directory.");
        Assert.Equal("CastoPet.Preview", preview.PackageId, "Preview should have a distinct package id.");
        Assert.False(preview.UpdatesEnabled, "Preview should not consume Stable updates without a dedicated feed.");
        Assert.False(stable.InstanceName == preview.InstanceName, "Both editions should be able to run concurrently.");
        Assert.False(stable.StartupValueName == preview.StartupValueName, "Both editions should own separate startup registrations.");
    }

    static void AppPathsFollowProductIdentity()
    {
        using var temp = TempDirectory.Create();
        var stable = AppPaths.ForProduct(CastoPetProductIdentity.Stable, temp.Path);
        var preview = AppPaths.ForProduct(CastoPetProductIdentity.Preview, temp.Path);

        Assert.Equal(System.IO.Path.Combine(temp.Path, "CastoPet"), stable.DataDirectory, "Stable data should use its identity directory.");
        Assert.Equal(System.IO.Path.Combine(temp.Path, "CastoPet-Preview"), preview.DataDirectory, "Preview data should use its identity directory.");
        Assert.False(stable.SettingsFile == preview.SettingsFile, "Settings files must not be shared across editions.");
        Assert.False(stable.ShortcutsFile == preview.ShortcutsFile, "Shortcut catalogs must not be shared across editions.");
        Assert.False(stable.CrashesDirectory == preview.CrashesDirectory, "Crash reports must identify their edition by directory.");
    }

    static void PreviewDataMigrationCopiesUserConfigurationOnce()
    {
        using var temp = TempDirectory.Create();
        var legacy = AppPaths.ForProduct(CastoPetProductIdentity.Stable, temp.Path);
        var preview = AppPaths.ForProduct(CastoPetProductIdentity.Preview, temp.Path);
        Directory.CreateDirectory(legacy.ShortcutsDirectory);
        File.WriteAllText(legacy.SettingsFile, "legacy-settings");
        File.WriteAllText(legacy.SettingsBackupFile, "legacy-backup");
        File.WriteAllText(legacy.ShortcutsFile, "legacy-shortcuts");
        Directory.CreateDirectory(legacy.LogsDirectory);
        File.WriteAllText(legacy.LogFile, "legacy-log");

        var logger = new LoggingService(preview);
        Assert.True(PreviewDataMigrationService.TryMigrate(CastoPetProductIdentity.Preview, temp.Path, preview, logger), "The first Preview run should complete migration.");
        Assert.Equal("legacy-settings", File.ReadAllText(preview.SettingsFile), "Settings should be copied without changing the legacy file.");
        Assert.Equal("legacy-shortcuts", File.ReadAllText(preview.ShortcutsFile), "Shortcut configuration should be copied.");
        Assert.False(File.Exists(System.IO.Path.Combine(preview.LogsDirectory, System.IO.Path.GetFileName(legacy.LogFile))), "Logs should remain isolated instead of being migrated.");

        File.WriteAllText(preview.SettingsFile, "preview-settings");
        File.WriteAllText(legacy.SettingsFile, "changed-legacy-settings");
        Assert.True(PreviewDataMigrationService.TryMigrate(CastoPetProductIdentity.Preview, temp.Path, preview, logger), "Repeated migration checks should remain successful.");
        Assert.Equal("preview-settings", File.ReadAllText(preview.SettingsFile), "A later Stable change must not overwrite Preview settings.");
        Assert.True(File.Exists(PreviewDataMigrationService.GetMarkerPath(preview)), "A durable marker should prevent repeated imports.");
    }

    static void SettingsRoundTripIncludesCrashAndUpdateState()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var service = new SettingsService(paths, new LoggingService(paths));
        var settings = new AppSettings
        {
            LastAcknowledgedCrashId = "crash-20260711-120000-test",
            LastAutomaticUpdateCheckDate = "2026-07-11",
        };

        service.Save(settings);
        var loaded = service.Load();

        Assert.Equal(settings.LastAcknowledgedCrashId, loaded.LastAcknowledgedCrashId, "Crash acknowledgement should round trip.");
        Assert.Equal(settings.LastAutomaticUpdateCheckDate, loaded.LastAutomaticUpdateCheckDate, "Update check date should round trip.");
    }

    static void SettingsCloneIncludesCrashAndUpdateState()
    {
        var settings = new AppSettings
        {
            LastAcknowledgedCrashId = "crash-id",
            LastAutomaticUpdateCheckDate = "2026-07-11",
        };

        var clone = settings.Clone();

        Assert.Equal(settings.LastAcknowledgedCrashId, clone.LastAcknowledgedCrashId, "Clone should retain crash acknowledgement.");
        Assert.Equal(settings.LastAutomaticUpdateCheckDate, clone.LastAutomaticUpdateCheckDate, "Clone should retain update check date.");
    }

    static void SettingsCloneIncludesThemeMode()
    {
        var settings = new AppSettings { ThemeMode = AppThemeMode.Light };

        Assert.Equal(AppThemeMode.Light, settings.Clone().ThemeMode, "Clone should retain the selected theme mode.");
    }

    static void ThemeModeResolvesSystemPreference()
    {
        Assert.Equal(AppThemeMode.Light, ThemeModeResolver.Resolve(AppThemeMode.Light, systemUsesDark: true), "Explicit light mode should ignore the system theme.");
        Assert.Equal(AppThemeMode.Dark, ThemeModeResolver.Resolve(AppThemeMode.Dark, systemUsesDark: false), "Explicit dark mode should ignore the system theme.");
        Assert.Equal(AppThemeMode.Dark, ThemeModeResolver.Resolve(AppThemeMode.System, systemUsesDark: true), "System mode should resolve to dark when Windows uses dark apps.");
        Assert.Equal(AppThemeMode.Light, ThemeModeResolver.Resolve(AppThemeMode.System, systemUsesDark: false), "System mode should resolve to light when Windows uses light apps.");
    }

    static void SettingsThemePaletteDefinesLightAndDarkContrast()
    {
        var light = SettingsThemePalette.Create(AppThemeMode.Light);
        var dark = SettingsThemePalette.Create(AppThemeMode.Dark);
        var fallback = SettingsThemePalette.Create(AppThemeMode.Light, translucent: false);

        Assert.Equal(SettingsThemePalette.RequiredBrushKeys.Count, light.Count, "Light theme should define every required settings brush.");
        Assert.Equal(SettingsThemePalette.RequiredBrushKeys.Count, dark.Count, "Dark theme should define every required settings brush.");
        Assert.True(SettingsThemePalette.RequiredBrushKeys.All(light.ContainsKey), "Light theme should contain every required key.");
        Assert.True(SettingsThemePalette.RequiredBrushKeys.All(dark.ContainsKey), "Dark theme should contain every required key.");
        Assert.False(light["SurfaceBrush"].Equals(dark["SurfaceBrush"]), "Light and dark surfaces should be visibly distinct.");
        Assert.False(light["TextBrush"].Equals(dark["TextBrush"]), "Text colors should adapt to surface brightness.");
        Assert.True(light["WindowTintBrush"].A < 255 && dark["WindowTintBrush"].A < 255, "Both themes should retain translucent fallback tinting.");
        Assert.Equal((byte)255, fallback["WindowTintBrush"].A, "Unsupported systems should receive an opaque window tint.");
        Assert.Equal((byte)255, fallback["SurfaceBrush"].A, "Unsupported systems should not expose a black native window background.");
    }

    static void SettingsThemePaletteReplacesFrozenBrushes()
    {
        var resources = new System.Windows.ResourceDictionary();
        var frozen = new Dictionary<string, System.Windows.Media.SolidColorBrush>(StringComparer.Ordinal);
        foreach (var key in SettingsThemePalette.RequiredBrushKeys)
        {
            var brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            brush.Freeze();
            resources[key] = brush;
            frozen[key] = brush;
        }

        SettingsThemePalette.Apply(resources, AppThemeMode.Light);
        SettingsThemePalette.Apply(resources, AppThemeMode.Dark);
        SettingsThemePalette.Apply(resources, AppThemeMode.Light);

        var expected = SettingsThemePalette.Create(AppThemeMode.Light);
        foreach (var key in SettingsThemePalette.RequiredBrushKeys)
        {
            var current = resources[key] as System.Windows.Media.SolidColorBrush;
            Assert.True(current is not null, $"Theme resource {key} should remain a solid color brush.");
            Assert.False(ReferenceEquals(frozen[key], current), $"Theme resource {key} should replace the frozen brush instead of mutating it.");
            Assert.Equal(expected[key], current!.Color, $"Theme resource {key} should use the requested color after repeated application.");
        }
    }

    static void WindowsSystemThemeReaderHandlesAppPreference()
    {
        Assert.True(WindowsSystemThemeReader.ParseUsesDarkApps(0), "AppsUseLightTheme=0 should mean dark apps.");
        Assert.False(WindowsSystemThemeReader.ParseUsesDarkApps(1), "AppsUseLightTheme=1 should mean light apps.");
        Assert.False(WindowsSystemThemeReader.ParseUsesDarkApps(null), "Missing preference should use the safe light fallback.");
    }

    static void SettingsBackdropTargetsSupportedWindowsVersions()
    {
        Assert.False(SettingsBackdropService.IsSupported(new Version(10, 0, 22000)), "The backdrop attribute should not be used before Windows 11 22621.");
        Assert.True(SettingsBackdropService.IsSupported(new Version(10, 0, 22621)), "Windows 11 22621 should support system backdrop type.");
        Assert.True(SettingsBackdropService.IsSupported(new Version(10, 0, 26100)), "Newer Windows builds should keep backdrop support.");
        Assert.Equal(0x88776655u, SettingsBackdropService.PackAccentColor(0x88, 0x55, 0x66, 0x77), "Accent tint should use the native ABGR byte order.");
    }

    static void CrashReportsSanitizeUserPathsAndIncludeExceptionChains()
    {
        var context = new CrashReportContext(
            TimestampUtc: new DateTimeOffset(2026, 7, 11, 8, 30, 0, TimeSpan.Zero),
            AppVersion: "0.1.0",
            OperatingSystem: "Windows 11",
            ProcessArchitecture: "X64",
            UserProfilePath: @"C:\Users\lemon",
            UserName: "lemon");
        var exception = new InvalidOperationException(
            @"Failed at C:\Users\lemon\Documents\CastoPet",
            new IOException("inner failure"));

        var report = CrashReportFormatter.Format(context, exception, Array.Empty<string>());

        Assert.Contains(report, "2026-07-11T08:30:00.0000000+00:00", "Report should include the UTC timestamp.");
        Assert.Contains(report, "CastoPet version: 0.1.0", "Report should include the application version.");
        Assert.Contains(report, "InvalidOperationException", "Report should include the outer exception.");
        Assert.Contains(report, "IOException", "Report should include the inner exception.");
        Assert.Contains(report, "%USERPROFILE%", "User profile paths should use a neutral placeholder.");
        Assert.False(report.Contains("lemon", StringComparison.OrdinalIgnoreCase), "Report should not contain the Windows username.");
    }

    static void CrashReportsIncludeEditionAndSourceCommit()
    {
        var context = new CrashReportContext(
            TimestampUtc: new DateTimeOffset(2026, 8, 13, 2, 0, 0, TimeSpan.Zero),
            AppVersion: "0.2.0-preview.3",
            OperatingSystem: "Windows 11",
            ProcessArchitecture: "X64",
            UserProfilePath: @"C:\Users\TestUser",
            UserName: "TestUser",
            ProductEdition: "Preview",
            SourceCommit: "0123456789abcdef0123456789abcdef01234567",
            ReportKind: CrashReportKind.Fatal);

        var report = CrashReportFormatter.Format(context, new Exception("failure"), []);

        Assert.Contains(report, "CastoPet edition: Preview", "Crash reports should identify Stable versus Preview.");
        Assert.Contains(report, "Source commit: 0123456789abcdef0123456789abcdef01234567", "Crash reports should identify the exact source revision.");
        Assert.Contains(report, "Report kind: Fatal", "Crash reports should distinguish fatal failures from diagnostics.");
    }

    static void BuildInformationParsesSdkSourceRevisions()
    {
        var preview = CastoPetBuildInfo.Parse(
            CastoPetEdition.Preview,
            "0.2.0-preview.3+0123456789abcdef0123456789abcdef01234567",
            "0.2.0");
        var stable = CastoPetBuildInfo.Parse(CastoPetEdition.Stable, "0.1.0", "0.1.0");

        Assert.Equal("0.2.0-preview.3", preview.Version, "The semantic version should exclude build metadata.");
        Assert.Equal(CastoPetEdition.Preview, preview.Edition, "The build edition should come from the compiled feature profile.");
        Assert.Equal("0123456789abcdef0123456789abcdef01234567", preview.SourceCommit, "The SDK source revision should be preserved in full.");
        Assert.Equal("unknown", stable.SourceCommit, "Direct builds without source metadata should use an explicit fallback.");
    }

    static void CrashReportsKeepABoundedLogTail()
    {
        var context = new CrashReportContext(
            DateTimeOffset.UtcNow,
            "0.1.0",
            "Windows",
            "X64",
            @"C:\Users\TestUser",
            "TestUser");
        var lines = Enumerable.Range(0, 100).Select(index => $"log-{index:000}").ToArray();

        var report = CrashReportFormatter.Format(context, new Exception("failure"), lines);

        Assert.False(report.Contains("log-019", StringComparison.Ordinal), "Old log lines should be excluded.");
        Assert.Contains(report, "log-020", "The last 80 log lines should be included.");
        Assert.Contains(report, "log-099", "The newest log line should be included.");
    }

    static void CrashReportServiceWritesAndAcknowledgesReports()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var service = new CrashReportService(
            paths,
            new LoggingService(paths),
            buildInfo: new CastoPetBuildInfo(
                "0.2.0-preview.3",
                CastoPetEdition.Preview,
                "0123456789abcdef0123456789abcdef01234567"));

        var written = service.TryWriteReport(new InvalidOperationException("test crash"), out var report);

        Assert.True(written, "Crash report write should succeed in a writable data directory.");
        Assert.True(report is not null, "A successful write should return report metadata.");
        Assert.True(File.Exists(report!.Path), "Crash report metadata should point to the written file.");
        var content = File.ReadAllText(report.Path);
        Assert.Contains(content, "CastoPet edition: Preview", "The service should pass its compiled edition into the report.");
        Assert.Contains(content, "Source commit: 0123456789abcdef0123456789abcdef01234567", "The service should pass its source revision into the report.");
        Assert.Equal(report.Id, System.IO.Path.GetFileNameWithoutExtension(report.Path), "Report ID should match its filename.");
        Assert.Equal(report.Id, service.GetLatestUnacknowledged(null)?.Id, "An unacknowledged report should be discovered.");
        Assert.True(service.GetLatestUnacknowledged(report.Id) is null, "Acknowledged reports should not be returned again.");
    }

    static void DiagnosticReportsDoNotTriggerCrashNotifications()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var service = new CrashReportService(paths, new LoggingService(paths));

        var written = service.TryWriteReport(
            new AggregateException("unobserved task"),
            CrashReportKind.UnobservedTask,
            out var report);

        Assert.True(written, "Unobserved task diagnostics should still be persisted locally.");
        Assert.True(report is not null && report.Id.StartsWith("diagnostic-", StringComparison.Ordinal), "Non-fatal reports should use a diagnostic identity.");
        Assert.True(service.GetLatestUnacknowledged(null) is null, "A diagnostic report should not be presented as a previous application crash.");
    }

    static void CrashReportServiceContainsFileSystemFailures()
    {
        using var temp = TempDirectory.Create();
        var blockedDataPath = System.IO.Path.Combine(temp.Path, "blocked");
        File.WriteAllText(blockedDataPath, "not a directory");
        var paths = new AppPaths(blockedDataPath);
        var service = new CrashReportService(paths, new LoggingService(paths));

        var written = service.TryWriteReport(new Exception("failure"), out var report);

        Assert.False(written, "Crash report failures should be contained.");
        Assert.True(report is null, "Failed writes should not return report metadata.");
    }

    static void CrashReportServicePrunesOldReports()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var timestamp = new DateTimeOffset(2026, 7, 17, 8, 0, 0, TimeSpan.Zero);
        var nextReport = -1;
        var service = new CrashReportService(
            paths,
            new LoggingService(paths),
            maxReports: 3,
            nowProvider: () => timestamp.AddMilliseconds(Interlocked.Increment(ref nextReport)));

        for (var index = 0; index < 5; index++)
        {
            Assert.True(service.TryWriteReport(new Exception($"failure-{index}"), out _), "Crash report write should succeed.");
        }

        var reports = Directory.EnumerateFiles(paths.CrashesDirectory, "crash-*.txt").Order().ToArray();
        Assert.Equal(3, reports.Length, "Crash retention should keep only the configured number of reports.");
        Assert.False(File.ReadAllText(reports[0]).Contains("failure-0", StringComparison.Ordinal), "The oldest report should be pruned first.");
        Assert.Contains(File.ReadAllText(reports[^1]), "failure-4", "The newest report should remain available.");
    }

    static void CrashReportRetentionOrdersFatalAndDiagnosticReportsTogether()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var timestamp = new DateTimeOffset(2026, 8, 13, 3, 0, 0, TimeSpan.Zero);
        var nextReport = -1;
        var service = new CrashReportService(
            paths,
            new LoggingService(paths),
            maxReports: 2,
            nowProvider: () => timestamp.AddSeconds(Interlocked.Increment(ref nextReport)));

        Assert.True(service.TryWriteReport(new Exception("old diagnostic"), CrashReportKind.UnobservedTask, out _), "The diagnostic should be written.");
        Assert.True(service.TryWriteReport(new Exception("middle fatal"), out _), "The first fatal report should be written.");
        Assert.True(service.TryWriteReport(new Exception("new fatal"), out _), "The latest fatal report should be written.");

        var reports = Directory.EnumerateFiles(paths.CrashesDirectory, "*.txt").ToArray();
        Assert.Equal(2, reports.Length, "Retention should apply one shared chronological budget.");
        Assert.False(reports.Any(path => System.IO.Path.GetFileName(path).StartsWith("diagnostic-", StringComparison.Ordinal)), "The oldest diagnostic should be pruned before newer fatal reports.");
    }

    static void UnobservedTasksDoNotConsumeTheFatalCrashQuota()
    {
        var recordedKinds = new List<CrashReportKind>();
        var capture = new CrashCaptureCoordinator((_, kind) =>
        {
            recordedKinds.Add(kind);
            return true;
        });
        var unobserved = new UnobservedTaskExceptionEventArgs(
            new AggregateException(new InvalidOperationException("background failure")));

        capture.HandleUnobservedTaskException(unobserved);
        var firstFatal = capture.TryRecordFatal(new InvalidOperationException("fatal failure"));
        var duplicateFatal = capture.TryRecordFatal(new InvalidOperationException("duplicate fatal failure"));

        Assert.True(unobserved.Observed, "Handled task exceptions should always be marked observed.");
        Assert.True(firstFatal, "A later fatal exception should retain the one available fatal report slot.");
        Assert.False(duplicateFatal, "Only one fatal exception should be persisted during a process lifetime.");
        Assert.Equal(2, recordedKinds.Count, "One diagnostic and one fatal report should be written.");
        Assert.Equal(CrashReportKind.UnobservedTask, recordedKinds[0], "The task failure should be classified as non-fatal.");
        Assert.Equal(CrashReportKind.Fatal, recordedKinds[1], "The fatal failure should use the independent fatal gate.");
    }

    static void ApplicationRegistersAllUnhandledExceptionSources()
    {
        var workspace = FindWorkspaceRoot();
        var appSource = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "App.xaml.cs"));

        Assert.Contains(appSource, "DispatcherUnhandledException", "WPF dispatcher exceptions should be recorded.");
        Assert.Contains(appSource, "AppDomain.CurrentDomain.UnhandledException", "Non-UI fatal exceptions should be recorded.");
        Assert.Contains(appSource, "TaskScheduler.UnobservedTaskException", "Unobserved task exceptions should be recorded.");
    }

    static void ApplicationCancelsAutomaticUpdateWorkOnExit()
    {
        var workspace = FindWorkspaceRoot();
        var appSource = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "App.xaml.cs"));

        Assert.Contains(appSource, "_applicationLifetime.Cancel()", "Application exit should cancel pending background work.");
        Assert.Contains(appSource, "Task.Delay(TimeSpan.FromSeconds(10), cancellationToken)", "Startup update delay should observe application cancellation.");
        Assert.Contains(appSource, "CheckAsync(manual: false, cancellationToken)", "Automatic update checks should observe application cancellation.");
    }

    static void CrashNotificationIsLocalOnly()
    {
        var workspace = FindWorkspaceRoot();
        var xamlPath = System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "CrashNotificationWindow.xaml");
        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains(xaml, "打开日志目录", "Crash notification should provide local report access.");
        Assert.Contains(xaml, "忽略", "Crash notification should support acknowledgement.");
        Assert.False(xaml.Contains("上传", StringComparison.Ordinal), "Crash notification should not imply network upload.");
    }

    static void UpdatePolicyChecksAtMostOncePerLocalDay()
    {
        var today = new DateOnly(2026, 7, 11);

        Assert.True(UpdateCheckPolicy.ShouldCheckAutomatically(null, today), "A missing date should allow an automatic check.");
        Assert.True(UpdateCheckPolicy.ShouldCheckAutomatically("2026-07-10", today), "An older date should allow an automatic check.");
        Assert.True(UpdateCheckPolicy.ShouldCheckAutomatically("invalid", today), "An invalid date should allow recovery through a check.");
        Assert.False(UpdateCheckPolicy.ShouldCheckAutomatically("2026-07-11", today), "The same local day should not check twice.");
        Assert.Equal("2026-07-11", UpdateCheckPolicy.FormatDate(today), "Persisted dates should use ISO format.");
    }

    static void ManualUpdateChecksBypassTheDailyGate()
    {
        Assert.True(
            UpdateCheckPolicy.ShouldCheck(manual: true, "2026-07-11", new DateOnly(2026, 7, 11)),
            "Manual checks should bypass the daily gate.");
    }

    static void UpdateCoordinatorSkipsDevelopmentBuilds()
    {
        var service = new FakeUpdateService { IsInstalled = false };
        var settings = AppSettings.Default;
        var coordinator = new UpdateCoordinator(service, settings, _ => true, () => new DateOnly(2026, 7, 11));

        var result = coordinator.CheckAsync(manual: true).GetAwaiter().GetResult();

        Assert.Equal(UpdateCheckStatus.DevelopmentBuild, result.Status, "Direct builds should not invoke installed update operations.");
        Assert.Equal(0, service.CheckCount, "Development builds should not contact the update source.");
    }

    static void PreviewUpdateServiceStaysDisabled()
    {
        var service = new DisabledUpdateService("0.1.0-preview");

        Assert.False(service.IsInstalled, "A disabled update service should never present Preview as updater-managed.");
        Assert.Equal("0.1.0-preview", service.CurrentVersion, "Preview should still expose its build version.");
        Assert.True(service.CheckForUpdatesAsync(CancellationToken.None).GetAwaiter().GetResult() is null, "Disabled updates should never return a Stable release.");
    }

    static void UpdateCoordinatorRecordsAutomaticAttemptsBeforeNetwork()
    {
        var settings = AppSettings.Default;
        var savedBeforeCheck = false;
        var service = new FakeUpdateService
        {
            OnCheck = () =>
            {
                savedBeforeCheck = settings.LastAutomaticUpdateCheckDate == "2026-07-11";
                return null;
            },
        };
        var coordinator = new UpdateCoordinator(service, settings, _ => true, () => new DateOnly(2026, 7, 11));

        var result = coordinator.CheckAsync(manual: false).GetAwaiter().GetResult();

        Assert.True(savedBeforeCheck, "The daily attempt should be persisted before awaiting the network.");
        Assert.Equal(UpdateCheckStatus.Current, result.Status, "No available release should report current.");
    }

    static void UpdateCoordinatorMapsNetworkFailures()
    {
        var service = new FakeUpdateService { Exception = new HttpRequestException("offline") };
        var coordinator = new UpdateCoordinator(service, AppSettings.Default, _ => true, () => new DateOnly(2026, 7, 11));

        var result = coordinator.CheckAsync(manual: true).GetAwaiter().GetResult();

        Assert.Equal(UpdateCheckStatus.Failed, result.Status, "Network errors should map to a retryable failed status.");
    }

    static void UpdateCoordinatorLogsNetworkFailures()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var logger = new LoggingService(paths);
        var service = new FakeUpdateService { Exception = new HttpRequestException("offline-for-test") };
        var coordinator = new UpdateCoordinator(
            service,
            AppSettings.Default,
            _ => true,
            () => new DateOnly(2026, 7, 17),
            logger: logger);

        var result = coordinator.CheckAsync(manual: true).GetAwaiter().GetResult();

        Assert.Equal(UpdateCheckStatus.Failed, result.Status, "A logged network error should remain retryable.");
        var log = File.ReadAllText(paths.LogFile);
        Assert.Contains(log, "Manual update check failed", "Update logs should identify the failed operation.");
        Assert.Contains(log, "offline-for-test", "Update logs should retain the underlying exception details.");
    }

    static void UpdateCoordinatorRejectsConcurrentChecks()
    {
        var gate = new TaskCompletionSource<UpdateAvailability?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new FakeUpdateService { PendingCheck = gate.Task };
        var coordinator = new UpdateCoordinator(service, AppSettings.Default, _ => true, () => new DateOnly(2026, 7, 11));

        var first = coordinator.CheckAsync(manual: true);
        var second = coordinator.CheckAsync(manual: true).GetAwaiter().GetResult();
        gate.SetResult(null);
        first.GetAwaiter().GetResult();

        Assert.Equal(UpdateCheckStatus.Busy, second.Status, "A second in-flight check should return busy.");
        Assert.Equal(1, service.CheckCount, "Only one source request should run concurrently.");
    }

    static void ProjectPinsSemanticVersionAndVelopack()
    {
        var workspace = FindWorkspaceRoot();
        var project = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "CastoPet.csproj"));
        var sharedProperties = File.ReadAllText(System.IO.Path.Combine(workspace, "Directory.Build.props"));

        Assert.Contains(sharedProperties, "<VersionPrefix>0.1.0</VersionPrefix>", "The repository should have one explicit semantic version source.");
        Assert.False(project.Contains("<Version>", StringComparison.Ordinal), "The application project should inherit the central semantic version.");
        Assert.Contains(project, "<PackageReference Include=\"Velopack\" Version=\"1.2.0\"", "Velopack should be pinned to the verified stable version.");
    }

    static void ApplicationDefinesPackagedIcon()
    {
        var workspace = FindWorkspaceRoot();
        var projectRoot = System.IO.Path.Combine(workspace, "src", "CastoPet");
        var project = File.ReadAllText(System.IO.Path.Combine(projectRoot, "CastoPet.csproj"));
        var iconPath = System.IO.Path.Combine(projectRoot, "Assets", "AppIcon.ico");

        Assert.Contains(project, @"<ApplicationIcon>Assets\AppIcon.ico</ApplicationIcon>", "The Windows executable should embed the CastoPet icon.");
        Assert.True(File.Exists(iconPath), "The configured application icon should exist.");
        var icon = File.ReadAllBytes(iconPath);
        Assert.True(icon.Length > 6, "The application icon should contain an ICO directory.");
        Assert.True(icon[0] == 0 && icon[1] == 0 && icon[2] == 1 && icon[3] == 0, "The application icon should use the ICO signature.");
        var imageCount = icon[4] | icon[5] << 8;
        Assert.True(imageCount >= 4, "The application icon should contain multiple sizes for Windows shell surfaces.");
    }

    static void ApplicationSurfacesShareOneIcon()
    {
        var workspace = FindWorkspaceRoot();
        var projectRoot = System.IO.Path.Combine(workspace, "src", "CastoPet");
        var project = File.ReadAllText(System.IO.Path.Combine(projectRoot, "CastoPet.csproj"));
        var petWindow = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Presentation", "Windows", "PetWindow.xaml"));
        var settingsWindow = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Presentation", "Windows", "SettingsWindow.xaml"));
        var crashWindow = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Presentation", "Windows", "CrashNotificationWindow.xaml"));
        var trayService = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Infrastructure", "Platform", "TrayService.cs"));
        var iconService = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Infrastructure", "Platform", "ApplicationIconService.cs"));

        Assert.Contains(project, @"<Resource Include=""Assets\AppIcon.ico"" />", "The shared icon should be available as a WPF resource.");
        Assert.Contains(petWindow, "Icon=\"/CastoPet;component/Assets/AppIcon.ico\"", "The pet taskbar surface should use the shared icon.");
        Assert.Contains(settingsWindow, "Icon=\"/CastoPet;component/Assets/AppIcon.ico\"", "Settings should use the shared icon.");
        Assert.Contains(crashWindow, "Icon=\"/CastoPet;component/Assets/AppIcon.ico\"", "Crash notifications should use the shared icon.");
        Assert.Contains(trayService, "ApplicationIconService.LoadTrayIcon()", "The notification-area icon should use the shared icon service.");
        Assert.False(trayService.Contains("SystemIcons.Application", StringComparison.Ordinal), "The notification area should not fall back to the generic Windows application icon.");
        Assert.Contains(iconService, "/CastoPet;component/Assets/AppIcon.ico", "The tray icon service should load the icon from the CastoPet assembly.");
        using var trayIcon = ApplicationIconService.LoadTrayIcon();
        Assert.True(trayIcon.Width > 0 && trayIcon.Height > 0, "The packaged icon should decode for the notification area at runtime.");
    }

    static void TrayServiceDisposesOwnedMenuResources()
    {
        var workspace = FindWorkspaceRoot();
        var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Infrastructure", "Platform", "TrayService.cs"));

        Assert.Contains(source, "Forms.ContextMenuStrip _contextMenu", "TrayService should retain ownership of its native menu component.");
        Assert.Contains(source, "_notifyIcon.ContextMenuStrip = null;", "TrayService should detach the menu before disposing native components.");
        Assert.Contains(source, "_contextMenu.Dispose();", "TrayService should explicitly release its context menu and item handles.");
        Assert.Contains(source, "if (_disposed)", "TrayService disposal should be idempotent.");
    }

    static void SettingsWindowAvoidsDuplicateTaskbarEntry()
    {
        var workspace = FindWorkspaceRoot();
        var projectRoot = System.IO.Path.Combine(workspace, "src", "CastoPet");
        var settingsWindow = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Presentation", "Windows", "SettingsWindow.xaml"));
        var app = File.ReadAllText(System.IO.Path.Combine(projectRoot, "App.xaml.cs"));

        Assert.Contains(settingsWindow, "ShowInTaskbar=\"False\"", "Settings should remain an auxiliary window instead of creating a second taskbar button.");
        Assert.Contains(app, "Owner = _window", "Settings should be owned by the pet window for activation and lifetime behavior.");
    }

    static void ContinuousIntegrationBuildsBothConfigurations()
    {
        var workspace = FindWorkspaceRoot();
        var workflow = File.ReadAllText(System.IO.Path.Combine(workspace, ".github", "workflows", "build.yml"));

        Assert.Contains(workflow, "runs-on: windows-latest", "WPF CI should run on Windows.");
        Assert.Contains(workflow, "uses: actions/checkout@v6", "CI should use the current official checkout action.");
        Assert.Contains(workflow, "uses: actions/setup-dotnet@v5", "CI should use the current official .NET setup action.");
        Assert.Contains(workflow, "dotnet-version: 10.0.x", "CI should install the .NET 10 SDK.");
        Assert.Contains(workflow, "configuration: [Debug, Release]", "CI should cover both supported build configurations.");
        Assert.Contains(workflow, "dotnet run --project tests/CastoPet.Tests/CastoPet.Tests.csproj", "CI should execute the repository test harness.");
        Assert.Contains(workflow, "dotnet build CastoPet.sln", "CI should build the complete solution.");
        Assert.False(workflow.Contains("dotnet publish", StringComparison.OrdinalIgnoreCase), "Build CI should not publish release artifacts.");
    }

    static void ProjectSupportsStableAndPreviewResourceProfiles()
    {
        var workspace = FindWorkspaceRoot();
        var project = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "CastoPet.csproj"));
        var workflow = File.ReadAllText(System.IO.Path.Combine(workspace, ".github", "workflows", "build.yml"));

        Assert.Contains(project, "<CastoPetEdition Condition=", "The app project should define a default edition.");
        Assert.Contains(project, "CASTOPET_STABLE", "Stable builds should expose one centralized compilation symbol.");
        Assert.Contains(project, "'$(CastoPetEdition)' == 'Stable'", "Stable resources should be selected through the edition property.");
        Assert.Contains(project, @"States\Idle\*.png", "Stable builds should package idle frames.");
        Assert.Contains(project, @"States\Blink\*.png", "Stable builds should package blink frames.");
        Assert.Contains(project, @"States\Castorice.Dragging.png", "Stable builds should retain the dragging visual.");
        Assert.Contains(project, @"Assets\Runtime\Castorice\**\*.png", "Preview builds should retain the complete runtime asset set.");
        Assert.Contains(workflow, "edition: [Preview, Stable]", "CI should verify both product editions.");
        Assert.Contains(workflow, "-p:CastoPetEdition=${{ matrix.edition }}", "CI should pass the edition explicitly to tests and builds.");
    }

    static void PackagingScriptBuildsTraceableEditionSpecificInstallers()
    {
        var workspace = FindWorkspaceRoot();
        var scriptPath = System.IO.Path.Combine(workspace, "eng", "package.ps1");
        var toolManifestPath = System.IO.Path.Combine(workspace, ".config", "dotnet-tools.json");
        var ignore = File.ReadAllText(System.IO.Path.Combine(workspace, ".gitignore"));

        Assert.True(File.Exists(scriptPath), "The repository should own its packaging entry point.");
        Assert.True(File.Exists(toolManifestPath), "Packaging should use the pinned local vpk tool manifest.");
        var script = File.ReadAllText(scriptPath);
        Assert.Contains(script, "[ValidateSet(\"Stable\", \"Preview\")]", "The script should require an explicit edition.");
        Assert.Contains(script, "CastoPet.Preview", "Preview packages should use a distinct package identity.");
        Assert.Contains(script, "git status --porcelain", "Release packaging should reject uncommitted inputs by default.");
        Assert.Contains(script, "AllowDirty", "Local smoke tests should be able to opt into a clearly marked dirty build.");
        Assert.Contains(script, "dotnet", "Packaging should run through the pinned .NET SDK.");
        Assert.Contains(script, "publish", "Packaging should publish the application before invoking Velopack.");
        Assert.Contains(script, "tests/CastoPet.Tests/CastoPet.Tests.csproj", "Packaging should run the edition's Release tests before publishing.");
        Assert.Contains(script, "--self-contained", "Installer payloads should include the required .NET runtime.");
        Assert.Contains(script, "CastoPetEdition=$Edition", "Publishing should select the same Stable/Preview feature profile.");
        Assert.Contains(script, "tool", "Packaging should restore and invoke the local vpk tool.");
        Assert.Contains(script, "vpk", "Packaging should create a Velopack installer and update packages.");
        Assert.Contains(script, "build-metadata.json", "Every package should retain edition, source commit, and file hashes.");
        Assert.False(script.Contains("vpk upload", StringComparison.OrdinalIgnoreCase), "The local packaging script must not publish releases.");
        Assert.False(script.Contains("gh release", StringComparison.OrdinalIgnoreCase), "The local packaging script must not create GitHub releases.");
        Assert.Contains(ignore, "artifacts/packages/", "Generated installer payloads should remain outside source control.");
    }

    static void PackagingWorkflowProducesManualArtifactsWithoutPublishing()
    {
        var workspace = FindWorkspaceRoot();
        var workflowPath = System.IO.Path.Combine(workspace, ".github", "workflows", "package.yml");

        Assert.True(File.Exists(workflowPath), "Packaging should have a manually controlled CI workflow.");
        var workflow = File.ReadAllText(workflowPath);
        Assert.Contains(workflow, "workflow_dispatch:", "Installer generation should require an explicit manual dispatch.");
        Assert.Contains(workflow, "type: choice", "The workflow should make Stable/Preview selection explicit.");
        Assert.Contains(workflow, "eng/package.ps1", "CI and local packaging should share one implementation.");
        Assert.Contains(workflow, "actions/upload-artifact@v7", "The verified package should be exposed only as a short-lived workflow artifact.");
        Assert.Contains(workflow, "retention-days: 7", "Unsigned test packages should not be retained indefinitely.");
        Assert.False(workflow.Contains("gh release", StringComparison.OrdinalIgnoreCase), "The validation workflow must not publish a GitHub release.");
        Assert.False(workflow.Contains("vpk upload", StringComparison.OrdinalIgnoreCase), "The validation workflow must not upload to the release repository.");
    }

    static void RepositoryIgnoresLocalWorkingAssets()
    {
        var workspace = FindWorkspaceRoot();
        var gitignore = File.ReadAllText(System.IO.Path.Combine(workspace, ".gitignore"));

        Assert.Contains(gitignore, "/.codex/", "Repository-local Codex state should remain untracked.");
        Assert.Contains(gitignore, "/artwork/references/", "Reference images should remain untracked outside the source tree.");
        Assert.Contains(gitignore, "artifacts/builds/", "Repository-local build artifacts should remain untracked.");
        Assert.Contains(gitignore, "artifacts/reports/", "Stability and archived task reports should remain untracked.");
        Assert.Contains(gitignore, "artifacts/temp/", "Temporary generated output should remain untracked.");
        Assert.Contains(gitignore, "artifacts/generation/*/runs/", "Large image-generation runs should remain untracked.");
    }

    static void RepositoryKeepsAuthoringArtworkOutsideSource()
    {
        var workspace = FindWorkspaceRoot();
        var sourceAssets = System.IO.Path.Combine(workspace, "src", "CastoPet", "Assets");
        var artwork = System.IO.Path.Combine(workspace, "artwork");
        var gitignore = File.ReadAllText(System.IO.Path.Combine(workspace, ".gitignore"));

        Assert.False(Directory.Exists(System.IO.Path.Combine(sourceAssets, "CandidateSet")), "Candidate artwork should not live under the application source tree.");
        Assert.False(Directory.Exists(System.IO.Path.Combine(sourceAssets, "Skins")), "Editable skin artwork should not live under the application source tree.");
        Assert.True(Directory.Exists(System.IO.Path.Combine(artwork, "candidates", "Castorice")), "Reviewed candidate artwork should live under artwork/candidates/Castorice.");
        Assert.True(Directory.Exists(System.IO.Path.Combine(artwork, "authoring", "Castorice")), "Editable skin artwork should live under artwork/authoring/Castorice.");
        Assert.False(gitignore.Contains("/artwork/candidates/", StringComparison.Ordinal), "Reviewed candidate artwork must remain tracked by Git.");
    }

    static void ProductionCodeIsOrganizedByArchitecture()
    {
        var workspace = FindWorkspaceRoot();
        var projectRoot = System.IO.Path.Combine(workspace, "src", "CastoPet");
        var coreRoot = System.IO.Path.Combine(projectRoot, "Core");

        Assert.True(File.Exists(System.IO.Path.Combine(coreRoot, "Animation", "PetAnimationController.cs")), "Pure animation behavior should live under Core/Animation.");
        Assert.True(File.Exists(System.IO.Path.Combine(projectRoot, "Application", "Updates", "UpdateCoordinator.cs")), "Update orchestration should live under Application/Updates.");
        Assert.True(File.Exists(System.IO.Path.Combine(projectRoot, "Infrastructure", "Platform", "WindowsInputHookService.cs")), "Windows integrations should live under Infrastructure/Platform.");
        Assert.True(File.Exists(System.IO.Path.Combine(projectRoot, "Presentation", "Windows", "PetWindow.xaml")), "Pet window markup should live under Presentation/Windows.");
        Assert.True(File.Exists(System.IO.Path.Combine(projectRoot, "Presentation", "Windows", "SettingsWindow.xaml")), "Settings window markup should live under Presentation/Windows.");
        Assert.True(File.Exists(System.IO.Path.Combine(projectRoot, "Presentation", "Windows", "CrashNotificationWindow.xaml")), "Crash notification markup should live under Presentation/Windows.");
        Assert.True(File.Exists(System.IO.Path.Combine(projectRoot, "Presentation", "Styling", "RadialWheelStyle.cs")), "Radial wheel styling should live under Presentation/Styling.");
        Assert.True(File.Exists(System.IO.Path.Combine(projectRoot, "Presentation", "Styling", "SettingsThemePalette.cs")), "Settings colors should live under Presentation/Styling.");
        Assert.True(File.Exists(System.IO.Path.Combine(projectRoot, "Presentation", "Shortcuts", "ShortcutIconService.cs")), "WPF shortcut icons should live under Presentation/Shortcuts.");
        Assert.True(File.Exists(System.IO.Path.Combine(projectRoot, "Infrastructure", "Platform", "SettingsBackdropService.cs")), "Native Windows backdrop integration should live under Infrastructure/Platform.");
        Assert.False(File.Exists(System.IO.Path.Combine(projectRoot, "PetWindow.xaml")), "Window markup should not remain loose at the project root.");
        var legacyPresentationRoot = System.IO.Path.Combine(projectRoot, "Infrastructure", "Presentation");
        Assert.True(
            !Directory.Exists(legacyPresentationRoot) || !Directory.EnumerateFiles(legacyPresentationRoot, "*.cs", SearchOption.AllDirectories).Any(),
            "Infrastructure should not retain presentation-layer source files.");
        Assert.Equal(0, Directory.EnumerateFiles(coreRoot, "*.cs", SearchOption.TopDirectoryOnly).Count(), "Core should not retain ungrouped source files.");

        foreach (var layer in new[] { "Application", "Core", "Infrastructure", "Presentation" })
        {
            var layerRoot = System.IO.Path.Combine(projectRoot, layer);
            foreach (var sourcePath in Directory.EnumerateFiles(layerRoot, "*.cs", SearchOption.AllDirectories))
            {
                var relativeDirectory = System.IO.Path.GetRelativePath(projectRoot, System.IO.Path.GetDirectoryName(sourcePath)!);
                var expectedNamespace = $"namespace CastoPet.{relativeDirectory.Replace(System.IO.Path.DirectorySeparatorChar, '.')};";
                var source = File.ReadAllText(sourcePath);
                Assert.Contains(source, expectedNamespace, $"{System.IO.Path.GetRelativePath(projectRoot, sourcePath)} should match its architecture directory namespace.");
            }
        }
    }

    static void ArchitectureDependenciesPointInward()
    {
        var workspace = FindWorkspaceRoot();
        var projectRoot = System.IO.Path.Combine(workspace, "src", "CastoPet");

        AssertLayerDoesNotReference(
            System.IO.Path.Combine(projectRoot, "Core"),
            "Core",
            "System.Windows",
            "CastoPet.Application",
            "CastoPet.Infrastructure",
            "CastoPet.Presentation");
        AssertLayerDoesNotReference(
            System.IO.Path.Combine(projectRoot, "Application"),
            "Application",
            "System.Windows",
            "CastoPet.Presentation");
        AssertLayerDoesNotReference(
            System.IO.Path.Combine(projectRoot, "Application", "Settings"),
            "Application/Settings",
            "CastoPet.Infrastructure");
        AssertLayerDoesNotReference(
            System.IO.Path.Combine(projectRoot, "Application", "Updates"),
            "Application/Updates",
            "CastoPet.Infrastructure");

        var settingsContract = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Application", "Settings", "ISettingsStore.cs"));
        var settingsImplementation = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Infrastructure", "Persistence", "SettingsService.cs"));
        var updateContract = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Application", "Updates", "IUpdateService.cs"));
        var updateImplementation = File.ReadAllText(System.IO.Path.Combine(projectRoot, "Infrastructure", "Updates", "VelopackUpdateService.cs"));
        Assert.Contains(settingsContract, "interface ISettingsStore", "The settings persistence boundary should be owned by Application.");
        Assert.Contains(settingsImplementation, ": ISettingsStore", "Infrastructure should implement the settings persistence boundary.");
        Assert.Contains(updateContract, "interface IUpdateService", "The update boundary should be owned by Application.");
        Assert.Contains(updateImplementation, ": IUpdateService", "Infrastructure should implement the update boundary.");
    }

    static void AssertLayerDoesNotReference(string root, string layer, params string[] forbiddenReferences)
    {
        foreach (var sourcePath in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(sourcePath);
            foreach (var forbiddenReference in forbiddenReferences)
            {
                Assert.False(
                    source.Contains(forbiddenReference, StringComparison.Ordinal),
                    $"{layer} must not reference {forbiddenReference}: {System.IO.Path.GetRelativePath(root, sourcePath)}.");
            }
        }
    }

    static void VelopackRunsAtTheApplicationEntryPoint()
    {
        var workspace = FindWorkspaceRoot();
        var program = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Program.cs"));
        var app = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "App.xaml.cs"));

        Assert.Contains(program, "VelopackApp.Build().Run();", "Velopack hooks should run at the beginning of Main.");
        Assert.Contains(program, "static void Main", "The application should expose an explicit entry point.");
        Assert.False(app.Contains("VelopackApp.Build().Run();", StringComparison.Ordinal), "Velopack hooks should not wait until the App constructor.");
    }

    static void UpdateSourcePointsToThePublicReleasesRepository()
    {
        Assert.Equal(
            "https://github.com/sunboming/CastoPet-Releases",
            VelopackUpdateService.RepositoryUrl,
            "Installed builds should use the public releases repository without a client token.");
    }

    static void SettingsWindowExposesCrashAndUpdateActions()
    {
        var workspace = FindWorkspaceRoot();
        var xaml = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "SettingsWindow.xaml"));

        Assert.Contains(xaml, "OpenCrashReportsButton", "Settings should expose local crash reports.");
        Assert.Contains(xaml, "CheckForUpdatesButton", "Settings should expose manual update checks.");
        Assert.Contains(xaml, "UpdateStatusText", "Settings should display update status.");
        Assert.Contains(xaml, "CurrentVersionText", "Settings should display the current version.");
    }

    static void PetWindowSettingsSnapshotCopiesRuntimeFlags()
    {
        var settings = new AppSettings
        {
            Topmost = false,
            ClickThrough = true,
            ShowInTaskbar = true,
            ActiveMovement = true,
            PushCursor = true,
        };

        var snapshot = PetWindowSettingsSnapshot.FromSettings(settings);

        Assert.False(snapshot.Topmost, "Topmost should be copied for immediate window application.");
        Assert.True(snapshot.ClickThrough, "Click-through should be copied for Win32 window style application.");
        Assert.True(snapshot.ShowInTaskbar, "Taskbar visibility should be copied for window application.");
        Assert.True(snapshot.ActiveMovement, "Active movement should be copied for movement runtime state.");
        Assert.True(snapshot.PushCursor, "Push cursor should be copied for movement runtime state.");
    }

    static void PetWindowSettingsSnapshotCopiesInputReactiveMode()
    {
        var settings = new AppSettings
        {
            InputReactiveMode = true,
        };

        var snapshot = PetWindowSettingsSnapshot.FromSettings(settings);

        Assert.True(snapshot.InputReactiveMode, "Input reactive mode should be copied for window runtime state.");
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
