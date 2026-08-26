namespace CastoPet.Tests;

internal static partial class TestSuite
{
    static void InputKeyboardLayoutMapsCommonKeys()
    {
        Assert.True(InputKeyboardLayout.TryGetKeyBounds("A", out var a), "A should have a key rectangle.");
        Assert.True(InputKeyboardLayout.TryGetKeyBounds("Space", out var space), "Space should have a key rectangle.");
        Assert.True(InputKeyboardLayout.TryGetKeyBounds("Enter", out var enter), "Enter should have a key rectangle.");
        Assert.False(InputKeyboardLayout.TryGetKeyBounds("Unknown", out _), "Unknown keys should not map to a rectangle.");
        Assert.True(
            a.X >= 0 && a.Y >= 0 && a.Right <= InputKeyboardLayout.VisualWidth && a.Bottom <= InputKeyboardLayout.VisualHeight,
            "A should fit inside the visual bounds.");
        Assert.True(space.Width > a.Width, "Space should be wider than a letter key.");
        Assert.True(enter.Height >= a.Height, "Enter should be at least as tall as a letter key.");
    }

    static void InputKeyboardLayoutExposesDrawableKeys()
    {
        Assert.True(InputKeyboardLayout.KeyIds.Contains("A"), "Drawable keys should include A.");
        Assert.True(InputKeyboardLayout.KeyIds.Contains("Space"), "Drawable keys should include Space.");
        Assert.True(InputKeyboardLayout.KeyIds.Contains("MouseLeft"), "Drawable keys should include mouse feedback zones.");
    }

    static void InputKeyboardLayoutExposesKeyLabels()
    {
        Assert.Equal("A", InputKeyboardLayout.GetDisplayLabel("A"), "Letter keys should display themselves.");
        Assert.Equal("Space", InputKeyboardLayout.GetDisplayLabel("Space"), "Space should display a readable label.");
        Assert.Equal("←", InputKeyboardLayout.GetDisplayLabel("Left"), "Arrow keys should display arrow glyphs.");
        Assert.Equal("L", InputKeyboardLayout.GetDisplayLabel("MouseLeft"), "Mouse left feedback should display a compact label.");
    }

    static void InputReactiveStateExpiresHighlights()
    {
        var state = new InputReactiveState();

        state.AddKey("A", TimeSpan.Zero);

        Assert.True(state.GetActiveHighlights(TimeSpan.FromMilliseconds(100)).Contains("A"), "A should remain active before expiration.");
        Assert.False(state.GetActiveHighlights(TimeSpan.FromMilliseconds(300)).Contains("A"), "A should expire after the highlight duration.");
    }

    static void WindowsInputHookNormalizesCommonKeys()
    {
        Assert.Equal("A", WindowsInputHookService.NormalizeVirtualKey(0x41), "VK_A should normalize to A.");
        Assert.Equal("Space", WindowsInputHookService.NormalizeVirtualKey(0x20), "VK_SPACE should normalize to Space.");
        Assert.Equal("Enter", WindowsInputHookService.NormalizeVirtualKey(0x0D), "VK_RETURN should normalize to Enter.");
        Assert.Equal("Left", WindowsInputHookService.NormalizeVirtualKey(0x25), "VK_LEFT should normalize to Left.");
    }

