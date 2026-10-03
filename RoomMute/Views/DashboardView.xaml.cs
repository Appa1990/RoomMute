using System.Windows;
using RoomMute.ViewModels;
namespace RoomMute.Views;
public partial class DashboardView : System.Windows.Controls.UserControl
{
    public DashboardView() => InitializeComponent();
    private void RecordShortcut(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel model) return;
        model.SetShortcutRecording(true);
        try
        {
            var dialog = new ShortcutCaptureWindow { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true && dialog.CapturedKey is { } captured)
                model.Settings.PushToTalkKey = captured;
        }
        finally { model.SetShortcutRecording(false); }
    }
}
