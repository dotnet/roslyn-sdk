@echo off
powershell -NoProfile -ExecutionPolicy ByPass -command "& """%~dp0eng\common\Build.ps1""" -restore %*"
exit /b %ErrorLevel%