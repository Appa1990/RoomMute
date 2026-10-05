using NAudio.CoreAudioApi;
using NAudio.Wave;
using RoomMute.Core.Audio;
namespace RoomMute.Services;

public sealed record AudioDevice(string Id, string Name) { public override string ToString() => Name; }
public sealed record AudioReading(double Decibels, long ReceivedAt, double? SpeechProbability = null);

public sealed class AudioMonitorService : IDisposable
{
    private readonly object sync = new();
    private MMDevice? device;
    private WasapiCapture? capture;
    private DetectionAudioProcessor? processor;
    private bool failed;
    private AudioReading latest = new(-96, 0);
    public AudioReading Latest => Volatile.Read(ref latest);
    public event Action<Exception>? Failed;

    public static IReadOnlyList<AudioDevice> GetDevices()
    {
        using var enumerator = new MMDeviceEnumerator();
        var result = new List<AudioDevice>();
        foreach (var item in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
        {
            using (item) result.Add(new(item.ID, item.FriendlyName));
        }
        return result;
    }

    public void Start(string deviceId, bool noiseSuppression = true)
    {
        Dispose();
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            device = enumerator.GetDevice(deviceId);
            capture = new WasapiCapture(device, true, 20);
            var format = capture.WaveFormat;
            bool supported = (format.Encoding == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32) ||
                (format.Encoding == WaveFormatEncoding.Pcm && format.BitsPerSample is 16 or 24 or 32);
            if (!supported) throw new NotSupportedException($"Nicht unterstütztes Mikrofonformat: {format}");
                        RnNoiseSuppressor? suppressor;
            try { suppressor = noiseSuppression ? new RnNoiseSuppressor() : null; }
            catch (Exception error) when (error is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
            { throw new IOException(UiText.Current["FilterLoadFailed"], error); }
            try { processor = new(format.SampleRate, format.Channels, format.BitsPerSample,
                format.Encoding == WaveFormatEncoding.IeeeFloat ? MicrophoneSampleEncoding.Float : MicrophoneSampleEncoding.Pcm, suppressor); }
            catch { suppressor?.Dispose(); throw; }
            capture.DataAvailable += OnData;
            capture.RecordingStopped += OnStopped;
            failed = false;
            latest = new(-96, Environment.TickCount64, noiseSuppression ? 0 : null);
            capture.StartRecording();
        }
        catch { Dispose(); throw; }
    }

    private void OnData(object? sender, WaveInEventArgs e)
    {
        Exception? failure = null;
        lock (sync)
        {
            if (sender != capture || processor == null || failed) return;
            try
            {
                if (processor.Push(e.Buffer.AsSpan(0, e.BytesRecorded)) && processor.Latest is { } reading)
                    Volatile.Write(ref latest, new(reading.Decibels, Environment.TickCount64, reading.SpeechProbability));
            }
            catch (Exception error) { failed = true; failure = error; }
        }
        if (failure != null) Failed?.Invoke(failure);
    }
    private void OnStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null) Failed?.Invoke(e.Exception);
    }
    public void Dispose()
    {
        WasapiCapture? previous;
        lock (sync) { previous = capture; capture = null; }
        try
        {
            if (previous != null)
            {
                previous.DataAvailable -= OnData;
                previous.RecordingStopped -= OnStopped;
                // Capture disposal may join the audio thread; never hold sync here.
                previous.Dispose();
            }
        }
        finally
        {
            lock (sync) { processor?.Dispose(); processor = null; }
            device?.Dispose(); device = null;
        }
    }
}
