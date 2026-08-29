namespace CastoPet.Tests;

internal static partial class TestSuite
{
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

    static void MovementControllerAdvancesLogicalPositions()
    {
        var controller = new PetMovementController(CreateTestMovementSettings(), new Random(7));
        controller.BeginRendering(left: 0, top: 0);
        controller.SetTarget(new PetMovementTarget(100, 0));

        var initial = controller.Advance(TimeSpan.Zero, currentLeft: 0, currentTop: 0);
        var moved = controller.Advance(TimeSpan.FromSeconds(1), currentLeft: 0, currentTop: 0);

        Assert.True(initial is null, "The first rendering sample should establish timing without moving.");
        Assert.True(moved is not null, "A later rendering sample should advance toward the target.");
        Assert.Equal(90d, moved!.Value.NextLeft, "The configured base speed should determine logical movement.");
        Assert.Equal(0d, moved.Value.NextTop, "Horizontal movement should preserve the vertical coordinate.");
        Assert.Equal(90d, moved.Value.Distance, "Movement output should report the traveled distance.");
    }

    static void MovementControllerResumesWithoutAccumulatingRenderPause()
    {
        var controller = new PetMovementController(CreateTestMovementSettings(), new Random(7));
        controller.BeginRendering(left: 0, top: 0);
        controller.SetTarget(new PetMovementTarget(100, 0));

        controller.Advance(TimeSpan.Zero, currentLeft: 0, currentTop: 0);
        var beforePause = controller.Advance(TimeSpan.FromMilliseconds(100), currentLeft: 0, currentTop: 0);
        Assert.True(beforePause is not null, "Movement should begin before the render pause.");

        controller.StopRendering();
        controller.BeginRendering(beforePause!.Value.NextLeft, beforePause.Value.NextTop);
        var resumed = controller.Advance(
            TimeSpan.FromMilliseconds(600),
            beforePause.Value.NextLeft,
            beforePause.Value.NextTop);
        var nextFrame = controller.Advance(
            TimeSpan.FromMilliseconds(700),
            beforePause.Value.NextLeft,
            beforePause.Value.NextTop);

        Assert.True(resumed is null, "The first sample after a render pause should only synchronize rendering time.");
        Assert.True(nextFrame is not null, "Movement should continue on the next rendering sample.");
        Assert.Equal(18d, nextFrame!.Value.NextLeft, "Only post-resume frame time should contribute to movement.");
        Assert.Equal(9d, nextFrame.Value.Distance, "The render pause must not be converted into a large catch-up step.");
    }

    static void MovementControllerSchedulesBoundedWanderTargets()
    {
        var controller = new PetMovementController(CreateTestMovementSettings(), new Random(11));
        var bounds = new PetMovementBounds(0, 0, 500, 400);
        var now = new DateTime(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);

        Assert.True(controller.TryChooseWanderTarget(now, 450, 350, 100, 100, bounds), "A due controller should choose a wander target.");
        Assert.True(controller.Target.Left >= 0 && controller.Target.Left <= 400, "Wander target should keep the pet inside horizontal bounds.");
        Assert.True(controller.Target.Top >= 0 && controller.Target.Top <= 300, "Wander target should keep the pet inside vertical bounds.");

        controller.CompleteTarget(now);
        Assert.False(controller.IsWanderDue(now), "Completing a target should schedule a rest interval.");
        Assert.True(controller.IsWanderDue(now.AddSeconds(3)), "The next wander should become due after the maximum rest interval.");
    }

    static void MovementControllerAdvancesFramesByDistance()
    {
        var controller = new PetMovementController(CreateTestMovementSettings(), new Random(3));

        Assert.Equal(new PetMoveFrameAdvance(2, true), controller.AdvanceMoveFrame(distance: 25, frameCount: 8), "Twenty-five pixels should cross two ten-pixel frames.");
        Assert.Equal(new PetMoveFrameAdvance(3, true), controller.AdvanceMoveFrame(distance: 5, frameCount: 8), "The carried distance should advance the next frame.");
        controller.ResetMoveFrames();
        Assert.Equal(0, controller.MoveFrameIndex, "Reset should restore movement frame zero.");
    }

    static void MovementDefinitionSelectsDirectionalClips()
    {
        var movement = new PetMovementDefinition(new PetMovementSettings(), ["left.png"], ["right0.png", "right1.png"]);
        Assert.Equal("left.png", movement.GetDirectionalFramePaths(PetHorizontalDirection.Left)[0], "Left should use the authored left clip.");
        Assert.Equal(2, movement.GetDirectionalFramePaths(PetHorizontalDirection.Right).Count, "Directional clips may have different frame counts.");
        Assert.Throws<ArgumentOutOfRangeException>(() => movement.GetDirectionalFramePaths((PetHorizontalDirection)0));
    }

