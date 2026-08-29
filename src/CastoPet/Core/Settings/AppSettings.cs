namespace CastoPet.Core.Settings;

public sealed class AppSettings
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public bool Topmost { get; set; } = true;
    public bool ClickThrough { get; set; }
    public bool ShowInTaskbar { get; set; }
    public bool StartWithWindows { get; set; }
    public bool ActiveMovement { get; set; }
    public bool PushCursor { get; set; }
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.System;
    public string? SkinManifestPath { get; set; }
    public string? LastAcknowledgedCrashId { get; set; }
    public string? LastAutomaticUpdateCheckDate { get; set; }

    public static AppSettings Default => new();

    public AppSettings Clone()
    {
        return new AppSettings
        {
            SchemaVersion = SchemaVersion,
            Topmost = Topmost,
            ClickThrough = ClickThrough,
            ShowInTaskbar = ShowInTaskbar,
            StartWithWindows = StartWithWindows,
            ActiveMovement = ActiveMovement,
            PushCursor = PushCursor,
            ThemeMode = ThemeMode,
            SkinManifestPath = SkinManifestPath,
            LastAcknowledgedCrashId = LastAcknowledgedCrashId,
            LastAutomaticUpdateCheckDate = LastAutomaticUpdateCheckDate,
        };
    }

    public void CopyFrom(AppSettings source)
    {
        SchemaVersion = source.SchemaVersion;
        Topmost = source.Topmost;
        ClickThrough = source.ClickThrough;
        ShowInTaskbar = source.ShowInTaskbar;
        StartWithWindows = source.StartWithWindows;
        ActiveMovement = source.ActiveMovement;
        PushCursor = source.PushCursor;
        ThemeMode = source.ThemeMode;
        SkinManifestPath = source.SkinManifestPath;
        LastAcknowledgedCrashId = source.LastAcknowledgedCrashId;
        LastAutomaticUpdateCheckDate = source.LastAutomaticUpdateCheckDate;
    }
}
