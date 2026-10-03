using NAudio.CoreAudioApi;
namespace RoomMute.Services;

public sealed class MicrophoneService : IMicrophone, IDisposable
{
    private readonly LogService log;
    private readonly Guid context = Guid.NewGuid();
    private MMDevice? device;
    private DuckOwnership? ownership;
    public double DuckPercent { get; set; } = 20;
    public event Action? ExternalChange;
    public MicrophoneService(LogService log) => this.log = log;
    public bool Muted => device?.AudioEndpointVolume.Mute ?? false;
    public bool AttenuatedByRoomMute => ownership?.AttenuatedByRoomMute ?? false;
    public bool HasDevice => device != null;
    public void Select(string id)
    {
        Dispose();
        using var enumerator = new MMDeviceEnumerator();
        device = enumerator.GetDevice(id);
        var endpoint = device.AudioEndpointVolume;
        endpoint.NotificationGuid = context;
        ownership = new(() => endpoint.MasterVolumeLevelScalar,
            value => endpoint.MasterVolumeLevelScalar = value, () => endpoint.Mute)
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

