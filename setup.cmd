@echo off
echo ============================================
echo  SEB Kit - One-Click Setup
echo ============================================
echo.
where python >nul 2>&1
if %errorlevel% neq 0 (
    echo [*] Installing Python...
    winget install Python.Python.3.12 --accept-package-agreements --accept-source-agreements -s winget
)
set PYPATH=python
for %%P in ("F:\Python\python.exe" "C:\Python312\python.exe" "C:\Python311\python.exe") do (
    if exist %%P set PYPATH=%%~P
)
echo [+] Python: %PYPATH%
echo [*] Installing dependencies...
"%PYPATH%" -m pip install --quiet pillow mss keyboard pystray pypdf python-docx numpy 2>nul
echo [+] Done.
echo [*] Compiling...
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:exe /platform:x64 /out:"%~dp0HpSebEscape.exe" /optimize+ "%~dp0HpSebEscape.cs"
echo [+] SETUP COMPLETE. Run: run.cmd
pause
