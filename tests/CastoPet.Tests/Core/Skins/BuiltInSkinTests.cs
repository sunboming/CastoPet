namespace CastoPet.Tests;

internal static partial class TestSuite
{
    static void BuiltInCastoriceSkinDefinesRequiredActions()
    {
        var skin = BuiltInPetSkins.Castorice;

        Assert.Equal("castorice", skin.Id, "Built-in skin id should be stable.");
        Assert.Equal("Castorice", skin.DisplayName, "Built-in skin display name should be stable.");
        Assert.Equal("Assets/Runtime/Castorice/Castorice.png", skin.DefaultCharacterPath, "Default character path should use runtime root.");
        Assert.Equal("Assets/Runtime/Castorice/States/Castorice.Dragging.png", skin.DraggingCharacterPath, "Dragging path should use runtime root.");
        Assert.True(skin.TryGetAction(PetActionKind.Idle, out _), "Castorice should define idle.");
        Assert.True(skin.TryGetAction(PetActionKind.Move, out _), "Castorice should define move.");
        Assert.True(skin.TryGetAction(PetActionKind.Blink, out _), "Castorice should define blink.");
        Assert.True(skin.TryGetAction(PetActionKind.ExpressionTransitionIn, out _), "Castorice should define transition in.");
        Assert.True(skin.TryGetAction(PetActionKind.ExpressionTransitionOut, out _), "Castorice should define transition out.");
    }

    static void BuiltInCastoriceSkinUsesRuntimeAssetRoot()
    {
        var skin = BuiltInPetSkins.Castorice;
        var paths = new List<string>
        {
            skin.DefaultCharacterPath,
            skin.DraggingCharacterPath,
        };
        paths.AddRange(skin.Actions.SelectMany(action => action.FramePaths));
        var movement = skin.GetRequiredAction(PetActionKind.Move).Movement!;
        paths.AddRange(movement.LeftFramePaths);
        paths.AddRange(movement.RightFramePaths);
        paths.AddRange(skin.Expressions.Select(expression => expression.ResourcePath));

        Assert.True(paths.All(path => path.StartsWith("Assets/Runtime/Castorice/", StringComparison.Ordinal)), "Built-in runtime paths should live under Assets/Runtime/Castorice.");
    }

    static void BuiltInCastoriceIdleActionPreservesCurrentFrames()
    {
        var idle = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Idle);

