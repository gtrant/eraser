#include "Eraser.iss"
[Setup]
OutputBaseFilename={#EraserSafeVerString}_x86
ArchitecturesAllowed=x86

[Files]
Source: win32\release\Eraser.exe; DestDir: {app}; Flags: ignoreversion restartreplace uninsrestartdelete 32bit
Source: win32\release\Eraserl.exe; DestDir: {sys}; Flags: ignoreversion restartreplace uninsrestartdelete 32bit
Source: win32\release\Eraser.dll; DestDir: {sys}; Flags: ignoreversion restartreplace uninsrestartdelete 32bit
Source: win32\release\Erasext.dll; DestDir: {sys}; Flags: ignoreversion restartreplace uninsrestartdelete 32bit
Source: win32\release\Verify.exe; DestDir: {app}; Flags: ignoreversion restartreplace uninsrestartdelete 32bit; Components: Verify
#ifndef VS90
Source: vcredist_x86.exe; DestDir: {tmp}; DestName: vcredist.exe; Flags: deleteafterinstall
#endif
#if defined(VS90) && !defined(PRIVATE_BUILD)
Source: C:\Program Files\Microsoft Visual Studio 9\VC\redist\x86\Microsoft.VC90.CRT\*; DestDir: {app}; Flags: 32bit
Source: C:\Program Files\Microsoft Visual Studio 9\VC\redist\x86\Microsoft.VC90.CRT\*; DestDir: {sys}; Flags: 32bit
Source: C:\Program Files\Microsoft Visual Studio 9\VC\redist\x86\Microsoft.VC90.MFC\*; DestDir: {app}; Flags: 32bit
Source: C:\Program Files\Microsoft Visual Studio 9\VC\redist\x86\Microsoft.VC90.MFC\*; DestDir: {sys}; Flags: 32bit
#endif