    static void MovementControllerUsesSharedSpeedInBothDirections()
    {
        var left = new PetMovementController(CreateTestMovementSettings());
        var right = new PetMovementController(CreateTestMovementSettings());
        left.BeginRendering(0, 0);
        right.BeginRendering(0, 0);
        left.SetTarget(new PetMovementTarget(-100, 0));
        right.SetTarget(new PetMovementTarget(100, 0));
        left.Advance(TimeSpan.Zero, 0, 0);
        right.Advance(TimeSpan.Zero, 0, 0);
        var l = left.Advance(TimeSpan.FromMilliseconds(100), 0, 0)!.Value;
        var r = right.Advance(TimeSpan.FromMilliseconds(100), 0, 0)!.Value;
        Assert.Equal(-r.NextLeft, l.NextLeft, "Opposite directions must have the same speed.");
        Assert.Equal(r.Distance, l.Distance, "Distance-driven playback must be symmetric.");
    }

    static void MovementSettingsRejectInvalidValues()
    {
        foreach (var settings in new[]
        {
            new PetMovementSettings(DistancePerFrame: 0),
            new PetMovementSettings(BaseSpeedPixelsPerSecond: double.NaN),
            new PetMovementSettings(MinSpeedPixelsPerSecond: -1),
            new PetMovementSettings(MaxSpeedPixelsPerSecond: double.PositiveInfinity),
            new PetMovementSettings(BaseSpeedPixelsPerSecond: 200),
            new PetMovementSettings(MinSpeedPixelsPerSecond: 100),
        })
        {
            Assert.Throws<ArgumentException>(() => new PetMovementController(settings));
        }
    }

    static void MovementFramesKeepDistanceWhenChangingClipLength()
    {
        var controller = new PetMovementController(CreateTestMovementSettings());
        controller.AdvanceMoveFrame(65, 7);
        var changed = controller.AdvanceMoveFrame(5, 5);
        Assert.Equal(2, changed.FrameIndex, "Changing clip length should wrap the current index and retain carried distance.");
        Assert.True(changed.Changed, "Crossing a frame distance after switching direction should advance immediately.");
    }

    static void MovementKindsContainNoDirectionalOrTurnActions()
    {
        var names = Enum.GetNames<PetActionKind>();
        Assert.Equal(1, names.Count(name => name.StartsWith("Move", StringComparison.Ordinal)), "Movement should have exactly one action kind.");
        Assert.False(names.Any(name => name.StartsWith("Turn", StringComparison.Ordinal)), "Retired turn kinds should not remain in the runtime model.");
    }

    static PetMovementSettings CreateTestMovementSettings() => new();

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
        Assert.Equal(144d, result.X, "A one-shot push should move the cursor a clearly visible fixed distance.");
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

    static void CursorPushGateBlocksRepeatsUntilCursorExits()
    {
        var gate = new CursorPushGate();

        Assert.True(gate.AllowsPush, "A fresh cursor proximity session should allow one push.");
        gate.CompletePush();
        Assert.False(gate.AllowsPush, "Completing a push should block repeated pushes in the same proximity session.");

        gate.ObserveCursorDistance(100, PetMovementPlanner.MouseInterestRadius);
        Assert.False(gate.AllowsPush, "Keeping the cursor nearby should preserve the completed-push latch.");

        gate.ObserveCursorDistance(400, PetMovementPlanner.MouseInterestRadius);
        Assert.True(gate.AllowsPush, "Leaving the interest radius should arm the next proximity session.");
    }

    static void AnimationControllerLoopsIdleFrames()
    {
        var controller = new PetAnimationController();

        Assert.Equal(1, controller.AdvanceIdle(3), "Idle should advance to frame 1.");
        Assert.Equal(2, controller.AdvanceIdle(3), "Idle should advance to frame 2.");
        Assert.Equal(0, controller.AdvanceIdle(3), "Idle should loop to frame 0.");
        controller.ResetIdle();
        Assert.Equal(0, controller.IdleFrameIndex, "Idle reset should restore frame 0.");
    }

