using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using RoomMute.Core.Audio;
using System.Security.Cryptography;
namespace RoomMute.Services;

/// <summary>RNNoise suppression plus independent Silero speech evidence from the original signal.</summary>
public sealed class SpeechVerifiedSuppressor : INoiseSuppressor
{
    private const string ExpectedHash = "1a153a22f4509e292a94e67d6f9b85e8deb25b4988682b7e174c65279d8788e3";
    private readonly InferenceSession session;
    private readonly RnNoiseSuppressor denoiser;
    private readonly float[] input = new float[576], state = new float[256];
    private readonly List<NamedOnnxValue> inputs;
    private int count;
    private float previousScore, speechScore;
    private bool disposed;
    public SpeechVerifiedSuppressor(string? modelPath = null)
    {
        modelPath ??= Path.Combine(AppContext.BaseDirectory, "Models", "silero_vad.onnx");
        using (var file = File.OpenRead(modelPath))
            if (Convert.ToHexString(SHA256.HashData(file)).ToLowerInvariant() != ExpectedHash)
                throw new InvalidDataException("Speech model integrity check failed. Extract the complete RoomMute package again.");
        using var options = new SessionOptions { InterOpNumThreads = 1, IntraOpNumThreads = 1, ExecutionMode = ExecutionMode.ORT_SEQUENTIAL };
        session = new InferenceSession(modelPath, options);
        try
        {
            denoiser = new RnNoiseSuppressor();
            inputs = new()
            {
                NamedOnnxValue.CreateFromTensor("input", new DenseTensor<float>(input, new[] { 1, 576 })),
                NamedOnnxValue.CreateFromTensor("state", new DenseTensor<float>(state, new[] { 2, 1, 128 })),
                NamedOnnxValue.CreateFromTensor("sr", new DenseTensor<long>(new long[] { 16000 }, Array.Empty<int>()))
            };
        }
        catch { session.Dispose(); throw; }
    }
    public float Process(float[] samples, float[] output)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        float rnScore = denoiser.Process(samples, output);
        // A three-sample average converts 48 kHz mono to 16 kHz without packet-dependent phase.
        for (int i = 0; i < samples.Length; i += 3)
        {
            input[64 + count++] = (samples[i] + samples[i + 1] + samples[i + 2]) / (3 * 32768f);
            if (count != 512) continue;
            using var result = session.Run(inputs);
            float score = result.First(x => x.Name == "output").AsTensor<float>().GetValue(0);
            if (!float.IsFinite(score) || score is < 0 or > 1) throw new InvalidDataException("Invalid speech model score.");
            result.First(x => x.Name == "stateN").AsTensor<float>().ToArray().CopyTo(state, 0);
            // Two successive 32 ms windows must support speech; isolated clicks cannot start a turn.
            speechScore = Math.Min(previousScore, score); previousScore = score;
            Array.Copy(input, 512, input, 0, 64); count = 0;
        }
        return Math.Min(rnScore, speechScore);
    }
    public void Dispose()
    {
        if (disposed) return; disposed = true;
        try { denoiser.Dispose(); } finally { session.Dispose(); }
    }
}
