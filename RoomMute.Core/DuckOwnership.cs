namespace RoomMute.Core;

/// <summary>Owns only the temporary volume reduction; never changes the mute switch.</summary>
public sealed class DuckOwnership(Func<float> readVolume, Action<float> writeVolume, Func<bool> readMute) : IMicrophone
{
    private readonly object gate = new();
    private bool owned, externalOverride;
    private float original, applied;
    public double RemainingPercent { get; set; } = 20;
    public bool Muted => readMute();
    public bool AttenuatedByRoomMute { get { lock (gate) return owned; } }
    private bool StillApplied() => Math.Abs(readVolume() - applied) < 0.0001f;
    public void AttenuateAutomatically()
    {
        lock (gate)
        {
            if (owned || externalOverride || readMute()) return;
            if (!double.IsFinite(RemainingPercent) || RemainingPercent is < 1 or > 100)
                throw new InvalidOperationException("Ducking-Restpegel muss 1–100 % betragen.");
            original = readVolume();
            float target = original * (float)(RemainingPercent / 100);
            if (Math.Abs(target - original) < 0.0001f) return;
            applied = target;
            owned = true;
            writeVolume(target);
            // Drivers may quantize the scalar: compare against the actual applied value.
            applied = readVolume();
            owned = true;
        }
    }
    public void Restore()
    {
        lock (gate)
        {
            if (!owned) return;
            if (StillApplied()) writeVolume(original);
            owned = false; // Failed writes retain ownership for retry.
        }
    }
    public void ExternalChange()
    {
        lock (gate)
        {
            externalOverride = true;
            // A changed mute switch alone must not strand our lowered volume.
            if (owned && !StillApplied()) owned = false;
        }
    }
    public void AllowAutomation() { lock (gate) externalOverride = false; }
}


