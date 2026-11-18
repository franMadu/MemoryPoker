param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
# Deterministic synthesis only: creates WAV files without playing them or opening Unity.
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Text;
public static class PokerSoundSynthesis
{
    public const int SampleRate = 44100;
    static double Tau = 2 * Math.PI;

    static void Tone(double[] buffer, double start, double length, double frequency, double amplitude, double decay)
    {
        for (int i = (int)(start * SampleRate); i < buffer.Length; i++)
        {
            double t = (double)i / SampleRate - start;
            if (t < 0) continue;
            if (t >= length) break;
            double envelope = Math.Min(1, t / 0.004) * Math.Min(1, (length - t) / 0.012) * Math.Exp(-decay * t / length);
            double wave = Math.Sin(Tau * frequency * t) + 0.22 * Math.Sin(Tau * frequency * 2.001 * t) + 0.07 * Math.Sin(Tau * frequency * 3.003 * t);
            buffer[i] += amplitude * envelope * wave;
        }
    }

    static void Sweep(double[] buffer, double start, double length, double from, double to, double amplitude)
    {
        for (int i = (int)(start * SampleRate); i < buffer.Length; i++)
        {
            double t = (double)i / SampleRate - start;
            if (t < 0) continue;
            if (t >= length) break;
            double phase = Tau * (from * t + (to - from) * t * t / (2 * length));
            buffer[i] += amplitude * Math.Pow(Math.Sin(Math.PI * t / length), 2) * Math.Sin(phase);
        }
    }

    static void Rustle(double[] buffer, double start, double length, double amplitude, int seed)
    {
        var random = new Random(seed);
        double previous = 0;
        double filtered = 0;
        for (int i = (int)(start * SampleRate); i < buffer.Length; i++)
        {
            double t = (double)i / SampleRate - start;
            if (t < 0) continue;
            if (t >= length) break;
            double sample = random.NextDouble() * 2 - 1;
            filtered = filtered * 0.55 + (sample - previous) * 0.45;
            previous = sample;
            double envelope = Math.Pow(Math.Sin(Math.PI * t / length), 2) * Math.Pow(1 - t / length, 0.5);
            buffer[i] += amplitude * envelope * filtered;
        }
    }

    static void Melody(double[] buffer, double[] notes, double interval, double length)
    {
        for (int i = 0; i < notes.Length; i++)
        {
            Tone(buffer, i * interval, length, notes[i], 0.45, 4.5);
            Tone(buffer, i * interval + 0.035, length * 0.8, notes[i] * 2, 0.07, 5);
        }
    }

    public static void Generate(string directory)
    {
        Directory.CreateDirectory(directory);
        var flip = new double[(int)(SampleRate * 0.34)];
        Rustle(flip, 0, 0.15, 0.7, 101);
        Rustle(flip, 0.1, 0.22, 0.65, 102);
        Sweep(flip, 0.02, 0.23, 340, 150, 0.12);
        Save(directory, "card-flip", flip, 0.68);

        var select = new double[(int)(SampleRate * 0.22)];
        Tone(select, 0, 0.18, 145, 0.8, 7);
        Tone(select, 0.008, 0.13, 850, 0.12, 7);
        Rustle(select, 0, 0.045, 0.7, 201);
        Save(directory, "seal-select", select, 0.75);

        var deselect = new double[(int)(SampleRate * 0.18)];
        Sweep(deselect, 0, 0.12, 370, 190, 0.4);
        Rustle(deselect, 0.02, 0.14, 0.55, 202);
        Save(directory, "seal-deselect", deselect, 0.6);

        var tick = new double[(int)(SampleRate * 0.07)];
        Tone(tick, 0, 0.05, 1250, 0.5, 9);
        Rustle(tick, 0, 0.024, 0.3, 301);
        Save(directory, "countdown-tick", tick, 0.5);

        var warning = new double[(int)(SampleRate * 0.16)];
        Tone(warning, 0, 0.13, 880, 0.45, 3.8);
        Tone(warning, 0.025, 0.1, 1320, 0.12, 4);
        Save(directory, "countdown-warning", warning, 0.65);

        var end = new double[(int)(SampleRate * 0.55)];
        Tone(end, 0, 0.48, 523.25, 0.4, 5);
        Tone(end, 0.07, 0.43, 392, 0.3, 5);
        Save(directory, "round-end", end, 0.68);

        var topOne = new double[(int)(SampleRate * 1.35)];
        Melody(topOne, new[] { 523.25, 659.25, 783.99, 1046.5 }, 0.15, 0.82);
        Tone(topOne, 0.47, 0.8, 659.25, 0.13, 4);
        Tone(topOne, 0.47, 0.8, 783.99, 0.13, 4);
        Save(directory, "top-1", topOne, 0.78);

        var topTwo = new double[(int)(SampleRate * 1.1)];
        Melody(topTwo, new[] { 523.25, 659.25, 783.99 }, 0.15, 0.75);
        Save(directory, "top-2", topTwo, 0.75);

        var topThree = new double[(int)(SampleRate * 0.85)];
        Melody(topThree, new[] { 523.25, 659.25 }, 0.16, 0.65);
        Save(directory, "top-3", topThree, 0.72);

        var newRound = new double[(int)(SampleRate * 0.18)];
        Tone(newRound, 0, 0.12, 440, 0.3, 5);
        Tone(newRound, 0.045, 0.12, 660, 0.3, 5);
        Rustle(newRound, 0, 0.035, 0.2, 401);
        Save(directory, "new-round", newRound, 0.65);
    }

