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

"C:\codesign\signtool.exe" sign /a /t http://time.certum.pl !binaries!
if %errorlevel% geq 1 (
	echo The Eraser binaries were not signed; see the signtool log for details.
	exit 0;
)
