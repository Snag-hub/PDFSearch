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
Var IsUpdate
Var ExistingInstallDir

; Initialize logging and checks
Function .onInit
  StrCpy $LogFile "$TEMP\FindInPdf_install.log"
  FileOpen $0 "$LogFile" w
  FileWrite $0 "FindInPDFs Setup Log - Started at ${__TIMESTAMP__}$\r$\n"
  FileWrite $0 "Installer initialized. Target directory: $INSTDIR$\r$\n"
  FileClose $0

  ; Check for existing installation
  StrCpy $IsUpdate 0
  StrCpy $ExistingInstallDir ""
  ReadRegStr $ExistingInstallDir HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\FindInPDFs" "UninstallString"
  ${If} $ExistingInstallDir != ""
    ; Extract the directory from the uninstall string (e.g., '"C:\Program Files\No1Knows\FindInPDFs\uninst.exe"' -> "C:\Program Files\No1Knows\FindInPDFs")
    StrCpy $ExistingInstallDir $ExistingInstallDir -1 1 ; Remove the trailing quote
    StrCpy $ExistingInstallDir $ExistingInstallDir "" 1 ; Remove the leading quote and everything after the last backslash
    ${GetParent} $ExistingInstallDir $ExistingInstallDir
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Existing installation detected at $ExistingInstallDir$\r$\n"
    FileClose $0
    StrCpy $IsUpdate 1
    StrCpy $INSTDIR $ExistingInstallDir ; Ensure we install to the same directory
  ${Else}
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "No existing installation detected$\r$\n"
    FileClose $0
  ${EndIf}

  Call CheckAcrobat
FunctionEnd

