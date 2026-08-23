using Wpf = System.Windows;

using CastoPet.Application.Menus;

namespace CastoPet.Presentation.Services;

public sealed class WpfUserNotificationService : IUserNotificationService
{
    public void ShowWarning(string message, string title)
    {
        Wpf.MessageBox.Show(message, title, Wpf.MessageBoxButton.OK, Wpf.MessageBoxImage.Warning);
    }
}

public sealed class WpfApplicationShutdown : IApplicationShutdown
{
    public void Shutdown()
    {
        System.Windows.Application.Current.Shutdown();
    }
}
