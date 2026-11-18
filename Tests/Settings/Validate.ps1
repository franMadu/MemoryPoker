param([string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.3.16f1/Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$runtime = Join-Path $UnityEditor 'Data/NetCoreRuntime/dotnet.exe'
$compiler = Join-Path $UnityEditor 'Data/DotNetSdkRoslyn/csc.dll'
$framework = Join-Path $UnityEditor 'Data/MonoBleedingEdge/lib/mono/4.7.1-api'
$testOutput = Join-Path ([IO.Path]::GetTempPath()) ('MemoryPokerSettingsTests-' + [guid]::NewGuid().ToString('N') + '.exe')
$sources = @('Assets/Scripts/UI/GameLocalization.cs','Assets/Scripts/Game/GamePreferences.cs','Tests/Settings/UnityStubs.cs','Tests/Settings/SettingsTests.cs') | ForEach-Object { Join-Path $projectRoot $_ }
try {
    & $runtime $compiler /nologo /noconfig /nostdlib+ /langversion:9 /target:exe "/out:$testOutput" "/reference:$framework/mscorlib.dll" "/reference:$framework/System.dll" "/reference:$framework/System.Core.dll" $sources
    if ($LASTEXITCODE -ne 0) { throw 'Settings test compilation failed.' }
    & $testOutput
    if ($LASTEXITCODE -ne 0) { throw 'Settings tests failed.' }
}
finally { if (Test-Path -LiteralPath $testOutput) { Remove-Item -LiteralPath $testOutput } }
