$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$projectRoot = Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent
$assetDirectory = Join-Path $projectRoot 'Assets/Images/PlayingCardBacks'
$backupDirectory = Join-Path $PSScriptRoot 'before-full-bleed'
$stageDirectory = Join-Path $PSScriptRoot 'full-bleed'
if(Test-Path -LiteralPath $backupDirectory){throw 'Backup already exists; refusing to crop the backs twice.'}
$files = @(Get-ChildItem -LiteralPath $assetDirectory -Filter '*.png')
if($files.Count -ne 17){throw 'Expected 17 card backs.'}
New-Item -ItemType Directory -Path $backupDirectory,$stageDirectory -Force | Out-Null
Copy-Item -Path (Join-Path $assetDirectory '*.png') -Destination $backupDirectory

# Reuse the authorized crop, resampling and exact rounded alpha mask.
$faceScript = Get-Content -LiteralPath (Join-Path (Split-Path $PSScriptRoot -Parent) 'normalize-cards.ps1') -Raw
$match = [regex]::Match($faceScript,"(?s)Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'\r?\n(.*?)\r?\n'@")
if(!$match.Success){throw 'Cannot find the shared normalizer.'}
$source = $match.Groups[1].Value
$start = $source.IndexOf('// Remove the connected original outline')
$end = $source.IndexOf('using(var cleaned=', $start)
if($start -lt 0 -or $end -lt 0){throw 'Cannot locate outline processing.'}
$source = $source.Substring(0,$start) + 'int count=0;' + $source.Substring($end)
$source = $source.Replace('double ratio=Coverage(x,y,4)/outer;', 'double ratio=1;').Replace('border=4','border=0')
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition $source

function Test-NavyPixel($color) {
    return ($color.A -gt 240 -and $color.R -lt 65 -and $color.G -lt 110 -and $color.B -lt 170 -and $color.B -gt ($color.R+8))
}
$report = @()
foreach($file in $files){
    $bitmap = [System.Drawing.Bitmap]::FromFile($file.FullName)
    $left=@();$right=@();$top=@();$bottom=@()
    foreach($step in 0..16){
        $y=[int]($bitmap.Height*(0.25+0.5*$step/16))
        $x=[int]($bitmap.Width*(0.25+0.5*$step/16))
        for($p=50;$p -lt $bitmap.Width/2;$p++){if(Test-NavyPixel ($bitmap.GetPixel($p,$y))){$left+=$p;break}}
        for($p=973;$p -gt $bitmap.Width/2;$p--){if(Test-NavyPixel ($bitmap.GetPixel($p,$y))){$right+=$p;break}}
        for($p=75;$p -lt $bitmap.Height/2;$p++){if(Test-NavyPixel ($bitmap.GetPixel($x,$p))){$top+=$p;break}}
        for($p=1460;$p -gt $bitmap.Height/2;$p--){if(Test-NavyPixel ($bitmap.GetPixel($x,$p))){$bottom+=$p;break}}
    }
    $bitmap.Dispose()
    if($left.Count -ne 17 -or $right.Count -ne 17 -or $top.Count -ne 17 -or $bottom.Count -ne 17){throw ('Cannot locate printed panel: '+$file.Name)}
    # Use the median panel edge and inset by two pixels to exclude cream antialiasing.
    $l=($left|Sort-Object)[8]+2;$r=($right|Sort-Object)[8]-2
    $t=($top|Sort-Object)[8]+2;$b=($bottom|Sort-Object)[8]-2
    if($l -lt 60 -or $l -gt 160 -or $r -lt 860 -or $r -gt 963 -or $t -lt 85 -or $t -gt 190 -or $b -lt 1340 -or $b -gt 1450){throw ('Unexpected panel bounds: '+$file.Name)}
    $outputPath = Join-Path $stageDirectory $file.Name
    [void][CardNormalizer]::Normalize($file.FullName,$outputPath,$l,$t,$r,$b)
    $validation = [CardNormalizer]::Validate($outputPath)
    $check = [System.Drawing.Bitmap]::FromFile($outputPath)
    $whiteEdgePixels=0
    foreach($y in 180..1350){foreach($x in @(60,963)){
        $c=$check.GetPixel($x,$y)
        if($c.R -gt 205 -and $c.G -gt 205 -and $c.B -gt 205){$whiteEdgePixels++}
    }}
    foreach($x in 150..870){foreach($y in @(85,1450)){
        $c=$check.GetPixel($x,$y)
        if($c.R -gt 205 -and $c.G -gt 205 -and $c.B -gt 205){$whiteEdgePixels++}
    }}
    $check.Dispose()
    if($whiteEdgePixels -ne 0){throw ('White rim remains: '+$file.Name)}
    $report += [PSCustomObject]@{File=$file.Name;PrintedPanelCrop=@($l,$t,$r,$b);WhiteEdgePixels=$whiteEdgePixels;Validation=$validation}
}
if($report.Count -ne 17){throw 'Incomplete output set.'}
Copy-Item -Path (Join-Path $stageDirectory '*.png') -Destination $assetDirectory -Force
$report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'full-bleed-report.json') -Encoding UTF8

# Refresh the gallery, including the back without a letter.
$names=@('A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','blank')
$preview=New-Object System.Drawing.Bitmap(1024,2040)
$g=[System.Drawing.Graphics]::FromImage($preview)
$light=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(190,199,198))
$dark=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(145,157,155))
for($y=0;$y -lt 2040;$y+=16){for($x=0;$x -lt 1024;$x+=16){
    $brush=if(([int]($x/16)+[int]($y/16))%2 -eq 0){$light}else{$dark}
    $g.FillRectangle($brush,$x,$y,16,16)
}}
$font=New-Object System.Drawing.Font('Arial',12)
for($i=0;$i -lt $names.Count;$i++){
    $row=[int][Math]::Floor($i/4);$col=$i%4
    $card=[System.Drawing.Image]::FromFile((Join-Path $assetDirectory ('back_'+$names[$i]+'.png')))
    $g.DrawImage($card,$col*256,$row*408,256,384);$card.Dispose()
    $g.FillRectangle([System.Drawing.Brushes]::DarkSlateGray,$col*256,$row*408+384,256,24)
    $label=if($names[$i] -eq 'blank'){'Sin letra'}else{$names[$i]}
    $g.DrawString($label,$font,[System.Drawing.Brushes]::White,$col*256+100,$row*408+386)
}
$preview.Save((Join-Path $PSScriptRoot 'backs-preview.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose();$preview.Dispose();$font.Dispose();$light.Dispose();$dark.Dispose()
Write-Output 'Corrected 17 backs: artwork fills the entire card, no white rim; original bounds and alpha preserved.'
