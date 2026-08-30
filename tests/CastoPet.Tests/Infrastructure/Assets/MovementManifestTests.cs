namespace CastoPet.Tests;

internal static partial class TestSuite
{
    static string UnifiedMovementJson() => """
        {
          "schemaVersion": 3, "id": "movement", "displayName": "Movement",
          "resourceRoot": "Resources", "defaultCharacter": "Default.png",
          "actions": [
            { "id": "idle", "kind": "idle", "frames": ["Idle/00.png"] },
            { "id": "move", "kind": "move", "movement": {
              "distancePerFrame": 10, "baseSpeedPixelsPerSecond": 90,
              "minSpeedPixelsPerSecond": 80, "maxSpeedPixelsPerSecond": 105,
              "leftFrames": ["Left/00.png"], "rightFrames": ["Right/00.png"]
            } },
            { "id": "blink", "kind": "blink", "frames": ["Blink/00.png"] }
          ]
        }
        """;

    static void UnifiedMovementLoadsWithoutGenericFrames()
    {
        var skin = PetSkinManifestLoader.LoadFromJson(UnifiedMovementJson());
        Assert.Equal(3, skin.Actions.Count, "Directional clips should belong to one movement action.");
        Assert.Equal(0, skin.GetRequiredAction(PetActionKind.Move).FramePaths.Count, "Generic fallback frames should be optional.");
        var written = PetSkinManifestWriter.ToJson(skin);
        using var json = System.Text.Json.JsonDocument.Parse(written);
        var move = json.RootElement.GetProperty("actions").EnumerateArray().Single(action => action.GetProperty("kind").GetString() == "move");
        Assert.Equal("Left/00.png", move.GetProperty("movement").GetProperty("leftFrames")[0].GetString(), "Left clips should round trip relative to the resource root.");
        Assert.Equal("Right/00.png", move.GetProperty("movement").GetProperty("rightFrames")[0].GetString(), "Right clips should round trip separately with shared settings.");
        Assert.Equal(3, PetSkinManifestLoader.LoadFromJson(written).Actions.Count, "Unified export should remain loadable.");
    }

    static void LegacyMovementMergesAndDropsRetiredTurns()
    {
        var skin = BuiltInPetSkins.Castorice;
        Assert.Equal(1, skin.Actions.Count(action => action.Kind.ToString().StartsWith("Move", StringComparison.Ordinal)), "Legacy left/right entries should merge into one Move action.");
        Assert.False(skin.Actions.Any(action => action.Kind.ToString().StartsWith("Turn", StringComparison.Ordinal)), "Retired turns should not enter the runtime model.");
        using var json = System.Text.Json.JsonDocument.Parse(PetSkinManifestWriter.ToJson(skin));
        Assert.Equal(3, json.RootElement.GetProperty("schemaVersion").GetInt32(), "Unified writer should emit schema version 3.");
        var move = json.RootElement.GetProperty("actions").EnumerateArray().Single(action => action.GetProperty("kind").GetString() == "move");
        var movement = move.GetProperty("movement");
        Assert.Equal(5, movement.GetProperty("leftFrames").GetArrayLength(), "All current left walking frames should remain.");
        Assert.Equal(7, movement.GetProperty("rightFrames").GetArrayLength(), "All current right walking frames should remain.");
        Assert.Equal(90d, movement.GetProperty("baseSpeedPixelsPerSecond").GetDouble(), "Legacy shared speed should remain effective.");
    }

    static void LegacyTurnImagesAreNotRequired()
    {
        using var temp = TempDirectory.Create();
        CreateExternalSkinResources(temp.Path);
        var path = WriteExternalSkinManifest(temp.Path);
        var json = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;
        json["actions"]!.AsArray().Add(System.Text.Json.Nodes.JsonNode.Parse("""
            { "id": "old-turn", "kind": "turn-left", "frames": ["Deleted/Turn.png"] }
            """));
        File.WriteAllText(path, json.ToJsonString());
        var skin = PetSkinManifestLoader.LoadFromFile(path);
        Assert.Equal(3, skin.Actions.Count, "Deleting obsolete turn files must not invalidate a legacy skin.");
    }

