using System.Globalization;
namespace RoomMute.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly AppConfig original;
    public SettingsViewModel(AppConfig config)
    {
        original = config.Copy();
        PushToTalkKey = config.PushToTalkKey;
        DuckPercent = config.DuckPercent;
        ClientName = config.ClientName; PartnerIp = config.PartnerIp; Port = config.Port.ToString();
        Priority = config.Priority.ToString(); AudioDeviceId = config.AudioDeviceId;
        SpeakThreshold = config.SpeakThreshold; SilenceThreshold = config.SilenceThreshold;
        AttackDelay = config.AttackDelay.ToString(); ReleaseDelay = config.ReleaseDelay.ToString();
        FirstSpeakerWins = config.FirstSpeakerWins; StartWithWindows = config.StartWithWindows;
        StartMinimized = config.StartMinimized; MinimizeToTray = config.MinimizeToTray;
    }
    private string pushToTalkKey = "F8";
    public string PushToTalkKey { get => pushToTalkKey; set { if (Set(ref pushToTalkKey, value)) Changed(nameof(ShortcutLabel)); } }
    public string ShortcutLabel => ShortcutGesture.TryParse(PushToTalkKey, out var gesture) ? gesture.Display(UiText.Current.Language) : PushToTalkKey;
    public void NotifyLanguageChanged() => Changed(nameof(ShortcutLabel));
    private string clientName = "";
    public string ClientName { get => clientName; set => Set(ref clientName, value); }
    private string partnerIp = "";
    public string PartnerIp { get => partnerIp; set => Set(ref partnerIp, value); }
    private string port = "";
    public string Port { get => port; set => Set(ref port, value); }
    private string priority = "";
    public string Priority { get => priority; set => Set(ref priority, value); }
    private string audioDeviceId = "";
    public string AudioDeviceId { get => audioDeviceId; set => Set(ref audioDeviceId, value); }
    private double duckPercent;
    public double DuckPercent { get => duckPercent; set => Set(ref duckPercent, value); }
    private double speakThreshold;
    public double SpeakThreshold { get => speakThreshold; set => Set(ref speakThreshold, value); }
    private double silenceThreshold;
    public double SilenceThreshold { get => silenceThreshold; set => Set(ref silenceThreshold, value); }
    private string attackDelay = "";
    public string AttackDelay { get => attackDelay; set => Set(ref attackDelay, value); }
    private string releaseDelay = "";
    public string ReleaseDelay { get => releaseDelay; set => Set(ref releaseDelay, value); }
    private bool firstSpeakerWins;
    public bool FirstSpeakerWins { get => firstSpeakerWins; set => Set(ref firstSpeakerWins, value); }
    private bool startWithWindows;
    public bool StartWithWindows { get => startWithWindows; set => Set(ref startWithWindows, value); }
    private bool startMinimized;
    public bool StartMinimized { get => startMinimized; set => Set(ref startMinimized, value); }
    private bool minimizeToTray;
    public bool MinimizeToTray { get => minimizeToTray; set => Set(ref minimizeToTray, value); }
    public AppConfig Build(bool enabled)
    {
        var result = original.Copy();
        result.PushToTalkKey = PushToTalkKey;
        result.Language = UiText.Current.Language;
        result.DuckPercent = DuckPercent;
        result.ClientName = ClientName.Trim(); result.PartnerIp = PartnerIp.Trim();
        result.AudioDeviceId = AudioDeviceId ?? "";
        result.Port = Number(Port, "Port"); result.Priority = Number(Priority, UiText.Current["Priority"]);
        result.AttackDelay = Number(AttackDelay, "Attack"); result.ReleaseDelay = Number(ReleaseDelay, "Release");
        result.SpeakThreshold = SpeakThreshold; result.SilenceThreshold = SilenceThreshold;
        result.FirstSpeakerWins = FirstSpeakerWins; result.Enabled = enabled;
        result.StartWithWindows = StartWithWindows; result.StartMinimized = StartMinimized;
        result.MinimizeToTray = MinimizeToTray;
        if (result.Validate(false) is { } error) throw new InvalidDataException(error);
        return result;
    }
    private static int Number(string value, string name) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
        ? result : throw new InvalidDataException(UiText.Current.Format("InvalidNumber", name));
}






