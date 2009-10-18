setlocal EnableDelayedExpansion 
for /r %%i in (%1\bin\Release\*.dll) do set binaries=!binaries! "%%i"
for /r %%i in (%1\bin\Release\*.exe) do (
	set j=%%i
	if "!j:~-10!" neq "vshost.exe" (
		set binaries=!binaries! "%%i"
	)
)

signtool sign /a /t http://timestamp.verisign.com/scripts/timestamp.dll !binaries!
