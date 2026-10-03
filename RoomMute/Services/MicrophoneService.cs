using NAudio.CoreAudioApi;
namespace RoomMute.Services;

public sealed class MicrophoneService : IMicrophone, IDisposable
{
    private readonly LogService log;
    private readonly Guid context = Guid.NewGuid();
    private readonly VolumeRecoveryJournal recovery = new(Path.Combine(LogService.DataDirectory, "VolumeRecovery"));
    private MMDevice? device;
    private DuckOwnership? ownership;
    public double DuckPercent { get; set; } = 20;
    public event Action? ExternalChange;
    public MicrophoneService(LogService log) => this.log = log;
    public bool Muted => device?.AudioEndpointVolume.Mute ?? false;
    public bool AttenuatedByRoomMute => ownership?.AttenuatedByRoomMute ?? false;
    public bool HasDevice => device != null;
    public double VolumePercent => device == null ? 0 : device.AudioEndpointVolume.MasterVolumeLevelScalar * 100d;
    public void SetVolume(double percent)
    {
        if (device == null || !double.IsFinite(percent) || percent is < 0 or > 100) throw new InvalidOperationException("Invalid microphone volume.");
        Restore();
        ownership?.ExternalChange();
        device.AudioEndpointVolume.MasterVolumeLevelScalar = (float)(percent / 100d);
        log.Write($"Microphone input set manually to {percent:0}%");
    }
    public void RecoverPending()
    {
        IReadOnlyList<VolumeRecovery> records;
        try { records = recovery.ReadAll(error => log.Write("Invalid microphone recovery record retained", error)); }
        catch (Exception error) { log.Write("Microphone recovery record could not be read", error); return; }
        foreach (var record in records)
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                using var previous = enumerator.GetDevice(record.DeviceId);
                previous.AudioEndpointVolume.NotificationGuid = context;
                bool restored = recovery.Recover(record, () => previous.AudioEndpointVolume.MasterVolumeLevelScalar,
                    value => previous.AudioEndpointVolume.MasterVolumeLevelScalar = value);
                log.Write(restored ? "Recovered microphone input from previous session" : "Preserved microphone input changed after previous session");
            }
            catch (Exception error) { log.Write("Microphone recovery deferred; device may be unavailable", error); }
        }
    }
    public void Select(string id)
    {
        Dispose();
        using var enumerator = new MMDeviceEnumerator();
        RecoverPending();
        device = enumerator.GetDevice(id);
        var endpoint = device.AudioEndpointVolume;
        endpoint.NotificationGuid = context;
        ownership = new(() => endpoint.MasterVolumeLevelScalar,
            value => endpoint.MasterVolumeLevelScalar = value, () => endpoint.Mute,
            (original, applied) => recovery.Save(id, original, applied), () => recovery.Clear(id))
            { RemainingPercent = DuckPercent };
        endpoint.OnVolumeNotification += OnNotification;
        log.Write("Audio device: " + device.FriendlyName);
    }
    private void OnNotification(AudioVolumeNotificationData data)
    {
        if (data.EventContext == context) return;
        ownership?.ExternalChange();
        log.Write("External microphone change; automation paused");
        ExternalChange?.Invoke();
    }
    public void AttenuateAutomatically()
    {
        bool before = AttenuatedByRoomMute;
        ownership?.AttenuateAutomatically();
        if (!before && AttenuatedByRoomMute) log.Write($"Microphone ducked to {DuckPercent:0}% of original input setting");
    }
    public void Restore()
    {
        bool before = AttenuatedByRoomMute;
        ownership?.Restore();
        if (before) log.Write("Original microphone input volume restored");
    }
    public void ManualMute()
    {
        Restore();
        if (device != null) device.AudioEndpointVolume.Mute = true;
        log.Write("Microphone manually muted");
    }
    public void AllowAutomation() => ownership?.AllowAutomation();
    public void Dispose()
    {
        Restore();
        if (device != null)
        {
            device.AudioEndpointVolume.OnVolumeNotification -= OnNotification;
            device.Dispose();
            device = null;
            ownership = null;
        }
    }
}




