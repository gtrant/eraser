setlocal EnableDelayedExpansion 
for /r "bin\Win32\Release_Unicode" %%i in (*.dll) do set binaries=!binaries! "%%i"
for /r "bin\Win32\Release_Unicode" %%i in (*.exe) do set binaries=!binaries! "%%i"
for /r "bin\Win32\Standalone Release Unicode" %%i in (*.dll) do set binaries=!binaries! "%%i"
for /r "bin\Win32\Standalone Release Unicode" %%i in (*.exe) do set binaries=!binaries! "%%i"
for /r "bin\x64\Release_Unicode" %%i in (*.dll) do set binaries=!binaries! "%%i"
for /r "bin\x64\Release_Unicode" %%i in (*.exe) do set binaries=!binaries! "%%i"

signtool sign /a /t http://timestamp.verisign.com/scripts/timestamp.dll !binaries!
