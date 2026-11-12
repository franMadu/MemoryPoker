$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$projectRoot = Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent
$assetDirectory = Join-Path $projectRoot 'Assets/Images/PlayingCardBacks'
$originalDirectory = Join-Path $PSScriptRoot 'originals'
$stagingDirectory = Join-Path $PSScriptRoot 'normalized'
$letters = 'ABCDEFGHIJKLMNOP'.ToCharArray()
foreach ($letter in $letters) {
    if (!(Test-Path -LiteralPath (Join-Path $originalDirectory ('back_'+$letter+'.png')))) { throw ('Missing original: '+$letter) }
}
# Reuse the exact geometry and pixel-by-pixel mask validator used for the 52 faces.
$faceScript = Get-Content -LiteralPath (Join-Path (Split-Path $PSScriptRoot -Parent) 'normalize-cards.ps1') -Raw
$classMatch = [regex]::Match($faceScript, "(?s)Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'\r?\n(.*?)\r?\n'@")
if (!$classMatch.Success) { throw 'Cannot find the shared card normalizer.' }
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition $classMatch.Groups[1].Value
New-Item -ItemType Directory -Path $stagingDirectory, $assetDirectory -Force | Out-Null
$report = @()
foreach ($letter in $letters) {
    $name = 'back_'+$letter+'.png'
    $inputPath = Join-Path $originalDirectory $name
    $bitmap = [System.Drawing.Bitmap]::FromFile($inputPath)
    $left=@();$right=@();$top=@();$bottom=@()
    foreach ($step in 0..16) {
        $y=[int]($bitmap.Height*(0.2+0.6*$step/16))
        $x=[int]($bitmap.Width*(0.2+0.6*$step/16))
        for($p=0;$p -lt $bitmap.Width;$p++) { $c=$bitmap.GetPixel($p,$y);if($c.A -gt 200 -and $c.R -lt 180 -and $c.G -lt 180 -and $c.B -lt 180){$left+=$p;break} }
        for($p=$bitmap.Width-1;$p -ge 0;$p--) { $c=$bitmap.GetPixel($p,$y);if($c.A -gt 200 -and $c.R -lt 180 -and $c.G -lt 180 -and $c.B -lt 180){$right+=$p;break} }
        for($p=0;$p -lt $bitmap.Height;$p++) { $c=$bitmap.GetPixel($x,$p);if($c.A -gt 200 -and $c.R -lt 180 -and $c.G -lt 180 -and $c.B -lt 180){$top+=$p;break} }
        for($p=$bitmap.Height-1;$p -ge 0;$p--) { $c=$bitmap.GetPixel($x,$p);if($c.A -gt 200 -and $c.R -lt 180 -and $c.G -lt 180 -and $c.B -lt 180){$bottom+=$p;break} }
    }
    $bitmap.Dispose()
    if($left.Count -ne 17 -or $right.Count -ne 17 -or $top.Count -ne 17 -or $bottom.Count -ne 17){throw ('Incomplete outline detection: '+$name)}
    $l=($left|Sort-Object)[8];$r=($right|Sort-Object)[8];$t=($top|Sort-Object)[8];$b=($bottom|Sort-Object)[8]
    $outlinePixels = [CardNormalizer]::Normalize($inputPath,(Join-Path $stagingDirectory $name),$l,$t,$r,$b)
    $validation = [CardNormalizer]::Validate((Join-Path $stagingDirectory $name))
    $report += [PSCustomObject]@{File=$name;Letter=[string]$letter;SourceBounds=@($l,$t,$r,$b);RemovedOutlinePixels=$outlinePixels;Validation=$validation}
}
if($report.Count -ne 16){throw 'Incomplete set; assets were not copied.'}
Copy-Item -Path (Join-Path $stagingDirectory '*.png') -Destination $assetDirectory -Force
foreach($letter in $letters){
    $name='back_'+$letter+'.png'
    if((Get-FileHash -LiteralPath (Join-Path $stagingDirectory $name)).Hash -ne (Get-FileHash -LiteralPath (Join-Path $assetDirectory $name)).Hash){throw ('Installed image mismatch: '+$name)}
}
$report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'normalization-report.json') -Encoding UTF8

$preview = New-Object System.Drawing.Bitmap(1024,1632)
$g = [System.Drawing.Graphics]::FromImage($preview)
$light=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(190,199,198))
$dark=New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(145,157,155))
for($y=0;$y -lt 1632;$y+=16){for($x=0;$x -lt 1024;$x+=16){
    $brush=if(([int]($x/16)+[int]($y/16))%2 -eq 0){$light}else{$dark}
    $g.FillRectangle($brush,$x,$y,16,16)
}}
$font=New-Object System.Drawing.Font('Arial',12)
for($i=0;$i -lt 16;$i++){
    $row=[int][Math]::Floor($i/4);$col=$i%4
    $source=[System.Drawing.Image]::FromFile((Join-Path $assetDirectory ('back_'+$letters[$i]+'.png')))
    $g.DrawImage($source,$col*256,$row*408,256,384)
    $source.Dispose()
    $g.FillRectangle([System.Drawing.Brushes]::DarkSlateGray,$col*256,$row*408+384,256,24)
    $g.DrawString([string]$letters[$i],$font,[System.Drawing.Brushes]::White,$col*256+120,$row*408+386)
}
$preview.Save((Join-Path $PSScriptRoot 'backs-preview.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose();$preview.Dispose();$font.Dispose();$light.Dispose();$dark.Dispose()
Write-Output 'Validated and installed 16 card backs, A to P, using the same mask and bounds as the 52 faces.'
