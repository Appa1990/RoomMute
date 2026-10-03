using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace RoomMute.Views;

public sealed class ShortcutCaptureWindow : Window
{
    private readonly ShortcutRecorder recorder = new();
    public string? CapturedKey { get; private set; }
    public ShortcutCaptureWindow()
    {
        var text = UiText.Current;
        Title = text["RecordTitle"]; Width = 480; Height = 270; ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner; ShowInTaskbar = false;
        Background = (Brush)Application.Current.FindResource("BackgroundBrush");
        Foreground = (Brush)Application.Current.FindResource("TextBrush");
        FontFamily = new FontFamily("Segoe UI"); FontSize = 13;
        var layout = new StackPanel { Margin = new Thickness(24) };
        layout.Children.Add(new TextBlock { Text = text["RecordWaiting"], FontSize = 21, FontWeight = FontWeights.Bold });
        layout.Children.Add(new TextBlock { Text = text["RecordHint"], TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 14, 0, 20) });
        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        var clear = new Button { Content = text["RecordClear"], Margin = new Thickness(0, 0, 12, 0) };
        clear.Click += (_, _) => { CapturedKey = "None"; DialogResult = true; };
        var cancel = new Button { Content = text["RecordCancel"], IsCancel = true };
        buttons.Children.Add(clear); buttons.Children.Add(cancel); layout.Children.Add(buttons);
        Content = layout;
        PreviewKeyDown += Capture;
        PreviewMouseDown += CaptureMouse;
        Loaded += (_, _) => Focus();
        Deactivated += (_, _) => { if (IsVisible) Close(); };
    }
    private void Capture(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        if (e.IsRepeat) return;
        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        uint modifiers = CurrentModifiers();
        if (!recorder.Press(KeyInterop.VirtualKeyFromKey(key), modifiers)) return;
        if (recorder.Canceled) { DialogResult = false; return; }
        CapturedKey = recorder.Result;
        DialogResult = true;
    }
    private void CaptureMouse(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle) return;
        e.Handled = true;
        if (!recorder.Press(0x04, CurrentModifiers())) return;
        CapturedKey = recorder.Result;
        DialogResult = true;
    }
    private static uint CurrentModifiers()
    {
        uint modifiers = 0;
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) modifiers |= 2;
        if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0) modifiers |= 1;
        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) modifiers |= 4;
        if ((Keyboard.Modifiers & ModifierKeys.Windows) != 0) modifiers |= 8;
        return modifiers;
    }
}

