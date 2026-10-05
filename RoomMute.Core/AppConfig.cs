using System.Net;
using System.Net.Sockets;

namespace RoomMute.Core;

public sealed class AppConfig
{
    public string PushToTalkKey { get; set; } = "F8";
    public string Language { get; set; } = "de";
    public string ClientId { get; set; } = Guid.NewGuid().ToString("N");
    public string ClientName { get; set; } = Environment.MachineName;
    public string PartnerIp { get; set; } = "";
    public int Port { get; set; } = 48731;
    public int Priority { get; set; } = 10;
    public string AudioDeviceId { get; set; } = "";
    public string NoiseFilterMode { get; set; } = "speech";
    public bool NoiseSuppression { get; set; } = true;
    public double SpeechConfidence { get; set; } = 60;
    public double SpeakThreshold { get; set; } = -35;
    public double SilenceThreshold { get; set; } = -42;
    public double DuckPercent { get; set; } = 20;
    public int AttackDelay { get; set; } = 40;
    public int ReleaseDelay { get; set; } = 350;
    public int HeartbeatInterval { get; set; } = 1000;
    public int PartnerTimeout { get; set; } = 3000;
    public bool Enabled { get; set; } = true;
    public bool FirstSpeakerWins { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool StartMinimized { get; set; }
    public bool MinimizeToTray { get; set; } = true;

    public string? Validate(bool requirePartner = true)
    {
        if (NoiseFilterMode is not ("rnnoise" or "speech")) return "Ungültiger Geräuschfilter-Modus.";
        if (!ShortcutGesture.TryParse(PushToTalkKey, out _)) return "Ungültiger Push-to-talk-Shortcut.";
        if (Language is not ("de" or "en")) return "Ungültige Sprache.";
        if (string.IsNullOrWhiteSpace(ClientName) || ClientName.Length > 64) return "Der PC-Name muss 1–64 Zeichen enthalten.";
        if (!Guid.TryParseExact(ClientId, "N", out _)) return "Die Client-ID ist ungültig.";
        if ((requirePartner || !string.IsNullOrWhiteSpace(PartnerIp)) &&
            (!IPAddress.TryParse(PartnerIp, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork ||
            ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.Broadcast) || ip.GetAddressBytes()[0] >= 224))
            return "Bitte eine gültige IPv4-Adresse des Partner-PCs eingeben.";
        if (Port is < 1024 or > 65535) return "Der Port muss zwischen 1024 und 65535 liegen.";
        if (Priority is < 0 or > 10000) return "Die Priorität muss zwischen 0 und 10000 liegen.";
        if (!double.IsFinite(SpeakThreshold) || !double.IsFinite(SilenceThreshold) ||
            SpeakThreshold is < -90 or > 0 || SilenceThreshold is < -96 or > 0 ||
            SilenceThreshold >= SpeakThreshold) return "Die Ruhe-Schwelle muss unter der Sprech-Schwelle liegen.";
        if (!double.IsFinite(SpeechConfidence) || SpeechConfidence is < 20 or > 95) return "Sprachsicherheit muss zwischen 20 und 95 % liegen.";
        if (!double.IsFinite(DuckPercent) || DuckPercent is < 1 or > 100) return "Ducking-Restpegel muss zwischen 1 und 100 % liegen.";
        if (AttackDelay is < 0 or > 2000 || ReleaseDelay is < 50 or > 5000) return "Attack: 0–2000 ms; Release: 50–5000 ms.";
        if (HeartbeatInterval is < 250 or > 1000 || PartnerTimeout is < 2000 or > 10000 ||
            PartnerTimeout < HeartbeatInterval * 3) return "Ungültige Heartbeat-/Timeout-Einstellungen.";
        return null;
    }
    public AppConfig Copy() => (AppConfig)MemberwiseClone();
}
