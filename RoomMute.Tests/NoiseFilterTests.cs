using RoomMute.Core;
using RoomMute.Core.Audio;
using RoomMute.Services;
using System.Diagnostics;
using System.Text.Json;
namespace RoomMute.Tests;

public static class NoiseFilterTests
{
    public static int Run()
    {
        int passed = 0;
        void Check(string name, Action test) { test(); passed++; Console.WriteLine("PASS " + name); }
        void Assert(bool condition) { if (!condition) throw new Exception("Noise filter assertion failed."); }
        byte[] Floats(float[] values) { byte[] bytes = new byte[values.Length * 4]; Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length); return bytes; }
        Check("Old configuration enables local noise filtering and settings persist", () =>
        {
            var old = JsonSerializer.Deserialize<AppConfig>("{}")!;
            Assert(old.NoiseSuppression && old.SpeechConfidence == 60);
            old.NoiseSuppression = false; old.SpeechConfidence = 75;
            var copy = JsonSerializer.Deserialize<AppConfig>(JsonSerializer.Serialize(old))!;
            Assert(!copy.NoiseSuppression && copy.SpeechConfidence == 75);
            foreach (double value in new[] { double.NaN, double.PositiveInfinity, 19d, 96d })
                Assert(new AppConfig { SpeechConfidence = value }.Validate(false) != null);
        });
        Check("Speech confidence rejects loud non-speech and keeps attack/release hysteresis", () =>
        {
            var config = new AppConfig(); var vad = new VoiceActivityDetector();
            vad.Update(-10, 0, 1000, config, .1); vad.Update(-10, 500, 1500, config, .1);
            Assert(!vad.Speaking);
            vad.Update(-25, 600, 1600, config, .9); vad.Update(-25, 640, 1640, config, .9);
            Assert(vad.Speaking);
            vad.Update(-25, 700, 1700, config, .45); Assert(vad.Speaking);
            vad.Update(-25, 800, 1800, config, .1); vad.Update(-25, 1149, 2149, config, .1); Assert(vad.Speaking);
            vad.Update(-25, 1150, 2150, config, .1); Assert(!vad.Speaking);
            vad.Update(-20, 1200, 2200, config, double.NaN); Assert(!vad.Speaking);
        });
        Check("Disabled filter retains unfiltered level detection", () =>
        {
            var config = new AppConfig { NoiseSuppression = false }; var vad = new VoiceActivityDetector();
            vad.Update(-20, 0, 1000, config, 0); vad.Update(-20, 40, 1040, config, 0); Assert(vad.Speaking);
            using var processor = new DetectionAudioProcessor(48000, 1, 32, MicrophoneSampleEncoding.Float);
            processor.Push(Floats(Enumerable.Repeat(.1f, 480).ToArray()));
            Assert(Math.Abs(processor.Latest!.Decibels + 20) < .01 && processor.Latest.SpeechProbability == null);
        });
        Check("PCM16/24/32 and float decode and preserve split channel samples", () =>
        {
            foreach (int bits in new[] { 16, 24, 32 })
            {
                int width = bits / 8;
                var bytes = new byte[width * 2]; bytes[width - 1] = 0x40; bytes[2 * width - 1] = 0xC0;
                using var processor = new DetectionAudioProcessor(48000, 2, bits, MicrophoneSampleEncoding.Pcm);
                processor.Push(bytes.AsSpan(0, 1)); processor.Push(bytes.AsSpan(1));
                Assert(Math.Abs(processor.Latest!.Decibels + 6.0206) < .001);
            }
            using var floatProcessor = new DetectionAudioProcessor(48000, 1, 32, MicrophoneSampleEncoding.Float);
            floatProcessor.Push(Floats(new[] { float.NaN, float.PositiveInfinity, .5f, -.5f }));
            Assert(double.IsFinite(floatProcessor.Latest!.Decibels));
        });
        Check("Resampling, downmix and 10 ms framing are independent of packet boundaries", () =>
        {
            foreach (int rate in new[] { 8000, 16000, 22050, 44100, 48000, 96000, 192000 })
            {
                var samples = Enumerable.Range(0, rate / 5 * 2).Select(i => (float)(.1 * Math.Sin(i / 2d * 2 * Math.PI * 200 / rate))).ToArray();
                byte[] bytes = Floats(samples);
                var wholeFilter = new EchoFilter(); var splitFilter = new EchoFilter();
                using var whole = new DetectionAudioProcessor(rate, 2, 32, MicrophoneSampleEncoding.Float, wholeFilter);
                using var split = new DetectionAudioProcessor(rate, 2, 32, MicrophoneSampleEncoding.Float, splitFilter);
                whole.Push(bytes);
                for (int i = 0; i < bytes.Length; i += 137) split.Push(bytes.AsSpan(i, Math.Min(137, bytes.Length - i)));
                Assert(whole.ProcessedFrames == split.ProcessedFrames && whole.ProcessedFrames >= 19);
                Assert(whole.Latest == split.Latest && wholeFilter.Checksum == splitFilter.Checksum);
                Assert(whole.Latest!.SpeechProbability == .75 && whole.Latest.Decibels is > -30 and < -20);
            }
        });
        Check("Processor owns its filter and rejects invalid native output", () =>
        {
            var filter = new EchoFilter(); var processor = new DetectionAudioProcessor(48000, 1, 32, MicrophoneSampleEncoding.Float, filter);
            processor.Dispose(); processor.Dispose(); Assert(filter.Disposed == 1);
            try { processor.Push(new byte[4]); throw new Exception("Disposed processor accepted input."); }
            catch (ObjectDisposedException) { }
            using var invalid = new DetectionAudioProcessor(48000, 1, 32, MicrophoneSampleEncoding.Float, new EchoFilter { Invalid = true });
            try { invalid.Push(new byte[1920]); throw new Exception("Invalid filter output accepted."); }
            catch (InvalidDataException) { }
        });
        Check("Bundled native RNNoise handles silence and repeated lifecycle", () =>
        {
            for (int i = 0; i < 20; i++)
            {
                using var native = new RnNoiseSuppressor();
                var output = new float[480]; float probability = native.Process(new float[480], output);
                Assert(float.IsFinite(probability) && probability is >= 0 and <= 1 && output.All(float.IsFinite));
            }
            using var processor = new DetectionAudioProcessor(48000, 1, 32, MicrophoneSampleEncoding.Float, new RnNoiseSuppressor());
            processor.Push(new byte[1920 * 30]); Assert(processor.Latest!.Decibels == -96);
        });
        Check("Native RNNoise suppresses sustained broadband hiss without claiming speech", () =>
        {
            var random = new Random(1234); var samples = Enumerable.Range(0, 48000 * 3).Select(_ => (float)((random.NextDouble() * 2 - 1) * .15)).ToArray();
            using var processor = new DetectionAudioProcessor(48000, 1, 32, MicrophoneSampleEncoding.Float, new RnNoiseSuppressor());
            double energy = 0, probability = 0; var watch = Stopwatch.StartNew();
            for (int i = 0; i < 300; i++)
            {
                processor.Push(Floats(samples.AsSpan(i * 480, 480).ToArray()));
                if (i >= 100) { energy += processor.Latest!.Decibels; probability += processor.Latest.SpeechProbability!.Value; }
            }
            watch.Stop(); double db = energy / 200, confidence = probability / 200;
            Console.WriteLine($"  Native hiss: filtered {db:0.0} dBFS, confidence {confidence:P0}; 3 s processed in {watch.Elapsed.TotalMilliseconds:0} ms.");
            Assert(db < -35 && confidence < .3);
        });
        Check("Every analysis frame reaches detection; UI sampling cannot turn isolated peaks into speech", () =>
        {
            using var processor = new DetectionAudioProcessor(48000, 1, 32, MicrophoneSampleEncoding.Float, new SpikyFilter());
            var vad = new VoiceActivityDetector(); var sampled = new VoiceActivityDetector(); var config = new AppConfig();
            int frame = 0;
            processor.ReadingAvailable += reading =>
            {
                vad.Update(reading.Decibels, frame * 10L, 1000 + frame * 10L, config, reading.SpeechProbability);
                if (frame % 4 == 3) sampled.Update(reading.Decibels, frame * 10L, 1000 + frame * 10L, config, reading.SpeechProbability);
                frame++;
            };
            processor.Push(Floats(Enumerable.Repeat(.1f, 480 * 40).ToArray()));
            Assert(frame == 40 && !vad.Speaking && sampled.Speaking);
        });
        Check("Stricter mode is the legacy default and invalid mode is rejected", () =>
        {
            Assert(JsonSerializer.Deserialize<AppConfig>("{}")!.NoiseFilterMode == "speech");
            Assert(new AppConfig { NoiseFilterMode = "unknown" }.Validate(false) != null);
            var config = new AppConfig { NoiseFilterMode = "rnnoise", NoiseSuppression = false };
            Assert(JsonSerializer.Deserialize<AppConfig>(JsonSerializer.Serialize(config))!.NoiseFilterMode == "rnnoise");
        });
        Check("Bundled Silero classifier handles silence and repeated clean disposal", () =>
        {
            for (int i = 0; i < 3; i++)
            {
                using var strict = new SpeechVerifiedSuppressor(); var input = new float[480]; var output = new float[480];
                for (int j = 0; j < 20; j++) Assert(strict.Process(input, output) < .1);
                strict.Dispose(); strict.Dispose();
                try { strict.Process(input, output); throw new Exception("Disposed classifier accepted samples."); }
                catch (ObjectDisposedException) { }
            }
        });
        Check("Changed speech model is rejected before inference", () =>
        {
            string path = Path.Combine(Path.GetTempPath(), "RoomMute-bad-model-" + Guid.NewGuid().ToString("N"));
            try
            {
                File.WriteAllText(path, "changed model");
                try { using var filter = new SpeechVerifiedSuppressor(path); throw new Exception("Tampered model accepted."); }
                catch (InvalidDataException) { }
            }
            finally { File.Delete(path); }
        });
        return passed;
    }
    private sealed class SpikyFilter : INoiseSuppressor
    {
        private int count;
        public float Process(float[] input, float[] output) { Array.Copy(input, output, input.Length); return ++count % 4 == 0 ? .9f : .1f; }
        public void Dispose() { }
    }
    private sealed class EchoFilter : INoiseSuppressor
    {
        public double Checksum; public int Disposed; public bool Invalid;
        public float Process(float[] input, float[] output) { Array.Copy(input, output, input.Length); Checksum += input.Sum(x => (double)x); return Invalid ? float.NaN : .75f; }
        public void Dispose() => Disposed++;
    }
}
