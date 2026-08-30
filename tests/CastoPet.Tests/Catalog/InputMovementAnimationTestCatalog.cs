namespace CastoPet.Tests;

internal static partial class TestSuite
{
    private static IReadOnlyList<TestCase> InputMovementAnimationTestCases { get; } =
    [
        new("Movement planner clamps targets to work area", MovementPlannerClampsTargetsToWorkArea),
        new("Movement planner approaches mouse with cursor offset", MovementPlannerApproachesMouseWithCursorOffset),
        new("Movement planner eases toward target", MovementPlannerEasesTowardTarget),
        new("Movement planner detects close targets", MovementPlannerDetectsCloseTargets),
        new("Movement planner detects mouse approach rest position", MovementPlannerDetectsMouseApproachRestPosition),
        new("Movement controller advances logical positions", MovementControllerAdvancesLogicalPositions),
        new("Movement controller resumes without accumulating render pause", MovementControllerResumesWithoutAccumulatingRenderPause),
        new("Movement controller schedules bounded wander targets", MovementControllerSchedulesBoundedWanderTargets),
        new("Movement controller advances frames by distance", MovementControllerAdvancesFramesByDistance),
        new("Movement controller uses shared speed in both directions", MovementControllerUsesSharedSpeedInBothDirections),
        new("Movement definition selects directional clips", MovementDefinitionSelectsDirectionalClips),
        new("Movement settings reject invalid values", MovementSettingsRejectInvalidValues),
        new("Movement frames keep distance when changing clip length", MovementFramesKeepDistanceWhenChangingClipLength),
        new("Movement kinds contain no directional or turn actions", MovementKindsContainNoDirectionalOrTurnActions),
        new("Cursor nudge planner nudges nearby cursor", CursorNudgePlannerNudgesNearbyCursor),
        new("Cursor nudge planner ignores distant cursor", CursorNudgePlannerIgnoresDistantCursor),
        new("Cursor nudge planner clamps to work area", CursorNudgePlannerClampsToWorkArea),
        new("Cursor nudge planner detects manual movement cooldown", CursorNudgePlannerDetectsManualMovementCooldown),
        new("Cursor nudge planner blocks while mouse button is pressed", CursorNudgePlannerBlocksWhileMouseButtonIsPressed),
        new("Cursor nudge planner limits continuous push duration", CursorNudgePlannerLimitsContinuousPushDuration),
        new("Cursor push gate blocks repeats until the cursor exits", CursorPushGateBlocksRepeatsUntilCursorExits),
        new("Animation controller loops idle frames", AnimationControllerLoopsIdleFrames),
        new("Pet frame timing resolves authored overrides", PetFrameTimingResolvesAuthoredOverrides),
        new("Animation controller completes one-shot actions", AnimationControllerCompletesOneShotActions),
        new("Animation controller completes expression transitions", AnimationControllerCompletesExpressionTransitions),
        new("Animation controller centralizes passive blockers", AnimationControllerCentralizesPassiveBlockers),
        new("Pet animation timings are responsive", PetAnimationTimingsAreResponsive),
        new("Idle breathing values are neutral during stabilization", IdleBreathingValuesAreNeutralDuringStabilization),
        new("Character stationary animations are enabled", CharacterStationaryAnimationsAreEnabled),
        new("Character assets decode at pet display width", CharacterAssetsDecodeAtPetDisplayWidth),
        new("Asset diagnostics include group and resource path", AssetDiagnosticsIncludeGroupAndResourcePath),
        new("Packaged character assets are display sized", PackagedCharacterAssetsAreDisplaySized),
        new("Packaged expression transitions have complete source and runtime endpoints", PackagedExpressionTransitionsHaveCompleteSourceAndRuntimeEndpoints),
    ];
}
