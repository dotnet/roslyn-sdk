@echo off
powershell -ExecutionPolicy ByPass -command "& """%~dp0eng\common\Build.ps1""" -restore -projects %~dp0Roslyn-SDK.sln %*"
exit /b %ErrorLevel%