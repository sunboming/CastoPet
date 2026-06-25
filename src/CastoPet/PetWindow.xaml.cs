using System.Windows;
using CastoPet.Core;

namespace CastoPet;

public partial class PetWindow : Window
{
    private readonly LoggingService _logger;

    public PetWindow(AssetService assets, LoggingService logger)
    {
        InitializeComponent();
        _logger = logger;

        try
        {
            CharacterImage.Source = assets.LoadDefaultCharacter();
        }
        catch
        {
            System.Windows.MessageBox.Show(
                "CastoPet could not load the built-in character image Castorice.png.",
                "CastoPet",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        Loaded += (_, _) => WindowPlacementService.MoveToBottomRight(this);
    }

    public void ApplySettings(AppSettings settings)
    {
        Topmost = settings.Topmost;
        ShowInTaskbar = settings.ShowInTaskbar;
    }

    public void ShowOrRestore()
    {
        if (!IsVisible)
        {
            Show();
        }

        WindowState = WindowState.Normal;
        Activate();
        WindowPlacementService.MoveToBottomRight(this);
        _logger.Info("Pet window shown or restored.");
    }
}