    static void Save(string directory, string name, double[] samples, double targetPeak)
    {
        double mean = 0;
        foreach (double sample in samples) mean += sample;
        mean /= samples.Length;
        double peak = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            double fade = Math.Min(1, (double)i / (SampleRate * 0.003)) * Math.Min(1, (double)(samples.Length - 1 - i) / (SampleRate * 0.012));
            samples[i] = (samples[i] - mean) * fade;
            peak = Math.Max(peak, Math.Abs(samples[i]));
        }
        using (var writer = new BinaryWriter(File.Create(Path.Combine(directory, name + ".wav"))))
        {
            writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples.Length * 2);
            writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
            writer.Write((short)1); writer.Write((short)1); writer.Write(SampleRate);
            writer.Write(SampleRate * 2); writer.Write((short)2); writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length * 2);
            foreach (double sample in samples) writer.Write((short)Math.Round(sample / Math.Max(peak, 0.0001) * targetPeak * 32767));
        }
    }
}
'@
$audioDirectory = Join-Path $ProjectRoot 'Assets/Audio/Sfx'
[PokerSoundSynthesis]::Generate($audioDirectory)
function Get-AudioGuid([string]$Name) {
    $hash = [System.Security.Cryptography.MD5]::Create()
    try { return ([BitConverter]::ToString($hash.ComputeHash([Text.Encoding]::UTF8.GetBytes('MemoryPoker.Audio.' + $Name)))).Replace('-', '').ToLowerInvariant() }
    finally { $hash.Dispose() }
}
foreach ($folder in @('Assets/Audio', 'Assets/Audio/Sfx')) {
    $folderMeta = Join-Path $ProjectRoot ($folder + '.meta')
    if (-not (Test-Path -LiteralPath $folderMeta)) {
        [IO.File]::WriteAllText($folderMeta, "fileFormatVersion: 2`nguid: $(Get-AudioGuid $folder)`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData:`n  assetBundleName:`n  assetBundleVariant:`n")
    }
}
foreach ($clip in Get-ChildItem -LiteralPath $audioDirectory -Filter '*.wav') {
    $metaPath = $clip.FullName + '.meta'
    if (Test-Path -LiteralPath $metaPath) { continue }
    $guid = Get-AudioGuid $clip.BaseName
    $metaContent = @"
fileFormatVersion: 2
guid: $guid
AudioImporter:
  externalObjects: {}
  serializedVersion: 6
  defaultSettings:
    loadType: 0
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 0
    quality: 1
    conversionMode: 0
  platformSettingOverrides: {}
  forceToMono: 1
  normalize: 0
  preloadAudioData: 1
  loadInBackground: 0
  ambisonic: 0
  3D: 0
  userData: ''
  assetBundleName: ''
  assetBundleVariant: ''
"@
    [IO.File]::WriteAllText($metaPath, $metaContent + "`n")
}
Write-Output 'Created ten original effects as mono 44.1 kHz, 16-bit PCM WAV files. No audio was played.'
