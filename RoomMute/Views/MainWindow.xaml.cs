using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using RoomMute.ViewModels;
namespace RoomMute.Views;

public partial class MainWindow : Window
{
    public bool AllowClose { get; set; }
    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            int dark = 1;
            DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 20, ref dark, sizeof(int));
        };
        Closing += OnClosing;
        StateChanged += (_, _) =>
        {
            if (WindowState == WindowState.Minimized && DataContext is MainViewModel { MinimizeToTray: true }) Hide();
        };
    }
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!AllowClose && DataContext is MainViewModel { MinimizeToTray: true })
        {
            e.Cancel = true;
            Hide();
        }
        else Application.Current.Shutdown();
    }
    public void ShowDashboard()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}

