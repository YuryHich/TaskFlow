@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0start-taskflow.ps1" %*
exit /b %ERRORLEVEL%
