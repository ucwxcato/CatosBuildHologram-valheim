@echo off
setlocal EnableExtensions

set "REPO=%~dp0.."
for %%I in ("%REPO%") do set "REPO=%%~fI"
set "SERVER=C:\Program Files (x86)\Steam\steamapps\common\Valheim dedicated server"
set "PROFILE=C:\Users\magni\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\CatosBuildHologram"
set "WORLD_SOURCE=C:\Users\magni\Documents\BotsnCoding\Valheim\Dedicated"
set "SAVE_ROOT=C:\Users\magni\Documents\BotsnCoding\Valheim"
set "MOUNT=%SAVE_ROOT%\worlds_local\Dedicated"
set "CLIENT_PROJECT=%REPO%\src\CatosBuildHologram.Client\CatosBuildHologram.Client.csproj"
set "CLIENT_DLL=%REPO%\src\CatosBuildHologram.Client\bin\Release\net48\net48\CatosBuildHologram.Client.dll"
set "CONTRACTS_DLL=%REPO%\src\CatosBuildHologram.Shared\bin\Release\net48\net48\CatosBuildContracts.dll"
set "CLIENT_PLUGINS=%PROFILE%\BepInEx\plugins"
set "SERVER_PLUGINS=%SERVER%\BepInEx\plugins"
set "DEVCOMMANDS_SOURCE=%CLIENT_PLUGINS%\JereKuusela-Server_devcommands"
set "DEVCOMMANDS_DEST=%SERVER_PLUGINS%\JereKuusela-Server_devcommands"
set "SteamAppId=892970"

tasklist /FI "IMAGENAME eq valheim.exe" 2>nul | find /I "valheim.exe" >nul && goto :running
tasklist /FI "IMAGENAME eq valheim_server.exe" 2>nul | find /I "valheim_server.exe" >nul && goto :running

if not exist "%SERVER%\valheim_server.exe" goto :missing
if not exist "%PROFILE%\BepInEx\core\BepInEx.dll" goto :missing
if not exist "%WORLD_SOURCE%\*.db2" goto :missing
if not exist "%WORLD_SOURCE%\*.fwl2" goto :missing
if not exist "%CLIENT_PROJECT%" goto :missing
if not exist "%DEVCOMMANDS_SOURCE%\ServerDevcommands.dll" goto :missing

dotnet build "%CLIENT_PROJECT%" -c Release
if errorlevel 1 goto :failed
if not exist "%CLIENT_DLL%" goto :missing
if not exist "%CONTRACTS_DLL%" goto :missing

if not exist "%SAVE_ROOT%\worlds_local" mkdir "%SAVE_ROOT%\worlds_local"
if exist "%MOUNT%" goto :checkmount
mklink /J "%MOUNT%" "%WORLD_SOURCE%" >nul
if errorlevel 1 goto :failed
goto :deploy

:checkmount
powershell -NoProfile -Command "$p=Get-Item -LiteralPath '%MOUNT%' -ErrorAction SilentlyContinue; if($null -eq $p -or -not ($p.Attributes -band [IO.FileAttributes]::ReparsePoint)){exit 1}; $t=@($p.Target)[0]; if((Get-Item -LiteralPath $t).FullName -ne (Get-Item -LiteralPath '%WORLD_SOURCE%').FullName){exit 1}"
if errorlevel 1 goto :failed

:deploy
if not exist "%CLIENT_PLUGINS%" mkdir "%CLIENT_PLUGINS%"
copy /Y "%CLIENT_DLL%" "%CLIENT_PLUGINS%\CatosBuildHologram.Client.dll" >nul
if errorlevel 1 goto :failed
copy /Y "%CONTRACTS_DLL%" "%CLIENT_PLUGINS%\CatosBuildContracts.dll" >nul
if errorlevel 1 goto :failed

if not exist "%SERVER_PLUGINS%" mkdir "%SERVER_PLUGINS%"
xcopy "%DEVCOMMANDS_SOURCE%\*" "%DEVCOMMANDS_DEST%\" /E /I /Y >nul
if errorlevel 4 goto :failed
if not exist "%DEVCOMMANDS_DEST%\ServerDevcommands.dll" goto :failed

REM Client-only mode: remove only CatosBuildHologram-related server DLLs.
if exist "%SERVER_PLUGINS%\CatosBuildHologram*.dll" del /F /Q "%SERVER_PLUGINS%\CatosBuildHologram*.dll"
if exist "%SERVER_PLUGINS%\CatosBuildContracts.dll" del /F /Q "%SERVER_PLUGINS%\CatosBuildContracts.dll"

if exist "%REPO%\TEST_SERVER\adminlist.txt" copy /Y "%REPO%\TEST_SERVER\adminlist.txt" "%SAVE_ROOT%\adminlist.txt" >nul

echo.
echo CatosBuildHologram CLIENT-ONLY test
echo Client: %CLIENT_PLUGINS%\CatosBuildHologram.Client.dll
echo Server: no CatosBuildHologram DLLs
echo Utility: %DEVCOMMANDS_DEST%\ServerDevcommands.dll
echo World:  %WORLD_SOURCE%
echo Join:   127.0.0.1:2462
echo Launch the CatosBuildHologram profile through r2modman.
echo.
cd /d "%SERVER%"
"%SERVER%\valheim_server.exe" -nographics -batchmode -name "CatosBuildHologram Vanilla Test" -port 2462 -world "Dedicated" -password "696969" -savedir "%SAVE_ROOT%" -public 0
exit /b %ERRORLEVEL%

:running
echo ERROR: Close Valheim and the dedicated server first.
pause
exit /b 1
:missing
echo ERROR: Required profile, world, project, executable, or DLL is missing.
pause
exit /b 1
:failed
echo ERROR: Build, junction validation, or deployment failed.
pause
exit /b 1
