@echo off
rem Copies what players need into ..\IsoRim_Release, ready to upload to the Steam Workshop.
rem Run build.bat first.
cd /d "%~dp0"
for %%V in (1.5 1.6 1.7) do (
  if not exist %%V\Assemblies\IsoRim.dll (
    echo %%V\Assemblies\IsoRim.dll is missing: run build.bat first.
    pause
    exit /b 1
  )
)
set OUT=..\IsoRim_Release
if exist "%OUT%" rmdir /s /q "%OUT%"
mkdir "%OUT%"
for %%D in (About 1.5 1.6 1.7 Defs Models Textures LUTs Shaders) do xcopy /e /i /q "%%D" "%OUT%\%%D" >nul
copy README.txt "%OUT%" >nul
copy LICENSE "%OUT%" >nul
echo Done: %OUT%
pause
