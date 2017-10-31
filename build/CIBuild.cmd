@echo off
powershell -ExecutionPolicy ByPass %~dp0Build.ps1 -restore -build -sign -pack -ci %*
exit /b %ErrorLevel%