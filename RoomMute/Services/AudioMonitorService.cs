using NAudio.CoreAudioApi;
using NAudio.Wave;
namespace RoomMute.Services;

public sealed record AudioDevice(string Id, string Name) { public override string ToString() => Name; }
public sealed record AudioReading(double Decibels, long ReceivedAt);

public sealed class AudioMonitorService : IDisposable
{
    private MMDevice? device;
    private WasapiCapture? capture;
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

    public void Start(string deviceId)
    {
        Dispose();
        using var enumerator = new MMDeviceEnumerator();
        device = enumerator.GetDevice(deviceId);
        capture = new WasapiCapture(device, true, 20);
        var format = capture.WaveFormat;
        bool supported = (format.Encoding == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32) ||
            (format.Encoding == WaveFormatEncoding.Pcm && format.BitsPerSample is 16 or 24 or 32);
        if (!supported) throw new NotSupportedException($"Nicht unterstütztes Mikrofonformat: {format}");
        capture.DataAvailable += OnData;
        capture.RecordingStopped += OnStopped;
        latest = new(-96, Environment.TickCount64);
        capture.StartRecording();
    }

    private void OnData(object? sender, WaveInEventArgs e)
    {
        if (sender is not WasapiCapture source) return;
        var format = source.WaveFormat;
        int width = format.BitsPerSample / 8;
        int count = e.BytesRecorded / width;
        double sum = 0;
        for (int index = 0; index + width <= e.BytesRecorded; index += width)
        {
            double value;
            if (format.Encoding == WaveFormatEncoding.IeeeFloat) value = BitConverter.ToSingle(e.Buffer, index);
            else if (width == 2) value = BitConverter.ToInt16(e.Buffer, index) / 32768.0;
            else if (width == 3)
            {
                int sample = e.Buffer[index] | e.Buffer[index + 1] << 8 | e.Buffer[index + 2] << 16;
                sample = (sample << 8) >> 8;
                value = sample / 8388608.0;
            }
            else value = BitConverter.ToInt32(e.Buffer, index) / 2147483648.0;
            if (double.IsFinite(value)) sum += value * value;
        }
        double rms = count == 0 ? 0 : Math.Sqrt(sum / count);
        Volatile.Write(ref latest, new(Math.Clamp(20 * Math.Log10(Math.Max(rms, 0.0000158489)), -96, 0), Environment.TickCount64));
        // Buffers remain in memory only. No file writer or network access exists in this service.
    }
    private void OnStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null) Failed?.Invoke(e.Exception);
    }
    public void Dispose()
    {
        if (capture != null)
        {
            capture.DataAvailable -= OnData;
            capture.RecordingStopped -= OnStopped;
            capture.Dispose();
            capture = null;
        }
        device?.Dispose();
        device = null;
    }
}

