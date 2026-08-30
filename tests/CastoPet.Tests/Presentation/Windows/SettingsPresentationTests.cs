namespace CastoPet.Tests;

internal static partial class TestSuite
{
    static void SettingsWindowExposesCrashAndUpdateActions()
    {
        var workspace = FindWorkspaceRoot();
        var xaml = File.ReadAllText(System.IO.Path.Combine(workspace, "src", "CastoPet", "Presentation", "Windows", "SettingsWindow.xaml"));

        Assert.Contains(xaml, "OpenCrashReportsButton", "Settings should expose local crash reports.");
        Assert.Contains(xaml, "CheckForUpdatesButton", "Settings should expose manual update checks.");
        Assert.Contains(xaml, "UpdateStatusText", "Settings should display update status.");
        Assert.Contains(xaml, "CurrentVersionText", "Settings should display the current version.");
    }

    static void PetWindowSettingsSnapshotCopiesRuntimeFlags()
    {
        var settings = new AppSettings
        {
            Topmost = false,
            ClickThrough = true,
            ShowInTaskbar = true,
            ActiveMovement = true,
            PushCursor = true,
        };

        var snapshot = PetWindowSettingsSnapshot.FromSettings(settings);

        Assert.False(snapshot.Topmost, "Topmost should be copied for immediate window application.");
        Assert.True(snapshot.ClickThrough, "Click-through should be copied for Win32 window style application.");
        Assert.True(snapshot.ShowInTaskbar, "Taskbar visibility should be copied for window application.");
        Assert.True(snapshot.ActiveMovement, "Active movement should be copied for movement runtime state.");
        Assert.True(snapshot.PushCursor, "Push cursor should be copied for movement runtime state.");
    }

}
