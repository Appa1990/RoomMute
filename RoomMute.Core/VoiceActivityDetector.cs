namespace RoomMute.Core;

/// <summary>Energy and optional local speech confidence, with monotonic timing. Durations use a monotonic clock.</summary>
public sealed class VoiceActivityDetector
{
    private long? attackSince;
    private long? silenceSince;
    public bool Speaking { get; private set; }
    public long StartedAt { get; private set; }

    public bool Update(double db, long elapsedMs, long utcMs, AppConfig config, double? speechProbability = null)
    {
        bool before = Speaking;
        if (config.NoiseSuppression && speechProbability is { } probability)
        {
            // Require stronger evidence to start than to keep a phrase; preserve short fricatives/pauses.
            double minimum = Math.Max(.1, config.SpeechConfidence / 100 - (Speaking ? .2 : 0));
            if (!double.IsFinite(probability) || probability < minimum) db = -96;
        }
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
