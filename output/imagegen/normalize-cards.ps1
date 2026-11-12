$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$projectRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$assetDirectory = Join-Path $projectRoot 'Assets/Images/PlayingCards'
$backupDirectory = Join-Path $PSScriptRoot 'originals-before-normalization'
$stagingDirectory = Join-Path $PSScriptRoot 'normalized'
if (Test-Path -LiteralPath $backupDirectory) { throw 'Original backup already exists; refusing to process the deck twice.' }
$geometry = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'geometry-before.json') -Raw | ConvertFrom-Json
if ($geometry.Count -ne 52) { throw 'Expected geometry for 52 cards.' }
New-Item -ItemType Directory -Path $backupDirectory, $stagingDirectory -Force | Out-Null
Copy-Item -Path (Join-Path $assetDirectory '*.png') -Destination $backupDirectory
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'deck-preview.jpg') -Destination (Join-Path $PSScriptRoot 'deck-preview-before.jpg')

Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public static class CardNormalizer {
    const int W=1024,H=1536,L=50,T=75,CW=924,CH=1386;
    public static int[] Read(Bitmap image) {
        var rect=new Rectangle(0,0,image.Width,image.Height);
        var data=image.LockBits(rect,ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        try { var pixels=new int[image.Width*image.Height]; Marshal.Copy(data.Scan0,pixels,0,pixels.Length); return pixels; }
        finally { image.UnlockBits(data); }
    }
    static bool Dark(int c) { return ((c>>16)&255)<210 && ((c>>8)&255)<210 && (c&255)<210; }
    static double Distance(double x,double y,double left,double top,double width,double height,double radius) {
        double qx=Math.Abs(x-left-width/2)-(width/2-radius);
        double qy=Math.Abs(y-top-height/2)-(height/2-radius);
        return Math.Sqrt(Math.Max(qx,0)*Math.Max(qx,0)+Math.Max(qy,0)*Math.Max(qy,0))+Math.Min(Math.Max(qx,qy),0)-radius;
    }
    static double Coverage(int x,int y,int inset) {
        double d=Distance(x+0.5,y+0.5,L+inset,T+inset,CW-2*inset,CH-2*inset,48-inset);
        if(d < -0.8) return 1;
        if(d > 0.8) return 0;
        int inside=0;
        for(int sy=0;sy<4;sy++) for(int sx=0;sx<4;sx++)
            if(Distance(x+(sx+0.5)/4,y+(sy+0.5)/4,L+inset,T+inset,CW-2*inset,CH-2*inset,48-inset)<=0) inside++;
        return inside/16.0;
    }
    public static int Normalize(string input,string output,int left,int top,int right,int bottom) {
        int sw=right-left+1,sh=bottom-top+1;
        int[] pixels;
        using(var source=new Bitmap(input))
        using(var crop=new Bitmap(sw,sh,PixelFormat.Format32bppArgb)) {
            using(var g=Graphics.FromImage(crop)) {
                g.Clear(Color.White);
                g.DrawImage(source,new Rectangle(0,0,sw,sh),new Rectangle(left,top,sw,sh),GraphicsUnit.Pixel);
            }
            pixels=Read(crop);
        }
        // Remove the connected original outline, leaving the printed artwork intact.
        var border=new bool[pixels.Length];
        var queue=new Queue<int>();
        int seed=-1;
        for(int x=0;x<Math.Min(sw,20);x++) if(Dark(pixels[(sh/2)*sw+x])) {seed=(sh/2)*sw+x;break;}
        if(seed<0) throw new Exception("Cannot locate card outline: "+input);
        border[seed]=true;queue.Enqueue(seed);int count=0;
        while(queue.Count>0) {
            int p=queue.Dequeue();count++;int x=p%sw,y=p/sw;
            foreach(int n in new int[]{x>0?p-1:-1,x+1<sw?p+1:-1,y>0?p-sw:-1,y+1<sh?p+sw:-1})
                if(n>=0 && !border[n] && Dark(pixels[n])) {border[n]=true;queue.Enqueue(n);}
        }
        if(count<1000 || count>pixels.Length/12) throw new Exception("Unexpected outline component size: "+input+" ("+count+")");
        // The four corner regions outside the old outline must also become white.
        var exterior=new bool[pixels.Length];queue.Clear();
        foreach(int p in new int[]{0,sw-1,(sh-1)*sw,pixels.Length-1}) if(!border[p] && !exterior[p]) {exterior[p]=true;queue.Enqueue(p);}
        int outsideCount=0;
        while(queue.Count>0) {
            int p=queue.Dequeue();outsideCount++;int x=p%sw,y=p/sw;
            foreach(int n in new int[]{x>0?p-1:-1,x+1<sw?p+1:-1,y>0?p-sw:-1,y+1<sh?p+sw:-1})
                if(n>=0 && !border[n] && !exterior[n]) {exterior[n]=true;queue.Enqueue(n);}
        }
        if(outsideCount>pixels.Length/10) throw new Exception("Original outline is not closed: "+input);
        var erase=(bool[])exterior.Clone();
        for(int p=0;p<pixels.Length;p++) if(border[p]) {
            int x=p%sw,y=p/sw;
            for(int dy=-2;dy<=2;dy++) for(int dx=-2;dx<=2;dx++)
                if(x+dx>=0 && x+dx<sw && y+dy>=0 && y+dy<sh) erase[(y+dy)*sw+x+dx]=true;
        }
        for(int p=0;p<pixels.Length;p++) if(erase[p]) pixels[p]=unchecked((int)0xffffffff);
        using(var cleaned=new Bitmap(sw,sh,PixelFormat.Format32bppArgb))
        using(var resized=new Bitmap(W,H,PixelFormat.Format32bppArgb)) {
            var data=cleaned.LockBits(new Rectangle(0,0,sw,sh),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
            Marshal.Copy(pixels,0,data.Scan0,pixels.Length);cleaned.UnlockBits(data);
            using(var g=Graphics.FromImage(resized)) {
                g.Clear(Color.White);
                g.InterpolationMode=InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                g.DrawImage(cleaned,new Rectangle(L,T,CW,CH),new Rectangle(0,0,sw,sh),GraphicsUnit.Pixel);
            }
            var final=Read(resized);
            for(int y=0;y<H;y++) for(int x=0;x<W;x++) {
                int p=y*W+x;
                double outer=Coverage(x,y,0);
                if(outer==0) {final[p]=0;continue;}
                double ratio=Coverage(x,y,4)/outer;
                int c=final[p],a=(int)Math.Round(255*outer);
                int r=(int)Math.Round(((c>>16)&255)*ratio),g=(int)Math.Round(((c>>8)&255)*ratio),b=(int)Math.Round((c&255)*ratio);
                final[p]=(a<<24)|(r<<16)|(g<<8)|b;
            }
            data=resized.LockBits(new Rectangle(0,0,W,H),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
            Marshal.Copy(final,0,data.Scan0,final.Length);resized.UnlockBits(data);
            resized.Save(output,ImageFormat.Png);
        }
        return count;
    }
    public static string Validate(string path) {
        using(var b=new Bitmap(path)) {
            if(b.Width!=W || b.Height!=H) throw new Exception("Wrong canvas: "+path);
            var p=Read(b);int minX=W,minY=H,maxX=-1,maxY=-1;
            for(int y=0;y<H;y++) for(int x=0;x<W;x++) {
                int alpha=(int)((uint)p[y*W+x]>>24);
                int expected=(int)Math.Round(Coverage(x,y,0)*255);
                if(alpha!=expected) throw new Exception("Alpha mask mismatch: "+path);
                if(alpha>0) {minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
            }
            if(minX!=L || minY!=T || maxX!=L+CW-1 || maxY!=T+CH-1) throw new Exception("Wrong card bounds: "+path);
            return "1024x1536; card=(50,75,924,1386); exterior transparent; interior opaque; radius=48; border=4";
        }
    }
}
'@

$report = @()
foreach ($card in $geometry) {
    $name = $card.Name + '.png'
    $outlinePixels = [CardNormalizer]::Normalize((Join-Path $backupDirectory $name), (Join-Path $stagingDirectory $name), $card.Left, $card.Top, $card.Right, $card.Bottom)
    $validation = [CardNormalizer]::Validate((Join-Path $stagingDirectory $name))
    $report += [PSCustomObject]@{ File=$name; RemovedOutlinePixels=$outlinePixels; Validation=$validation }
}
if ($report.Count -ne 52) { throw 'Incomplete deck; assets were not replaced.' }
Copy-Item -Path (Join-Path $stagingDirectory '*.png') -Destination $assetDirectory -Force
$report | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'normalization-report.json') -Encoding UTF8

# Checkerboard makes exterior transparency visible in the overview.
$preview = New-Object System.Drawing.Bitmap(2340, 1200)
$graphics = [System.Drawing.Graphics]::FromImage($preview)
$light = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(190,199,198))
$dark = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(145,157,155))
for ($y=0; $y -lt 1200; $y+=16) {
    for ($x=0; $x -lt 2340; $x+=16) {
        $brush = if (([int]($x/16)+[int]($y/16))%2 -eq 0) {$light} else {$dark}
        $graphics.FillRectangle($brush,$x,$y,16,16)
    }
}
$font = New-Object System.Drawing.Font('Arial',12)
$suits = @('spades','hearts','diamonds','clubs')
$ranks = @('A','2','3','4','5','6','7','8','9','10','J','Q','K')
for ($row=0; $row -lt 4; $row++) {
    for ($col=0; $col -lt 13; $col++) {
        $name = $suits[$row] + '_' + $ranks[$col] + '.png'
        $source = [System.Drawing.Image]::FromFile((Join-Path $assetDirectory $name))
        $graphics.DrawImage($source,$col*180,$row*300,180,270)
        $source.Dispose()
        $graphics.FillRectangle([System.Drawing.Brushes]::DarkSlateGray,$col*180,$row*300+270,180,30)
        $graphics.DrawString(($suits[$row]+' '+$ranks[$col]),$font,[System.Drawing.Brushes]::White,$col*180+8,$row*300+273)
    }
}
$preview.Save((Join-Path $PSScriptRoot 'deck-preview.jpg'),[System.Drawing.Imaging.ImageFormat]::Jpeg)
$graphics.Dispose();$preview.Dispose();$font.Dispose();$light.Dispose();$dark.Dispose()
Write-Output ('Corrected and validated '+$report.Count+' cards. Originals: '+$backupDirectory)
