@echo off
powershell -ExecutionPolicy ByPass -command "& """%~dp0eng\common\Build.ps1""" -test -projects %~dp0Roslyn-SDK.sln %*"
exit /b %ErrorLevel%