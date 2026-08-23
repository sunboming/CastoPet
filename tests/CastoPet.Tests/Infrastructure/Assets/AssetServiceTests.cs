namespace CastoPet.Tests;

internal static partial class TestSuite
{
    static void AssetServiceDefaultsToBuiltInSkin()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var logger = new LoggingService(paths);
        var service = new AssetService(logger);

        Assert.Equal(BuiltInPetSkins.Castorice, service.Skin, "Asset service should default to the built-in Castorice skin.");
    }

    static void AssetServiceUsesConfiguredSkinPaths()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var logger = new LoggingService(paths);
        var skin = BuiltInPetSkins.Castorice with
        {
            Id = "custom",
            DefaultCharacterPath = "Skins/Custom/Missing.png",
        };
        var service = new AssetService(logger, skin);

        _ = Assert.Throws<Exception>(() => service.LoadDefaultCharacter());

        var logText = File.ReadAllText(Directory.EnumerateFiles(paths.LogsDirectory, "*.log").Single());
        Assert.Contains(logText, "Skins/Custom/Missing.png", "Asset service should load the configured skin path.");
    }

    static void AssetServiceLoadsFileSystemSkinImagePaths()
    {
        using var temp = TempDirectory.Create();
        var sourcePath = System.IO.Path.Combine(FindWorkspaceRoot(), "src", "CastoPet", "Assets", "Runtime", "Castorice", "Castorice.png");
        var skinImagePath = System.IO.Path.Combine(temp.Path, "Skin", "Default.png");
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(skinImagePath)!);
        File.Copy(sourcePath, skinImagePath);
        var paths = new AppPaths(System.IO.Path.Combine(temp.Path, "Data"));
        var logger = new LoggingService(paths);
        var skin = BuiltInPetSkins.Castorice with
        {
            Id = "file-skin",
            DefaultCharacterPath = skinImagePath,
        };
        var service = new AssetService(logger, skin);

        var image = service.LoadDefaultCharacter();

        Assert.True(image.PixelWidth > 0, "File-system skin images should load through AssetService.");
    }

    static void AssetServiceLoadsExpressionImagesWithIsolatedTransitions()
    {
        using var temp = TempDirectory.Create();
        var sourcePath = System.IO.Path.Combine(FindWorkspaceRoot(), "src", "CastoPet", "Assets", "Runtime", "Castorice", "Castorice.png");
        var finalPath = System.IO.Path.Combine(temp.Path, "Happy.png");
        var transitionPath = System.IO.Path.Combine(temp.Path, "Happy.00.png");
        File.Copy(sourcePath, finalPath);
        File.Copy(sourcePath, transitionPath);
        var paths = new AppPaths(System.IO.Path.Combine(temp.Path, "Data"));
        var logger = new LoggingService(paths);
        var expression = new PetExpressionDefinition(
            "happy",
            "Happy",
            finalPath,
            new[] { transitionPath, System.IO.Path.Combine(temp.Path, "Missing.png") },
            TimeSpan.FromMilliseconds(66));
        var skin = BuiltInPetSkins.Castorice with
        {
            Expressions = new[] { expression },
            Actions = BuiltInPetSkins.Castorice.Actions
                .Where(action => action.Kind is not (PetActionKind.ExpressionTransitionIn or PetActionKind.ExpressionTransitionOut))
                .ToArray(),
        };
        var service = new AssetService(logger, skin);

        var assets = service.LoadExpressionAssets();

        Assert.Equal(1, assets.Count, "A valid final expression image should remain available.");
        Assert.True(assets.ContainsKey(expression.Id), "Expression assets should be keyed directly by stable expression ID.");
        Assert.Equal(0, assets.Values.Single().TransitionFrames.Count, "One missing transition frame should discard only that transition sequence.");
        Assert.Equal(expression, assets.Values.Single().Definition, "Loaded expression assets should retain their definition.");
        Assert.Equal(0, service.LoadExpressionTransitionInFrames().Count, "A missing generic transition-in action should return no fallback frames.");
        Assert.Equal(0, service.LoadExpressionTransitionOutFrames().Count, "A missing generic transition-out action should return no fallback frames.");
    }

    static void AssetServiceTreatsMissingPettingFramesAsOptional()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var logger = new LoggingService(paths);
        var skin = BuiltInPetSkins.Castorice with
        {
            Actions = BuiltInPetSkins.Castorice.Actions
                .Where(action => action.Kind != PetActionKind.Petting)
                .ToArray(),
        };
        var service = new AssetService(logger, skin);

        Assert.Equal(0, service.LoadPettingFrames().Count, "Old skins without petting should use the runtime fallback instead of failing.");
    }

    static void BoundedAssetCacheEvictsLeastRecentlyUsedEntries()
    {
        var loads = new List<string>();
        var cache = new BoundedLruCache<string, string>(2, key =>
        {
            loads.Add(key);
            return $"asset:{key}";
        });

        Assert.Equal("asset:first", cache.Get("first"), "The first lookup should load its value.");
        Assert.Equal("asset:second", cache.Get("second"), "A different key should load independently.");
        Assert.Equal("asset:first", cache.Get("first"), "A cache hit should retain the existing value.");
        Assert.Equal("asset:third", cache.Get("third"), "A full cache should still admit a new value.");
        Assert.Equal("asset:second", cache.Get("second"), "The least recently used value should reload after eviction.");
        Assert.Equal(4, loads.Count, "Only the evicted key should require a second load.");
        Assert.Equal(2, cache.Count, "The cache must stay within its configured capacity.");
    }

    static void PetWindowDefersOptionalAnimationAssets()
    {
        var workspace = FindWorkspaceRoot();
        var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "PetWindow.xaml.cs"));
        const string constructorStart = "public PetWindow(\n        AssetService assets,";
        var constructor = ExtractSourceSection(
            source,
            constructorStart,
            "\n    private void ShutdownRuntimeResources");

        Assert.Contains(constructor, "assets.LoadIdleFrames()", "Idle frames should remain eager for smooth startup playback.");
        Assert.Contains(constructor, "assets.LoadBlinkFrames()", "Blink frames should remain eager for smooth passive playback.");
        Assert.False(constructor.Contains("assets.LoadExpressionAssets()", StringComparison.Ordinal),
            "Expression images and transitions should not all decode during startup.");
        Assert.False(constructor.Contains("assets.LoadPettingFrames()", StringComparison.Ordinal),
            "Petting frames should wait until the first petting action.");
        Assert.False(constructor.Contains("assets.LoadMoveLeftFrames()", StringComparison.Ordinal),
            "Directional movement frames should wait until movement is used.");
        Assert.Contains(source, "new BoundedLruCache<string, PetExpressionAsset?>",
            "Expression assets should use a bounded cache instead of permanent bulk retention.");
    }

    static void SourceSectionExtractionHandlesLineEndings()
    {
        const string lfSource = "before\npublic PetWindow(\n    body\n    private void Next";
        var crlfSource = lfSource.ReplaceLineEndings("\r\n");
        const string expected = "public PetWindow(\n    body";

        Assert.Equal(
            expected,
            ExtractSourceSection(lfSource, "public PetWindow(\n", "\n    private void Next"),
            "Source extraction should support LF files.");
        Assert.Equal(
            expected,
            ExtractSourceSection(crlfSource, "public PetWindow(\n", "\n    private void Next"),
            "Source extraction should support CRLF files.");

        var missingStart = Assert.Throws<InvalidOperationException>(() =>
            ExtractSourceSection(lfSource, "missing start", "\n    private void Next"));
        Assert.Contains(missingStart.Message, "start marker", "A missing start marker should produce a useful failure.");

        var missingEnd = Assert.Throws<InvalidOperationException>(() =>
            ExtractSourceSection(lfSource, "public PetWindow(\n", "missing end"));
        Assert.Contains(missingEnd.Message, "end marker", "A missing end marker should produce a useful failure.");
    }

    static string ExtractSourceSection(string source, string startMarker, string endMarker)
    {
        var normalizedSource = source.ReplaceLineEndings("\n");
        var normalizedStartMarker = startMarker.ReplaceLineEndings("\n");
        var normalizedEndMarker = endMarker.ReplaceLineEndings("\n");
        var start = normalizedSource.IndexOf(normalizedStartMarker, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException($"Source start marker was not found: {normalizedStartMarker}");
        }

        var end = normalizedSource.IndexOf(
            normalizedEndMarker,
            start + normalizedStartMarker.Length,
            StringComparison.Ordinal);
        if (end < 0)
        {
            throw new InvalidOperationException($"Source end marker was not found: {normalizedEndMarker}");
        }

        return normalizedSource[start..end];
    }

}
