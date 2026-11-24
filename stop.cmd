@echo off
taskkill /f /im HpSebEscape.exe >nul 2>&1
taskkill /f /im pythonw.exe >nul 2>&1
taskkill /f /im SecureExamBrowser.exe >nul 2>&1
echo All stopped.
pause
