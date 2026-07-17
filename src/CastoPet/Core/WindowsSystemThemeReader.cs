using Microsoft.Win32;

namespace CastoPet.Core;

public static class WindowsSystemThemeReader
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public static bool UsesDarkApps()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            return ParseUsesDarkApps(key?.GetValue("AppsUseLightTheme"));
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool ParseUsesDarkApps(object? value) => value switch
    {
        int number => number == 0,
        long number => number == 0,
        _ => false,
    };
}
