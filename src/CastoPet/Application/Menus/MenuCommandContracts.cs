using CastoPet.Core.Settings;

namespace CastoPet.Application.Menus;

public interface IPetCommandTarget
{
    void ShowOrRestore();

    void ApplySettings(AppSettings settings);
}

public interface IStartupRegistration
{
    bool SetEnabled(bool enabled, string executablePath);
}

public interface IUserNotificationService
{
    void ShowWarning(string message, string title);
}

public interface IApplicationShutdown
{
    void Shutdown();
}
