using System.Windows.Media.Imaging;

namespace CastoPet.Core;

public sealed class AssetService
{
    private readonly LoggingService _logger;

    public AssetService(LoggingService logger)
    {
        _logger = logger;
    }

    public BitmapImage LoadDefaultCharacter()
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri("pack://application:,,,/Assets/Castorice.png", UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load built-in Castorice.png.", ex);
            throw;
        }
    }
}
