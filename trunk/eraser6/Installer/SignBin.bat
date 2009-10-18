goto :Sign

:: pass params to a subroutine
SET _params=%*
CALL :Sign "%_params%"
GOTO :eof

:Sign
@rem Core binaries
setlocal EnableDelayedExpansion 
for /r %%i in (%1\bin\Release\*.dll) do set binaries=!binaries! %%i
for /r %%i in (%1\bin\Release\*.exe) do set binaries=!binaries! %%i
signtool sign /a  /t http://timestamp.verisign.com/scripts/timestamp.dll !binaries!
