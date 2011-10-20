@echo off
rem Replace all template version placeholders in the given template file for keywords.

if not exist "%ProgramFiles%\TortoiseSVN\bin\SubWCRev.exe" goto searchWoWProgramFiles
"%ProgramFiles%\TortoiseSVN\bin\SubWCRev.exe" %1 %2 %3 -f
if ERRORLEVEL 1 (
	goto noSubWCRev
)
goto end

:searchWoWProgramFiles

"%ProgramW6432%\TortoiseSVN\bin\SubWCRev.exe" %1 %2 %3 -f
if ERRORLEVEL 1 (
	goto noSubWCRev
)

goto end

:noSubWCRev
echo No TortoiseSVN-Client (SubWCRev.exe) detected! >&2
exit /b 1

:end