    static void PetFrameTimingResolvesAuthoredOverrides()
    {
        var action = new PetActionDefinition(
            Id: "irregular",
            Kind: PetActionKind.Idle,
            FramePaths: ["0.png", "1.png", "2.png"],
            FrameInterval: TimeSpan.FromMilliseconds(100),
            FrameDurations:
            [
                TimeSpan.FromMilliseconds(240),
                null,
                TimeSpan.FromMilliseconds(60),
            ]);
        var fallback = TimeSpan.FromMilliseconds(80);

        Assert.Equal(TimeSpan.FromMilliseconds(240), PetFrameTiming.GetDuration(action, 0, fallback), "Frame zero should use its override.");
        Assert.Equal(TimeSpan.FromMilliseconds(100), PetFrameTiming.GetDuration(action, 1, fallback), "A null override should use frameIntervalMs.");
        Assert.Equal(TimeSpan.FromMilliseconds(60), PetFrameTiming.GetDuration(action, 2, fallback), "Frame two should use its override.");
        Assert.Equal(TimeSpan.FromMilliseconds(400), PetFrameTiming.GetTotalDuration(action, 3, fallback), "Total duration should combine overrides and defaults.");

        var noDefault = action with { FrameInterval = null, FrameDurations = [null, null, null] };
        Assert.Equal(fallback, PetFrameTiming.GetDuration(noDefault, 1, fallback), "A frame without either authored duration should use the runtime fallback.");
    }

    static void AnimationControllerCompletesOneShotActions()
    {
        var controller = new PetAnimationController();

        Assert.True(controller.BeginBlink(3), "Blink should start when frames exist.");
        Assert.Equal(new PetFrameAdvance(1, false), controller.AdvanceBlink(3), "Blink should advance through authored frames.");
        Assert.Equal(new PetFrameAdvance(2, false), controller.AdvanceBlink(3), "Blink should expose its final authored frame.");
        Assert.Equal(new PetFrameAdvance(0, true), controller.AdvanceBlink(3), "Blink should complete after its final frame.");
        Assert.False(controller.IsBlinking, "Completed blink should clear its active state.");

        Assert.True(controller.BeginPetting(2), "Petting should start even when the visual sequence is a fallback.");
        Assert.Equal(new PetFrameAdvance(1, false), controller.AdvancePetting(2), "Petting should advance through authored frames.");
        Assert.Equal(new PetFrameAdvance(0, true), controller.AdvancePetting(2), "Petting should complete once.");
        Assert.False(controller.IsPetting, "Completed petting should clear its active state.");
    }

    static void AnimationControllerCompletesExpressionTransitions()
    {
        var controller = new PetAnimationController();
        Assert.True(controller.BeginExpressionTransition(PetExpressionTransitionMode.In, 2), "Expression transition should start with frames.");

        var middle = controller.AdvanceExpressionTransition(2);
        var completed = controller.AdvanceExpressionTransition(2);

        Assert.Equal(1, middle.FrameIndex, "Expression transition should expose its final frame.");
        Assert.False(middle.Completed, "Expression transition should not complete before the final frame has displayed.");
        Assert.True(completed.Completed, "Expression transition should complete after its final frame.");
        Assert.Equal(PetExpressionTransitionMode.In, completed.CompletedMode, "Completion should retain the transition direction.");
        Assert.Equal(PetExpressionTransitionMode.None, controller.ExpressionTransitionMode, "Completed transition should return to none.");
    }

    static void AnimationControllerCentralizesPassiveBlockers()
    {
        var controller = new PetAnimationController();
        var available = new PetPassiveAnimationContext(
            PassiveAnimationAllowed: true,
            IsDragging: false,
            HasActiveMovementTarget: false,
            IsRadialWheelOpen: false,
            HasTemporaryExpression: false);

        Assert.True(controller.CanRunIdle(available, frameCount: 8), "Idle should run when no higher-priority activity is present.");
        Assert.True(controller.CanBeginBlink(available, frameCount: 3), "Blink should run when no higher-priority activity is present.");

        controller.BeginPetting(1);
        Assert.False(controller.CanRunIdle(available, frameCount: 8), "Petting should block idle playback.");
        Assert.False(controller.CanBeginBlink(available, frameCount: 3), "Petting should block blink playback.");
        controller.StopPetting();

        var blocked = available with { IsRadialWheelOpen = true };
        Assert.False(controller.CanRunIdle(blocked, frameCount: 8), "The radial wheel should block idle playback.");
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
        var message = AssetService.FormatLoadFailureMessage("Idle frames", "Assets/Runtime/Castorice/States/Idle/Castorice.Idle.03.png");

        Assert.Contains(message, "Idle frames", "Asset diagnostics should include the resource group.");
        Assert.Contains(message, "Assets/Runtime/Castorice/States/Idle/Castorice.Idle.03.png", "Asset diagnostics should include the resource path.");
    }

