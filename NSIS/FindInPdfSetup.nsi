!include MUI2.nsh
!include FileFunc.nsh
!include LogicLib.nsh
!include x64.nsh

; General Settings
Name "FindInPDFs"
OutFile "FindInPdfsSetup.exe"
InstallDir "$PROGRAMFILES64\No1Knows\FindInPDFs"
RequestExecutionLevel admin
BrandingText "N1K Technology"

; UI Settings
!define MUI_PAGE_HEADER_TEXT "FindInPDFs Installer"
!define MUI_WELCOMEPAGE_TITLE "Welcome to FindInPDFs Setup"
!define MUI_FINISHPAGE_TITLE "FindInPDFs Setup Complete"
!define MUI_ABORTWARNING
!define MUI_ICON "E:\Freelance Work\Farohar\PDFSearch\PDFSearch\FIP_ICON.ico"

; Pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; Language
!insertmacro MUI_LANGUAGE "English"

; Variables
Var DotNetInstalled
Var DotNetVersion
Var LogFile

; Initialize logging and checks
Function .onInit
  StrCpy $LogFile "$TEMP\FindInPdf_install.log"
  FileOpen $0 "$LogFile" w
  FileWrite $0 "FindInPDFs Setup Log - Started$\r$\n"
  FileClose $0
  Call CheckAcrobat
FunctionEnd

; Check for Adobe Acrobat
Function CheckAcrobat
  ReadRegStr $0 HKCR "Acrobat.Document.DC" ""
  ${If} $0 == ""
    MessageBox MB_OK "Adobe Acrobat DC is required. Please install it and rerun the setup."
    Abort
  ${EndIf}
FunctionEnd

; Check .NET Desktop Runtime
Function CheckDotNet
  StrCpy $DotNetInstalled 0
  StrCpy $DotNetVersion "Unknown"
  ; Check for any 8.0.x version in the runtime folder
  FindFirst $1 $2 "C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App\8.0.*"
  ${If} $2 != ""
    StrCpy $DotNetVersion $2
    StrCpy $DotNetInstalled 1
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Folder check - .NET Desktop Runtime $DotNetVersion found$\r$\n"
    FileClose $0
  ${Else}
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Folder check - No .NET Desktop Runtime 8.0.x found$\r$\n"
    FileClose $0
  ${EndIf}
  FindClose $1
  FileOpen $0 "$LogFile" a
  FileSeek $0 0 END
  FileWrite $0 "Final .NET Desktop Runtime check: Installed=$DotNetInstalled, Version=$DotNetVersion$\r$\n"
  FileClose $0
FunctionEnd

