namespace CastoPet.Core.Settings;

public sealed class AppSettings
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public bool Topmost { get; set; } = true;
    public bool ClickThrough { get; set; }
    public bool ShowInTaskbar { get; set; }
    public bool StartWithWindows { get; set; }
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
        LastAcknowledgedCrashId = source.LastAcknowledgedCrashId;
        LastAutomaticUpdateCheckDate = source.LastAutomaticUpdateCheckDate;
    }
}
