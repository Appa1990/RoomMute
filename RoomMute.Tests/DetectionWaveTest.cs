using RoomMute.Core;
using RoomMute.Core.Audio;
using RoomMute.Services;
namespace RoomMute.Tests;
public static class DetectionWaveTest
{
    public static void Run(string path)
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
                if (reader.ReadUInt16() != 1) throw new InvalidDataException("Expected PCM speech fixture.");
                channels = reader.ReadUInt16(); rate = reader.ReadInt32(); reader.ReadInt32(); reader.ReadUInt16(); bits = reader.ReadUInt16();
            }
            else if (chunk == "data") audio = reader.ReadBytes(size);
            reader.BaseStream.Position = end + (size % 2);
        }
        if (audio == null) throw new InvalidDataException("No wave data.");
        using var processor = new DetectionAudioProcessor(rate, channels, bits, MicrophoneSampleEncoding.Pcm, new RnNoiseSuppressor());
        var vad = new VoiceActivityDetector(); var config = new AppConfig();
        int block = rate / 100 * channels * (bits / 8), speaking = 0, count = 0; double maximum = 0;
        for (int i = 0; i + block <= audio.Length; i += block)
        {
            if (!processor.Push(audio.AsSpan(i, block))) continue;
            var result = processor.Latest!; maximum = Math.Max(maximum, result.SpeechProbability!.Value);
            vad.Update(result.Decibels, count * 10L, 1000 + count * 10L, config, result.SpeechProbability);
            if (vad.Speaking) speaking++; count++;
        }
        Console.WriteLine($"Speech fixture: {speaking}/{count} frames speaking, maximum confidence {maximum:P0}.");
        if (maximum < .8 || speaking < count * .3) throw new Exception("Native filter did not preserve speech detection.");
        Console.WriteLine("PASS Native filter preserves synthetic spoken phrases with the default thresholds.");
    }
}
