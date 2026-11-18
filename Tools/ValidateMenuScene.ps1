param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$scene = [IO.File]::ReadAllText((Join-Path $ProjectRoot 'Assets/Scenes/SampleScene.unity'))
if ($scene -match '\{(?:fileID|x|r):\s*\}') { throw 'Invalid empty scene property.' }
$matches = [regex]::Matches($scene, '(?ms)^--- !u!(\d+) &(\d+)\r?\n.*?(?=^--- |\z)')
$blocks = @{}
foreach ($match in $matches) {
    $id = $match.Groups[2].Value
    if ($blocks.ContainsKey($id)) { throw "Duplicate scene ID: $id" }
    $blocks[$id] = $match.Value
}
$yamlAssembly = 'C:/Program Files/Unity/Hub/Editor/6000.3.16f1/Editor/Data/Tools/BuildPipeline/SharpYaml.dll'
[void][Reflection.Assembly]::LoadFrom($yamlAssembly)
$serializer = New-Object SharpYaml.Serialization.Serializer
$parsedCount = 0
foreach ($match in $matches) {
    $id = $match.Groups[2].Value
    if ($id -notmatch '^9[45678]0000' -and $id -ne '638637170' -and $id -ne '638637167') { continue }
    $plainYaml = [regex]::Replace($match.Value, '^--- !u!\d+ &\d+\r?\n', '')
    $reader = New-Object IO.StringReader($plainYaml)
    try { $parsed = $serializer.Deserialize($reader) }
    finally { $reader.Dispose() }
    if ($null -eq $parsed) { throw "Empty YAML block: $id" }
    $parsedCount++
    foreach ($reference in [regex]::Matches($match.Value, '\{fileID: (9[45678]0000\d+)\}')) {
        if (!$blocks.ContainsKey($reference.Groups[1].Value)) { throw "Missing menu reference in $id" }
    }
    if ($match.Groups[1].Value -eq '224') {
        $parentId = [regex]::Match($match.Value, 'm_Father: \{fileID: (\d+)\}').Groups[1].Value
        if (!$blocks.ContainsKey($parentId) -or !$blocks[$parentId].Contains('  - {fileID: ' + $id + '}')) { throw "Broken parent/child relationship: $id" }
    }
}
$controller = $blocks['638637170']
foreach ($field in @('playButton','settingsButton','menuButton','resultsMenuButton','backButton','applyButton','defaultsButton','languageButton','screenButton','resolutionButton','qualityButton','vSyncButton','frameLimitButton')) {
    $id = [regex]::Match($controller, '  ' + $field + ': \{fileID: (\d+)\}').Groups[1].Value
    if (!$blocks.ContainsKey($id) -or !$blocks[$id].Contains('4e29b1a8efbd4b44bb3f3716e73f07ff')) { throw "Unassigned button: $field" }
}
foreach ($field in @('masterVolume','effectsVolume','musicVolume')) {
    $id = [regex]::Match($controller, '  ' + $field + ': \{fileID: (\d+)\}').Groups[1].Value
    if (!$blocks.ContainsKey($id) -or !$blocks[$id].Contains('67db9e8f0e2ae9c40bc1e2b64352a6b4')) { throw "Unassigned slider: $field" }
    foreach ($property in @('m_FillRect','m_HandleRect')) {
        $rectId = [regex]::Match($blocks[$id], $property + ': \{fileID: (\d+)\}').Groups[1].Value
        if (!$blocks.ContainsKey($rectId)) { throw "Unassigned slider visual: $property" }
    }
}
if ($blocks['232414835'] -notmatch 'm_Enabled: 0') { throw 'Round must wait for the Play button.' }
foreach ($field in @('mainPanel','settingsPanel','navigation')) {
    $id = [regex]::Match($controller, '  ' + $field + ': \{fileID: (\d+)\}').Groups[1].Value
    $active = if ($field -eq 'mainPanel') { 1 } else { 0 }
    if ($blocks[$id] -notmatch ('m_IsActive: ' + $active)) { throw "Unexpected initial visibility: $field" }
}
Write-Output "PASS: $parsedCount menu YAML blocks parsed, all navigation/settings controls assigned, reciprocal hierarchy and correct initial menu state. No Unity editor actions."
