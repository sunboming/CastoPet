using System.Windows;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;

using CastoPet.Core.Settings;

namespace CastoPet.Presentation.Styling;

public static class SettingsThemePalette
{
    public static IReadOnlyList<string> RequiredBrushKeys { get; } =
    [
        "WindowTintBrush",
        "SurfaceBrush",
        "SurfaceElevatedBrush",
        "LavenderBrush",
        "NavigationBrush",
        "DividerBrush",
        "BorderBrush",
        "TextBrush",
        "SecondaryTextBrush",
        "PurpleBrush",
        "PurpleHoverBrush",
        "AccentForegroundBrush",
        "ButtonBrush",
        "ButtonHoverBrush",
        "InputBrush",
        "SelectionBrush",
        "ToggleTrackBrush",
        "ToggleThumbBrush",
        "DangerBrush",
        "ShadowBrush",
        "GlassHighlightBrush",
        "FrostedGrainBrush",
    ];

    public static IReadOnlyDictionary<string, MediaColor> Create(AppThemeMode mode, bool translucent = true)
    {
        var colors = mode == AppThemeMode.Dark ? CreateDark() : CreateLight();
        return translucent
            ? colors
            : colors.ToDictionary(pair => pair.Key, pair => MediaColor.FromArgb(255, pair.Value.R, pair.Value.G, pair.Value.B));
    }

    public static void Apply(ResourceDictionary resources, AppThemeMode mode, bool translucent = true)
    {
        foreach (var (key, color) in Create(mode, translucent))
        {
            resources[key] = new SolidColorBrush(color);
        }
    }

    private static IReadOnlyDictionary<string, MediaColor> CreateLight() => new Dictionary<string, MediaColor>
    {
        ["WindowTintBrush"] = MediaColor.FromArgb(96, 232, 219, 244),
        ["SurfaceBrush"] = MediaColor.FromArgb(72, 248, 245, 251),
        ["SurfaceElevatedBrush"] = MediaColor.FromArgb(104, 255, 253, 255),
        ["LavenderBrush"] = MediaColor.FromArgb(135, 221, 202, 237),
        ["NavigationBrush"] = MediaColor.FromArgb(150, 209, 184, 232),
        ["DividerBrush"] = MediaColor.FromArgb(185, 161, 132, 184),
        ["BorderBrush"] = MediaColor.FromArgb(205, 174, 144, 199),
        ["TextBrush"] = MediaColor.FromRgb(43, 38, 48),
        ["SecondaryTextBrush"] = MediaColor.FromRgb(98, 90, 106),
        ["PurpleBrush"] = MediaColor.FromRgb(118, 83, 147),
        ["PurpleHoverBrush"] = MediaColor.FromRgb(99, 67, 128),
        ["AccentForegroundBrush"] = Colors.White,
        ["ButtonBrush"] = MediaColor.FromArgb(135, 229, 218, 238),
        ["ButtonHoverBrush"] = MediaColor.FromArgb(170, 218, 200, 232),
        ["InputBrush"] = MediaColor.FromArgb(145, 255, 255, 255),
        ["SelectionBrush"] = MediaColor.FromArgb(188, 205, 178, 226),
        ["ToggleTrackBrush"] = MediaColor.FromRgb(199, 190, 207),
        ["ToggleThumbBrush"] = Colors.White,
        ["DangerBrush"] = MediaColor.FromRgb(161, 79, 98),
        ["ShadowBrush"] = MediaColor.FromArgb(125, 81, 60, 96),
        ["GlassHighlightBrush"] = MediaColor.FromArgb(242, 255, 255, 255),
        ["FrostedGrainBrush"] = MediaColor.FromArgb(48, 70, 52, 82),
    };

    private static IReadOnlyDictionary<string, MediaColor> CreateDark() => new Dictionary<string, MediaColor>
    {
        ["WindowTintBrush"] = MediaColor.FromArgb(115, 31, 23, 42),
        ["SurfaceBrush"] = MediaColor.FromArgb(88, 39, 29, 52),
        ["SurfaceElevatedBrush"] = MediaColor.FromArgb(118, 50, 38, 64),
        ["LavenderBrush"] = MediaColor.FromArgb(150, 86, 61, 108),
        ["NavigationBrush"] = MediaColor.FromArgb(165, 93, 65, 119),
        ["DividerBrush"] = MediaColor.FromArgb(200, 148, 120, 168),
        ["BorderBrush"] = MediaColor.FromArgb(225, 138, 112, 160),
        ["TextBrush"] = MediaColor.FromRgb(248, 243, 251),
        ["SecondaryTextBrush"] = MediaColor.FromRgb(220, 210, 228),
        ["PurpleBrush"] = MediaColor.FromRgb(205, 158, 235),
        ["PurpleHoverBrush"] = MediaColor.FromRgb(224, 188, 247),
        ["AccentForegroundBrush"] = MediaColor.FromRgb(36, 22, 45),
        ["ButtonBrush"] = MediaColor.FromArgb(150, 96, 75, 115),
        ["ButtonHoverBrush"] = MediaColor.FromArgb(185, 119, 89, 143),
        ["InputBrush"] = MediaColor.FromArgb(150, 56, 45, 70),
        ["SelectionBrush"] = MediaColor.FromArgb(180, 131, 84, 165),
        ["ToggleTrackBrush"] = MediaColor.FromRgb(96, 82, 107),
        ["ToggleThumbBrush"] = MediaColor.FromRgb(249, 244, 251),
        ["DangerBrush"] = MediaColor.FromRgb(226, 135, 157),
        ["ShadowBrush"] = MediaColor.FromArgb(140, 0, 0, 0),
        ["GlassHighlightBrush"] = MediaColor.FromArgb(130, 255, 255, 255),
        ["FrostedGrainBrush"] = MediaColor.FromArgb(50, 255, 255, 255),
    };
}