; Check for Adobe Acrobat
Function CheckAcrobat
  FileOpen $0 "$LogFile" a
  FileSeek $0 0 END
  FileWrite $0 "Checking for Adobe Acrobat or Reader installation...$\r$\n"
  FileClose $0

  ; --- Check Acrobat Pro/Standard ---
  ReadRegStr $0 HKCR "Acrobat.Document.2024" ""
  ${If} $0 != ""
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Adobe Acrobat (version 2024) found in registry (HKCR\Acrobat.Document.2024).$\r$\n"
    FileClose $0
    Return
  ${EndIf}

  ReadRegStr $0 HKCR "Acrobat.Document.2023" ""
  ${If} $0 != ""
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Adobe Acrobat (version 2023) found in registry (HKCR\Acrobat.Document.2023).$\r$\n"
    FileClose $0
    Return
  ${EndIf}

  ReadRegStr $0 HKCR "Acrobat.Document.2022" ""
  ${If} $0 != ""
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Adobe Acrobat (version 2022) found in registry (HKCR\Acrobat.Document.2022).$\r$\n"
    FileClose $0
    Return
  ${EndIf}

  ReadRegStr $0 HKCR "Acrobat.Document.2021" ""
  ${If} $0 != ""
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Adobe Acrobat (version 2021) found in registry (HKCR\Acrobat.Document.2021).$\r$\n"
    FileClose $0
    Return
  ${EndIf}

  ReadRegStr $0 HKCR "Acrobat.Document.2020" ""
  ${If} $0 != ""
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Adobe Acrobat (version 2020) found in registry (HKCR\Acrobat.Document.2020).$\r$\n"
    FileClose $0
    Return
  ${EndIf}

  ReadRegStr $0 HKCR "Acrobat.Document.2017" ""
  ${If} $0 != ""
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Adobe Acrobat (version 2017) found in registry (HKCR\Acrobat.Document.2017).$\r$\n"
    FileClose $0
    Return
  ${EndIf}

  ReadRegStr $0 HKCR "Acrobat.Document.DC" ""
  ${If} $0 != ""
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Adobe Acrobat (version DC) found in registry (HKCR\Acrobat.Document.DC).$\r$\n"
    FileClose $0
    Return
  ${EndIf}

  ReadRegStr $0 HKCR "Acrobat.Document.11" ""
  ${If} $0 != ""
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Adobe Acrobat (version 11) found in registry (HKCR\Acrobat.Document.11).$\r$\n"
    FileClose $0
    Return
  ${EndIf}

  ReadRegStr $0 HKCR "Acrobat.Document.10" ""
  ${If} $0 != ""
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Adobe Acrobat (version 10) found in registry (HKCR\Acrobat.Document.10).$\r$\n"
    FileClose $0
    Return
  ${EndIf}

  ReadRegStr $0 HKCR "Acrobat.Document" ""
  ${If} $0 != ""
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Adobe Acrobat (generic version) found in registry (HKCR\Acrobat.Document).$\r$\n"
    FileClose $0
    Return
  ${EndIf}

  ; --- Check Acrobat Reader ---
  ReadRegStr $0 HKCR "AcroExch.Document.2024" ""
  ${If} $0 != ""
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Adobe Acrobat Reader (version 2024) found in registry (HKCR\AcroExch.Document.2024).$\r$\n"
    FileClose $0
    Return
  ${EndIf}

  ReadRegStr $0 HKCR "AcroExch.Document.2023" ""
  ${If} $0 != ""
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Adobe Acrobat Reader (version 2023) found in registry (HKCR\AcroExch.Document.2023).$\r$\n"
    FileClose $0
    Return
  ${EndIf}

  ReadRegStr $0 HKCR "AcroExch.Document.2022" ""
  ${If} $0 != ""
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Adobe Acrobat Reader (version 2022) found in registry (HKCR\AcroExch.Document.2022).$\r$\n"
    FileClose $0
    Return
  ${EndIf}

  ReadRegStr $0 HKCR "AcroExch.Document.2021" ""
  ${If} $0 != ""
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Adobe Acrobat Reader (version 2021) found in registry (HKCR\AcroExch.Document.2021).$\r$\n"
    FileClose $0
    Return
  ${EndIf}

  ReadRegStr $0 HKCR "AcroExch.Document.2020" ""
  ${If} $0 != ""
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Adobe Acrobat Reader (version 2020) found in registry (HKCR\AcroExch.Document.2020).$\r$\n"
    FileClose $0
    Return
  ${EndIf}

  ReadRegStr $0 HKCR "AcroExch.Document.2017" ""
  ${If} $0 != ""
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Adobe Acrobat Reader (version 2017) found in registry (HKCR\AcroExch.Document.2017).$\r$\n"
    FileClose $0
    Return
  ${EndIf}

  ReadRegStr $0 HKCR "AcroExch.Document.DC" ""
  ${If} $0 != ""
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Adobe Acrobat Reader (version DC) found in registry (HKCR\AcroExch.Document.DC).$\r$\n"
    FileClose $0
    Return
  ${EndIf}

  ReadRegStr $0 HKCR "AcroExch.Document.11" ""
  ${If} $0 != ""
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Adobe Acrobat Reader (version 11) found in registry (HKCR\AcroExch.Document.11).$\r$\n"
    FileClose $0
    Return
  ${EndIf}

  ReadRegStr $0 HKCR "AcroExch.Document.10" ""
  ${If} $0 != ""
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Adobe Acrobat Reader (version 10) found in registry (HKCR\AcroExch.Document.10).$\r$\n"
    FileClose $0
    Return
  ${EndIf}

  ReadRegStr $0 HKCR "AcroExch.Document" ""
  ${If} $0 != ""
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Adobe Acrobat Reader (generic version) found in registry (HKCR\AcroExch.Document).$\r$\n"
    FileClose $0
    Return
  ${EndIf}

  ; --- Fallback: Check Uninstall Keys ---
  SetRegView 64
  StrCpy $0 0
  UninstallLoop:
    EnumRegKey $1 HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall" $0
    IntOp $0 $0 + 1
    StrCmp $1 "" Check32Bit
    ReadRegStr $2 HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$1" "DisplayName"
    StrCmp $2 "" UninstallLoop
    ${If} $2 == "Adobe Acrobat"
    ${OrIf} $2 == "Adobe Acrobat Reader"
      FileOpen $0 "$LogFile" a
      FileSeek $0 0 END
      FileWrite $0 "Adobe Acrobat/Reader found in uninstall keys (HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$1).$\r$\n"
      FileClose $0
      Return ; Found in uninstall keys (truthy)
    ${EndIf}
    Goto UninstallLoop

  Check32Bit:
    SetRegView 32
    StrCpy $0 0
    UninstallLoop32:
      EnumRegKey $1 HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall" $0
      IntOp $0 $0 + 1
      StrCmp $1 "" NotFound
      ReadRegStr $2 HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$1" "DisplayName"
      StrCmp $2 "" UninstallLoop32
      ${If} $2 == "Adobe Acrobat"
      ${OrIf} $2 == "Adobe Acrobat Reader"
        FileOpen $0 "$LogFile" a
        FileSeek $0 0 END
        FileWrite $0 "Adobe Acrobat/Reader found in uninstall keys (HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$1).$\r$\n"
        FileClose $0
        Return ; Found in uninstall keys (truthy)
      ${EndIf}
      Goto UninstallLoop32

  NotFound:
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Adobe Acrobat or Reader (version 10+) not found. Aborting installation.$\r$\n"
    FileClose $0
    MessageBox MB_OK|MB_ICONSTOP "Adobe Acrobat or Reader (version 10+) is required."
    Abort
