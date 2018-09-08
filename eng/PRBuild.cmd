@echo off
powershell -ExecutionPolicy ByPass -command "& """%~dp0common\Build.ps1""" -restore -build -ci %*"
exit /b %ErrorLevel%
