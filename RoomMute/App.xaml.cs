using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RoomMute.Services;
using RoomMute.ViewModels;
using RoomMute.Views;
namespace RoomMute;

public partial class App : Application
{
    private readonly LogService log = new();
    private Mutex? instance;
    private bool ownsMutex;
    private MainViewModel? model;
    private TrayService? tray;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Length == 2 && e.Args[0] == "--wait-for-process" && int.TryParse(e.Args[1], out int previousId))
        {
            try
            {
                using var previous = Process.GetProcessById(previousId);
                if (previous.ProcessName == "RoomMute" && !previous.WaitForExit(15000)) { Shutdown(1); return; }
            }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException) { } // The old process already exited.
        }
        UiText.Current.Language = new ConfigurationService(log).Load().Language;
        if (e.Args.Length == 2 && e.Args[0] == "--verify-shortcut")
        {
            try
            {
                GlobalPushToTalkService.VerifyLifecycle();
                File.WriteAllText(e.Args[1], "PASS Native shortcut: idle without polling, held notification, release detection and cleanup.");
                Shutdown();
            }
            catch (Exception ex) { File.WriteAllText(e.Args[1], "FAIL " + ex); Shutdown(1); }
            return;
        }
        DispatcherUnhandledException += (_, args) =>
        {
            log.Write("Unhandled UI error", args.Exception);
            model?.EmergencyRestore();
            args.Handled = true;
            if (e.Args.Length == 2 && e.Args[0] == "--render-preview")
            { File.WriteAllText(Path.Combine(e.Args[1], "preview-error.log"), args.Exception.ToString()); Shutdown(1); return; }
            MessageBox.Show(UiText.Current["Fatal"] + "\n\n" + args.Exception.Message, "RoomMute");
            Shutdown(1);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            log.Write("Unhandled process error", args.ExceptionObject as Exception);
            model?.EmergencyRestore();
        };
        SessionEnding += (_, _) =>
        {
            if (MainWindow is MainWindow window) window.AllowClose = true;
            model?.PrepareForShutdown();
        };
        try
        {
            bool preview = e.Args.Length == 2 && e.Args[0] == "--render-preview";
            if (!preview)
            {
                instance = new Mutex(true, @"Local\RoomMute-" + Environment.UserName, out ownsMutex);
                if (!ownsMutex) { MessageBox.Show(UiText.Current["AlreadyRunning"], "RoomMute"); Shutdown(); return; }
            }
            model = new(log);
            var window = new MainWindow { DataContext = model };
            MainWindow = window;
            if (preview)
            {
                RenderPreview(window, e.Args[1]);
                Shutdown();
                return;
            }
            tray = new(model, window.ShowDashboard, () => { window.AllowClose = true; Shutdown(); });
            if (!model.StartMinimized) window.Show();
            model.Initialize();
            log.Write("RoomMute started");
        }
        catch (Exception error)
        {
            log.Write("Startup failed", error);
            model?.EmergencyRestore();
            if (e.Args.Length == 2 && e.Args[0] == "--render-preview")
            { File.WriteAllText(Path.Combine(e.Args[1], "preview-error.log"), error.ToString()); Shutdown(1); return; }
            MessageBox.Show(UiText.Current["StartupFailed"] + "\n\n" + error.Message, "RoomMute");
            Shutdown(1);
        }
    }
    private static void RenderPreview(MainWindow window, string directory)
    {
        Directory.CreateDirectory(directory);
        using var trace = new TextWriterTraceListener(Path.Combine(directory, "wpf-binding.log"));
        PresentationTraceSources.DataBindingSource.Listeners.Add(trace);
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -32000; window.Top = -32000;
        window.ShowActivated = false; window.ShowInTaskbar = false;
        window.Show();
        if (window.Background is not SolidColorBrush background || background.Color != Color.FromRgb(11, 16, 24))
            throw new InvalidOperationException("Window background must be dark.");
        string originalLanguage = UiText.Current.Language;
        foreach (var language in new[] { "de", "en" })
        foreach (var size in new[] { new Size(1240, 800), new Size(1040, 740) })
        {
            UiText.Current.Language = language; // Preview only: do not save a language preference.
            window.Width = size.Width; window.Height = size.Height;
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            window.UpdateLayout();
            var root = (DashboardView)window.Content;
            foreach (FrameworkElement control in new FrameworkElement[] { root.AudioActions, root.ShortcutSettings, root.VolumeControls, root.UpdateButton, root.ApplyButton, root.NetworkCard })
            {
                var bounds = control.TransformToAncestor(root).TransformBounds(new Rect(0, 0, control.ActualWidth, control.ActualHeight));
                if (bounds.Left < 0 || bounds.Top < 0 || bounds.Right > root.ActualWidth + 1 || bounds.Bottom > root.ActualHeight + 1)
                    throw new InvalidOperationException("Dashboard control clipped: " + control.Name);
            }
            if (root.AudioActions.TransformToAncestor(root).Transform(new Point(0, root.AudioActions.ActualHeight)).Y >
                root.AudioCard.TransformToAncestor(root).Transform(new Point(0, root.AudioCard.ActualHeight)).Y)
                throw new InvalidOperationException("Audio controls overflow their card.");
            if (root.ShortcutSettings.TransformToAncestor(root).Transform(new Point(0, root.ShortcutSettings.ActualHeight)).Y >
                root.AudioActions.TransformToAncestor(root).Transform(new Point(0, 0)).Y)
                throw new InvalidOperationException("Shortcut overlaps audio actions.");
            var bitmap = new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth), (int)Math.Ceiling(root.ActualHeight), 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(root);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(Path.Combine(directory, $"dashboard-{language}-{size.Width:0}x{size.Height:0}.png"));
            encoder.Save(stream);
        }
        foreach (string language in new[] { "de", "en" })
        {
            UiText.Current.Language = language;
            var capture = new ShortcutCaptureWindow
            {
                Owner = window, WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -32000, Top = -32000, ShowActivated = false
            };
            capture.Show(); capture.UpdateLayout();
            var captureBitmap = new RenderTargetBitmap((int)capture.ActualWidth, (int)capture.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            captureBitmap.Render(capture);
            var captureEncoder = new PngBitmapEncoder();
            captureEncoder.Frames.Add(BitmapFrame.Create(captureBitmap));
            using (var captureStream = File.Create(Path.Combine(directory, $"shortcut-recorder-{language}.png"))) captureEncoder.Save(captureStream);
            capture.Close();
            var updatePreview = new UpdateWindow((MainViewModel)window.DataContext)
            {
                Owner = window, WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -32000, Top = -32000, ShowActivated = false
            };
            updatePreview.Show(); updatePreview.UpdateLayout();
            var updateBitmap = new RenderTargetBitmap((int)updatePreview.ActualWidth, (int)updatePreview.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            updateBitmap.Render(updatePreview);
            var updateEncoder = new PngBitmapEncoder(); updateEncoder.Frames.Add(BitmapFrame.Create(updateBitmap));
            using (var updateStream = File.Create(Path.Combine(directory, $"updates-{language}.png"))) updateEncoder.Save(updateStream);
            updatePreview.Close();
        }
        UiText.Current.Language = originalLanguage;
        trace.Flush();
        PresentationTraceSources.DataBindingSource.Listeners.Remove(trace);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        model?.Dispose();
        tray?.Dispose();
        if (ownsMutex) instance?.ReleaseMutex();
        instance?.Dispose();
        log.Write("RoomMute exited");
        base.OnExit(e);
    }
}











