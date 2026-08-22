using System.Windows;

namespace CastoPet;

public partial class CrashNotificationWindow : Window
{
    private readonly Action _openReports;
    private readonly Action _acknowledge;
    private bool _acknowledged;

    public CrashNotificationWindow(Action openReports, Action acknowledge)
    {
        InitializeComponent();
        _openReports = openReports;
        _acknowledge = acknowledge;
        Closed += (_, _) => AcknowledgeOnce();
    }

    private void OpenReportsButton_Click(object sender, RoutedEventArgs e)
    {
        _openReports();
        Close();
    }

    private void IgnoreButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void AcknowledgeOnce()
    {
        if (_acknowledged)
        {
            return;
        }

        _acknowledged = true;
        _acknowledge();
    }
}
