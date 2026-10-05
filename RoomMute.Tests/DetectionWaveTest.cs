using RoomMute.Core;
using RoomMute.Core.Audio;
using RoomMute.Services;
namespace RoomMute.Tests;
public static class DetectionWaveTest
{
    public static void Run(string path, bool expectSpeech = true)
    {
        using var reader = new BinaryReader(File.OpenRead(path));
        if (new string(reader.ReadChars(4)) != "RIFF") throw new InvalidDataException("Expected RIFF.");
        reader.ReadInt32(); if (new string(reader.ReadChars(4)) != "WAVE") throw new InvalidDataException("Expected WAVE.");
        int rate = 0, channels = 0, bits = 0; byte[]? audio = null;
        while (reader.BaseStream.Position + 8 <= reader.BaseStream.Length)
        {
            string chunk = new(reader.ReadChars(4)); int size = reader.ReadInt32(); long end = reader.BaseStream.Position + size;
            if (chunk == "fmt ")
            {
                if (reader.ReadUInt16() != 1) throw new InvalidDataException("Expected PCM fixture.");
                channels = reader.ReadUInt16(); rate = reader.ReadInt32(); reader.ReadInt32(); reader.ReadUInt16(); bits = reader.ReadUInt16();
            }
            else if (chunk == "data") audio = reader.ReadBytes(size);
            reader.BaseStream.Position = end + (size % 2);
        }
        if (audio == null) throw new InvalidDataException("No wave data.");
        foreach (double gain in expectSpeech ? new[] { 1d } : new[] { 1d, 2d, 4d, 8d })
        foreach (bool strict in new[] { false, true })
        foreach (int threshold in new[] { -35, -25 })
        {
            byte[] fixture = audio;
            if (gain != 1)
            {
                if (bits != 16) throw new NotSupportedException("Gain sweep expects PCM16.");
                fixture = new byte[audio.Length];
                for (int i = 0; i < audio.Length; i += 2)
                    BitConverter.TryWriteBytes(fixture.AsSpan(i, 2), (short)Math.Clamp(BitConverter.ToInt16(audio, i) * gain, short.MinValue, short.MaxValue));
            }
            using INoiseSuppressor filter = strict ? new SpeechVerifiedSuppressor() : new RnNoiseSuppressor();
            using var processor = new DetectionAudioProcessor(rate, channels, bits, MicrophoneSampleEncoding.Pcm, filter);
            var vad = new VoiceActivityDetector(); var sampledVad = new VoiceActivityDetector();
            var config = new AppConfig { SpeakThreshold = threshold, SilenceThreshold = threshold - 7 };
            int speaking = 0, count = 0, starts = 0, sampledStarts = 0; double maximum = 0, peak = -96;
            processor.ReadingAvailable += result =>
            {
                maximum = Math.Max(maximum, result.SpeechProbability!.Value); peak = Math.Max(peak, result.Decibels);
                if (vad.Update(result.Decibels, count * 10L, 1000 + count * 10L, config, result.SpeechProbability) && vad.Speaking) starts++;
                if (count % 4 == 3 && sampledVad.Update(result.Decibels, count * 10L, 1000 + count * 10L, config, result.SpeechProbability) && sampledVad.Speaking) sampledStarts++;
                if (vad.Speaking) speaking++; count++;
            };
            int block = rate / 100 * channels * (bits / 8);
            for (int i = 0; i + block <= audio.Length; i += block) processor.Push(fixture.AsSpan(i, block));
            Console.WriteLine($"{(strict ? "STRICT" : "RNNOISE")}, gain {gain:0}x, threshold {threshold}: {starts} speaking starts; {speaking}/{count} speech frames; max score {maximum:P1}, peak {peak:0.0} dBFS; old 40 ms sampling: {sampledStarts} starts.");
            if (!strict) continue;
            if (expectSpeech && threshold == -35 && (maximum < .8 || speaking < count * .3)) throw new Exception("Strict model did not preserve speech detection.");
            if (!expectSpeech && starts != 0) throw new Exception("Strict model still triggered speech for this noise fixture.");
        }
        Console.WriteLine(expectSpeech ? "PASS Speech preserved by stricter model." : "PASS Supplied noise does not start a speaking turn in stricter mode.");
    }
}
