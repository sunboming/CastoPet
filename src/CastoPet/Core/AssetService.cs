using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace CastoPet.Core;

public sealed class AssetService
{
    public const string DefaultCharacterPath = "Assets/Castorice.png";
    public const string DraggingCharacterPath = "Assets/States/Castorice.Dragging.png";
    public const string InputReactiveBasePath = "Assets/States/InputReactive/Castorice.InputReactive.Base.png";
    public const int CharacterDecodePixelWidth = 320;

    private readonly LoggingService _logger;

    public AssetService(LoggingService logger)
    {
        _logger = logger;
    }

    public BitmapImage LoadDefaultCharacter()
    {
        return LoadCharacter(DefaultCharacterPath, "Default character");
    }

    public BitmapImage LoadDraggingCharacter()
    {
        return LoadCharacter(DraggingCharacterPath, "Dragging character");
    }

    public ImageSource? TryLoadInputReactiveBase()
    {
        try
        {
            return LoadCharacter(InputReactiveBasePath, "Input reactive base");
        }
        catch
        {
            return null;
        }
    }

    public IReadOnlyList<ImageSource> LoadIdleFrames()
    {
        return IdleFrameSequence.FramePaths.Select(path => LoadCharacter(path, "Idle frames")).ToArray();
    }

    public IReadOnlyList<ImageSource> LoadBlinkFrames()
    {
        return BlinkFrameSequence.FramePaths.Select(path => LoadCharacter(path, "Blink frames")).ToArray();
    }

    public IReadOnlyList<ImageSource> LoadMoveFrames()
    {
        return MoveFrameSequence.FramePaths.Select(path => LoadCharacter(path, "Move frames")).ToArray();
    }

    public IReadOnlyList<ImageSource> LoadExpressionTransitionInFrames()
    {
        return ExpressionTransitionSequence.InFramePaths.Select(path => LoadCharacter(path, "Expression transition in frames")).ToArray();
    }

    public IReadOnlyList<ImageSource> LoadExpressionTransitionOutFrames()
    {
        return ExpressionTransitionSequence.OutFramePaths.Select(path => LoadCharacter(path, "Expression transition out frames")).ToArray();
    }

    public IReadOnlyDictionary<ExpressionWheelItem, ImageSource> LoadExpressionWheelImages()
    {
        var images = new Dictionary<ExpressionWheelItem, ImageSource>();

        foreach (var item in ExpressionWheelCatalog.Items)
        {
            try
            {
                images[item] = LoadCharacter(item.ResourcePath, "Expression wheel images");
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

    private BitmapImage LoadCharacter(string resourcePath, string resourceGroup)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = CharacterDecodePixelWidth;
            image.UriSource = new Uri($"pack://application:,,,/{resourcePath}", UriKind.Absolute);
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
}
