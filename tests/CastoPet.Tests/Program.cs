using System.Drawing;
using CastoPet.Core;

var tests = new (string Name, Action Test)[]
{
    ("Default settings match MVP defaults", DefaultSettingsMatchMvpDefaults),
    ("Default active movement is disabled", DefaultActiveMovementIsDisabled),
    ("Default push cursor is disabled", DefaultPushCursorIsDisabled),
    ("Default input reactive mode is disabled", DefaultInputReactiveModeIsDisabled),
    ("Settings round trip as JSON", SettingsRoundTripAsJson),
    ("Settings round trip includes active movement", SettingsRoundTripIncludesActiveMovement),
    ("Settings round trip includes push cursor", SettingsRoundTripIncludesPushCursor),
    ("Settings round trip includes input reactive mode", SettingsRoundTripIncludesInputReactiveMode),
    ("Pet window settings snapshot copies runtime flags", PetWindowSettingsSnapshotCopiesRuntimeFlags),
    ("Pet window settings snapshot copies input reactive mode", PetWindowSettingsSnapshotCopiesInputReactiveMode),
    ("Invalid settings file falls back to defaults", InvalidSettingsFallsBackToDefaults),
    ("Logging writes a dated log file", LoggingWritesDatedLogFile),
    ("Bottom-right placement uses work area margin", BottomRightPlacementUsesWorkAreaMargin),
    ("Startup value name is CastoPet", StartupValueNameIsCastoPet),
    ("Startup registration matches current executable path", StartupRegistrationMatchesCurrentExecutablePath),
    ("Project does not keep template MainWindow", ProjectDoesNotKeepTemplateMainWindow),
    ("Single instance rejects a second owner", SingleInstanceRejectsSecondOwner),
    ("Single instance restore signal reaches primary", SingleInstanceRestoreSignalReachesPrimary),
    ("Runtime position starts at default", RuntimePositionStartsAtDefault),
    ("Runtime position tracks drag for current run only", RuntimePositionTracksDragForCurrentRunOnly),
    ("Show restore keeps hidden position but resets visible position", ShowRestoreKeepsHiddenPositionButResetsVisiblePosition),
    ("Idle frame sequence defines eight slow frame paths", IdleFrameSequenceDefinesEightSlowFramePaths),
    ("Idle frame diagnostics read all packaged frames", IdleFrameDiagnosticsReadAllPackagedFrames),
    ("Blink frame sequence defines random blink frames", BlinkFrameSequenceDefinesRandomBlinkFrames),
    ("Move frame sequence defines eight distance-driven paths", MoveFrameSequenceDefinesEightDistanceDrivenPaths),
    ("Move frame paths use app resources", MoveFramePathsUseAppResources),
    ("Move speed constants stay in smooth range", MoveSpeedConstantsStayInSmoothRange),
    ("Expression wheel defines eight items", ExpressionWheelDefinesEightItems),
    ("Expression wheel paths use app resources", ExpressionWheelPathsUseAppResources),
    ("Expression transition sequence defines shared frames", ExpressionTransitionSequenceDefinesSharedFrames),
    ("Expression transition paths use app resources", ExpressionTransitionPathsUseAppResources),
    ("Expression wheel style is text only with dividers", ExpressionWheelStyleIsTextOnlyWithDividers),
    ("Expression wheel selector maps pointer positions", ExpressionWheelSelectorMapsPointerPositions),
    ("Tray menu exposes active movement text", TrayMenuExposesActiveMovementText),
    ("Tray menu exposes push cursor text", TrayMenuExposesPushCursorText),
    ("Tray menu exposes input reactive mode text", TrayMenuExposesInputReactiveModeText),
    ("Movement planner clamps targets to work area", MovementPlannerClampsTargetsToWorkArea),
    ("Movement planner approaches mouse with cursor offset", MovementPlannerApproachesMouseWithCursorOffset),
    ("Movement planner eases toward target", MovementPlannerEasesTowardTarget),
    ("Movement planner detects close targets", MovementPlannerDetectsCloseTargets),
    ("Movement planner detects mouse approach rest position", MovementPlannerDetectsMouseApproachRestPosition),
    ("Cursor nudge planner nudges nearby cursor", CursorNudgePlannerNudgesNearbyCursor),
    ("Cursor nudge planner ignores distant cursor", CursorNudgePlannerIgnoresDistantCursor),
    ("Cursor nudge planner clamps to work area", CursorNudgePlannerClampsToWorkArea),
    ("Cursor nudge planner detects manual movement cooldown", CursorNudgePlannerDetectsManualMovementCooldown),
    ("Cursor nudge planner blocks while mouse button is pressed", CursorNudgePlannerBlocksWhileMouseButtonIsPressed),
    ("Cursor nudge planner limits continuous push duration", CursorNudgePlannerLimitsContinuousPushDuration),
    ("Pet animation timings are responsive", PetAnimationTimingsAreResponsive),
    ("Idle breathing values are neutral during stabilization", IdleBreathingValuesAreNeutralDuringStabilization),
    ("Character stationary animations are enabled", CharacterStationaryAnimationsAreEnabled),
    ("Character assets decode at pet display width", CharacterAssetsDecodeAtPetDisplayWidth),
    ("Asset diagnostics include group and resource path", AssetDiagnosticsIncludeGroupAndResourcePath),
    ("Packaged character assets are display sized", PackagedCharacterAssetsAreDisplaySized),
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

static void IdleFrameSequenceDefinesEightSlowFramePaths()
{
    Assert.Equal(8, IdleFrameSequence.FrameCount, "Idle should use eight frames.");
    Assert.Equal(TimeSpan.FromMilliseconds(200), IdleFrameSequence.FrameInterval, "Idle frames should advance slowly.");
    Assert.Equal("Assets/States/Idle/Castorice.Idle.00.png", IdleFrameSequence.FramePaths[0], "First idle frame path should be zero padded.");
    Assert.Equal("Assets/States/Idle/Castorice.Idle.07.png", IdleFrameSequence.FramePaths[^1], "Last idle frame path should be zero padded.");
}

static void IdleFrameDiagnosticsReadAllPackagedFrames()
{
    var diagnostics = ReadIdleFrameDiagnostics();

    Assert.Equal(IdleFrameSequence.FrameCount, diagnostics.Count, "Diagnostics should include all idle frames.");
    Assert.True(diagnostics.All(frame => frame.Width == AssetService.CharacterDecodePixelWidth), "Idle frames should keep the display width.");
    Assert.True(diagnostics.All(frame => frame.Height == AssetService.CharacterDecodePixelWidth), "Idle frames should keep the display height.");
    Assert.True(diagnostics.All(frame => frame.Bounds.Width > 0 && frame.Bounds.Height > 0), "Idle frames should have visible alpha bounds.");
    Assert.True(diagnostics.Max(frame => frame.Bounds.Bottom) - diagnostics.Min(frame => frame.Bounds.Bottom) <= 1, "Idle frame bottom edges should stay anchored.");
    Assert.True(diagnostics.Max(frame => frame.CenterX) - diagnostics.Min(frame => frame.CenterX) <= 1.0, "Idle frame centers should stay horizontally anchored.");
    Assert.Equal("Castorice.Idle.07.png", diagnostics[^1].Name, "Diagnostics should preserve frame order.");
}

static void BlinkFrameSequenceDefinesRandomBlinkFrames()
{
    Assert.Equal(3, BlinkFrameSequence.FrameCount, "Blink should use three frames.");
    Assert.Equal(TimeSpan.FromMilliseconds(90), BlinkFrameSequence.FrameInterval, "Blink frames should advance quickly.");
    Assert.Equal(TimeSpan.FromSeconds(3), BlinkFrameSequence.MinScheduleDelay, "Blink should not repeat too frequently.");
    Assert.Equal(TimeSpan.FromSeconds(7), BlinkFrameSequence.MaxScheduleDelay, "Blink should remain occasional.");
    Assert.Equal("Assets/States/Blink/Castorice.Blink.00.png", BlinkFrameSequence.FramePaths[0], "First blink frame path should be zero padded.");
    Assert.Equal("Assets/States/Blink/Castorice.Blink.02.png", BlinkFrameSequence.FramePaths[^1], "Last blink frame path should be zero padded.");
}

static void MoveFrameSequenceDefinesEightDistanceDrivenPaths()
{
    Assert.Equal(8, MoveFrameSequence.FrameCount, "Move should use eight frames.");
    Assert.Equal(10d, MoveFrameSequence.DistancePerFrame, "Move frames should advance by travel distance.");
    Assert.Equal("Assets/States/Move/Castorice.Move.00.png", MoveFrameSequence.FramePaths[0], "First move frame path should be zero padded.");
    Assert.Equal("Assets/States/Move/Castorice.Move.07.png", MoveFrameSequence.FramePaths[^1], "Last move frame path should be zero padded.");
}

static void MoveFramePathsUseAppResources()
{
    for (var index = 0; index < MoveFrameSequence.FrameCount; index++)
    {
        Assert.Equal($"Assets/States/Move/Castorice.Move.{index:00}.png", MoveFrameSequence.FramePaths[index], "Move frame should use the resource path convention.");
    }
}

static void MoveSpeedConstantsStayInSmoothRange()
{
    Assert.Equal(90d, MoveFrameSequence.BaseSpeedPixelsPerSecond, "Move speed should have a stable base.");
    Assert.Equal(80d, MoveFrameSequence.MinSpeedPixelsPerSecond, "Move speed lower bound should stay near the base.");
    Assert.Equal(105d, MoveFrameSequence.MaxSpeedPixelsPerSecond, "Move speed upper bound should stay near the base.");
    Assert.Equal(9d, MoveFrameSequence.StepDistance(TimeSpan.FromMilliseconds(100), distanceToTarget: 200), "100ms at base speed should move 9px.");
}

static void ExpressionWheelDefinesEightItems()
{
    Assert.Equal(8, ExpressionWheelCatalog.Items.Count, "Expression wheel should use eight first-version items.");
    Assert.Equal("Happy", ExpressionWheelCatalog.Items[0].Label, "First expression should be Happy.");
    Assert.Equal("Shy", ExpressionWheelCatalog.Items[1].Label, "Second expression should be Shy.");
    Assert.Equal("Sleepy", ExpressionWheelCatalog.Items[2].Label, "Third expression should be Sleepy.");
    Assert.Equal("Surprised", ExpressionWheelCatalog.Items[3].Label, "Fourth expression should be Surprised.");
    Assert.Equal("Pouting", ExpressionWheelCatalog.Items[4].Label, "Fifth expression should be Pouting.");
    Assert.Equal("Confused", ExpressionWheelCatalog.Items[5].Label, "Sixth expression should be Confused.");
    Assert.Equal("Proud", ExpressionWheelCatalog.Items[6].Label, "Seventh expression should be Proud.");
    Assert.Equal("Crying", ExpressionWheelCatalog.Items[7].Label, "Eighth expression should be Crying.");
    Assert.Equal(TimeSpan.FromMilliseconds(250), ExpressionWheelCatalog.HoldDelay, "Wheel hold delay should be short but deliberate.");
    Assert.Equal(TimeSpan.FromSeconds(2), ExpressionWheelCatalog.ExpressionDuration, "Selected expression should be temporary.");
}

static void ExpressionWheelPathsUseAppResources()
{
    foreach (var item in ExpressionWheelCatalog.Items)
    {
        var expected = $"Assets/Expressions/Castorice.Expression.{item.Label}.png";
        Assert.Equal(expected, item.ResourcePath, $"{item.Label} should use the expression resource path convention.");
    }
}

static void ExpressionTransitionSequenceDefinesSharedFrames()
{
    Assert.Equal(4, ExpressionTransitionSequence.InFrameCount, "Transition-in should use four shared frames for smoother expression changes.");
    Assert.Equal(4, ExpressionTransitionSequence.OutFrameCount, "Transition-out should use four shared frames for smoother expression changes.");
    Assert.Equal(TimeSpan.FromMilliseconds(55), ExpressionTransitionSequence.FrameInterval, "More transition frames should stay brief overall.");
    Assert.Equal(ExpressionTransitionSequence.InFrameCount, ExpressionTransitionSequence.InFramePaths.Count, "Transition-in paths should match frame count.");
    Assert.Equal(ExpressionTransitionSequence.OutFrameCount, ExpressionTransitionSequence.OutFramePaths.Count, "Transition-out paths should match frame count.");
}

static void ExpressionTransitionPathsUseAppResources()
{
    Assert.Equal("Assets/Expressions/Transition/Castorice.ExpressionTransition.In.00.png", ExpressionTransitionSequence.InFramePaths[0], "First transition-in path should use the transition resource convention.");
    Assert.Equal("Assets/Expressions/Transition/Castorice.ExpressionTransition.In.03.png", ExpressionTransitionSequence.InFramePaths[^1], "Last transition-in path should use the transition resource convention.");
    Assert.Equal("Assets/Expressions/Transition/Castorice.ExpressionTransition.Out.00.png", ExpressionTransitionSequence.OutFramePaths[0], "First transition-out path should use the transition resource convention.");
    Assert.Equal("Assets/Expressions/Transition/Castorice.ExpressionTransition.Out.03.png", ExpressionTransitionSequence.OutFramePaths[^1], "Last transition-out path should use the transition resource convention.");
}

static void ExpressionWheelStyleIsTextOnlyWithDividers()
{
    Assert.False(ExpressionWheelCatalog.UsesPreviewImages, "Wheel items should use text labels instead of in-wheel expression previews.");
    Assert.Equal(ExpressionWheelCatalog.ItemCount, ExpressionWheelCatalog.DividerCount, "Wheel should draw one divider per item boundary.");
    Assert.Equal(280d, ExpressionWheelCatalog.WheelDiameter, "Wheel surface should keep the first-version compact size.");
    Assert.Equal(256d, ExpressionWheelCatalog.WheelOuterDiameter, "Outer background should fit inside the wheel surface.");
    Assert.Equal(84d, ExpressionWheelCatalog.WheelInnerDiameter, "Inner no-selection zone should remain visible.");
    Assert.Equal(1.18d, ExpressionWheelCatalog.SelectedScale, "Selected wheel item should still scale up visibly.");
}

static void ExpressionWheelSelectorMapsPointerPositions()
{
    Assert.Equal(
        null,
        ExpressionWheelSelector.GetSelectedIndex(
            pointerX: 0,
            pointerY: 0,
            originX: 0,
            originY: 0,
            itemCount: 8),
        "Pointer inside the inner radius should not select an item.");
    Assert.Equal(
        null,
        ExpressionWheelSelector.GetSelectedIndex(
            pointerX: ExpressionWheelCatalog.OuterRadius + 20,
            pointerY: 0,
            originX: 0,
            originY: 0,
            itemCount: 8),
        "Pointer outside the outer radius should not select an item.");
    Assert.Equal(
        0,
        ExpressionWheelSelector.GetSelectedIndex(
            pointerX: 0,
            pointerY: -ExpressionWheelCatalog.InnerRadius - 10,
            originX: 0,
            originY: 0,
            itemCount: 8),
        "Pointer above the origin should select the top item.");
    Assert.Equal(
        2,
        ExpressionWheelSelector.GetSelectedIndex(
            pointerX: ExpressionWheelCatalog.InnerRadius + 10,
            pointerY: 0,
            originX: 0,
            originY: 0,
            itemCount: 8),
        "Pointer right of the origin should select the right item.");
    Assert.Equal(
        4,
        ExpressionWheelSelector.GetSelectedIndex(
            pointerX: 0,
            pointerY: ExpressionWheelCatalog.InnerRadius + 10,
            originX: 0,
            originY: 0,
            itemCount: 8),
        "Pointer below the origin should select the bottom item.");
}

static void TrayMenuExposesActiveMovementText()
{
    Assert.Equal("主动移动", TrayService.ActiveMovementText, "Active movement menu text should be localized.");
}

static void TrayMenuExposesPushCursorText()
{
    Assert.Equal("推动鼠标", TrayService.PushCursorText, "Push cursor menu text should be localized.");
}

static void TrayMenuExposesInputReactiveModeText()
{
    Assert.Equal("输入响应模式", TrayService.InputReactiveModeText, "Input reactive menu text should be localized.");
}

static void MovementPlannerClampsTargetsToWorkArea()
{
    var bounds = new PetMovementBounds(0, 0, 500, 400);

    var target = PetMovementPlanner.ClampTarget(
        left: 460,
        top: 390,
        windowWidth: 100,
        windowHeight: 120,
        bounds);

    Assert.Equal(400d, target.Left, "Target left should keep the full pet inside the work area.");
    Assert.Equal(280d, target.Top, "Target top should keep the full pet inside the work area.");
}

static void MovementPlannerApproachesMouseWithCursorOffset()
{
    var bounds = new PetMovementBounds(0, 0, 800, 600);

    var target = PetMovementPlanner.CalculateMouseApproachTarget(
        petLeft: 100,
        petTop: 100,
        petWidth: 100,
        petHeight: 100,
        mouseX: 300,
        mouseY: 150,
        bounds);

    var targetCenterX = target.Left + 50;
    var targetCenterY = target.Top + 50;
    var distance = Math.Sqrt(Math.Pow(targetCenterX - 300, 2) + Math.Pow(targetCenterY - 150, 2));

    Assert.True(distance >= PetMovementPlanner.MinMouseApproachOffset, "Target should not cover the cursor.");
    Assert.True(distance <= PetMovementPlanner.MaxMouseApproachOffset, "Target should stop close to the cursor.");
    Assert.True(target.Left > 100, "Target should move toward the mouse.");
}

static void MovementPlannerEasesTowardTarget()
{
    var next = PetMovementPlanner.StepToward(
        currentLeft: 0,
        currentTop: 0,
        target: new PetMovementTarget(100, 50));

    Assert.True(next.Left > 0, "Next left should move forward.");
    Assert.True(next.Left < 100, "Next left should ease instead of jumping.");
    Assert.True(next.Top > 0, "Next top should move forward.");
    Assert.True(next.Top < 50, "Next top should ease instead of jumping.");
}

static void MovementPlannerDetectsCloseTargets()
{
    var target = new PetMovementTarget(12, 16);

    Assert.True(PetMovementPlanner.IsClose(10, 14, target), "Nearby coordinates should be close.");
    Assert.False(PetMovementPlanner.IsClose(0, 0, target), "Distant coordinates should not be close.");
}

static void MovementPlannerDetectsMouseApproachRestPosition()
{
    var bounds = new PetMovementBounds(0, 0, 800, 600);
    var target = PetMovementPlanner.CalculateMouseApproachTarget(
        petLeft: 100,
        petTop: 100,
        petWidth: 100,
        petHeight: 100,
        mouseX: 300,
        mouseY: 150,
        bounds);

    Assert.True(
        PetMovementPlanner.IsAtMouseApproachTarget(
            target.Left + 1,
            target.Top + 1,
            petWidth: 100,
            petHeight: 100,
            mouseX: 300,
            mouseY: 150,
            bounds),
        "Pet should be treated as stationary when already near the mouse approach target.");
    Assert.False(
        PetMovementPlanner.IsAtMouseApproachTarget(
            petLeft: 100,
            petTop: 100,
            petWidth: 100,
            petHeight: 100,
            mouseX: 300,
            mouseY: 150,
            bounds),
        "Pet should still move when away from the mouse approach target.");
}

static void CursorNudgePlannerNudgesNearbyCursor()
{
    var bounds = new PetMovementBounds(0, 0, 500, 400);
    var result = CursorNudgePlanner.CalculateNudge(
        cursorX: 120,
        cursorY: 120,
        petCenterX: 130,
        petCenterY: 120,
        movementDeltaX: 10,
        movementDeltaY: 0,
        bounds);

    Assert.True(result.ShouldMove, "Nearby cursor should be nudged.");
    Assert.Equal(123d, result.X, "Nudge should clamp to the per-frame maximum.");
    Assert.Equal(120d, result.Y, "Horizontal movement should not change Y.");
}

static void CursorNudgePlannerIgnoresDistantCursor()
{
    var bounds = new PetMovementBounds(0, 0, 500, 400);
    var result = CursorNudgePlanner.CalculateNudge(
        cursorX: 20,
        cursorY: 20,
        petCenterX: 200,
        petCenterY: 200,
        movementDeltaX: 10,
        movementDeltaY: 0,
        bounds);

    Assert.False(result.ShouldMove, "Distant cursor should not be nudged.");
}

static void CursorNudgePlannerClampsToWorkArea()
{
    var bounds = new PetMovementBounds(0, 0, 100, 100);
    var result = CursorNudgePlanner.CalculateNudge(
        cursorX: 99,
        cursorY: 99,
        petCenterX: 98,
        petCenterY: 98,
        movementDeltaX: 10,
        movementDeltaY: 10,
        bounds);

    Assert.True(result.ShouldMove, "Nearby cursor should still be nudged at the edge.");
    Assert.Equal(99d, result.X, "Cursor should stay inside the work area.");
    Assert.Equal(99d, result.Y, "Cursor should stay inside the work area.");
}

static void CursorNudgePlannerDetectsManualMovementCooldown()
{
    Assert.True(
        CursorNudgePlanner.IsManualMovement(
            currentX: 140,
            currentY: 100,
            expectedX: 100,
            expectedY: 100),
        "Large unexpected cursor movement should count as manual input.");
    Assert.False(
        CursorNudgePlanner.CanNudgeAfterManualMovement(
            now: TimeSpan.FromMilliseconds(500),
            lastManualMovement: TimeSpan.Zero),
        "Push should pause during the manual movement cooldown.");
    Assert.True(
        CursorNudgePlanner.CanNudgeAfterManualMovement(
            now: TimeSpan.FromMilliseconds(1200),
            lastManualMovement: TimeSpan.Zero),
        "Push should resume after the manual movement cooldown.");
}

static void CursorNudgePlannerBlocksWhileMouseButtonIsPressed()
{
    Assert.False(
        CursorNudgePlanner.CanNudge(
            isMouseButtonPressed: true,
            now: TimeSpan.FromSeconds(2),
            lastManualMovement: null,
            pushStartedAt: TimeSpan.FromSeconds(1)),
        "Cursor push should stop while any mouse button is pressed.");
}

static void CursorNudgePlannerLimitsContinuousPushDuration()
{
    Assert.True(
        CursorNudgePlanner.CanNudge(
            isMouseButtonPressed: false,
            now: TimeSpan.FromMilliseconds(500),
            lastManualMovement: null,
            pushStartedAt: TimeSpan.Zero),
        "Cursor push should be allowed before the continuous duration cap.");
    Assert.False(
        CursorNudgePlanner.CanNudge(
            isMouseButtonPressed: false,
            now: TimeSpan.FromMilliseconds(2500),
            lastManualMovement: null,
            pushStartedAt: TimeSpan.Zero),
        "Cursor push should stop after the continuous duration cap.");
}

static void PetAnimationTimingsAreResponsive()
{
    Assert.Equal(TimeSpan.FromMilliseconds(120), PetAnimationTimings.ExpressionEnterDuration, "Expression enter should be quick.");
    Assert.Equal(TimeSpan.FromMilliseconds(180), PetAnimationTimings.ExpressionExitDuration, "Expression exit should be smooth but short.");
    Assert.Equal(TimeSpan.FromMilliseconds(120), PetAnimationTimings.WheelOpenDuration, "Wheel open should feel immediate.");
    Assert.Equal(TimeSpan.FromMilliseconds(90), PetAnimationTimings.WheelSelectionDuration, "Selection emphasis should respond quickly.");
    Assert.Equal(TimeSpan.FromMilliseconds(250), PetAnimationTimings.ActiveMovementProbeInterval, "Active movement should use a low-frequency stationary probe.");
}

static void IdleBreathingValuesAreNeutralDuringStabilization()
{
    Assert.Equal(TimeSpan.FromMilliseconds(1900), PetAnimationTimings.IdleBreathingCycleDuration, "Idle breathing cycle duration should stay available for later tuning.");
    Assert.Equal(0d, PetAnimationTimings.IdleBreathingTranslateY, "Idle breathing vertical movement should be disabled while stabilizing frame anchors.");
    Assert.Equal(0d, PetAnimationTimings.IdleBreathingScaleDelta, "Idle breathing scale should be disabled while stabilizing frame anchors.");
    Assert.Equal(0.96, PetAnimationTimings.ExpressionDimmedOpacity, "Expression transition should only slightly dim during swaps.");
    Assert.Equal(0.92, PetAnimationTimings.WheelOpenStartScale, "Wheel should open from a small scale change.");
}

static void CharacterStationaryAnimationsAreEnabled()
{
    Assert.True(PetAnimationTimings.CharacterFrameAnimationEnabled, "Idle frame animation should be restored only while stationary.");
    Assert.True(PetAnimationTimings.BlinkFrameAnimationEnabled, "Blink should be restored while stationary.");
    Assert.True(PetAnimationTimings.ActiveMovementScaleDelta > 0, "Active movement should use a subtle visual state after static movement validated window smoothness.");
    Assert.True(PetAnimationTimings.ActiveMovementScaleDelta <= 0.006, "Active movement scale should stay subtle.");
    Assert.True(PetAnimationTimings.DragMovementScaleDelta > PetAnimationTimings.ActiveMovementScaleDelta, "Dragging should use a slightly stronger visual state than automatic movement.");
    Assert.True(PetAnimationTimings.DragMovementScaleDelta <= 0.012, "Dragging scale should stay subtle.");
}

static void CharacterAssetsDecodeAtPetDisplayWidth()
{
    Assert.Equal(320, AssetService.CharacterDecodePixelWidth, "Character assets should decode near their display width to avoid full-size frame memory.");
}

static void AssetDiagnosticsIncludeGroupAndResourcePath()
{
    var message = AssetService.FormatLoadFailureMessage("Idle frames", "Assets/States/Idle/Castorice.Idle.03.png");

    Assert.Contains(message, "Idle frames", "Asset diagnostics should include the resource group.");
    Assert.Contains(message, "Assets/States/Idle/Castorice.Idle.03.png", "Asset diagnostics should include the resource path.");
}

static void PackagedCharacterAssetsAreDisplaySized()
{
    var workspace = FindWorkspaceRoot();
    var assetsRoot = System.IO.Path.Combine(workspace, "src", "CastoPet", "Assets");
    var excludedSegments = new[]
    {
        $"{System.IO.Path.DirectorySeparatorChar}CandidateSet{System.IO.Path.DirectorySeparatorChar}",
    };
    var assets = Directory
        .EnumerateFiles(assetsRoot, "*.png", SearchOption.AllDirectories)
        .Where(path => !System.IO.Path.GetFileName(path).Equals("blink-preview.png", StringComparison.OrdinalIgnoreCase))
        .Where(path => !excludedSegments.Any(segment => path.Contains(segment, StringComparison.OrdinalIgnoreCase)));

    foreach (var asset in assets)
    {
        var (width, height) = ReadPngSize(asset);

        Assert.True(
            width <= AssetService.CharacterDecodePixelWidth && height <= AssetService.CharacterDecodePixelWidth,
            $"{asset} should be no larger than {AssetService.CharacterDecodePixelWidth}px, got {width}x{height}.");
    }
}

static IReadOnlyList<IdleFrameDiagnostic> ReadIdleFrameDiagnostics()
{
    var workspace = FindWorkspaceRoot();
    var idleRoot = System.IO.Path.Combine(workspace, "src", "CastoPet", "Assets", "States", "Idle");
    var frames = Directory
        .EnumerateFiles(idleRoot, "Castorice.Idle.*.png", SearchOption.TopDirectoryOnly)
        .OrderBy(System.IO.Path.GetFileName, StringComparer.Ordinal)
        .ToArray();

    var diagnostics = new List<IdleFrameDiagnostic>();
    for (var index = 0; index < frames.Length; index++)
    {
        using var bitmap = new Bitmap(frames[index]);
        var bounds = FindVisibleBounds(bitmap);
        diagnostics.Add(new IdleFrameDiagnostic(
            Name: System.IO.Path.GetFileName(frames[index]),
            Width: bitmap.Width,
            Height: bitmap.Height,
            Bounds: bounds,
            CenterX: bounds.Left + bounds.Width / 2d,
            AdjacentAverageDelta: 0));
    }

    for (var index = 0; index < diagnostics.Count; index++)
    {
        var current = frames[index];
        var next = frames[(index + 1) % frames.Length];
        using var currentBitmap = new Bitmap(current);
        using var nextBitmap = new Bitmap(next);
        diagnostics[index] = diagnostics[index] with
        {
            AdjacentAverageDelta = CalculateAverageRgbaDelta(currentBitmap, nextBitmap),
        };
    }

    return diagnostics;
}

static Rectangle FindVisibleBounds(Bitmap bitmap)
{
    var minX = bitmap.Width;
    var minY = bitmap.Height;
    var maxX = -1;
    var maxY = -1;

    for (var y = 0; y < bitmap.Height; y++)
    {
        for (var x = 0; x < bitmap.Width; x++)
        {
            if (bitmap.GetPixel(x, y).A <= 8)
            {
                continue;
            }

            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
        }
    }

    if (maxX < minX || maxY < minY)
    {
        return Rectangle.Empty;
    }

    return Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
}

static double CalculateAverageRgbaDelta(Bitmap current, Bitmap next)
{
    if (current.Width != next.Width || current.Height != next.Height)
    {
        throw new InvalidOperationException("Idle frames must have matching dimensions.");
    }

    long total = 0;
    long samples = 0;
    for (var y = 0; y < current.Height; y += 2)
    {
        for (var x = 0; x < current.Width; x += 2)
        {
            var a = current.GetPixel(x, y);
            var b = next.GetPixel(x, y);
            total += Math.Abs(a.R - b.R);
            total += Math.Abs(a.G - b.G);
            total += Math.Abs(a.B - b.B);
            total += Math.Abs(a.A - b.A);
            samples++;
        }
    }

    return total / (samples * 4d);
}

static string FindWorkspaceRoot()
{
    var current = new DirectoryInfo(Environment.CurrentDirectory);
    while (current is not null)
    {
        if (File.Exists(System.IO.Path.Combine(current.FullName, "CastoPet.sln")))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    throw new InvalidOperationException("Could not find workspace root.");
}

static (int Width, int Height) ReadPngSize(string path)
{
    Span<byte> header = stackalloc byte[24];
    using var stream = File.OpenRead(path);
    if (stream.Read(header) != header.Length)
    {
        throw new InvalidOperationException($"{path} is not a valid PNG.");
    }

    var width = ReadBigEndianInt32(header[16..20]);
    var height = ReadBigEndianInt32(header[20..24]);
    return (width, height);
}

static int ReadBigEndianInt32(ReadOnlySpan<byte> bytes)
{
    return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
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

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message} Expected {expected}, got {actual}.");
        }
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

readonly record struct IdleFrameDiagnostic(
    string Name,
    int Width,
    int Height,
    Rectangle Bounds,
    double CenterX,
    double AdjacentAverageDelta);