        Assert.Equal(8, idle.FramePaths.Count, "Idle should keep eight frames.");
        Assert.Equal(TimeSpan.FromMilliseconds(125), idle.FrameInterval, "Idle should play at the authored 8 FPS rate.");
        Assert.Equal("Assets/Runtime/Castorice/States/Idle/Castorice.Idle.00.png", idle.FramePaths[0], "First idle frame path should stay compatible.");
        Assert.Equal("Assets/Runtime/Castorice/States/Idle/Castorice.Idle.07.png", idle.FramePaths[^1], "Last idle frame path should stay compatible.");
    }

    static void BuiltInCastoriceMoveActionPreservesMovementValues()
    {
        var move = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Move);

        Assert.Equal(8, move.FramePaths.Count, "Move should keep eight frames.");
        Assert.Equal(10d, move.Movement!.Settings.DistancePerFrame, "Move distance per frame should stay compatible.");
        Assert.Equal(90d, move.Movement!.Settings.BaseSpeedPixelsPerSecond, "Move base speed should stay compatible.");
        Assert.Equal(80d, move.Movement!.Settings.MinSpeedPixelsPerSecond, "Move min speed should stay compatible.");
        Assert.Equal(105d, move.Movement!.Settings.MaxSpeedPixelsPerSecond, "Move max speed should stay compatible.");
    }

    static void BuiltInCastoriceDefinesUnifiedDirectionalMovement()
    {
        var move = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Move);
        var movement = move.Movement!;
        Assert.Equal(5, movement.LeftFramePaths.Count, "Left movement should keep its five stable frames.");
        Assert.Equal(7, movement.RightFramePaths.Count, "Right movement should keep its seven stable frames.");
        Assert.Equal("Assets/Runtime/Castorice/States/MoveLeft/Castorice.MoveLeft.01.png", movement.LeftFramePaths[0], "Left movement should begin with the stable side-facing sequence.");
        Assert.Equal("Assets/Runtime/Castorice/States/MoveLeft/Castorice.MoveLeft.05.png", movement.LeftFramePaths[^1], "Left movement should end before the eye direction changes.");
        Assert.Equal("Assets/Runtime/Castorice/States/MoveRight/Castorice.MoveRight.01.png", movement.RightFramePaths[0], "Right movement should begin with the stable side-facing sequence.");
        Assert.True(move.FrameInterval is null && move.FrameDurations is null, "Movement should remain distance-driven.");
    }

    static void BuiltInDirectionalFramesAreEmbeddedWpfResources()
    {
        var assembly = typeof(BuiltInPetSkins).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith(".g.resources", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"WPF resource stream {resourceName} is missing.");
        using var reader = new ResourceReader(stream);
        var resourcePaths = reader
            .Cast<System.Collections.DictionaryEntry>()
            .Select(entry => entry.Key?.ToString() ?? string.Empty)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var previewOnly = new[]
        {
            "assets/runtime/castorice/states/moveleft/castorice.moveleft.00.png",
            "assets/runtime/castorice/states/moveright/castorice.moveright.00.png",
            "assets/runtime/castorice/states/petting/castorice.petting.00.png",
        };

        if (CastoPetFeatureProfile.Current.Edition == CastoPetEdition.Stable)
        {
            Assert.True(resourcePaths.Contains("assets/runtime/castorice/castorice.png"), "Stable output should embed the default character.");
            Assert.True(resourcePaths.Contains("assets/runtime/castorice/states/castorice.dragging.png"), "Stable output should embed the dragging visual.");
            Assert.True(resourcePaths.Contains("assets/runtime/castorice/states/idle/castorice.idle.00.png"), "Stable output should embed idle frames.");
            Assert.True(resourcePaths.Contains("assets/runtime/castorice/states/blink/castorice.blink.00.png"), "Stable output should embed blink frames.");
            foreach (var path in previewOnly)
            {
                Assert.False(resourcePaths.Contains(path), $"Stable output should exclude preview resource {path}.");
            }

            Assert.False(resourcePaths.Any(path => path.StartsWith("assets/runtime/castorice/expressions/", StringComparison.OrdinalIgnoreCase)), "Stable output should exclude expression resources.");
            return;
        }

        foreach (var path in previewOnly.Take(2))
        {
            Assert.True(resourcePaths.Contains(path), $"Preview output should embed directional WPF resource {path}.");
        }
    }

    static void BuiltInCastoriceBlinkActionPreservesSchedule()
    {
        var blink = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Blink);

        Assert.Equal(5, blink.FramePaths.Count, "Blink should use a complete five-frame close and reopen sequence.");
        Assert.Equal(TimeSpan.FromMilliseconds(45), blink.FrameInterval, "Blink should use a short fallback interval.");
        Assert.True(
            new TimeSpan?[]
            {
                TimeSpan.FromMilliseconds(40),
                TimeSpan.FromMilliseconds(45),
                TimeSpan.FromMilliseconds(60),
                TimeSpan.FromMilliseconds(45),
                TimeSpan.FromMilliseconds(35),
            }.SequenceEqual(blink.FrameDurations ?? []),
            "Blink should preserve its authored close, hold, and reopen timing.");
        Assert.Equal("Assets/Runtime/Castorice/States/Blink/Castorice.Blink.00.png", blink.FramePaths[0], "Blink should start from frame zero.");
        Assert.Equal("Assets/Runtime/Castorice/States/Blink/Castorice.Blink.04.png", blink.FramePaths[^1], "Blink should reopen on frame four.");
        Assert.Equal(TimeSpan.FromSeconds(3), blink.MinScheduleDelay, "Blink min schedule should stay compatible.");
        Assert.Equal(TimeSpan.FromSeconds(7), blink.MaxScheduleDelay, "Blink max schedule should stay compatible.");
    }

    static void BuiltInCastoriceDefinesOptionalPettingAction()
    {
        Assert.True(BuiltInPetSkins.Castorice.TryGetAction(PetActionKind.Petting, out var petting), "Castorice should define petting without making it a required skin action.");
        Assert.Equal(8, petting.FramePaths.Count, "Petting should define eight authored frames.");
        Assert.Equal(TimeSpan.FromMilliseconds(80), petting.FrameInterval, "Petting should play once at 12.5 FPS.");
        Assert.Equal("Assets/Runtime/Castorice/States/Petting/Castorice.Petting.00.png", petting.FramePaths[0], "Petting paths should use the runtime convention.");
        Assert.Equal("Assets/Runtime/Castorice/States/Petting/Castorice.Petting.07.png", petting.FramePaths[^1], "Petting should end on frame seven.");
    }

    static void BuiltInPettingFramesArePackagedAndClean()
    {
        var workspace = FindWorkspaceRoot();
        var projectRoot = System.IO.Path.Combine(workspace, "src", "CastoPet");
        var runtimeRoot = System.IO.Path.Combine(projectRoot, "Assets", "Runtime", "Castorice", "States", "Petting");
        var frames = Enumerable.Range(0, 8)
            .Select(index => System.IO.Path.Combine(runtimeRoot, $"Castorice.Petting.{index:00}.png"))
            .ToArray();

        Assert.True(frames.All(File.Exists), "Built-in petting should include eight consecutive runtime PNGs.");
        foreach (var frame in frames)
        {
            using var bitmap = new Bitmap(frame);
            Assert.Equal(320, bitmap.Width, $"{System.IO.Path.GetFileName(frame)} should be 320 pixels wide.");
            Assert.Equal(320, bitmap.Height, $"{System.IO.Path.GetFileName(frame)} should be 320 pixels high.");
            Assert.True(bitmap.GetPixel(0, 0).A == 0, $"{System.IO.Path.GetFileName(frame)} should keep a transparent background.");

            var greenFringePixels = 0;
            for (var y = 0; y < bitmap.Height; y += 2)
            {
                for (var x = 0; x < bitmap.Width; x += 2)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    if (pixel.A > 8 && pixel.G > pixel.R + 40 && pixel.G > pixel.B + 40)
                    {
                        greenFringePixels++;
                    }
                }
            }

            Assert.True(greenFringePixels <= 2, $"{System.IO.Path.GetFileName(frame)} should not contain a visible cluster of green-key fringe pixels.");
        }

        using var idle = new Bitmap(System.IO.Path.Combine(projectRoot, "Assets", "Runtime", "Castorice", "States", "Idle", "Castorice.Idle.00.png"));
        using var pettingStart = new Bitmap(frames[0]);
        using var pettingEnd = new Bitmap(frames[^1]);
        Assert.True(CalculateAverageRgbaDelta(idle, pettingStart) < 35, "Petting should begin close enough to idle for a short authored reaction.");
        Assert.True(CalculateAverageRgbaDelta(idle, pettingEnd) < 35, "Petting should end close enough to idle to restore the passive loop cleanly.");

        var projectText = File.ReadAllText(System.IO.Path.Combine(projectRoot, "CastoPet.csproj"));
        Assert.Contains(projectText, @"Assets\Runtime\Castorice\**\*.png", "Petting frames should be covered by the runtime WPF resource glob.");

        using var temp = TempDirectory.Create();
        var petting = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Petting) with
        {
            FramePaths = frames,
        };
        var skin = BuiltInPetSkins.Castorice with
        {
            Actions = BuiltInPetSkins.Castorice.Actions
                .Where(action => action.Kind != PetActionKind.Petting)
                .Append(petting)
                .ToArray(),
        };
        var service = new AssetService(new LoggingService(new AppPaths(temp.Path)), skin);
        Assert.Equal(8, service.LoadPettingFrames().Count, "AssetService should load the complete built-in petting sequence.");
    }

    static void BuiltInCastoriceExpressionsAreOrderedSkinDefinitions()
    {
        var expressions = BuiltInPetSkins.Castorice.Expressions;

        Assert.Equal(8, expressions.Count, "Castorice should keep eight expression wheel items.");
        Assert.Equal("happy", expressions[0].Id, "First expression id should be stable.");
        Assert.Equal("Happy", expressions[0].Label, "First expression label should be stable.");
        Assert.Equal("Assets/Runtime/Castorice/Expressions/Castorice.Expression.Happy.png", expressions[0].ResourcePath, "First expression path should stay compatible.");
        Assert.Equal(6, expressions[0].TransitionFramePaths?.Count, "Each built-in expression should define six transition frames.");
        Assert.Equal("Assets/Runtime/Castorice/Expressions/Happy/Transition/Castorice.Expression.Happy.Transition.00.png", expressions[0].TransitionFramePaths?[0], "First expression transition frame should use the runtime convention.");
        Assert.Equal(TimeSpan.FromMilliseconds(1000d / 15d), expressions[0].TransitionFrameInterval, "Expression transitions should play at 15 FPS.");
        Assert.Equal("crying", expressions[^1].Id, "Last expression id should be stable.");
        Assert.Equal("Crying", expressions[^1].Label, "Last expression label should be stable.");
    }

    static void BuiltInCastoriceLoadsFromEmbeddedManifest()
    {
        const string resourceName = "CastoPet.Assets.Runtime.Castorice.skin.json";
        var assembly = typeof(BuiltInPetSkins).Assembly;
        Assert.True(
            assembly.GetManifestResourceNames().Contains(resourceName, StringComparer.Ordinal),
            "The built-in Castorice manifest should be embedded in the application assembly.");

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("The built-in Castorice manifest stream is missing.");
        using var reader = new StreamReader(stream);
        var manifestSkin = PetSkinManifestLoader.LoadFromJson(reader.ReadToEnd());

        Assert.Equal(BuiltInPetSkins.Castorice.Id, manifestSkin.Id, "The embedded manifest should define the built-in skin id.");
        Assert.Equal(BuiltInPetSkins.Castorice.Actions.Count, manifestSkin.Actions.Count, "The embedded manifest should define every built-in action.");
        Assert.Equal(BuiltInPetSkins.Castorice.Expressions.Count, manifestSkin.Expressions.Count, "The embedded manifest should define every built-in expression.");

        var workspace = FindWorkspaceRoot();
        var source = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Infrastructure", "Assets", "BuiltInPetSkins.cs"));
        Assert.False(source.Contains("new PetActionDefinition", StringComparison.Ordinal), "Built-in skin metadata should not be duplicated as action constructors.");
        Assert.False(source.Contains("CreateFramePaths", StringComparison.Ordinal), "Built-in frame lists should come from the manifest.");
    }

}
