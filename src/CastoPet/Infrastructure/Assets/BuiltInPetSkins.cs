using System.IO;

using CastoPet.Core.Skins;

namespace CastoPet.Infrastructure.Assets;

public static class BuiltInPetSkins
{
    private const string CastoriceManifestResourceName =
        "CastoPet.Assets.Runtime.Castorice.skin.json";

    public static readonly PetSkinDefinition Castorice = LoadCastorice();

    private static PetSkinDefinition LoadCastorice()
    {
        var assembly = typeof(BuiltInPetSkins).Assembly;
        using var stream = assembly.GetManifestResourceStream(CastoriceManifestResourceName)
            ?? throw new InvalidOperationException(
                $"Built-in skin manifest {CastoriceManifestResourceName} is missing.");
        using var reader = new StreamReader(stream);

        try
        {
            return PetSkinManifestLoader.LoadFromJson(reader.ReadToEnd());
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidOperationException("Built-in Castorice skin manifest is invalid.", ex);
        }
    }
}
