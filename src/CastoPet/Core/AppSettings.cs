namespace CastoPet.Core;

public sealed class AppSettings
{
    public bool Topmost { get; set; } = true;
    public bool ClickThrough { get; set; }
    public bool ShowInTaskbar { get; set; }
    public bool StartWithWindows { get; set; }
    public bool ActiveMovement { get; set; }
    public bool PushCursor { get; set; }
    public bool InputReactiveMode { get; set; }
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.System;
    public string? SkinManifestPath { get; set; }
    public string? LastAcknowledgedCrashId { get; set; }
    public string? LastAutomaticUpdateCheckDate { get; set; }

    public static AppSettings Default => new();

    public AppSettings Clone()
    {
        return new AppSettings
        {
            Topmost = Topmost,
            ClickThrough = ClickThrough,
            ShowInTaskbar = ShowInTaskbar,
            StartWithWindows = StartWithWindows,
            ActiveMovement = ActiveMovement,
            PushCursor = PushCursor,
            InputReactiveMode = InputReactiveMode,
            ThemeMode = ThemeMode,
            SkinManifestPath = SkinManifestPath,
            LastAcknowledgedCrashId = LastAcknowledgedCrashId,
            LastAutomaticUpdateCheckDate = LastAutomaticUpdateCheckDate,
        };
    }
}
