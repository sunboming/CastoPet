namespace CastoPet.Tests;

internal static partial class TestSuite
{
    private static IReadOnlyList<TestCase> SkinSelectionTestCases { get; } =
    [
        new("Pet skin selection defaults to built-in skin", PetSkinSelectionDefaultsToBuiltInSkin),
        new("Pet skin selection loads configured manifest", PetSkinSelectionLoadsConfiguredManifest),
        new("Pet skin selection falls back when manifest fails", PetSkinSelectionFallsBackWhenManifestFails),
    ];
}
