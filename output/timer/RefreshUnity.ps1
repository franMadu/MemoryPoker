$ErrorActionPreference='Stop'
Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class RefreshPokerEditor {
 [DllImport("user32.dll")] static extern IntPtr GetMenu(IntPtr window);
 [DllImport("user32.dll")] static extern IntPtr GetSubMenu(IntPtr menu,int position);
 [DllImport("user32.dll")] static extern int GetMenuItemCount(IntPtr menu);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetMenuString(IntPtr menu,uint item,StringBuilder text,int length,uint flags);
 [DllImport("user32.dll")] static extern uint GetMenuItemID(IntPtr menu,int position);
 [DllImport("user32.dll")] static extern bool PostMessage(IntPtr window,uint message,IntPtr wParam,IntPtr lParam);
 static string Label(IntPtr menu,int position){var s=new StringBuilder(256);GetMenuString(menu,(uint)position,s,s.Capacity,0x400);return s.ToString().Replace("&","");}
 public static void Refresh(IntPtr window){
  var menu=GetMenu(window);
  for(int i=0;i<GetMenuItemCount(menu);i++)if(Label(menu,i)=="Assets"){
   var assets=GetSubMenu(menu,i);
   for(int j=0;j<GetMenuItemCount(assets);j++)if(Label(assets,j).StartsWith("Refresh\t")){
    if(!PostMessage(window,0x111,(IntPtr)GetMenuItemID(assets,j),IntPtr.Zero))throw new Exception("Refresh request failed");
    return;
   }
  }
  throw new Exception("Assets > Refresh not found");
 }
}
'@
$window=Get-Process -Name Unity|Where-Object{$_.MainWindowTitle -like 'Memory Poker -*'}|Select-Object -First 1
if(!$window){throw 'Memory Poker editor not found.'}
[RefreshPokerEditor]::Refresh($window.MainWindowHandle)
Write-Output 'Requested Unity Assets > Refresh.'
