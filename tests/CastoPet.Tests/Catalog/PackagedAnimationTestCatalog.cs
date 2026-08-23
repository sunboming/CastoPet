namespace CastoPet.Tests;

internal static partial class TestSuite
{
    private static IReadOnlyList<TestCase> PackagedAnimationTestCases { get; } =
    [
        new("Built-in idle action defines eight authored-rate frame paths", BuiltInIdleActionDefinesEightAuthoredRateFramePaths),
        new("Idle frame diagnostics read all packaged frames", IdleFrameDiagnosticsReadAllPackagedFrames),
        new("Built-in blink action defines random blink frames", BuiltInBlinkActionDefinesRandomBlinkFrames),
        new("Built-in move action defines eight distance-driven paths", BuiltInMoveActionDefinesEightDistanceDrivenPaths),
        new("Move frame paths use app resources", MoveFramePathsUseAppResources),
        new("Move speed constants stay in smooth range", MoveSpeedConstantsStayInSmoothRange),
    ];
}
