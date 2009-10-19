setlocal EnableDelayedExpansion 
for /r %%i in (bin\Win32\Release_Unicode\*.dll) do set binaries=!binaries! "%%i"
for /r %%i in (bin\Win32\Release_Unicode\*.exe) do set binaries=!binaries! "%%i"
for /r %%i in ("bin\Win32\Standalone Release Unicode\*.dll") do set binaries=!binaries! "%%i"
for /r %%i in ("bin\Win32\Standalone Release Unicode\*.exe") do set binaries=!binaries! "%%i"
for /r %%i in (bin\x64\Release_Unicode\*.dll) do set binaries=!binaries! "%%i"
for /r %%i in (bin\x64\Release_Unicode\*.exe) do set binaries=!binaries! "%%i"

signtool sign /a /t http://timestamp.verisign.com/scripts/timestamp.dll !binaries!
