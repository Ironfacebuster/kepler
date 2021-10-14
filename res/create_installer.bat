@ECHO OFF

IF "%1" == "" (
    echo No arguments were provided!
    pause
)

setlocal enableextensions disabledelayedexpansion

echo Updating %1

@REM empty file contents
echo RequestExecutionLevel admin > %1
echo SetCompressor /SOLID zlib > %1
echo !include LogicLib.nsh >> %1

echo Function InstallDotNet >> %1
echo SetOutPath "$INSTDIR\tools" >> %1
echo File "%~dp0Tools\windowsdesktop-runtime-3.1.16-win-x64.exe" >> %1
echo DetailPrint "Installing Microsoft .NET Core Runtime 3.1" >> %1
echo SetDetailsPrint listonly >> %1
echo ExecWait '"$INSTDIR\tools\windowsdesktop-runtime-3.1.16-win-x64.exe" /passive /norestart' $0 >> %1
echo ${If} $0 ^== 3010 >> %1
echo ${OrIf} $0 ^== 1641 >> %1
echo DetailPrint "Microsoft .NET Core Runtime 3.1 installer requested reboot" >> %1
echo SetRebootFlag true >> %1
echo ${EndIf} >> %1
echo SetDetailsPrint lastused >> %1
echo DetailPrint "Microsoft .NET Core Runtime 3.1 installer returned $0" >> %1
echo FunctionEnd >> %1

echo Name "Kepler Installer" >> %1
echo Caption "Kepler v1-alpha.1.1 Installer V1" >> %1
echo Icon "%~dp0Resources\logo 256x256.ico" >> %1
echo OutFile "%~dp0BUILD\kepler_v1a1.1_installer_v1.exe" >> %1
echo SetDateSave on >> %1
echo SetDatablockOptimize on >> %1
echo CRCCheck on >> %1
echo SilentInstall normal >> %1

echo InstallDir "$PROGRAMFILES\kepler" >> %1
echo InstallDirRegKey HKLM "Software\kepler" "Install_Dir" >> %1
echo AutoCloseWindow false >> %1

echo Section "Install" >> %1

echo CreateDirectory $INSTDIR\kepler_static >> %1
echo CreateDirectory $INSTDIR\kepler_static\examples >> %1
echo CreateDirectory $INSTDIR\tools >> %1

@REM Include every file in the build folder
echo SetOutPath $INSTDIR >> %1
echo PATH: %~dp0
for %%i in ("%~dp0..\VS_PUBLISH_OUTPUT\*") do echo File "%%i" >> %1

echo SetOutPath "$INSTDIR\kepler_static" >> %1
for %%i in ("%~dp0..\VS_PUBLISH_OUTPUT\kepler_static\*") do echo File "%%i" >> %1

echo SetOutPath "$INSTDIR\kepler_static\examples" >> %1
for %%i in ("%~dp0..\VS_PUBLISH_OUTPUT\kepler_static\examples\*") do echo File "%%i" >> %1

@REM echo SetOutPath "$INSTDIR\tools" >> %1
@REM echo File "%~dp0Tools\windowsdesktop-runtime-3.1.16-win-x64" >> %1

echo Call InstallDotNet >> %1

echo EnVar::AddValue "PATH" "$INSTDIR">> %1
echo Pop $0 >> %1
echo DetailPrint "EnVar::AddValue returned=|$0|" >> %1

echo WriteUninstaller $INSTDIR\uninstaller.exe >> %1

echo SectionEnd >> %1

@REM Uninstaller things
echo Section "Uninstall" >> %1

echo Delete $INSTDIR\uninstaller.exe >> %1

for %%i in ("%~dp0..\VS_PUBLISH_OUTPUT\*") do echo Delete "$INSTDIR\%%~ni%%~xi" >> %1
for %%i in ("%~dp0..\VS_PUBLISH_OUTPUT\kepler_static\*") do echo Delete "$INSTDIR\kepler_static\%%~ni%%~xi" >> %1
for %%i in ("%~dp0..\VS_PUBLISH_OUTPUT\kepler_static\examples\*") do echo Delete "$INSTDIR\kepler_static\examples\%%~ni%%~xi" >> %1
for %%i in ("%~dp0..\VS_PUBLISH_OUTPUT\kepler_static\examples\*") do echo Delete "$INSTDIR\kepler_static\examples\%%~ni%%~xi" >> %1

echo Delete $INSTDIR\tools\windowsdesktop-runtime-3.1.16-win-x64.exe >> %1

echo RMDir $INSTDIR\tools >> %1
echo RMDir $INSTDIR\kepler_static\examples >> %1
echo RMDir $INSTDIR\kepler_static >> %1
echo RMDir $INSTDIR >> %1

echo EnVar::DeleteValue "PATH" "$INSTDIR" >> %1
echo Pop $0 >> %1
echo DetailPrint "EnVar::DeleteValue returned=|$0|" >> %1

echo SectionEnd >> %1

@REM pause
