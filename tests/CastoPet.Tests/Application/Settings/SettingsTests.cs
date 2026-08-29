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

}
