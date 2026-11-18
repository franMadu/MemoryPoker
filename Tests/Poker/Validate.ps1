param([string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.3.16f1/Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$runtime = Join-Path $UnityEditor 'Data/NetCoreRuntime/dotnet.exe'
$compiler = Join-Path $UnityEditor 'Data/DotNetSdkRoslyn/csc.dll'
$framework = Join-Path $UnityEditor 'Data/MonoBleedingEdge/lib/mono/4.7.1-api'
$testOutput = Join-Path ([System.IO.Path]::GetTempPath()) ('MemoryPokerTests-' + [guid]::NewGuid().ToString('N') + '.exe')
$sources = @(
    'Assets/Scripts/Card/Card.cs',
    'Assets/Scripts/Card/SuitEnum.cs',
    'Assets/Scripts/Game/PokerHandRank.cs',
    'Assets/Scripts/Game/PokerHandResult.cs',
    'Assets/Scripts/Game/PokerHandEvaluator.cs',
    'Assets/Scripts/Game/PokerHandSearch.cs',
    'Assets/Scripts/Game/PokerHandDisplay.cs',
    'Assets/Scripts/UI/GameLocalization.cs',
    'Tests/Poker/UnityStubs.cs',
    'Tests/Poker/PokerHandEvaluatorTests.cs'
) | ForEach-Object { Join-Path $projectRoot $_ }
try {
    & $runtime $compiler /nologo /noconfig /nostdlib+ /langversion:9 /target:exe "/out:$testOutput" "/reference:$framework/mscorlib.dll" "/reference:$framework/System.dll" "/reference:$framework/System.Core.dll" $sources
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
    & $testOutput
    if ($LASTEXITCODE -ne 0) { throw 'Evaluator tests failed.' }
}
finally {
    if (Test-Path -LiteralPath $testOutput) { Remove-Item -LiteralPath $testOutput }
}
