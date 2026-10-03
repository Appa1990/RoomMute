namespace RoomMute.Core;

/// <summary>Monotonic, bounded retries; an explicit Stop cancels the automatic intent.</summary>
public sealed class AutoStartPolicy
{
    private int failures;
    public bool Requested { get; private set; }
    public long? NextAttemptAt { get; private set; }
    public static bool IsConfigured(AppConfig config) =>
        config.Validate(false) == null && !string.IsNullOrWhiteSpace(config.PartnerIp) &&
        !string.IsNullOrWhiteSpace(config.AudioDeviceId);
    public void Request(long now) { Requested = true; failures = 0; NextAttemptAt = now; }
    public bool IsDue(long now) => Requested && NextAttemptAt is { } due && now >= due;
    public void Succeeded() { failures = 0; NextAttemptAt = null; }
    public void Failed(long now)
    {
        if (!Requested) return;
        int delay = failures switch { 0 => 2000, 1 => 5000, 2 => 10000, _ => 30000 };
        failures = Math.Min(failures + 1, 4);
        NextAttemptAt = now + delay;
    }
    public void Cancel() { Requested = false; failures = 0; NextAttemptAt = null; }
}
