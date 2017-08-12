@ECHO OFF

REM This File Calls into the bootstrapper for cake build script
REM As long as you invoke the build.ps1 file with this batch file, you do not have to change your execution policy for PowerShell scripts manually.
REM Do not use this file for other purposes!

powershell -ExecutionPolicy Unrestricted ./build.ps1 %CAKE_ARGS% %*

PAUSE