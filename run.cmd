@echo off
echo Starting SEB bypass...
start "" "%~dp0HpSebEscape.exe" 0
echo.
echo [+] Bypass running. Launch SEB now.
echo.
echo Controls:
echo   Ctrl+Space = toggle SEB / Default desktop
echo   Ctrl+X     = screenshot SEB screen (saves to Downloads\SEB_Captures)
echo   Ctrl+Z     = show/hide AI chat app
echo.
echo To use AI chat:
echo   1. Ctrl+Space to Default desktop
echo   2. Open 'Alternative A2DP Driver' from Start Menu or:
echo      "C:\Program Files\Luculent Systems\AltA2DP\AltA2dpConfig.exe"
echo   3. Type question, press Enter
echo   4. Say 'read recent screenshot' to analyze SEB captures
echo   5. Ctrl+Space back to SEB
echo.
echo Close this window anytime - bypass keeps running.
