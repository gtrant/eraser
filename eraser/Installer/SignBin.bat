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

"C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe" sign /a /tr http://rfc3161timestamp.globalsign.com/advanced /sha1 a77cb0890edacb1628c0a2e55f2bfc02a6ca26e8 /td SHA256 /fd SHA256 !binaries!
if %errorlevel% geq 1 (
	echo The Eraser binaries were not signed; see the signtool log for details.
	exit 0;
)
