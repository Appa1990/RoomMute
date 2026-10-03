namespace RoomMute.Core;

/// <summary>Only energy and timing; no speech analysis. Durations use a monotonic clock.</summary>
public sealed class VoiceActivityDetector
{
    private long? attackSince;
    private long? silenceSince;
    public bool Speaking { get; private set; }
    public long StartedAt { get; private set; }

    public bool Update(double db, long elapsedMs, long utcMs, AppConfig config)
    {
        bool before = Speaking;
        if (!Speaking)
        {
            if (db >= config.SpeakThreshold)
            {
                attackSince ??= elapsedMs;
                if (elapsedMs - attackSince >= config.AttackDelay)
                {
                    Speaking = true;
                    StartedAt = utcMs - (elapsedMs - attackSince.Value);
                    attackSince = null;
                }
            }
            else attackSince = null;
        }
        else
        {
            if (db < config.SilenceThreshold)
            {
                silenceSince ??= elapsedMs;
                if (elapsedMs - silenceSince >= config.ReleaseDelay) Reset();
            }
            else silenceSince = null;
        }
        return before != Speaking;
    }
    public void Reset() { Speaking = false; StartedAt = 0; attackSince = null; silenceSince = null; }
}
