namespace CastoPet.Tests;

internal static partial class TestSuite
{
    private static IReadOnlyList<TestCase> BuiltInSkinTestCases { get; } =
    [
        new("Built-in Castorice skin defines required actions", BuiltInCastoriceSkinDefinesRequiredActions),
        new("Built-in Castorice skin uses runtime asset root", BuiltInCastoriceSkinUsesRuntimeAssetRoot),
        new("Built-in Castorice idle action preserves current frames", BuiltInCastoriceIdleActionPreservesCurrentFrames),
        new("Built-in Castorice move action preserves movement values", BuiltInCastoriceMoveActionPreservesMovementValues),
        new("Built-in Castorice defines separate directional movement actions", BuiltInCastoriceDefinesSeparateDirectionalMovementActions),
        new("Built-in directional frames are embedded WPF resources", BuiltInDirectionalFramesAreEmbeddedWpfResources),
        new("Built-in Castorice blink action preserves schedule", BuiltInCastoriceBlinkActionPreservesSchedule),
        new("Built-in Castorice defines optional petting action", BuiltInCastoriceDefinesOptionalPettingAction),
        new("Built-in petting frames are packaged and clean", BuiltInPettingFramesArePackagedAndClean),
        new("Built-in Castorice expressions are ordered skin definitions", BuiltInCastoriceExpressionsAreOrderedSkinDefinitions),
        new("Built-in Castorice loads from embedded manifest", BuiltInCastoriceLoadsFromEmbeddedManifest),
    ];
}
