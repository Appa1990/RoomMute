namespace RoomMute.Core;

public readonly record struct SpeakingState(bool Speaking, long StartedAt, int Priority, string ClientId);

public static class Arbitration
{
    public static bool RemoteWins(SpeakingState local, SpeakingState remote, bool firstSpeakerWins)
    {
        if (!remote.Speaking) return false;
        if (!local.Speaking || !firstSpeakerWins) return true;
        // Both peers compare exactly the same tuple. ClientId resolves equal priorities.
        decimal difference = (decimal)local.StartedAt - remote.StartedAt;
        if (Math.Abs(difference) > 50) return difference > 0;
        if (local.Priority != remote.Priority) return remote.Priority < local.Priority;
        return string.CompareOrdinal(remote.ClientId, local.ClientId) < 0;
    }
}

public interface IMicrophone
{
    bool Muted { get; }
    bool AttenuatedByRoomMute { get; }
    void AttenuateAutomatically();
    void Restore();
}

/// <summary>Called on a single serialized control thread, including stop and timeout.</summary>
public sealed class AutoMuteService(IMicrophone microphone)
{
    private long? releaseAt;
    public bool PartnerHasPriority { get; private set; }

    public void Update(bool enabled, bool connected, SpeakingState local, SpeakingState remote,
        bool firstSpeakerWins, int releaseMs, long now, bool overrideHeld = false)
    {
        if (overrideHeld || !enabled || !connected) { Stop(); return; }
        PartnerHasPriority = Arbitration.RemoteWins(local, remote, firstSpeakerWins);
        if (PartnerHasPriority)
        {
            releaseAt = null;
            microphone.AttenuateAutomatically();
        }
        else if (microphone.AttenuatedByRoomMute)
        {
            releaseAt ??= now + releaseMs;
            if (now >= releaseAt) { microphone.Restore(); releaseAt = null; }
        }
        else releaseAt = null;
    }
    public void Stop() { releaseAt = null; PartnerHasPriority = false; microphone.Restore(); }
}


