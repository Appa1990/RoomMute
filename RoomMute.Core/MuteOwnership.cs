namespace RoomMute.Core;

/// <summary>Ownership, external override and retries are independent of the audio backend.</summary>
public sealed class MuteOwnership(Func<bool> read, Action<bool> write) : IMicrophone
{
    private readonly object gate = new();
    private bool owned;
    private bool externalOverride;
    public bool Muted { get { lock (gate) return read(); } }
    public bool AttenuatedByRoomMute { get { lock (gate) return owned; } }
    public void AttenuateAutomatically()
    {
        lock (gate)
        {
            if (owned || externalOverride || read()) return;
            owned = true;
            try { write(true); }
            catch { owned = false; throw; }
        }
    }
    public void Restore()
    {
        lock (gate)
        {
            if (!owned) return;
            write(false);
            owned = false; // Retain ownership if the write fails, permitting a later retry.
        }
    }
    public void ManualMute()
    {
        lock (gate)
        {
            write(true);
            owned = false;
            externalOverride = true;
        }
    }
    public void ExternalChange()
    {
        lock (gate) { owned = false; externalOverride = true; }
    }
    public void AllowAutomation() { lock (gate) externalOverride = false; }
}

