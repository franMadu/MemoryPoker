$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$projectRoot=Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent
$assetDirectory=Join-Path $projectRoot 'Assets/Images/PlayingCardBacks'
$backupDirectory=Join-Path $PSScriptRoot 'before-rounded-frame'
$stageDirectory=Join-Path $PSScriptRoot 'rounded-frame'
if(Test-Path -LiteralPath $backupDirectory){throw 'Cleanup already applied; backup exists.'}
$files=@(Get-ChildItem -LiteralPath $assetDirectory -Filter '*.png')
if($files.Count -ne 17){throw 'Expected 17 card backs.'}
New-Item -ItemType Directory -Path $backupDirectory,$stageDirectory -Force | Out-Null
Copy-Item -Path (Join-Path $assetDirectory '*.png') -Destination $backupDirectory
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class BackRoundedFrame {
 static int[] Read(Bitmap b) {
  var d=b.LockBits(new Rectangle(0,0,b.Width,b.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
  try {var p=new int[b.Width*b.Height];Marshal.Copy(d.Scan0,p,0,p.Length);return p;}finally{b.UnlockBits(d);}
 }
 static bool Perimeter(int x,int y){return x<94 || x>=930 || y<116 || y>=1420 || ((x<160 || x>=864) && (y<200 || y>=1336));}
 static bool Gray(int c){int r=(c>>16)&255,g=(c>>8)&255,b=c&255;return r>65 && g>80 && b>90 && b>r*0.65;}
 public static string Apply(string original,string reference,string destination) {
  using(var b=new Bitmap(original))using(var edit=new Bitmap(reference)) {
   if(b.Width!=1024 || b.Height!=1536 || edit.Width!=1024 || edit.Height!=1536)throw new Exception("Unexpected dimensions");
   int[] p=Read(b),e=Read(edit),before=(int[])p.Clone();int changed=0,grayBefore=0,grayAfter=0;
   for(int y=0;y<1536;y++)for(int x=0;x<1024;x++) {
    int i=y*1024+x,a=(int)((uint)p[i]>>24);
    if(a==0 || !Perimeter(x,y))continue;
    if(Gray(p[i]))grayBefore++;
    int rgb=e[i]&0x00ffffff;
    // The generated edit supplies the frame artwork; the exact established alpha is retained.
    if(((uint)e[i]>>24)<240)rgb=0x032039;
    p[i]=(a<<24)|rgb;
    if(Gray(p[i]))grayAfter++;
    if(p[i]!=before[i])changed++;
   }
   for(int y=0;y<1536;y++)for(int x=0;x<1024;x++) {
    int i=y*1024+x;
    if(((uint)p[i]>>24)!=((uint)before[i]>>24))throw new Exception("Alpha changed");
    if(!Perimeter(x,y) && p[i]!=before[i])throw new Exception("Interior artwork changed");
   }
   if(grayAfter!=0)throw new Exception("Gray frame pixels remain: "+grayAfter);
   var d=b.LockBits(new Rectangle(0,0,1024,1536),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
   Marshal.Copy(p,0,d.Scan0,p.Length);b.UnlockBits(d);b.Save(destination,ImageFormat.Png);
   return "Perimeter pixels changed="+changed+"; gray pixels before="+grayBefore+"; gray pixels after="+grayAfter+"; alpha and central artwork identical";
  }
 }
}
'@
$report=@()
foreach($file in $files){
    $result=[BackRoundedFrame]::Apply($file.FullName,(Join-Path $PSScriptRoot 'rounded-frame-generated.png'),(Join-Path $stageDirectory $file.Name))
    $report+=[PSCustomObject]@{File=$file.Name;Validation=$result}
}
if($report.Count -ne 17){throw 'Incomplete set.'}
Copy-Item -Path (Join-Path $stageDirectory '*.png') -Destination $assetDirectory -Force
$report | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'rounded-frame-report.json') -Encoding UTF8

# Update the gallery using the existing layout, without running the earlier crop again.
$galleryScript=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'remove-white-rims.ps1') -Raw
$galleryStart=$galleryScript.IndexOf('# Refresh the gallery')
if($galleryStart -lt 0){throw 'Cannot locate gallery renderer.'}
$galleryScript=$galleryScript.Substring($galleryStart)
$galleryScript=$galleryScript.Substring(0,$galleryScript.LastIndexOf('Write-Output'))
$galleryDirectory=$PSScriptRoot
$galleryScript=$galleryScript.Replace('$PSScriptRoot','$galleryDirectory')
& ([scriptblock]::Create($galleryScript))
Write-Output 'Rounded all 17 gold frames; alpha, dimensions and central letters preserved.'

