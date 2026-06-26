using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace CastoPet.Core;

public sealed class AssetService
{
    public const string DefaultCharacterPath = "Assets/Castorice.png";
    public const string DraggingCharacterPath = "Assets/States/Castorice.Dragging.png";

    private readonly LoggingService _logger;

    public AssetService(LoggingService logger)
    {
        _logger = logger;
    }

    public BitmapImage LoadDefaultCharacter()
    {
        return LoadCharacter(DefaultCharacterPath);
    }

    public BitmapImage LoadDraggingCharacter()
    {
        return LoadCharacter(DraggingCharacterPath);
    }

    public IReadOnlyList<ImageSource> LoadIdleFrames()
    {
        return IdleFrameSequence.FramePaths.Select(LoadCharacter).ToArray();
    }

    private BitmapImage LoadCharacter(string resourcePath)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri($"pack://application:,,,/{resourcePath}", UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load built-in character image {resourcePath}.", ex);
            throw;
        }
    }
}
