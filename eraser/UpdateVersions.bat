@echo off
rem Replace all template version placeholders in the given template file for keywords.

if not exist "%ProgramFiles%\TortoiseSVN\bin\SubWCRev.exe" goto searchWoWProgramFiles
"%ProgramFiles%\TortoiseSVN\bin\SubWCRev.exe" %1 %2 %3 -f
if ERRORLEVEL 1 (
	exit /b %ERRORLEVEL%
)
goto end

:searchWoWProgramFiles

if not exist "%ProgramFiles(x86)%\TortoiseSVN\bin\SubWCRev.exe" goto searchPath
"%ProgramFiles(x86)%\TortoiseSVN\bin\SubWCRev.exe" %1 %2 %3 -f
if ERRORLEVEL 1 (
	exit /b %ERRORLEVEL%
)
goto end

:searchPath

for %i in (SubWCRev.exe) do (
	if exist "%~$PATH:i" (
		"SubWCRev.exe" %1 %2 %3 -f
	) else (
		goto noSubWCRev
	)
)
if ERRORLEVEL 1 (
	exit /b %ERRORLEVEL%
)

goto end

:noSubWCRev
echo No TortoiseSVN-Client (SubWCRev.exe) detected! >&2
exit /b 1

:end