    static void PackagedCharacterAssetsAreDisplaySized()
    {
        var workspace = FindWorkspaceRoot();
        var assetsRoot = System.IO.Path.Combine(workspace, "src", "CastoPet", "Assets", "Runtime", "Castorice");
        var excludedSegments = Array.Empty<string>();
        var assets = Directory
            .EnumerateFiles(assetsRoot, "*.png", SearchOption.AllDirectories)
            .Where(path => !System.IO.Path.GetFileName(path).Equals("blink-preview.png", StringComparison.OrdinalIgnoreCase))
            .Where(path => !excludedSegments.Any(segment => path.Contains(segment, StringComparison.OrdinalIgnoreCase)));

        foreach (var asset in assets)
        {
            var (width, height) = ReadPngSize(asset);

            Assert.True(
                width <= AssetService.CharacterDecodePixelWidth && height <= 420,
                $"{asset} should fit the pet display bounds, got {width}x{height}.");
        }
    }

    static void PackagedExpressionTransitionsHaveCompleteSourceAndRuntimeEndpoints()
    {
        var workspace = FindWorkspaceRoot();
        var projectRoot = System.IO.Path.Combine(workspace, "src", "CastoPet");
        var authoringRoot = System.IO.Path.Combine(workspace, "artwork", "authoring", "Castorice");
        var labels = new[] { "Happy", "Shy", "Sleepy", "Surprised", "Pouting", "Confused", "Proud", "Crying" };
        var idlePath = System.IO.Path.Combine(projectRoot, "Assets", "Runtime", "Castorice", "States", "Idle", "Castorice.Idle.00.png");
        using var idle = new Bitmap(idlePath);

        foreach (var label in labels)
        {
            var id = label.ToLowerInvariant();
            var targetPath = System.IO.Path.Combine(authoringRoot, "expressions", "targets", $"{label}.png");
            var projectPath = System.IO.Path.Combine(authoringRoot, "actions", "expressions", $"{id}.transition.animator.json");
            var finalPath = System.IO.Path.Combine(projectRoot, "Assets", "Runtime", "Castorice", "Expressions", $"Castorice.Expression.{label}.png");
            var transitionRoot = System.IO.Path.Combine(projectRoot, "Assets", "Runtime", "Castorice", "Expressions", label, "Transition");
            Assert.True(File.Exists(targetPath), $"{label} should keep a CastoPet-owned source target.");
            Assert.True(File.Exists(projectPath), $"{label} should keep an editable transition project.");

            var frames = Enumerable.Range(0, 6)
                .Select(index => System.IO.Path.Combine(transitionRoot, $"Castorice.Expression.{label}.Transition.{index:00}.png"))
                .ToArray();
            Assert.True(frames.All(File.Exists), $"{label} should include six consecutive runtime transition frames.");
            foreach (var frame in frames)
            {
                using var bitmap = new Bitmap(frame);
                Assert.Equal(320, bitmap.Width, $"{System.IO.Path.GetFileName(frame)} should be 320 pixels wide.");
                Assert.Equal(320, bitmap.Height, $"{System.IO.Path.GetFileName(frame)} should be 320 pixels high.");
            }
            using var first = new Bitmap(frames[0]);
            using var finalTransition = new Bitmap(frames[^1]);
            using var final = new Bitmap(finalPath);
            Assert.True(CalculateAverageRgbaDelta(idle, first) < 35, $"{label} transition should begin visually close to Idle.00.");
            Assert.True(CalculateAverageRgbaDelta(finalTransition, final) < 35, $"{label} transition should end visually close to its expression image.");
            for (var index = 0; index < frames.Length - 1; index++)
            {
                using var current = new Bitmap(frames[index]);
                using var next = new Bitmap(frames[index + 1]);
                Assert.True(CalculateAverageRgbaDelta(current, next) < 35, $"{label} transition frames {index:00}-{index + 1:00} should remain visually continuous.");
            }
        }

        var projectText = File.ReadAllText(System.IO.Path.Combine(projectRoot, "CastoPet.csproj"));
        Assert.Contains(projectText, @"Assets\Runtime\Castorice\**\*.png", "Expression transition PNGs should be covered by the runtime WPF resource glob.");
    }
}
