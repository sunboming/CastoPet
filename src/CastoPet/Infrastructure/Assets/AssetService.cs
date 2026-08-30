using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;

using CastoPet.Core.Animation;
using CastoPet.Core.Skins;
using CastoPet.Infrastructure.Diagnostics;

namespace CastoPet.Infrastructure.Assets;

public sealed class AssetService
{
    public const int CharacterDecodePixelWidth = 320;

    private readonly LoggingService _logger;

    public AssetService(LoggingService logger)
        : this(logger, BuiltInPetSkins.Castorice)
    {
    }

    public AssetService(LoggingService logger, PetSkinDefinition skin)
    {
        _logger = logger;
        Skin = skin;
    }

    public PetSkinDefinition Skin { get; }

    public BitmapImage LoadDefaultCharacter()
    {
        return LoadCharacter(Skin.DefaultCharacterPath, "Default character");
    }

    public IReadOnlyList<ImageSource> LoadIdleFrames()
    {
        return LoadActionFrames(PetActionKind.Idle, "Idle frames");
    }

    public IReadOnlyList<ImageSource> LoadBlinkFrames()
    {
        return LoadActionFrames(PetActionKind.Blink, "Blink frames");
    }

    public static string FormatLoadFailureMessage(string resourceGroup, string resourcePath)
    {
        return $"Failed to load {resourceGroup}: {resourcePath}.";
    }

    private IReadOnlyList<ImageSource> LoadActionFrames(PetActionKind kind, string resourceGroup)
    {
        return Skin
            .GetRequiredAction(kind)
            .FramePaths
            .Select(path => LoadCharacter(path, resourceGroup))
            .ToArray();
    }

    private BitmapImage LoadCharacter(string resourcePath, string resourceGroup)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = CharacterDecodePixelWidth;
            image.UriSource = CreateImageUri(resourcePath);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex)
        {
            _logger.Error(FormatLoadFailureMessage(resourceGroup, resourcePath), ex);
            throw;
        }
    }

    private static Uri CreateImageUri(string resourcePath)
    {
        return Path.IsPathFullyQualified(resourcePath)
            ? new Uri(resourcePath, UriKind.Absolute)
            : new Uri($"pack://application:,,,/{resourcePath}", UriKind.Absolute);
    }
}
