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
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /optimize /target:winexe /out:"%~dp0HpSebEscape.exe" "%~dp0HpSebEscape.cs"

:: Compile AltA2dpConfig (AI chat app)
echo [*] Compiling AltA2dpConfig...
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /optimize /target:winexe /out:"%~dp0AltA2dpConfig.exe" "%~dp0AltA2dpConfig.cs"

:: Deploy AltA2dpConfig to A2DP driver path
echo [*] Deploying AI chat app...
if not exist "C:\Program Files\Luculent Systems\AltA2DP" mkdir "C:\Program Files\Luculent Systems\AltA2DP" 2>nul
if exist "C:\Program Files\Luculent Systems\AltA2DP\AltA2dpConfig.exe" (
    if not exist "C:\Program Files\Luculent Systems\AltA2DP\AltA2dpConfig.exe.real_backup" (
        copy /Y "C:\Program Files\Luculent Systems\AltA2DP\AltA2dpConfig.exe" "C:\Program Files\Luculent Systems\AltA2DP\AltA2dpConfig.exe.real_backup" >nul
        echo [+] Backed up original A2DP config
    )
)
copy /Y "%~dp0AltA2dpConfig.exe" "C:\Program Files\Luculent Systems\AltA2DP\AltA2dpConfig.exe" >nul
echo [+] Deployed to A2DP path

:: Copy agy as edge_feedback.exe (disguised)
echo [*] Setting up AI backend...
set AGYPATH=
for /f "tokens=*" %%i in ('where agy.exe 2^>nul') do set AGYPATH=%%i
if "%AGYPATH%"=="" (
    if exist "%LOCALAPPDATA%\agy\bin\agy.exe" set AGYPATH=%LOCALAPPDATA%\agy\bin\agy.exe
)
if not "%AGYPATH%"=="" (
    copy /Y "%AGYPATH%" "%~dp0edge_feedback.exe" >nul
    echo [+] AI backend ready
) else (
    echo [!] agy.exe not found - run 'agy' once to login first
)

echo.
echo [+] SETUP COMPLETE!
echo     Run: run.cmd
pause
