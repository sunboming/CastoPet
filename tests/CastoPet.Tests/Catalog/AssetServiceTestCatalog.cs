namespace CastoPet.Tests;

internal static partial class TestSuite
{
    private static IReadOnlyList<TestCase> AssetServiceTestCases { get; } =
    [
        new("Asset service defaults to built-in skin", AssetServiceDefaultsToBuiltInSkin),
        new("Asset service uses configured skin paths", AssetServiceUsesConfiguredSkinPaths),
        new("Asset service loads file system skin image paths", AssetServiceLoadsFileSystemSkinImagePaths),
        new("Asset service loads expression images with isolated transitions", AssetServiceLoadsExpressionImagesWithIsolatedTransitions),
        new("Asset service treats missing petting frames as optional", AssetServiceTreatsMissingPettingFramesAsOptional),
        new("Bounded asset cache evicts least recently used entries", BoundedAssetCacheEvictsLeastRecentlyUsedEntries),
        new("Source section extraction handles line endings", SourceSectionExtractionHandlesLineEndings),
        new("Pet window defers optional animation assets", PetWindowDefersOptionalAnimationAssets),
    ];
}
