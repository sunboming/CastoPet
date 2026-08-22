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
        Assert.Equal("Assets/Runtime/Castorice/States/InputReactive/Castorice.InputReactive.Base.png", skin.InputReactiveBasePath, "Input reactive path should use runtime root.");
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
            skin.InputReactiveBasePath,
        };
        paths.AddRange(skin.Actions.SelectMany(action => action.FramePaths));
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
        Assert.Equal(10d, move.DistancePerFrame, "Move distance per frame should stay compatible.");
        Assert.Equal(90d, move.BaseSpeedPixelsPerSecond, "Move base speed should stay compatible.");
        Assert.Equal(80d, move.MinSpeedPixelsPerSecond, "Move min speed should stay compatible.");
        Assert.Equal(105d, move.MaxSpeedPixelsPerSecond, "Move max speed should stay compatible.");
    }

    static void BuiltInCastoriceDefinesSeparateDirectionalMovementActions()
    {
        var moveLeft = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.MoveLeft);
        var moveRight = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.MoveRight);
        var turnLeft = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.TurnLeft);
        var turnRight = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.TurnRight);

        Assert.Equal(5, moveLeft.FramePaths.Count, "Left movement should omit the two frames with inconsistent eye direction.");
        Assert.Equal(7, moveRight.FramePaths.Count, "Right movement should omit the excessive three-quarter-view frame from playback.");
        Assert.Equal(6, turnLeft.FramePaths.Count, "Left turning should use six separately authored frames.");
        Assert.Equal(6, turnRight.FramePaths.Count, "Right turning should use six separately authored frames.");
        Assert.Equal("Assets/Runtime/Castorice/States/MoveLeft/Castorice.MoveLeft.01.png", moveLeft.FramePaths[0], "Left movement should begin with the stable side-facing sequence.");
        Assert.Equal("Assets/Runtime/Castorice/States/MoveLeft/Castorice.MoveLeft.05.png", moveLeft.FramePaths[^1], "Left movement should end before the eye direction changes.");
        Assert.Equal("Assets/Runtime/Castorice/States/MoveRight/Castorice.MoveRight.01.png", moveRight.FramePaths[0], "Right movement should begin with the stable side-facing sequence.");
        Assert.Equal(TimeSpan.FromMilliseconds(41.66666666666667), turnLeft.FrameInterval, "Turn timing should preserve the extracted 24 FPS cadence.");
        Assert.Equal(TimeSpan.FromMilliseconds(41.66666666666667), turnRight.FrameInterval, "Both physical turns should use the same cadence.");
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
            "assets/runtime/castorice/states/turnleft/castorice.turnleft.00.png",
            "assets/runtime/castorice/states/turnright/castorice.turnright.00.png",
            "assets/runtime/castorice/states/petting/castorice.petting.00.png",
            "assets/runtime/castorice/states/inputreactive/castorice.inputreactive.base.png",
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

        foreach (var path in previewOnly.Take(4))
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

    static void PetSkinManifestLoadsJsonResourcePaths()
    {
        var skin = PetSkinManifestLoader.LoadFromJson("""
            {
              "schemaVersion": 1,
              "id": "custom",
              "displayName": "Custom Skin",
              "resourceRoot": "Skins/Custom",
              "defaultCharacter": "Default.png",
              "draggingCharacter": "States/Dragging.png",
              "inputReactiveBase": "Input/Base.png",
              "actions": [
                {
                  "id": "idle",
                  "kind": "idle",
                  "frameIntervalMs": 200,
                  "frames": ["Idle/00.png", "Idle/01.png"]
                },
                {
                  "id": "move",
                  "kind": "move",
                  "distancePerFrame": 10,
                  "baseSpeedPixelsPerSecond": 90,
                  "minSpeedPixelsPerSecond": 80,
                  "maxSpeedPixelsPerSecond": 105,
                  "frames": ["Move/00.png"]
                },
                {
                  "id": "blink",
                  "kind": "blink",
                  "frameIntervalMs": 90,
                  "minScheduleDelayMs": 3000,
                  "maxScheduleDelayMs": 7000,
                  "frames": ["Blink/00.png"]
                }
              ],
              "expressions": {
                "Happy": "Expressions/Happy.png"
              }
            }
            """);

        Assert.Equal("custom", skin.Id, "Manifest id should load.");
        Assert.Equal("Custom Skin", skin.DisplayName, "Manifest display name should load.");
        Assert.Equal("Skins/Custom", skin.ResourceRoot, "Manifest resource root should load.");
        Assert.Equal("Skins/Custom/Default.png", skin.DefaultCharacterPath, "JSON manifest paths should resolve under resource root.");
        Assert.Equal("Skins/Custom/States/Dragging.png", skin.DraggingCharacterPath, "Optional dragging path should resolve under resource root.");
        Assert.Equal("Skins/Custom/Input/Base.png", skin.InputReactiveBasePath, "Optional input base path should resolve under resource root.");
        Assert.Equal("Skins/Custom/Idle/00.png", skin.GetRequiredAction(PetActionKind.Idle).FramePaths[0], "Action frames should resolve under resource root.");
        Assert.Equal(TimeSpan.FromMilliseconds(200), skin.GetRequiredAction(PetActionKind.Idle).FrameInterval, "Action frame interval should load.");
        Assert.Equal(10d, skin.GetRequiredAction(PetActionKind.Move).DistancePerFrame, "Move distance should load.");
        Assert.Equal(TimeSpan.FromMilliseconds(3000), skin.GetRequiredAction(PetActionKind.Blink).MinScheduleDelay, "Blink min schedule should load.");
        Assert.Equal("Happy", skin.Expressions[0].Label, "Expression labels should load.");
        Assert.Equal("Skins/Custom/Expressions/Happy.png", skin.Expressions[0].ResourcePath, "Expression paths should resolve under resource root.");
    }

    static void PetSkinManifestLoadsPerFrameActionDurations()
    {
        var skin = PetSkinManifestLoader.LoadFromJson("""
            {
              "schemaVersion": 2,
              "id": "irregular",
              "displayName": "Irregular",
              "defaultCharacter": "Default.png",
              "actions": [
                {
                  "id": "idle",
                  "kind": "idle",
                  "frameIntervalMs": 100,
                  "frameDurationsMs": [240, null, 60],
                  "frames": ["Idle/00.png", "Idle/01.png", "Idle/02.png"]
                },
                { "id": "move", "kind": "move", "frames": ["Move.png"] },
                { "id": "blink", "kind": "blink", "frames": ["Blink.png"] }
              ]
            }
            """);

        var idle = skin.GetRequiredAction(PetActionKind.Idle);
        Assert.Equal(3, idle.FrameDurations?.Count, "Per-frame durations should align with action frames.");
        Assert.Equal(TimeSpan.FromMilliseconds(240), idle.FrameDurations?[0], "A frame should load its authored duration override.");
        Assert.Equal(null, idle.FrameDurations?[1], "A null duration should preserve default-interval fallback semantics.");
        Assert.Equal(TimeSpan.FromMilliseconds(60), idle.FrameDurations?[2], "Later frame overrides should retain their order.");
    }

    static void PetSkinManifestLoadsExpressionTransitionMetadata()
    {
        var skin = PetSkinManifestLoader.LoadFromJson("""
            {
              "schemaVersion": 2,
              "id": "animated",
              "displayName": "Animated Skin",
              "resourceRoot": "Skins/Animated",
              "defaultCharacter": "Default.png",
              "actions": [
                { "id": "idle", "kind": "idle", "frames": ["Idle/00.png"] },
                { "id": "move", "kind": "move", "frames": ["Move/00.png"] },
                { "id": "blink", "kind": "blink", "frames": ["Blink/00.png"] }
              ],
              "expressions": {
                "Happy": {
                  "image": "Expressions/Happy.png",
                  "transitionFrames": [
                    "Expressions/Happy/Transition/00.png",
                    "Expressions/Happy/Transition/01.png"
                  ],
                  "transitionFrameIntervalMs": 66.6667
                }
              }
            }
            """);

        var expression = skin.Expressions.Single();
        Assert.Equal("Skins/Animated/Expressions/Happy.png", expression.ResourcePath, "Expression image should resolve under resource root.");
        Assert.Equal(2, expression.TransitionFramePaths?.Count, "Expression transition frames should load.");
        Assert.Equal("Skins/Animated/Expressions/Happy/Transition/00.png", expression.TransitionFramePaths?[0], "Transition frame path should resolve under resource root.");
        Assert.Equal(TimeSpan.FromMilliseconds(66.6667), expression.TransitionFrameInterval, "Expression transition interval should load.");
    }

    static void PetSkinManifestLoadsFilePathsRelativeToManifest()
    {
        using var temp = TempDirectory.Create();
        var manifestDirectory = System.IO.Path.Combine(temp.Path, "Pack");
        Directory.CreateDirectory(manifestDirectory);
        WriteTestPng(System.IO.Path.Combine(manifestDirectory, "Resources", "Default.png"));
        WriteTestPng(System.IO.Path.Combine(manifestDirectory, "Resources", "Idle", "00.png"));
        WriteTestPng(System.IO.Path.Combine(manifestDirectory, "Resources", "Move", "00.png"));
        WriteTestPng(System.IO.Path.Combine(manifestDirectory, "Resources", "Blink", "00.png"));
        var manifestPath = System.IO.Path.Combine(manifestDirectory, "skin.json");
        File.WriteAllText(manifestPath, """
            {
              "schemaVersion": 1,
              "id": "file-skin",
              "displayName": "File Skin",
              "resourceRoot": "Resources",
              "defaultCharacter": "Default.png",
              "actions": [
                { "id": "idle", "kind": "idle", "frames": ["Idle/00.png"] },
                { "id": "move", "kind": "move", "frames": ["Move/00.png"] },
                { "id": "blink", "kind": "blink", "frames": ["Blink/00.png"] }
              ]
            }
            """);

        var skin = PetSkinManifestLoader.LoadFromFile(manifestPath);
        var expectedRoot = System.IO.Path.Combine(manifestDirectory, "Resources");

        Assert.Equal(System.IO.Path.Combine(expectedRoot, "Default.png"), skin.DefaultCharacterPath, "File manifest paths should resolve relative to manifest directory.");
        Assert.Equal(System.IO.Path.Combine(expectedRoot, "Idle", "00.png"), skin.GetRequiredAction(PetActionKind.Idle).FramePaths[0], "File action paths should resolve relative to manifest directory.");
    }

    static void ExternalSkinRejectsPathsOutsideItsResourceRoot()
    {
        using var temp = TempDirectory.Create();
        var manifestDirectory = System.IO.Path.Combine(temp.Path, "Skin");
        CreateExternalSkinResources(manifestDirectory);
        var outsidePath = System.IO.Path.Combine(temp.Path, "outside.png");
        WriteTestPng(outsidePath);

        var traversalManifest = WriteExternalSkinManifest(manifestDirectory, idleFrame: "../../outside.png");
        var traversal = Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromFile(traversalManifest));
        Assert.Contains(traversal.Message, "resource root", "Parent traversal should identify the containment boundary.");

        var resourceRootManifest = WriteExternalSkinManifest(manifestDirectory, resourceRoot: "..");
        var resourceRootEscape = Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromFile(resourceRootManifest));
        Assert.Contains(resourceRootEscape.Message, "resource root", "The declared resource root must remain below the manifest directory.");

        var rootedManifest = WriteExternalSkinManifest(
            manifestDirectory,
            idleFrame: outsidePath);
        var rooted = Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromFile(rootedManifest));
        Assert.Contains(rooted.Message, "relative", "Rooted image paths should be rejected explicitly.");

        var uncManifest = WriteExternalSkinManifest(manifestDirectory, idleFrame: @"\\server\share\frame.png");
        var unc = Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromFile(uncManifest));
        Assert.Contains(unc.Message, "relative", "UNC image paths should never trigger network access.");

        var uncManifestPath = Assert.Throws<InvalidDataException>(() =>
            PetSkinManifestLoader.LoadFromFile(@"\\server\share\skin.json"));
        Assert.Contains(uncManifestPath.Message, "UNC", "UNC manifest paths should fail before any file access.");
    }

    static void ExternalSkinRejectsReparsePointEscapes()
    {
        var root = System.IO.Path.GetFullPath(@"C:\Skin\Resources");
        var candidate = System.IO.Path.Combine(root, "Linked", "frame.png");
        var containsReparsePoint = ExternalSkinPathPolicy.ContainsReparsePoint(
            root,
            candidate,
            path => path.EndsWith("Linked", StringComparison.OrdinalIgnoreCase)
                ? FileAttributes.Directory | FileAttributes.ReparsePoint
                : FileAttributes.Normal);

        Assert.True(containsReparsePoint, "A junction or symbolic-link segment should invalidate the resource path.");
    }

    static void ExternalSkinEnforcesManifestAndFrameBudgets()
    {
        using var temp = TempDirectory.Create();
        var manifestDirectory = System.IO.Path.Combine(temp.Path, "Skin");
        CreateExternalSkinResources(manifestDirectory);
        var manifestPath = WriteExternalSkinManifest(manifestDirectory);
        var currentLength = new FileInfo(manifestPath).Length;
        File.AppendAllText(
            manifestPath,
            new string(' ', checked((int)(ExternalSkinResourceLimits.MaxManifestBytes - currentLength + 1))));

        var oversized = Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromFile(manifestPath));
        Assert.Contains(oversized.Message, "manifest", "Oversized manifest files should fail before JSON parsing.");

        var frameList = string.Join(",", Enumerable
            .Range(0, ExternalSkinResourceLimits.MaxFramesPerAction + 1)
            .Select(_ => "\"Idle/00.png\""));
        File.WriteAllText(manifestPath, CreateExternalSkinJson(frameList));
        var tooManyFrames = Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromFile(manifestPath));
        Assert.Contains(tooManyFrames.Message, "frames", "An action should have a bounded frame count.");
    }

    static void ExternalSkinValidatesPngFilesAndDimensions()
    {
        using var temp = TempDirectory.Create();
        var manifestDirectory = System.IO.Path.Combine(temp.Path, "Skin");
        CreateExternalSkinResources(manifestDirectory);

        var jpgPath = System.IO.Path.Combine(manifestDirectory, "Resources", "Default.jpg");
        File.WriteAllText(jpgPath, "not an image");
        var wrongExtensionManifest = WriteExternalSkinManifest(manifestDirectory, defaultCharacter: "Default.jpg");
        var wrongExtension = Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromFile(wrongExtensionManifest));
        Assert.Contains(wrongExtension.Message, "PNG", "External skin images should use the supported PNG format only.");

        var invalidPngPath = System.IO.Path.Combine(manifestDirectory, "Resources", "Invalid.png");
        File.WriteAllText(invalidPngPath, "not a PNG");
        var invalidPngManifest = WriteExternalSkinManifest(manifestDirectory, defaultCharacter: "Invalid.png");
        var invalidPng = Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromFile(invalidPngManifest));
        Assert.Contains(invalidPng.Message, "valid PNG", "A PNG extension alone should not bypass header validation.");

        var largePath = System.IO.Path.Combine(manifestDirectory, "Resources", "Large.png");
        using (var stream = new FileStream(largePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.SetLength(ExternalSkinResourceLimits.MaxImageFileBytes + 1);
        }
        var largeManifest = WriteExternalSkinManifest(manifestDirectory, defaultCharacter: "Large.png");
        var tooLarge = Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromFile(largeManifest));
        Assert.Contains(tooLarge.Message, "bytes", "External PNG files should have a compressed-size budget.");

        var dimensionsPath = System.IO.Path.Combine(manifestDirectory, "Resources", "Dimensions.png");
        WritePngHeader(dimensionsPath, ExternalSkinResourceLimits.MaxImageDimension + 1, 1);
        var dimensionsManifest = WriteExternalSkinManifest(manifestDirectory, defaultCharacter: "Dimensions.png");
        var dimensions = Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromFile(dimensionsManifest));
        Assert.Contains(dimensions.Message, "dimensions", "External PNG dimensions should be validated before WPF decoding.");
    }

    static void PetSkinManifestRequiresCoreActions()
    {
        var ex = Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromJson("""
            {
              "schemaVersion": 1,
              "id": "broken",
              "displayName": "Broken",
              "defaultCharacter": "Default.png",
              "actions": [
                { "id": "idle", "kind": "idle", "frames": ["Idle/00.png"] }
              ]
            }
            """));

        Assert.Contains(ex.Message, "Move", "Manifest validation should identify missing move action.");
    }

    static void PetSkinManifestRejectsDuplicateActions()
    {
        var duplicateId = Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromJson("""
            {
              "schemaVersion": 2,
              "id": "duplicate-id",
              "displayName": "Duplicate Id",
              "defaultCharacter": "Default.png",
              "actions": [
                { "id": "shared", "kind": "idle", "frames": ["Idle.png"] },
                { "id": "shared", "kind": "move", "frames": ["Move.png"] },
                { "id": "blink", "kind": "blink", "frames": ["Blink.png"] }
              ]
            }
            """));
        Assert.Contains(duplicateId.Message, "Duplicate action id", "Manifest validation should identify duplicate action ids.");

        var duplicateKind = Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromJson("""
            {
              "schemaVersion": 2,
              "id": "duplicate-kind",
              "displayName": "Duplicate Kind",
              "defaultCharacter": "Default.png",
              "actions": [
                { "id": "idle-one", "kind": "idle", "frames": ["Idle-1.png"] },
                { "id": "idle-two", "kind": "idle", "frames": ["Idle-2.png"] },
                { "id": "move", "kind": "move", "frames": ["Move.png"] },
                { "id": "blink", "kind": "blink", "frames": ["Blink.png"] }
              ]
            }
            """));
        Assert.Contains(duplicateKind.Message, "Duplicate action kind", "Manifest validation should identify duplicate action kinds.");
    }

    static void PetSkinManifestRejectsInvalidActionMetadata()
    {
        var emptyFrames = Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromJson("""
            {
              "schemaVersion": 2,
              "id": "empty-frames",
              "displayName": "Empty Frames",
              "defaultCharacter": "Default.png",
              "actions": [
                { "id": "idle", "kind": "idle", "frames": [] },
                { "id": "move", "kind": "move", "frames": ["Move.png"] },
                { "id": "blink", "kind": "blink", "frames": ["Blink.png"] }
              ]
            }
            """));
        Assert.Contains(emptyFrames.Message, "must define at least one frame", "Manifest actions should not accept an empty frame list.");

        var invalidMove = Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromJson("""
            {
              "schemaVersion": 2,
              "id": "invalid-move",
              "displayName": "Invalid Move",
              "defaultCharacter": "Default.png",
              "actions": [
                { "id": "idle", "kind": "idle", "frames": ["Idle.png"] },
                { "id": "move", "kind": "move", "distancePerFrame": -1, "frames": ["Move.png"] },
                { "id": "blink", "kind": "blink", "frames": ["Blink.png"] }
              ]
            }
            """));
        Assert.Contains(invalidMove.Message, "distancePerFrame", "Manifest actions should reject non-positive movement distance.");

        var invalidSchedule = Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromJson("""
            {
              "schemaVersion": 2,
              "id": "invalid-schedule",
              "displayName": "Invalid Schedule",
              "defaultCharacter": "Default.png",
              "actions": [
                { "id": "idle", "kind": "idle", "frames": ["Idle.png"] },
                { "id": "move", "kind": "move", "frames": ["Move.png"] },
                { "id": "blink", "kind": "blink", "minScheduleDelayMs": 7000, "maxScheduleDelayMs": 3000, "frames": ["Blink.png"] }
              ]
            }
            """));
        Assert.Contains(invalidSchedule.Message, "schedule delay range", "Manifest actions should reject an inverted schedule range.");
    }

    static void PetSkinManifestRejectsInvalidPerFrameDurations()
    {
        var mismatched = Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromJson("""
            {
              "schemaVersion": 2,
              "id": "duration-count",
              "displayName": "Duration Count",
              "defaultCharacter": "Default.png",
              "actions": [
                { "id": "idle", "kind": "idle", "frameDurationsMs": [100], "frames": ["Idle-0.png", "Idle-1.png"] },
                { "id": "move", "kind": "move", "frames": ["Move.png"] },
                { "id": "blink", "kind": "blink", "frames": ["Blink.png"] }
              ]
            }
            """));
        Assert.Contains(mismatched.Message, "frameDurationsMs", "Duration count validation should identify the action property.");

        var nonPositive = Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromJson("""
            {
              "schemaVersion": 2,
              "id": "invalid-duration",
              "displayName": "Invalid Duration",
              "defaultCharacter": "Default.png",
              "actions": [
                { "id": "idle", "kind": "idle", "frameDurationsMs": [0], "frames": ["Idle.png"] },
                { "id": "move", "kind": "move", "frames": ["Move.png"] },
                { "id": "blink", "kind": "blink", "frames": ["Blink.png"] }
              ]
            }
            """));
        Assert.Contains(nonPositive.Message, "frameDurationsMs[0]", "Duration validation should identify the invalid frame index.");
    }

    static void PetSkinManifestWriterEmitsLoadableJson()
    {
        using var temp = TempDirectory.Create();
        var manifestPath = System.IO.Path.Combine(temp.Path, "skin.json");

        PetSkinManifestWriter.WriteToFile(manifestPath, BuiltInPetSkins.Castorice);
        var skin = PetSkinManifestLoader.LoadFromJson(File.ReadAllText(manifestPath));

        Assert.Equal(BuiltInPetSkins.Castorice.Id, skin.Id, "Written manifest should preserve skin id.");
        Assert.Equal(BuiltInPetSkins.Castorice.DisplayName, skin.DisplayName, "Written manifest should preserve display name.");
        Assert.Equal(BuiltInPetSkins.Castorice.DefaultCharacterPath, skin.DefaultCharacterPath, "Written manifest should reload default character path.");
        Assert.Equal(BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Idle).FramePaths[0], skin.GetRequiredAction(PetActionKind.Idle).FramePaths[0], "Written manifest should reload action frames.");
    }

    static void PetSkinManifestWriterRoundTripsPerFrameDurations()
    {
        var skin = PetSkinManifestLoader.LoadFromJson("""
            {
              "schemaVersion": 2,
              "id": "round-trip-durations",
              "displayName": "Round Trip Durations",
              "defaultCharacter": "Default.png",
              "actions": [
                { "id": "idle", "kind": "idle", "frameIntervalMs": 100, "frameDurationsMs": [220, null], "frames": ["Idle-0.png", "Idle-1.png"] },
                { "id": "move", "kind": "move", "frames": ["Move.png"] },
                { "id": "blink", "kind": "blink", "frames": ["Blink.png"] }
              ]
            }
            """);

        var json = PetSkinManifestWriter.ToJson(skin);
        Assert.Contains(json, "\"frameDurationsMs\": [", "Manifest writer should emit authored frame durations.");
        var reloaded = PetSkinManifestLoader.LoadFromJson(json).GetRequiredAction(PetActionKind.Idle);
        Assert.Equal(TimeSpan.FromMilliseconds(220), reloaded.FrameDurations?[0], "Written override duration should reload.");
        Assert.Equal(null, reloaded.FrameDurations?[1], "Written null duration should keep fallback semantics.");
    }

    static void PetSkinManifestWriterStoresPathsRelativeToResourceRoot()
    {
        using var temp = TempDirectory.Create();
        var manifestPath = System.IO.Path.Combine(temp.Path, "skin.json");

        PetSkinManifestWriter.WriteToFile(manifestPath, BuiltInPetSkins.Castorice);
        var json = File.ReadAllText(manifestPath);

        Assert.Contains(json, @"""resourceRoot"": ""Assets/Runtime/Castorice""", "Written manifest should keep the runtime resource root.");
        Assert.Contains(json, @"""defaultCharacter"": ""Castorice.png""", "Default character should be stored relative to resource root.");
        Assert.Contains(json, @"""States/Idle/Castorice.Idle.00.png""", "Action frame paths should be stored relative to resource root.");
    }

    static void PetSkinManifestRoundTripsOptionalPettingAction()
    {
        var skin = PetSkinManifestLoader.LoadFromJson("""
            {
              "schemaVersion": 2,
              "id": "pettable",
              "displayName": "Pettable",
              "resourceRoot": "Skins/Pettable",
              "defaultCharacter": "Default.png",
              "actions": [
                { "id": "idle", "kind": "idle", "frames": ["Idle/00.png"] },
                { "id": "move", "kind": "move", "frames": ["Move/00.png"] },
                { "id": "blink", "kind": "blink", "frames": ["Blink/00.png"] },
                { "id": "petting", "kind": "petting", "frameIntervalMs": 80, "frames": ["Petting/00.png", "Petting/01.png"] }
              ]
            }
            """);

        Assert.True(skin.TryGetAction(PetActionKind.Petting, out var petting), "Manifest should load optional petting actions.");
        Assert.Equal("Skins/Pettable/Petting/00.png", petting.FramePaths[0], "Petting paths should resolve under the resource root.");
        Assert.Equal(TimeSpan.FromMilliseconds(80), petting.FrameInterval, "Petting frame interval should load.");

        var json = PetSkinManifestWriter.ToJson(skin);
        Assert.Contains(json, @"""kind"": ""petting""", "Manifest writer should preserve optional petting actions.");
        var reloaded = PetSkinManifestLoader.LoadFromJson(json);
        Assert.True(reloaded.TryGetAction(PetActionKind.Petting, out _), "Written petting actions should remain loadable.");
    }

    static void PetSkinSelectionDefaultsToBuiltInSkin()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var logger = new LoggingService(paths);
        var service = new PetSkinSelectionService(logger);

        var skin = service.LoadCurrentSkin(AppSettings.Default);

        Assert.Equal(BuiltInPetSkins.Castorice, skin, "No configured manifest should use the built-in skin.");
    }

    static void PetSkinSelectionLoadsConfiguredManifest()
    {
        using var temp = TempDirectory.Create();
        var manifestDirectory = System.IO.Path.Combine(temp.Path, "CustomSkin");
        Directory.CreateDirectory(manifestDirectory);
        WriteTestPng(System.IO.Path.Combine(manifestDirectory, "Resources", "Default.png"));
        WriteTestPng(System.IO.Path.Combine(manifestDirectory, "Resources", "Idle", "00.png"));
        WriteTestPng(System.IO.Path.Combine(manifestDirectory, "Resources", "Move", "00.png"));
        WriteTestPng(System.IO.Path.Combine(manifestDirectory, "Resources", "Blink", "00.png"));
        var manifestPath = System.IO.Path.Combine(manifestDirectory, "skin.json");
        File.WriteAllText(manifestPath, """
            {
              "schemaVersion": 1,
              "id": "configured",
              "displayName": "Configured Skin",
              "resourceRoot": "Resources",
              "defaultCharacter": "Default.png",
              "actions": [
                { "id": "idle", "kind": "idle", "frames": ["Idle/00.png"] },
                { "id": "move", "kind": "move", "frames": ["Move/00.png"] },
                { "id": "blink", "kind": "blink", "frames": ["Blink/00.png"] }
              ]
            }
            """);
        var paths = new AppPaths(temp.Path);
        var logger = new LoggingService(paths);
        var service = new PetSkinSelectionService(logger);

        var skin = service.LoadCurrentSkin(new AppSettings { SkinManifestPath = manifestPath });

        Assert.Equal("configured", skin.Id, "Configured manifest should load as the active skin.");
        Assert.Equal(System.IO.Path.Combine(manifestDirectory, "Resources", "Default.png"), skin.DefaultCharacterPath, "Configured manifest paths should resolve from the manifest.");
    }

    static void PetSkinSelectionFallsBackWhenManifestFails()
    {
        using var temp = TempDirectory.Create();
        var paths = new AppPaths(temp.Path);
        var logger = new LoggingService(paths);
        var service = new PetSkinSelectionService(logger);
        var missingManifest = System.IO.Path.Combine(temp.Path, "Missing", "skin.json");

        var skin = service.LoadCurrentSkin(new AppSettings { SkinManifestPath = missingManifest });

        Assert.Equal(BuiltInPetSkins.Castorice, skin, "Failed external manifest load should fall back to the built-in skin.");
        var logText = File.ReadAllText(paths.LogFile);
        Assert.Contains(logText, "Failed to load configured skin manifest", "Fallback should log the manifest load failure.");
    }

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

    static void BuiltInIdleActionDefinesEightAuthoredRateFramePaths()
    {
        var idle = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Idle);

        Assert.Equal(8, idle.FramePaths.Count, "Idle should use eight frames.");
        Assert.Equal(TimeSpan.FromMilliseconds(125), idle.FrameInterval, "Idle frames should advance at the authored 8 FPS rate.");
        Assert.Equal("Assets/Runtime/Castorice/States/Idle/Castorice.Idle.00.png", idle.FramePaths[0], "First idle frame path should be zero padded.");
        Assert.Equal("Assets/Runtime/Castorice/States/Idle/Castorice.Idle.07.png", idle.FramePaths[^1], "Last idle frame path should be zero padded.");
    }

    static void IdleFrameDiagnosticsReadAllPackagedFrames()
    {
        var diagnostics = ReadIdleFrameDiagnostics();

        Assert.Equal(BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Idle).FramePaths.Count, diagnostics.Count, "Diagnostics should include all idle frames.");
        Assert.True(diagnostics.All(frame => frame.Width == AssetService.CharacterDecodePixelWidth), "Idle frames should keep the display width.");
        Assert.True(diagnostics.All(frame => frame.Height == AssetService.CharacterDecodePixelWidth), "Idle frames should keep the display height.");
        Assert.True(diagnostics.All(frame => frame.Bounds.Width > 0 && frame.Bounds.Height > 0), "Idle frames should have visible alpha bounds.");
        Assert.True(diagnostics.Max(frame => frame.Bounds.Bottom) - diagnostics.Min(frame => frame.Bounds.Bottom) <= 1, "Idle frame bottom edges should stay anchored.");
        Assert.True(diagnostics.Max(frame => frame.CenterX) - diagnostics.Min(frame => frame.CenterX) <= 1.0, "Idle frame centers should stay horizontally anchored.");
        Assert.Equal("Castorice.Idle.07.png", diagnostics[^1].Name, "Diagnostics should preserve frame order.");
    }

    static void BuiltInBlinkActionDefinesRandomBlinkFrames()
    {
        var blink = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Blink);

        Assert.Equal(5, blink.FramePaths.Count, "Blink should use five frames.");
        Assert.Equal(TimeSpan.FromMilliseconds(45), blink.FrameInterval, "Blink frames should advance quickly.");
        Assert.Equal(TimeSpan.FromSeconds(3), blink.MinScheduleDelay, "Blink should not repeat too frequently.");
        Assert.Equal(TimeSpan.FromSeconds(7), blink.MaxScheduleDelay, "Blink should remain occasional.");
        Assert.Equal("Assets/Runtime/Castorice/States/Blink/Castorice.Blink.00.png", blink.FramePaths[0], "First blink frame path should be zero padded.");
        Assert.Equal("Assets/Runtime/Castorice/States/Blink/Castorice.Blink.04.png", blink.FramePaths[^1], "Last blink frame path should be zero padded.");
    }

    static void BuiltInMoveActionDefinesEightDistanceDrivenPaths()
    {
        var move = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Move);

        Assert.Equal(8, move.FramePaths.Count, "Move should use eight frames.");
        Assert.Equal(10d, move.DistancePerFrame, "Move frames should advance by travel distance.");
        Assert.Equal("Assets/Runtime/Castorice/States/Move/Castorice.Move.00.png", move.FramePaths[0], "First move frame path should be zero padded.");
        Assert.Equal("Assets/Runtime/Castorice/States/Move/Castorice.Move.07.png", move.FramePaths[^1], "Last move frame path should be zero padded.");
    }

    static void MoveFramePathsUseAppResources()
    {
        var move = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Move);

        for (var index = 0; index < move.FramePaths.Count; index++)
        {
            Assert.Equal($"Assets/Runtime/Castorice/States/Move/Castorice.Move.{index:00}.png", move.FramePaths[index], "Move frame should use the resource path convention.");
        }
    }

    static void MoveSpeedConstantsStayInSmoothRange()
    {
        var move = BuiltInPetSkins.Castorice.GetRequiredAction(PetActionKind.Move);

        Assert.Equal(90d, move.BaseSpeedPixelsPerSecond, "Move speed should have a stable base.");
        Assert.Equal(80d, move.MinSpeedPixelsPerSecond, "Move speed lower bound should stay near the base.");
        Assert.Equal(105d, move.MaxSpeedPixelsPerSecond, "Move speed upper bound should stay near the base.");
    }
}
