param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
# Original deterministic ambient composition. Writes audio files only; never plays them.
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Text;
public static class MemoryPokerAmbient
{
    const int Rate = 44100;
    const int Seconds = 64;
    const double Tau = 2 * Math.PI;
    static double Frequency(int midi) { return 440 * Math.Pow(2, (midi - 69) / 12.0); }

    static void Note(double[] left, double[] right, double start, double duration,
                     int midi, double amplitude, double pan, bool pad)
    {
        double frequency = Frequency(midi);
        int first = (int)Math.Round(start * Rate);
        int count = (int)Math.Round(duration * Rate);
        for (int i = 0; i < count; i++)
        {
            double t = (double)i / Rate;
            double attack = Math.Min(1, t / (pad ? 1.8 : 0.12));
            double release = Math.Min(1, (duration - t) / (pad ? 3.0 : 2.0));
            // Smooth attacks and releases keep the loop free of clicks.
            double envelope = Math.Pow(Math.Sin(attack * Math.PI / 2), 2)
                * Math.Pow(Math.Sin(release * Math.PI / 2), 2);
            double wave;
            if (pad)
                wave = 0.72 * Math.Sin(Tau * frequency * t)
                     + 0.22 * Math.Sin(Tau * frequency * 1.0012 * t)
                     + 0.06 * Math.Sin(Tau * frequency * 2 * t);
            else
            {
                envelope *= Math.Exp(-t / 1.8);
                wave = Math.Sin(Tau * frequency * t)
                     + 0.14 * Math.Exp(-t / 0.7) * Math.Sin(Tau * frequency * 2 * t);
            }
            double sample = amplitude * envelope * wave;
            int index = ((first + i) % left.Length + left.Length) % left.Length;
            left[index] += sample * Math.Sqrt((1 - pan) * 0.5);
            right[index] += sample * Math.Sqrt((1 + pan) * 0.5);
        }
    }

    public static void Generate(string path)
    {
        var left = new double[Rate * Seconds];
        var right = new double[left.Length];
        // Cmaj9, Am7, Fmaj9, Gsus, with spacious voicings and no percussion.
        int[][] chords = {
            new[] {48, 55, 59, 62}, new[] {45, 55, 60, 64},
            new[] {41, 53, 57, 64}, new[] {43, 55, 60, 62},
            new[] {48, 55, 59, 64}, new[] {45, 52, 60, 67},
            new[] {41, 53, 60, 64}, new[] {43, 55, 60, 62}
        };
        for (int bar = 0; bar < chords.Length; bar++)
            for (int voice = 0; voice < chords[bar].Length; voice++)
                Note(left, right, bar * 8 - 1.5, 12, chords[bar][voice],
                    voice == 0 ? 0.065 : 0.045, (voice - 1.5) * 0.16, true);

        double[] starts = {2, 6.5, 11, 16, 21.5, 27, 33, 38, 43.5, 49, 54.5, 60};
        int[] notes = {64, 67, 62, 60, 64, 62, 67, 64, 60, 64, 62, 60};
        for (int i = 0; i < notes.Length; i++)
        {
            double pan = i % 2 == 0 ? -0.16 : 0.16;
            Note(left, right, starts[i], 7, notes[i], 0.07, pan, false);
            Note(left, right, starts[i] + 0.43, 7, notes[i], 0.012, -pan, false);
        }

        double peak = 0;
        double squareSum = 0;
        for (int i = 0; i < left.Length; i++)
            peak = Math.Max(peak, Math.Max(Math.Abs(left[i]), Math.Abs(right[i])));
        double gain = 0.68 / peak;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (var writer = new BinaryWriter(File.Create(path)))
        {
            writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + left.Length * 4);
            writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
            writer.Write((short)1); writer.Write((short)2); writer.Write(Rate);
            writer.Write(Rate * 4); writer.Write((short)4); writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(left.Length * 4);
            for (int i = 0; i < left.Length; i++)
            {
                writer.Write((short)Math.Round(left[i] * gain * 32767));
                writer.Write((short)Math.Round(right[i] * gain * 32767));
                squareSum += (left[i] * left[i] + right[i] * right[i]) * gain * gain;
            }
        }
        double seam = Math.Max(Math.Abs(left[0] - left[left.Length - 1]),
                               Math.Abs(right[0] - right[right.Length - 1])) * gain;
        if (seam > 0.01) throw new Exception("Unexpected loop discontinuity: " + seam);
        Console.WriteLine("PASS: 64-second stereo PCM loop, peak 0.68, RMS {0:F3}, boundary step {1:F5}. No audio playback.",
            Math.Sqrt(squareSum / (left.Length * 2)), seam);
    }
}
'@
[MemoryPokerAmbient]::Generate((Join-Path $ProjectRoot 'Assets/Audio/Music/quiet-table.wav'))
