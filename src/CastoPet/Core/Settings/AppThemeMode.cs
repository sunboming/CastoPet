namespace CastoPet.Core.Settings;

public enum AppThemeMode
{
    System,
    Light,
    Dark,
}

public static class ThemeModeResolver
{
    public static AppThemeMode Resolve(AppThemeMode mode, bool systemUsesDark) => mode switch
    {
        AppThemeMode.Light => AppThemeMode.Light,
        AppThemeMode.Dark => AppThemeMode.Dark,
        _ => systemUsesDark ? AppThemeMode.Dark : AppThemeMode.Light,
    };
}
