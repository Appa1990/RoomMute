using RoomMute.Services;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
namespace RoomMute.ViewModels;

public sealed partial class MainViewModel
{
    private readonly UpdateService updates = new();
    private readonly CancellationTokenSource updateCancellation = new();
    private DispatcherTimer? updateTimer;
    private UpdateRelease? availableRelease;
    private bool updateBusy;
    private string updateState = "UpdateIdle", updateError = "", downloadedPackage = "";
    private double updateProgress;
    private RelayCommand? checkUpdateCommand, downloadUpdateCommand, installUpdateCommand, openUpdateFolderCommand;
    public string CurrentVersion => (Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0)).ToString(3);
    public string UpdateSummary => "v" + CurrentVersion + " · " + Text[availableRelease?.NewerThan(new Version(CurrentVersion)) == true ? "UpdateAvailable" : "Updates"];
    public string UpdateStatus => Text[updateState] + (updateError.Length == 0 ? "" : "\n" + updateError);
    public string PatchNotes => availableRelease?.Notes ?? Text["UpdateNoNotes"];
    public string UpdateVersions => Text.Format("UpdateVersions", CurrentVersion, AvailableVersion);
    public string AvailableVersion => availableRelease?.Tag ?? "—";
    public double UpdateProgress => updateProgress;
    public bool CanInstallUpdate => downloadedPackage.Length > 0 && !updateBusy;
    public RelayCommand CheckUpdateCommand => checkUpdateCommand ??= new(() => _ = CheckForUpdates(), () => !updateBusy);
    public RelayCommand DownloadUpdateCommand => downloadUpdateCommand ??= new(() => _ = DownloadUpdate(),
        () => !updateBusy && availableRelease?.NewerThan(new Version(CurrentVersion)) == true);
    public RelayCommand InstallUpdateCommand => installUpdateCommand ??= new(() => _ = InstallUpdate(), () => CanInstallUpdate);
    public RelayCommand OpenUpdateFolderCommand => openUpdateFolderCommand ??= new(() => Guard(() =>
        Process.Start(new ProcessStartInfo(Path.GetDirectoryName(downloadedPackage)!) { UseShellExecute = true })), () => downloadedPackage.Length > 0);
    private void InitializeUpdates()
    {
        updateTimer = new DispatcherTimer(DispatcherPriority.Background, dispatcher) { Interval = TimeSpan.FromHours(6) };
        updateTimer.Tick += (_, _) => _ = CheckForUpdates();
        updateTimer.Start();
        _ = CheckForUpdates();
    }
    private void RefreshUpdates() { Changed(""); CommandManagerRefresh(); }
    private async Task CheckForUpdates()
    {
        if (updateBusy || disposed) return;
        updateBusy = true; updateState = "UpdateChecking"; updateError = ""; RefreshUpdates();
        try
        {
            var found = await updates.Check(updateCancellation.Token);
            if (disposed) return;
            if (availableRelease?.Tag != found.Tag) downloadedPackage = "";
            availableRelease = found;
            updateState = found.NewerThan(new Version(CurrentVersion)) ? "UpdateAvailable" : "UpdateCurrent";
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (disposed) return;
            updateState = "UpdateFailed"; updateError = error.Message; log.Write("Update check failed", error);
        }
        finally { updateBusy = false; if (!disposed) RefreshUpdates(); }
    }
    private async Task DownloadUpdate()
    {
        var release = availableRelease;
        if (release == null || updateBusy || !release.NewerThan(new Version(CurrentVersion))) return;
        updateBusy = true; updateProgress = 0; updateState = "UpdateDownloading"; updateError = ""; RefreshUpdates();
        try
        {
            downloadedPackage = await updates.Download(release, new Progress<double>(value =>
            { if (!disposed) { updateProgress = value; Changed(nameof(UpdateProgress)); } }), updateCancellation.Token);
            updateState = "UpdateReady";
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (disposed) return;
            updateState = "UpdateFailed"; updateError = error.Message; log.Write("Update download failed", error);
        }
        finally { updateBusy = false; if (!disposed) RefreshUpdates(); }
    }
    private async Task InstallUpdate()
    {
        if (!CanInstallUpdate) return;
        if (hasPendingChanges) { updateState = "UpdateApplyFirst"; RefreshUpdates(); return; }
        updateBusy = true; updateState = "UpdateInstalling"; updateError = ""; RefreshUpdates();
        try
        {
            string executable = await Task.Run(() => UpdateService.Extract(downloadedPackage, availableRelease!), updateCancellation.Token);
            if (disposed) return;
            Stop();
            if (microphone.AttenuatedByRoomMute) throw new IOException(Text["UpdateRestoreFailed"]);
            var start = new ProcessStartInfo(executable) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(executable)! };
            start.ArgumentList.Add("--wait-for-process"); start.ArgumentList.Add(Environment.ProcessId.ToString());
            if (Process.Start(start) == null) throw new IOException("The new RoomMute version could not start.");
            if (Application.Current.MainWindow is Views.MainWindow window) window.AllowClose = true;
            Application.Current.Shutdown();
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (disposed) return;
            updateState = "UpdateFailed"; updateError = error.Message; log.Write("Update installation failed", error);
        }
        finally { updateBusy = false; if (!disposed) RefreshUpdates(); }
    }
    private void DisposeUpdates() { updateTimer?.Stop(); updateCancellation.Cancel(); updates.Dispose(); }
    public bool HasMicrophone => microphone.HasDevice;
    public double MicrophoneVolume
    {
        get { try { return microphone.VolumePercent; } catch { return 0; } }
        set
        {
            if (!microphone.HasDevice || Math.Abs(value - MicrophoneVolume) < 0.05) return;
            Guard(() =>
            {
                enabled = false; automation.Stop(); microphone.SetVolume(value);
                notice = Text["VolumeManual"]; RefreshStatus();
            });
        }
    }
    public string MicrophoneVolumeText => HasMicrophone ? MicrophoneVolume.ToString("0", Text.Culture) + " %" : "—";
    private void SelectIdleMicrophone()
    {
        if (running || string.IsNullOrWhiteSpace(Settings.AudioDeviceId)) return;
        microphone.DuckPercent = config.DuckPercent;
        microphone.Select(Settings.AudioDeviceId);
        microphoneName = Devices.FirstOrDefault(item => item.Id == Settings.AudioDeviceId)?.Name ?? "Mikrofon";
        RefreshStatus();
    }
    public void PrepareForShutdown() { enabled = false; Stop(); EmergencyRestore(); }
}