    static void InputReactiveModeSuppressesPassiveAnimation()
    {
        Assert.False(
            InputReactiveModePolicy.AllowsPassiveAnimation(inputReactiveModeActive: true),
            "Input reactive mode should pause idle, blink, and active movement visuals.");
        Assert.True(
            InputReactiveModePolicy.AllowsPassiveAnimation(inputReactiveModeActive: false),
            "Normal passive animation should remain available outside input reactive mode.");
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

    static void MovementControllerAdvancesLogicalPositions()
    {
        var controller = new PetMovementController(CreateTestMoveAction(), new Random(7));
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

    static void MovementControllerResumesWithoutAccumulatingVisualPause()
    {
        var controller = new PetMovementController(CreateTestMoveAction(), new Random(7));
        controller.BeginRendering(left: 0, top: 0);
        controller.SetTarget(new PetMovementTarget(100, 0));

        controller.Advance(TimeSpan.Zero, currentLeft: 0, currentTop: 0);
        var beforeTurn = controller.Advance(TimeSpan.FromMilliseconds(100), currentLeft: 0, currentTop: 0);
        Assert.True(beforeTurn is not null, "Movement should begin before the visual turn pause.");

        controller.ResumeAfterVisualPause(beforeTurn!.Value.NextLeft, beforeTurn.Value.NextTop);
        var resumed = controller.Advance(
            TimeSpan.FromMilliseconds(600),
            beforeTurn.Value.NextLeft,
            beforeTurn.Value.NextTop);
        var nextFrame = controller.Advance(
            TimeSpan.FromMilliseconds(700),
            beforeTurn.Value.NextLeft,
            beforeTurn.Value.NextTop);

        Assert.True(resumed is null, "The first sample after a visual pause should only synchronize rendering time.");
        Assert.True(nextFrame is not null, "Movement should continue on the next rendering sample.");
        Assert.Equal(18d, nextFrame!.Value.NextLeft, "Only post-resume frame time should contribute to movement.");
        Assert.Equal(9d, nextFrame.Value.Distance, "The visual pause must not be converted into a large catch-up step.");
    }

    static void MovementControllerSchedulesBoundedWanderTargets()
    {
        var controller = new PetMovementController(CreateTestMoveAction(), new Random(11));
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
        var controller = new PetMovementController(CreateTestMoveAction(), new Random(3));

        Assert.Equal(new PetMoveFrameAdvance(2, true), controller.AdvanceMoveFrame(distance: 25, frameCount: 8), "Twenty-five pixels should cross two ten-pixel frames.");
        Assert.Equal(new PetMoveFrameAdvance(3, true), controller.AdvanceMoveFrame(distance: 5, frameCount: 8), "The carried distance should advance the next frame.");
        controller.ResetMoveFrames();
        Assert.Equal(0, controller.MoveFrameIndex, "Reset should restore movement frame zero.");
    }

    static void DirectionalMovementWithoutTurnFramesFacesImmediately()
    {
        var animator = new PetDirectionalMovementAnimator();
        Assert.False(animator.RequestDirection(PetHorizontalDirection.Left, frameCount: 0), "No turn frames should mean no transition to wait for.");
        Assert.Equal(PetFacingDirection.Left, animator.Facing, "Starting movement without transitions should immediately face left.");
        Assert.False(animator.IsTurning, "Immediate facing must not block movement.");

        Assert.False(animator.RequestDirection(PetHorizontalDirection.Right, frameCount: 0), "Changing sides without transitions should not start a timer.");
        Assert.Equal(PetFacingDirection.Right, animator.Facing, "Changing direction should immediately select the opposite side.");
        Assert.False(animator.RequestFront(frameCount: 0), "Stopping without transitions should not start a return animation.");
        Assert.Equal(PetFacingDirection.Front, animator.Facing, "Stopping should immediately restore the front-facing state.");

        animator.RequestDirection(PetHorizontalDirection.Left, frameCount: 3);
        animator.Advance(frameCount: 3);
        Assert.False(animator.RequestDirection(PetHorizontalDirection.Right, frameCount: 0), "Immediate facing should cancel any pending turn.");
        animator.Advance(frameCount: 3);
        Assert.Equal(PetFacingDirection.Right, animator.Facing, "A canceled turn must not overwrite the immediate facing.");
        Assert.Equal(PetTurnPhase.None, animator.Phase, "No transition phase should remain active.");
        Assert.Equal(0, animator.FrameIndex, "Immediate facing should clear the old turn frame index.");
    }

    static void DirectionalMovementTurnsFromFrontBeforeWalking()
    {
        var animator = new PetDirectionalMovementAnimator();

        Assert.True(animator.RequestDirection(PetHorizontalDirection.Right, frameCount: 3), "A front-facing pet should begin the requested turn.");
        Assert.Equal(PetTurnPhase.ToSide, animator.Phase, "The initial turn should face toward the walking side.");
        Assert.Equal(PetHorizontalDirection.Right, animator.TurnDirection, "The right turn must use the separately authored right-facing frames.");
        Assert.Equal(0, animator.FrameIndex, "A forward turn should begin at its first frame.");

        animator.Advance(frameCount: 3);
        animator.Advance(frameCount: 3);
        animator.Advance(frameCount: 3);

        Assert.False(animator.IsTurning, "The turn should complete after its final frame.");
        Assert.Equal(PetFacingDirection.Right, animator.Facing, "Completing the turn should leave the pet facing right.");
    }

    static void DirectionalMovementDoesNotRestartActiveTurn()
    {
        var animator = new PetDirectionalMovementAnimator();

        Assert.True(animator.RequestDirection(PetHorizontalDirection.Left, frameCount: 6), "The first request should start the turn timer sequence.");
        Assert.False(animator.RequestDirection(PetHorizontalDirection.Left, frameCount: 6), "A render-loop request during the same turn must not restart its timer.");
        Assert.True(animator.IsTurning, "The active turn should continue blocking movement until its timer advances.");
        Assert.Equal(0, animator.FrameIndex, "Repeated render-loop requests must leave progress to the turn timer.");
    }

    static void DirectionalMovementReturnsThroughSameAuthoredFrames()
    {
        var animator = CreateRightFacingAnimator(frameCount: 3);

        Assert.True(animator.RequestFront(frameCount: 3), "Stopping movement should begin a return to front.");
        Assert.Equal(PetTurnPhase.ToFront, animator.Phase, "The return should reverse the current side's turn sequence.");
        Assert.Equal(PetHorizontalDirection.Right, animator.TurnDirection, "Returning from right must preserve the authored right-side accessories.");
        Assert.Equal(2, animator.FrameIndex, "A reverse turn should start at the last side-facing frame.");

        animator.Advance(frameCount: 3);
        Assert.Equal(1, animator.FrameIndex, "The reverse turn should step backward through authored frames.");
        animator.Advance(frameCount: 3);
        Assert.Equal(0, animator.FrameIndex, "The reverse turn should expose the original first frame before completing.");
        animator.Advance(frameCount: 3);

        Assert.False(animator.IsTurning, "The return should complete once frame zero has been displayed.");
        Assert.Equal(PetFacingDirection.Front, animator.Facing, "The completed return should restore front-facing idle.");
    }

    static void DirectionalMovementChangesSidesThroughFront()
    {
        var animator = CreateRightFacingAnimator(frameCount: 3);

        Assert.True(animator.RequestDirection(PetHorizontalDirection.Left, frameCount: 3), "Changing direction should begin by returning from the current side.");
        Assert.Equal(PetTurnPhase.ToFront, animator.Phase, "Opposite directions must pass through front instead of mirroring.");
        Assert.Equal(PetHorizontalDirection.Right, animator.TurnDirection, "The first half should reverse the current right-facing sequence.");

        animator.Advance(frameCount: 3);
        animator.Advance(frameCount: 3);
        animator.Advance(frameCount: 3);

        Assert.True(animator.IsTurning, "After reaching front, the pending left turn should begin automatically.");
        Assert.Equal(PetTurnPhase.ToSide, animator.Phase, "The second half should turn from front to the new side.");
        Assert.Equal(PetHorizontalDirection.Left, animator.TurnDirection, "The second half must use separately authored left-facing frames.");
        Assert.Equal(0, animator.FrameIndex, "The new side's turn should begin at frame zero.");
    }

    static PetDirectionalMovementAnimator CreateRightFacingAnimator(int frameCount)
    {
        var animator = new PetDirectionalMovementAnimator();
        animator.RequestDirection(PetHorizontalDirection.Right, frameCount);
        for (var index = 0; index < frameCount; index++)
        {
            animator.Advance(frameCount);
        }

        return animator;
    }

    static PetActionDefinition CreateTestMoveAction()
    {
        return new PetActionDefinition(
            Id: "test-move",
            Kind: PetActionKind.Move,
            FramePaths: Array.Empty<string>(),
            DistancePerFrame: 10,
            BaseSpeedPixelsPerSecond: 90,
            MinSpeedPixelsPerSecond: 80,
            MaxSpeedPixelsPerSecond: 105);
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

    static void InputReactiveAssetPathUsesAppResource()
    {
        Assert.Equal(
            "Assets/Runtime/Castorice/States/InputReactive/Castorice.InputReactive.Base.png",
            BuiltInPetSkins.Castorice.InputReactiveBasePath,
            "Input reactive base should use an app resource path.");
    }

    static void InputReactiveAssetIsPackaged()
    {
        var workspace = FindWorkspaceRoot();
        var projectFile = System.IO.Path.Combine(workspace, "src", "CastoPet", "CastoPet.csproj");
        var projectText = File.ReadAllText(projectFile);

        Assert.Contains(
            projectText,
            @"Assets\Runtime\Castorice\**\*.png",
            "Input reactive base should be covered by the runtime WPF resource glob.");
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