FunctionEnd

; Check .NET Desktop Runtime
Function CheckDotNet
  StrCpy $DotNetInstalled 0
  StrCpy $DotNetVersion "Unknown"
  FileOpen $0 "$LogFile" a
  FileSeek $0 0 END
  FileWrite $0 "Checking for .NET Desktop Runtime 8.0.x...$\r$\n"
  FileClose $0

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

; Update Section (Runs if an existing installation is detected)
Section "UpdateSection" SEC_UPDATE
  ${If} $IsUpdate == 1
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Update detected. Starting update process for existing installation at $INSTDIR$\r$\n"
    FileClose $0

    ; Notify user
    MessageBox MB_OK|MB_ICONINFORMATION "An existing version of FindInPDFs has been detected. The installer will update the application to the latest version."

    ; Run the uninstaller silently
    IfFileExists "$INSTDIR\uninst.exe" 0 SkipUninstall
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Running uninstaller: $INSTDIR\uninst.exe /S$\r$\n"
    FileClose $0

    ExecWait '"$INSTDIR\uninst.exe" /S' $0
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Uninstaller completed with exit code: $0$\r$\n"
    FileClose $0

    ${If} $0 != 0
      FileOpen $0 "$LogFile" a
      FileSeek $0 0 END
      FileWrite $0 "Uninstaller failed with exit code: $0. Attempting to continue with installation.$\r$\n"
      FileClose $0
    ${EndIf}

    ; Ensure the directory is clean
    RMDir /r "$INSTDIR"
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Cleaned installation directory $INSTDIR after uninstall$\r$\n"
    FileClose $0

    SkipUninstall:
  ${EndIf}
SectionEnd

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
  ; Bundle the .NET Desktop Runtime installer
  File "E:\Freelance Work\Farohar\PDFSearch\PDFSearch\Resources\windowsdesktop-runtime-8.0.16-win-x64.exe"

  ; Log
  FileOpen $0 "$LogFile" a
  FileSeek $0 0 END
  FileWrite $0 "Copied all files from release folder to $INSTDIR, including .NET runtime installer$\r$\n"
  FileClose $0

  ; Check .NET 8.0
  Call CheckDotNet
  ${If} $DotNetInstalled == 0
    FileOpen $0 "$LogFile" a
    FileSeek $0 0 END
    FileWrite $0 "Installing .NET 8.0 Desktop Runtime$\r$\n"
    FileClose $0

    ; Use the bundled runtime installer
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

  ; Clean up the runtime installer (delete it whether installed or not)
  Delete "$INSTDIR\windowsdesktop-runtime-8.0.16-win-x64.exe"
  FileOpen $0 "$LogFile" a
  FileSeek $0 0 END
  FileWrite $0 "Deleted .NET runtime installer from $INSTDIR$\r$\n"
  FileClose $0

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
  FileWrite $0 "Registry entries set for context menu, taskbar, and uninstaller$\r$\n"
  FileClose $0

  ; Uninstaller
  WriteUninstaller "$INSTDIR\uninst.exe"
  FileOpen $0 "$LogFile" a
  FileSeek $0 0 END
  FileWrite $0 "Created uninstaller at $INSTDIR\uninst.exe$\r$\n"
  FileClose $0
SectionEnd

; Uninstaller
Section Uninstall
  FileOpen $0 "$LogFile" a
  FileSeek $0 0 END
  FileWrite $0 "Uninstalling FindInPDFs at ${__TIMESTAMP__}$\r$\n"
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
  FileWrite $0 "Uninstall complete. Removed files, registry entries, and notified shell.$\r$\n"
  FileClose $0
SectionEnd