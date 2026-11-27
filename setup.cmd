@echo off
echo ============================================
echo  SEB Kit v2 - One-Click Setup
echo ============================================
echo.

:: Find Python
where python >nul 2>&1
if %errorlevel% neq 0 (
    echo [*] Installing Python...
    winget install Python.Python.3.12 --accept-package-agreements --accept-source-agreements -s winget
)
set PYPATH=python
for %%P in ("F:\Python\python.exe" "C:\Python312\python.exe" "C:\Python311\python.exe" "C:\Python313\python.exe") do (
    if exist %%P set PYPATH=%%~P
)
echo [+] Python: %PYPATH%

:: Install agy CLI
echo [*] Installing agy CLI...
pip install antigravity-cli 2>nul
where agy >nul 2>&1 && echo [+] agy installed || echo [!] agy not found - run 'agy' once to login

:: Compile HpSebEscape
echo [*] Compiling HpSebEscape...
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /optimize /target:winexe