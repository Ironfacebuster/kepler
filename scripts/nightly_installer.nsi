;NSIS Modern User Interface
;Basic Example Script
;Written by Joost Verburg

;--------------------------------
;Include Modern UI

  !include "MUI2.nsh"
  ; !include "FileAssociation.nsh"

;--------------------------------
;General

  ;Name and file
  Name "Kepler Alpha 1.5 Nightly"
  ; !define MUI_ICON "D:\C# Projects\KeplerCompiler\bin\Resources\logo 256x256.ico" 
  !define MUI_ICON "..\res\logo 256x256.ico"
  OutFile "..\build\kepler_v1a1.5_nightly.exe" 
  Unicode True
  
  InstallDir "$PROGRAMFILES\kepler-nightly" 
  ;Get installation folder from registry if available
  InstallDirRegKey HKLM "Software\kepler-nightly" "Install_Dir" 

  ;Request application privileges for Windows Vista
  RequestExecutionLevel admin

;--------------------------------
;Interface Settings

  !define MUI_ABORTWARNING

;--------------------------------
;Pages

  !insertmacro MUI_PAGE_LICENSE "..\res\license.txt" 
  !insertmacro MUI_PAGE_COMPONENTS
  !insertmacro MUI_PAGE_DIRECTORY
  !insertmacro MUI_PAGE_INSTFILES
  
  !insertmacro MUI_UNPAGE_CONFIRM
  !insertmacro MUI_UNPAGE_INSTFILES
  
;--------------------------------
;Languages
 
  !insertmacro MUI_LANGUAGE "English"

;--------------------------------
;Installer Sections

Section "Install Kepler" SecDummy

  SetOutPath "$INSTDIR"
  
  CreateDirectory $INSTDIR\kepler_static 
  CreateDirectory $INSTDIR\examples 
  SetOutPath $INSTDIR 

  File "..\build\nightly\kepler-nightly.exe"
  File "..\build\nightly\clrcompression.dll"
  File "..\build\nightly\clrjit.dll"
  File "..\build\nightly\coreclr.dll"
  File "..\build\nightly\mscordaccore.dll"

  File "..\res\changelog.txt" 
  File "..\build\nightly\readme.txt" 
  
  ; SetOutPath "$INSTDIR\kepler_static" 
  ; File "..\res\kepler_static\static_values.kep" 
  
  ;Store installation folder
  WriteRegStr HKCU "Software\kepler-nightly" "" $INSTDIR
  
  ;Create uninstaller
  WriteUninstaller "$INSTDIR\Uninstall.exe"

SectionEnd

Section "Example Files" ExampleFiles

  DetailPrint "Writing example files..."

  SetOutPath "$INSTDIR\examples" 

  File "..\res\examples\hello_world.kep" 
  File "..\res\examples\functions.kep" 
  File "..\res\examples\if.kep"  
  File "..\res\examples\fizzbuzz.kep" 
  File "..\res\examples\digiroot.kep" 
  File "..\res\examples\prime.kep" 
  File "..\res\examples\input.kep"

SectionEnd

Section "Add to Path" AppendPath

  DetailPrint "Attempting to add to PATH..."
  EnVar::AddValue "PATH" $INSTDIR
  Pop $0 

SectionEnd

Section "Install .NET 5.0" InstallDotNet

  CreateDirectory $INSTDIR\tools 
  SetOutPath "$INSTDIR\tools" 
  File "..\res\dotnet-runtime-5.0.11-win-x64.exe" 
  DetailPrint "Installing Microsoft .NET Core Runtime 5.0" 
  SetDetailsPrint listonly 
  ExecWait '"$INSTDIR\tools\dotnet-runtime-5.0.11-win-x64.exe" /passive /norestart' $0 
  ${If} $0 == 3010 
  ${OrIf} $0 == 1641 
  DetailPrint "Microsoft .NET Core Runtime 5.0 installer requested reboot" 
  SetRebootFlag true 
  ${EndIf} 
  SetDetailsPrint lastused 
  DetailPrint "Microsoft .NET Core Runtime 5.0 installer returned $0" 

SectionEnd

; Section "Register .kep extension" RegisterFileExtension
;     ${registerExtension} "$INSTDIR\kepler.exe" ".kep" "Kepler File"
; SectionEnd

;--------------------------------
;Descriptions

  ;Language strings
  LangString DESC_SecDummy ${LANG_ENGLISH} "Install the Kepler Interpreter (you should probably do this)"
  LangString DESC_ExampleFiles ${LANG_ENGLISH} "Install example Kepler files."
  LangString DESC_RegisterExtension ${LANG_ENGLISH} "Register the .kep file extension"
  LangString DESC_InstallDotNet ${LANG_ENGLISH} "Install .NET 5.0 Desktop Runtime. (recommended)"
  LangString DESC_AppendPath ${LANG_ENGLISH} "Add Kepler to your current user's Path. (recommended)"

  ;Assign language strings to sections
  !insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
    !insertmacro MUI_DESCRIPTION_TEXT ${SecDummy} $(DESC_SecDummy)
    !insertmacro MUI_DESCRIPTION_TEXT ${ExampleFiles} $(DESC_ExampleFiles)
    !insertmacro MUI_DESCRIPTION_TEXT ${InstallDotNet} $(DESC_InstallDotNet)
    ; !insertmacro MUI_DESCRIPTION_TEXT ${RegisterFileExtension} $(DESC_RegisterExtension)
    !insertmacro MUI_DESCRIPTION_TEXT ${AppendPath} $(DESC_AppendPath)
  !insertmacro MUI_FUNCTION_DESCRIPTION_END

;--------------------------------
;Uninstaller Section

Section "Uninstall"

  Delete "$INSTDIR\kepler-nightly.exe"
  Delete "$INSTDIR\clrcompression.dll"
  Delete "$INSTDIR\clrjit.dll"
  Delete "$INSTDIR\coreclr.dll"
  Delete "$INSTDIR\mscordaccore.dll"

  Delete "$INSTDIR\changelog.txt" 
  Delete "$INSTDIR\readme.txt" 


  Delete "$INSTDIR\examples\hello_world.kep" 
  Delete "$INSTDIR\examples\functions.kep" 
  Delete "$INSTDIR\examples\if.kep" 
  Delete "$INSTDIR\examples\fizzbuzz.kep" 
  Delete "$INSTDIR\examples\digiroot.kep" 
  Delete "$INSTDIR\examples\prime.kep" 
  Delete "$INSTDIR\examples\input.kep" 

  Delete $INSTDIR\tools\dotnet-runtime-5.0.11-win-x64.exe
  RMDir $INSTDIR\tools 
  RMDir $INSTDIR\examples 
  RMDir $INSTDIR\kepler_static 
  RMDir $INSTDIR 

  DeleteRegKey /ifempty HKCU "Software\kepler-nightly"

  ;unregister extension
  ; ${unregisterExtension} ".kep" "Kepler File"

  DetailPrint "Attempting to remove from PATH..."
  EnVar::DeleteValue "PATH" $INSTDIR
  Pop $0 

SectionEnd