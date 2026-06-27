namespace CastoPet.Core;

public sealed class AppSettings
{
    public bool Topmost { get; set; } = true;
    public bool ClickThrough { get; set; }
    public bool ShowInTaskbar { get; set; }
    public bool StartWithWindows { get; set; }
    public bool ActiveMovement { get; set; }

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
        };
    }
}
