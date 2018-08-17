@echo off
powershell -ExecutionPolicy ByPass -command "& """%~dp0eng\common\Build.ps1""" -restore -build -projects %~dp0Roslyn-SDK.sln %*"
exit /b %ErrorLevel%