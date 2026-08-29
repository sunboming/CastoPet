namespace CastoPet.Tests;

internal static partial class TestSuite
{
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
        Assert.Equal("Skins/Custom/Idle/00.png", skin.GetRequiredAction(PetActionKind.Idle).FramePaths[0], "Action frames should resolve under resource root.");
        Assert.Equal(TimeSpan.FromMilliseconds(200), skin.GetRequiredAction(PetActionKind.Idle).FrameInterval, "Action frame interval should load.");
        Assert.Equal(10d, skin.GetRequiredAction(PetActionKind.Move).Movement!.Settings.DistancePerFrame, "Move distance should load.");
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

}
