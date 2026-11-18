param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$audioDirectory = Join-Path $ProjectRoot 'Assets/Audio/Sfx'
$clips = @(Get-ChildItem -LiteralPath $audioDirectory -Filter '*.wav')
if ($clips.Count -ne 10) { throw 'Expected ten generated effects.' }
$yamlAssembly = 'C:/Program Files/Unity/Hub/Editor/6000.3.16f1/Editor/Data/Tools/BuildPipeline/SharpYaml.dll'
$yamlParser = $null
if (Test-Path -LiteralPath $yamlAssembly) {
    [void][Reflection.Assembly]::LoadFrom($yamlAssembly)
    $yamlParser = New-Object SharpYaml.Serialization.Serializer
}
$scene = [IO.File]::ReadAllText((Join-Path $ProjectRoot 'Assets/Scenes/SampleScene.unity'))
$report = foreach ($clip in $clips) {
    $bytes = [IO.File]::ReadAllBytes($clip.FullName)
    if ([Text.Encoding]::ASCII.GetString($bytes, 0, 4) -ne 'RIFF' -or
        [Text.Encoding]::ASCII.GetString($bytes, 8, 4) -ne 'WAVE' -or
        [BitConverter]::ToInt32($bytes, 4) -ne $bytes.Length - 8 -or
        [BitConverter]::ToInt16($bytes, 20) -ne 1 -or
        [BitConverter]::ToInt16($bytes, 22) -ne 1 -or
        [BitConverter]::ToInt32($bytes, 24) -ne 44100 -or
        [BitConverter]::ToInt16($bytes, 34) -ne 16 -or
        [Text.Encoding]::ASCII.GetString($bytes, 36, 4) -ne 'data' -or
        [BitConverter]::ToInt32($bytes, 40) -ne $bytes.Length - 44) { throw ('Invalid PCM WAV: ' + $clip.Name) }
    $peak = 0.0; $sum = 0.0; $squares = 0.0
    for ($offset = 44; $offset -lt $bytes.Length; $offset += 2) {
        $sample = [BitConverter]::ToInt16($bytes, $offset) / 32768.0
        $peak = [Math]::Max($peak, [Math]::Abs($sample))
        $sum += $sample; $squares += $sample * $sample
    }
    $sampleCount = ($bytes.Length - 44) / 2
    $rms = [Math]::Sqrt($squares / $sampleCount)
    if ($peak -gt 0.9 -or $peak -lt 0.1 -or $rms -lt 0.001 -or [Math]::Abs($sum / $sampleCount) -gt 0.01) { throw ('Invalid audio levels: ' + $clip.Name) }
    if ([BitConverter]::ToInt16($bytes, 44) -ne 0 -or [BitConverter]::ToInt16($bytes, $bytes.Length - 2) -ne 0) { throw ('Missing endpoint fades: ' + $clip.Name) }
    $meta = [IO.File]::ReadAllText($clip.FullName + '.meta')
    if (-not $meta.EndsWith("`n")) { throw ('Audio metadata must end with a newline for Unity: ' + $clip.Name) }
    if ($yamlParser -ne $null) {
        $reader = New-Object IO.StringReader($meta)
        try { $parsed = $yamlParser.Deserialize($reader) }
        finally { $reader.Dispose() }
        if ($null -eq $parsed['AudioImporter'] -or $null -eq $parsed['guid']) { throw ('Invalid importer metadata: ' + $clip.Name) }
    }
    $guid = [regex]::Match($meta, 'guid: ([a-f0-9]{32})').Groups[1].Value
    if (-not $guid -or -not $scene.Contains('fileID: 8300000, guid: ' + $guid)) { throw ('Clip not assigned in scene: ' + $clip.Name) }
    [pscustomobject]@{ Clip=$clip.Name; Seconds=[Math]::Round($sampleCount / 44100.0, 2); Peak=[Math]::Round($peak, 3); RMS=[Math]::Round($rms, 3) }
}
$report | Format-Table -AutoSize
Write-Output 'PASS: ten WAVs, mono PCM 16-bit/44.1 kHz, non-silent audio, no clipping, clean endpoints, low DC offset and complete scene references. No audio played.'

# La música es estéreo y circular: no debe exigirse que termine en silencio como los efectos.
$musicPath = Join-Path $ProjectRoot 'Assets/Audio/Music/quiet-table.wav'
$music = [IO.File]::ReadAllBytes($musicPath)
if ($music.Length -ne 44 + 64 * 44100 * 4 -or
    [Text.Encoding]::ASCII.GetString($music, 0, 4) -ne 'RIFF' -or
    [Text.Encoding]::ASCII.GetString($music, 8, 4) -ne 'WAVE' -or
    [BitConverter]::ToInt32($music, 4) -ne $music.Length - 8 -or
    [BitConverter]::ToInt16($music, 20) -ne 1 -or
    [BitConverter]::ToInt16($music, 22) -ne 2 -or
    [BitConverter]::ToInt32($music, 24) -ne 44100 -or
    [BitConverter]::ToInt16($music, 34) -ne 16 -or
    [Text.Encoding]::ASCII.GetString($music, 36, 4) -ne 'data' -or
    [BitConverter]::ToInt32($music, 40) -ne $music.Length - 44) { throw 'Invalid background music WAV.' }
foreach ($channel in 0..1) {
    $first = [BitConverter]::ToInt16($music, 44 + $channel * 2) / 32768.0
    $last = [BitConverter]::ToInt16($music, $music.Length - 4 + $channel * 2) / 32768.0
    if ([Math]::Abs($first - $last) -gt 0.01) { throw 'Discontinuous background music loop.' }
}
$musicMeta = [IO.File]::ReadAllText($musicPath + '.meta')
$musicGuid = [regex]::Match($musicMeta, 'guid: ([a-f0-9]{32})').Groups[1].Value
if (!$musicGuid -or !$scene.Contains('backgroundMusic: {fileID: 8300000, guid: ' + $musicGuid) -or
    $musicMeta -notmatch 'forceToMono: 0') { throw 'Missing music assignment or stereo importer.' }
Write-Output 'PASS: background music is 64-second stereo PCM, both loop boundaries are continuous and the clip is assigned. No audio played.'
