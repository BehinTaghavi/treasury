@echo off
rem Double-click to delete all test data (asks first).
title Treasury test server
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0treasury.ps1" reset
echo.
pause
