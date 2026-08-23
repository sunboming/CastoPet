namespace CastoPet.Tests;

internal static partial class TestSuite
{
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

}
