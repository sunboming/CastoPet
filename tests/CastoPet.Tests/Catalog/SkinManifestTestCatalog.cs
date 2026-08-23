namespace CastoPet.Tests;

internal static partial class TestSuite
{
    private static IReadOnlyList<TestCase> SkinManifestTestCases { get; } =
    [
        new("Pet skin manifest loads JSON resource paths", PetSkinManifestLoadsJsonResourcePaths),
        new("Pet skin manifest loads per-frame action durations", PetSkinManifestLoadsPerFrameActionDurations),
        new("Pet skin manifest loads expression transition metadata", PetSkinManifestLoadsExpressionTransitionMetadata),
        new("Pet skin manifest loads file paths relative to manifest", PetSkinManifestLoadsFilePathsRelativeToManifest),
        new("External skin rejects paths outside its resource root", ExternalSkinRejectsPathsOutsideItsResourceRoot),
        new("External skin rejects reparse point escapes", ExternalSkinRejectsReparsePointEscapes),
        new("External skin enforces manifest and frame budgets", ExternalSkinEnforcesManifestAndFrameBudgets),
        new("External skin validates PNG files and dimensions", ExternalSkinValidatesPngFilesAndDimensions),
        new("Pet skin manifest requires core actions", PetSkinManifestRequiresCoreActions),
        new("Pet skin manifest rejects duplicate actions", PetSkinManifestRejectsDuplicateActions),
        new("Pet skin manifest rejects invalid action metadata", PetSkinManifestRejectsInvalidActionMetadata),
        new("Pet skin manifest rejects invalid per-frame durations", PetSkinManifestRejectsInvalidPerFrameDurations),
        new("Pet skin manifest writer emits loadable JSON", PetSkinManifestWriterEmitsLoadableJson),
        new("Pet skin manifest writer round trips per-frame durations", PetSkinManifestWriterRoundTripsPerFrameDurations),
        new("Pet skin manifest writer stores paths relative to resource root", PetSkinManifestWriterStoresPathsRelativeToResourceRoot),
        new("Pet skin manifest round trips optional petting action", PetSkinManifestRoundTripsOptionalPettingAction),
    ];
}