; Main Installation
Section "MainSection" SEC01
  SetOutPath "$INSTDIR"
  SetOverwrite on

  ; Log
  FileOpen $0 "$LogFile" a
  FileSeek $0 0 END
  FileWrite $0 "Installing to $INSTDIR$\r$\n"
  FileClose $0

  ; Install all files from the release folder
  File "E:\Freelance Work\Farohar\PDFSearch\PDFSearch\FIP_ICON.ico"
  File /r "E:\Freelance Work\Farohar\PDFSearch\PDFSearch\bin\Release\net8.0-windows\*.*"

  ; Log
  FileOpen $0 "$LogFile" a
  FileSeek $0 0 END
  FileWrite $0 "Copied all files from release folder to $INSTDIR$\r$\n"
  FileClose $0

  ; Check .NET 8.0
  Call CheckDotNet
  ${If} $DotNetInstalled == 0
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Installing .NET 8.0 Desktop Runtime$\r$\n"
    FileClose $0
    File "E:\Freelance Work\Farohar\PDFSearch\PDFSearch\Resources\windowsdesktop-runtime-8.0.16-win-x64.exe"
    ExecWait '"$INSTDIR\windowsdesktop-runtime-8.0.16-win-x64.exe" /install /quiet /norestart' $0
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 ".NET 8.0 installation completed with exit code: $0$\r$\n"
    FileClose $0
    ${If} $0 == 3010
      FileOpen $0 "$LogFile" a
      FileSeek $0 0 END
      FileWrite $0 "Reboot required to complete .NET runtime installation$\r$\n"
      FileClose $0
      MessageBox MB_OK "A system reboot is required to complete the .NET runtime installation. Please reboot and rerun the setup."
      Abort
    ${ElseIf} $0 != 0
      FileOpen $0 "$LogFile" a
      FileSeek $0 0 END
      FileWrite $0 "Failed to install .NET runtime (exit code: $0)$\r$\n"
      FileClose $0
      MessageBox MB_OK "Failed to install .NET 8.0 Desktop Runtime (exit code: $0). Please install it manually and rerun the setup."
      Abort
    ${EndIf}
    Delete "$INSTDIR\windowsdesktop-runtime-8.0.16-win-x64.exe"
    ; Re-check after installation
    Call CheckDotNet
    ${If} $DotNetInstalled == 0
      FileOpen $0 "$LogFile" a
      FileSeek $0 0 END
      FileWrite $0 "ERROR: .NET runtime installation succeeded, but runtime still not detected$\r$\n"
      FileClose $0
      MessageBox MB_OK "ERROR: .NET runtime installation succeeded, but the runtime is still not detected. Please install it manually and rerun the setup."
      Abort
    ${EndIf}
  ${Else}
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 ".NET 8.0 Desktop Runtime already installed (version: $DotNetVersion)$\r$\n"
    FileClose $0
  ${EndIf}

  ; Context menu (Folder)
  WriteRegStr HKCR "Directory\shell\FindInPDFs" "" "Search with FindInPDFs"
  WriteRegStr HKCR "Directory\shell\FindInPDFs" "Icon" "$INSTDIR\FIP_ICON.ico"
  WriteRegStr HKCR "Directory\shell\FindInPDFs\command" "" '"$INSTDIR\FindInPDFs.exe" "%1"'

  ; Context menu (Background)
  WriteRegStr HKCR "Directory\Background\shell\FindInPDFs" "" "Search with FindInPDFs"
  WriteRegStr HKCR "Directory\Background\shell\FindInPDFs" "Icon" "$INSTDIR\FIP_ICON.ico"
  WriteRegStr HKCR "Directory\Background\shell\FindInPDFs\command" "" '"$INSTDIR\FindInPDFs.exe" "%V"'

  ; Taskbar/Task Manager
  WriteRegStr HKLM "Software\No1Knows\FindInPDFs" "AppUserModelID" "No1Knows.FindInPDFs"
  WriteRegStr HKCR "Applications\FindInPDFs.exe\DefaultIcon" "" "$INSTDIR\FIP_ICON.ico"
  WriteRegStr HKCR "Applications\FindInPDFs.exe\shell\open\command" "" '"$INSTDIR\FindInPDFs.exe" "%1"'

  ; Programs and Features
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\FindInPDFs" "DisplayName" "FindInPDFs"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\FindInPDFs" "UninstallString" '"$INSTDIR\uninst.exe"'
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\FindInPDFs" "DisplayIcon" "$INSTDIR\FIP_ICON.ico"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\FindInPDFs" "Publisher" "No1Knows Technology"

  ; Log
  FileOpen $0 "$LogFile" a
  FileSeek $0 0 END
  FileWrite $0 "Registry set$\r$\n"
  FileClose $0

  ; Uninstaller
  WriteUninstaller "$INSTDIR\uninst.exe"
SectionEnd

; Uninstaller
Section Uninstall
  FileOpen $0 "$LogFile" a
  FileSeek $0 0 END
  FileWrite $0 "Uninstalling FindInPDFs$\r$\n"
  FileClose $0

  Delete "$INSTDIR\uninst.exe"
  RMDir /r "$INSTDIR"

  DeleteRegKey HKCR "Directory\shell\FindInPDFs"
  DeleteRegKey HKCR "Directory\Background\shell\FindInPDFs"
  DeleteRegKey HKLM "Software\No1Knows\FindInPDFs"
  DeleteRegKey HKCR "Applications\FindInPDFs.exe"
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\FindInPDFs"

  System::Call 'shell32.dll::SHChangeNotify(i, i, i, i) (0x08000000, 0, 0, 0)'

  FileOpen $0 "$LogFile" a
  FileSeek $0 0 END
  FileWrite $0 "Uninstall complete$\r$\n"
  FileClose $0
SectionEnd