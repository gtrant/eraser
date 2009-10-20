setlocal EnableDelayedExpansion 
for /r "%~1\bin\Release\" %%i in (*.dll) do set binaries=!binaries! "%%i"
for /r "%~1\bin\Release\" %%i in (*.exe) do (
	set j=%%i
	if "!j:~-10!" neq "vshost.exe" (
	if "!j:~-16!" neq "Bootstrapper.exe" (
		set binaries=!binaries! "%%i"
	)
	)
)

signtool sign /a /t http://timestamp.verisign.com/scripts/timestamp.dll !binaries!
