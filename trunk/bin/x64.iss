#include "Eraser.iss"
[Setup]
OutputBaseFilename={#EraserSafeVerString}_x64
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64

[Files]
Source: x64\release\Eraser.exe; DestDir: {app}; Flags: ignoreversion restartreplace uninsrestartdelete 64bit
Source: x64\release\Eraserl.exe; DestDir: {sys}; Flags: ignoreversion restartreplace uninsrestartdelete 64bit
Source: x64\release\Eraser.dll; DestDir: {sys}; Flags: ignoreversion restartreplace uninsrestartdelete 64bit
Source: x64\release\Erasext.dll; DestDir: {sys}; Flags: ignoreversion restartreplace uninsrestartdelete 64bit
Source: x64\release\Verify.exe; DestDir: {app}; Flags: ignoreversion restartreplace uninsrestartdelete 64bit; Components: Verify
#ifndef VS90
Source: vcredist_x64.exe; DestDir: {tmp}; DestName: vcredist.exe; Flags: deleteafterinstall
#endif
#if defined(VS90) && !defined(PRIVATE_BUILD)
Source: C:\Program Files\Microsoft Visual Studio 9\VC\redist\amd64\Microsoft.VC90.CRT\*; DestDir: {app}; Flags: 64bit
Source: C:\Program Files\Microsoft Visual Studio 9\VC\redist\amd64\Microsoft.VC90.CRT\*; DestDir: {sys}; Flags: 64bit
#endif
