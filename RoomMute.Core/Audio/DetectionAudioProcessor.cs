namespace RoomMute.Core.Audio;

public interface INoiseSuppressor : IDisposable
{
    // RNNoise uses 480 mono samples at 48 kHz in signed 16-bit amplitude units.
    float Process(float[] input, float[] output);
}

public enum MicrophoneSampleEncoding { Pcm, Float }
public sealed record DetectionReading(double Decibels, double? SpeechProbability);

/// <summary>Streaming detection only: no playback, audio files or network output.</summary>
public sealed class DetectionAudioProcessor : IDisposable
{
    private readonly int width, channels, sampleRate;
    private readonly MicrophoneSampleEncoding encoding;
    private readonly INoiseSuppressor? suppressor;
    private readonly byte[] sampleBytes;
    private readonly float[] input = new float[480], output = new float[480];
    private int byteCount, frameCount;
    private long sampleIndex;
    private double nextOutputPosition;
    private float previous;
    private bool disposed;
    public event Action<DetectionReading>? ReadingAvailable;
    public DetectionReading? Latest { get; private set; }
    public long ProcessedFrames { get; private set; }

    public DetectionAudioProcessor(int sampleRate, int channels, int bits, MicrophoneSampleEncoding encoding, INoiseSuppressor? suppressor = null)
    {
        if (sampleRate is < 8000 or > 192000 || channels is < 1 or > 32 ||
            (encoding == MicrophoneSampleEncoding.Float ? bits != 32 : bits is not (16 or 24 or 32)))
            throw new NotSupportedException("Unsupported microphone sample format.");
        this.sampleRate = sampleRate; this.channels = channels; width = bits / 8;
        this.encoding = encoding; this.suppressor = suppressor;
        sampleBytes = new byte[width * channels];
    }

    public bool Push(ReadOnlySpan<byte> bytes)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        bool updated = false;
        double rawEnergy = 0; int rawCount = 0;
        foreach (byte value in bytes)
        {
            sampleBytes[byteCount++] = value;
            if (byteCount != sampleBytes.Length) continue;
            byteCount = 0;
            double mono = 0;
            for (int channel = 0; channel < channels; channel++)
            {
                int i = channel * width;
                double sample = encoding == MicrophoneSampleEncoding.Float ? BitConverter.ToSingle(sampleBytes, i) : width switch
                {
                    2 => BitConverter.ToInt16(sampleBytes, i) / 32768d,
                    3 => ((sampleBytes[i] | sampleBytes[i + 1] << 8 | sampleBytes[i + 2] << 16) << 8 >> 8) / 8388608d,
                    _ => BitConverter.ToInt32(sampleBytes, i) / 2147483648d
                };
                sample = double.IsFinite(sample) ? Math.Clamp(sample, -1, 1) : 0;
                mono += sample;
                rawEnergy += sample * sample; rawCount++;
            }
            if (suppressor == null) continue;
            float current = (float)(mono / channels * 32768);
            // Persistent interpolation phase makes results independent of WASAPI packet sizes.
            while (nextOutputPosition <= sampleIndex)
            {
                double fraction = sampleIndex == 0 ? 1 : nextOutputPosition - (sampleIndex - 1);
                input[frameCount++] = previous + (current - previous) * (float)fraction;
                nextOutputPosition += sampleRate / 48000d;
                if (frameCount == input.Length)
                {
                    float probability = suppressor.Process(input, output);
                    if (!float.IsFinite(probability)) throw new InvalidDataException("Noise filter returned invalid speech confidence.");
                    double energy = 0;
                    foreach (float filtered in output)
                    {
                        if (!float.IsFinite(filtered)) throw new InvalidDataException("Noise filter returned invalid audio.");
                        double normalized = filtered / 32768d;
                        energy += normalized * normalized;
                    }
                    Latest = new(Decibels(energy, output.Length), Math.Clamp(probability, 0, 1));
                    ProcessedFrames++; frameCount = 0; updated = true;
                    ReadingAvailable?.Invoke(Latest);
                }
            }
            previous = current; sampleIndex++;
        }
        if (suppressor == null && rawCount > 0) { Latest = new(Decibels(rawEnergy, rawCount), null); updated = true; ReadingAvailable?.Invoke(Latest); }
        return updated;
    }
    private static double Decibels(double energy, int count) => Math.Clamp(20 * Math.Log10(Math.Max(Math.Sqrt(energy / count), 0.0000158489)), -96, 0);
    public void Dispose() { if (disposed) return; disposed = true; suppressor?.Dispose(); }
}
