@echo off
powershell -ExecutionPolicy ByPass -command "& """%~dp0eng\common\Build.ps1""" -restore -build -pack %*"
exit /b %ErrorLevel%