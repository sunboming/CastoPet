namespace CastoPet.Core;

public sealed class PetSkinSelectionService
{
    private readonly LoggingService _logger;

    public PetSkinSelectionService(LoggingService logger)
    {
        _logger = logger;
    }

    public PetSkinDefinition LoadCurrentSkin(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.SkinManifestPath))
        {
            return BuiltInPetSkins.Castorice;
        }

        try
        {
            return PetSkinManifestLoader.LoadFromFile(settings.SkinManifestPath);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load configured skin manifest: {settings.SkinManifestPath}. Built-in Castorice will be used.", ex);
            return BuiltInPetSkins.Castorice;
        }
    }
}
