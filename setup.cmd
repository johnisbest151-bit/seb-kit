@echo off
echo ============================================
echo  SEB Bypass PoC - One-Click Setup
echo ============================================
echo.

:: Check if Python exists
where python >nul 2>&1
if %errorlevel% neq 0 (
    where F:\Python\python.exe >nul 2>&1
    if %errorlevel% neq 0 (
        echo [!] Python not found. Installing via winget...
        winget install Python.Python.3.12 --accept-package-agreements --accept-source-agreements -s winget
        echo [+] Python installed. Restart this script.
        pause
        exit /b
    )
)

:: Find Python path
set PYPATH=python
if exist "F:\Python\python.exe" set PYPATH=F:\Python\python.exe
if exist "F:\Python\pythonw.exe" set PYWPATH=F:\Python\pythonw.exe
if "%PYWPATH%"=="" set PYWPATH=pythonw

echo [+] Python: %PYPATH%

:: Install conure dependencies
echo [*] Installing conure dependencies...
"%PYPATH%" -m pip install --quiet sherpa-onnx pyaudiowpatch numpy pillow mss keyboard pystray pypdf python-docx 2>nul
echo [+] Dependencies installed.

:: Compile bypass tool if needed
if not exist "%~dp0HpSebEscape.exe" (
    echo [*] Compiling bypass tool...
    C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:exe /platform:x64 /out:"%~dp0HpSebEscape.exe" /optimize+ "%~dp0HpSebEscape.cs"
    echo [+] Compiled.
)

echo.
echo ============================================
echo  SETUP COMPLETE
echo ============================================
echo.
echo  To use:
echo    1. Run:  "%~dp0HpSebEscape.exe" 0
echo    2. Launch SEB
echo    3. Ctrl+Space = toggle SEB / Default
echo    4. Ctrl+Z = show/hide conure on SEB
echo    5. Ctrl+X = screenshot SEB
echo.
pause
