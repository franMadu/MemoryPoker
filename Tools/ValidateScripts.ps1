param([string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.3.16f1/Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$assemblyPath = Join-Path ([IO.Path]::GetTempPath()) ('MemoryPokerScripts-' + [guid]::NewGuid().ToString('N') + '.dll')
$sources = Get-ChildItem (Join-Path $projectRoot 'Assets/Scripts') -Filter '*.cs' -Recurse | Select-Object -ExpandProperty FullName
$references = @(
    (Join-Path $UnityEditor 'Data/NetStandard/ref/2.1.0/netstandard.dll'),
    (Join-Path $UnityEditor 'Data/Managed/UnityEngine/UnityEngine.CoreModule.dll'),
    (Join-Path $UnityEditor 'Data/Managed/UnityEngine/UnityEngine.UIModule.dll'),
    (Join-Path $UnityEditor 'Data/Managed/UnityEngine/UnityEngine.TextRenderingModule.dll'),
    (Join-Path $UnityEditor 'Data/Managed/UnityEngine/UnityEngine.AudioModule.dll'),
    (Join-Path $projectRoot 'Library/ScriptAssemblies/UnityEngine.UI.dll')
) | ForEach-Object { '/reference:' + $_ }
try {
    # Compila en una biblioteca temporal; no abre Unity, ejecuta el juego ni reproduce audio.
    & (Join-Path $UnityEditor 'Data/NetCoreRuntime/dotnet.exe') (Join-Path $UnityEditor 'Data/DotNetSdkRoslyn/csc.dll') /nologo /noconfig /nostdlib+ /nowarn:0649,0169 /langversion:9 /target:library "/out:$assemblyPath" $references $sources
    if ($LASTEXITCODE -ne 0) { throw 'Game script compilation failed.' }
    Write-Output 'PASS: all game scripts compile. No Unity editor actions or audio playback.'
}
finally { if (Test-Path -LiteralPath $assemblyPath) { Remove-Item -LiteralPath $assemblyPath } }
