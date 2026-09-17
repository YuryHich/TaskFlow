@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0start-apps.ps1" %*
exit /b %ERRORLEVEL%
