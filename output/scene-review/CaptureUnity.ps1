param([string]$Name = 'after')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class UnityWindowSnapshot {
 [StructLayout(LayoutKind.Sequential)] struct Rect {public int Left,Top,Right,Bottom;}
 [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr window,out Rect rect);
 [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr window,IntPtr device,uint flags);
 public static void Save(IntPtr window,string path) {
  Rect r;if(!GetWindowRect(window,out r))throw new Exception("Unity window not found");
  using(var b=new Bitmap(r.Right-r.Left,r.Bottom-r.Top))using(var g=Graphics.FromImage(b)){
   IntPtr dc=g.GetHdc();bool ok;
   try{ok=PrintWindow(window,dc,2);}finally{g.ReleaseHdc(dc);}
   if(!ok)throw new Exception("Unity window capture failed");
   b.Save(path,ImageFormat.Png);
  }
 }
}
'@
$window=Get-Process -Name Unity|Where-Object{$_.MainWindowTitle -like 'Memory Poker -*'}|Select-Object -First 1
if(!$window){throw 'Memory Poker editor window not found.'}
$editorPath=Join-Path $PSScriptRoot ('editor-'+$Name+'.png')
[UnityWindowSnapshot]::Save($window.MainWindowHandle,$editorPath)
$source=[System.Drawing.Bitmap]::FromFile($editorPath)
try{
    $rect=New-Object System.Drawing.Rectangle([int]($source.Width*0.154),[int]($source.Height*0.095),[int]($source.Width*0.410),[int]($source.Height*0.565))
    $game=$source.Clone($rect,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try{$game.Save((Join-Path $PSScriptRoot ('game-'+$Name+'.png')),[System.Drawing.Imaging.ImageFormat]::Png)}finally{$game.Dispose()}
}finally{$source.Dispose()}
