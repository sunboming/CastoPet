using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;

namespace CastoPet.Core;

public sealed class AssetService
{
    public const string DefaultCharacterPath = "Assets/Castorice.png";
    public const string DraggingCharacterPath = "Assets/States/Castorice.Dragging.png";
    public const string InputReactiveBasePath = "Assets/States/InputReactive/Castorice.InputReactive.Base.png";
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

    public BitmapImage LoadDraggingCharacter()
    {
        return LoadCharacter(Skin.DraggingCharacterPath, "Dragging character");
    }

    public ImageSource? TryLoadInputReactiveBase()
    {
        try
        {
            return LoadCharacter(Skin.InputReactiveBasePath, "Input reactive base");
        }
        catch
        {
            return null;
        }
    }

    public IReadOnlyList<ImageSource> LoadIdleFrames()
    {
        return LoadActionFrames(PetActionKind.Idle, "Idle frames");
    }

    public IReadOnlyList<ImageSource> LoadBlinkFrames()
    {
        return LoadActionFrames(PetActionKind.Blink, "Blink frames");
    }

    public IReadOnlyList<ImageSource> LoadMoveFrames()
    {
        return LoadActionFrames(PetActionKind.Move, "Move frames");
    }

    public IReadOnlyList<ImageSource> LoadExpressionTransitionInFrames()
    {
        return LoadActionFrames(PetActionKind.ExpressionTransitionIn, "Expression transition in frames");
    }

    public IReadOnlyList<ImageSource> LoadExpressionTransitionOutFrames()
    {
        return LoadActionFrames(PetActionKind.ExpressionTransitionOut, "Expression transition out frames");
    }

    public IReadOnlyDictionary<ExpressionWheelItem, ImageSource> LoadExpressionWheelImages()
    {
        var images = new Dictionary<ExpressionWheelItem, ImageSource>();

        foreach (var expression in Skin.Expressions)
        {
            try
            {
                var item = new ExpressionWheelItem(expression.Label, expression.ResourcePath);
                images[item] = LoadCharacter(expression.ResourcePath, "Expression wheel images");
            }
            catch
            {
            }
        }

        return images;
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