    static void LegacyDirectionalMovementRejectsConflictingSettings()
    {
        var json = System.Text.Json.Nodes.JsonNode.Parse(CreateExternalSkinJson("\"Idle/00.png\""))!;
        json["actions"]!.AsArray().Add(System.Text.Json.Nodes.JsonNode.Parse("""
            { "id": "left", "kind": "move-left", "frames": ["Left.png"], "distancePerFrame": 99 }
            """));
        var error = Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromJson(json.ToJsonString()));
        Assert.Contains(error.Message, "distancePerFrame", "Conflicts must identify the differing movement setting.");
    }

    static void UnifiedMovementEnforcesFrameAndTimingRules()
    {
        var json = System.Text.Json.Nodes.JsonNode.Parse(UnifiedMovementJson())!;
        var move = json["actions"]![1]!;
        move["frameIntervalMs"] = 80;
        Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromJson(json.ToJsonString()));
        move.AsObject().Remove("frameIntervalMs");
        move["movement"]!["leftFrames"] = new System.Text.Json.Nodes.JsonArray(
            Enumerable.Range(0, ExternalSkinResourceLimits.MaxFramesPerAction + 1)
                .Select(_ => (System.Text.Json.Nodes.JsonNode?)System.Text.Json.Nodes.JsonValue.Create("Left.png")).ToArray());
        Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromJson(json.ToJsonString()));
    }

    static void UnifiedMovementRejectsInvalidSharedSettings()
    {
        foreach (var (name, value) in new (string, double)[]
        {
            ("distancePerFrame", 0), ("baseSpeedPixelsPerSecond", -1),
            ("minSpeedPixelsPerSecond", 100), ("maxSpeedPixelsPerSecond", 85),
        })
        {
            var json = System.Text.Json.Nodes.JsonNode.Parse(UnifiedMovementJson())!;
            json["actions"]![1]!["movement"]![name] = value;
            Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromJson(json.ToJsonString()));
        }
    }

    static void UnifiedMovementRequiresCompleteClipsOrFallback()
    {
        var json = System.Text.Json.Nodes.JsonNode.Parse(UnifiedMovementJson())!;
        var move = json["actions"]![1]!;
        move["movement"]!.AsObject().Remove("leftFrames");
        Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromJson(json.ToJsonString()));
        move["frames"] = new System.Text.Json.Nodes.JsonArray("Fallback.png");
        var skin = PetSkinManifestLoader.LoadFromJson(json.ToJsonString());
        Assert.Equal(0, skin.GetRequiredAction(PetActionKind.Move).Movement!.LeftFramePaths.Count, "A missing direction should use the generic fallback.");
        Assert.Equal(1, skin.GetRequiredAction(PetActionKind.Move).FramePaths.Count, "The fallback should remain available.");
        move.AsObject().Remove("movement");
        Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromJson(json.ToJsonString()));
    }

    static void UnifiedMovementRejectsMisplacedAndRetiredMetadata()
    {
        var json = System.Text.Json.Nodes.JsonNode.Parse(UnifiedMovementJson())!;
        var actions = json["actions"]!.AsArray();
        actions[0]!["movement"] = actions[1]!["movement"]!.DeepClone();
        Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromJson(json.ToJsonString()));
        actions[0]!.AsObject().Remove("movement");
        actions[1]!["distancePerFrame"] = 10;
        Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromJson(json.ToJsonString()));
        actions[1]!.AsObject().Remove("distancePerFrame");
        foreach (var kind in new[] { "move-left", "moveRight", "turn-left", "turnRight" })
        {
            actions.Add(System.Text.Json.Nodes.JsonNode.Parse($$"""
                { "id": "retired", "kind": "{{kind}}", "frames": ["Unused.png"] }
                """));
            Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromJson(json.ToJsonString()));
            actions.RemoveAt(actions.Count - 1);
        }
    }

    static void LegacyMovementAliasesAndDefaultsRemainCompatible()
    {
        foreach (var version in new[] { 1, 2 })
        {
            var json = System.Text.Json.Nodes.JsonNode.Parse(CreateExternalSkinJson("\"Idle/00.png\""))!;
            json["schemaVersion"] = version;
            json["actions"]!.AsArray().Add(System.Text.Json.Nodes.JsonNode.Parse("""
                { "id": "left", "kind": "moveLeft", "frames": ["Left.png"] }
                """));
            var skin = PetSkinManifestLoader.LoadFromJson(json.ToJsonString());
            var move = skin.GetRequiredAction(PetActionKind.Move);
            Assert.Equal(new PetMovementSettings(), move.Movement!.Settings, "Legacy defaults must remain identical.");
            Assert.True(move.Movement.LeftFramePaths[0].EndsWith("Left.png", StringComparison.Ordinal), "Camel-case aliases should migrate.");
            Assert.Equal(0, move.Movement.RightFramePaths.Count, "An absent direction should retain fallback behavior.");
            Assert.Equal(3, skin.Actions.Count, "Legacy directional entries must not remain as extra actions.");
        }
    }

    static void UnifiedMovementPathsUseExternalSkinSafetyRules()
    {
        using var temp = TempDirectory.Create();
        var resources = System.IO.Path.Combine(temp.Path, "Resources");
        foreach (var path in new[] { "Default.png", "Idle/00.png", "Blink/00.png", "Left/00.png", "Right/00.png" })
        {
            WriteTestPng(System.IO.Path.Combine(resources, path));
        }
        var manifestPath = System.IO.Path.Combine(temp.Path, "skin.json");
        var json = System.Text.Json.Nodes.JsonNode.Parse(UnifiedMovementJson())!;
        File.WriteAllText(manifestPath, json.ToJsonString());
        var skin = PetSkinManifestLoader.LoadFromFile(manifestPath);
        var written = PetSkinManifestWriter.ToJson(skin);
        Assert.Contains(written, "Left/00.png", "Absolute runtime paths must export relative to the resource root.");
        File.WriteAllText(manifestPath, written);
        Assert.Equal(skin.GetRequiredAction(PetActionKind.Move).Movement!.LeftFramePaths[0],
            PetSkinManifestLoader.LoadFromFile(manifestPath).GetRequiredAction(PetActionKind.Move).Movement!.LeftFramePaths[0],
            "File-based directional paths must round trip.");
        foreach (var direction in new[] { "leftFrames", "rightFrames" })
        {
            foreach (var invalid in new[] { "../../outside.png", @"C:\outside.png", "https://example.test/frame.png", "Missing.png" })
            {
                var invalidJson = System.Text.Json.Nodes.JsonNode.Parse(UnifiedMovementJson())!;
                invalidJson["actions"]![1]!["movement"]![direction] = new System.Text.Json.Nodes.JsonArray(invalid);
                File.WriteAllText(manifestPath, invalidJson.ToJsonString());
                Assert.Throws<InvalidDataException>(() => PetSkinManifestLoader.LoadFromFile(manifestPath));
            }
        }
    }
}
