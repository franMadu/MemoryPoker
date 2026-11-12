$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class TimerSpriteExport {
 static double Coverage(int x,int y,int size,double radius){
  double cx=x+0.5-size/2.0,cy=y+0.5-size/2.0;
  double distance=Math.Sqrt(cx*cx+cy*cy)-radius;
  if(distance < -0.8)return 1;
  if(distance > 0.8)return 0;
  int n=0;
  for(int j=0;j<4;j++)for(int i=0;i<4;i++){
   cx=x+(i+0.5)/4-size/2.0;cy=y+(j+0.5)/4-size/2.0;
   if(cx*cx+cy*cy<=radius*radius)n++;
  }
  return n/16.0;
 }
 static void Finish(Bitmap b,string path,double radius,bool white){
  var rect=new Rectangle(0,0,b.Width,b.Height);
  var data=b.LockBits(rect,ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
  var p=new int[b.Width*b.Height];Marshal.Copy(data.Scan0,p,0,p.Length);
  for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++){
   int i=y*b.Width+x,a=(int)Math.Round(Coverage(x,y,b.Width,radius)*255);
   p[i]=a==0?0:(a<<24)|(white?0xffffff:p[i]&0xffffff);
  }
  Marshal.Copy(p,0,data.Scan0,p.Length);b.UnlockBits(data);
  b.Save(path,ImageFormat.Png);
 }
 public static void Export(string source,string icon,string mask){
  using(var input=new Bitmap(source))using(var output=new Bitmap(1024,1024,PixelFormat.Format32bppArgb)){
   using(var g=Graphics.FromImage(output)){
    g.Clear(Color.FromArgb(7,25,52));
    g.InterpolationMode=InterpolationMode.HighQualityBicubic;
    g.PixelOffsetMode=PixelOffsetMode.HighQuality;
    g.DrawImage(input,new Rectangle(32,32,960,960),new Rectangle(28,40,1188,1172),GraphicsUnit.Pixel);
   }
   Finish(output,icon,480,false);
  }
  using(var output=new Bitmap(256,256,PixelFormat.Format32bppArgb)){
   using(var g=Graphics.FromImage(output))g.Clear(Color.White);
   Finish(output,mask,128,true);
  }
 }
}
'@
$projectRoot=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
[TimerSpriteExport]::Export((Join-Path $PSScriptRoot 'clock-generated.png'),(Join-Path $projectRoot 'Assets/Images/UI/clock-timer.png'),(Join-Path $projectRoot 'Assets/Images/UI/timer-circle-mask.png'))
Write-Output 'Exported a circular clock with an opaque face and a separate radial shading mask.'
