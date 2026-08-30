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
}
