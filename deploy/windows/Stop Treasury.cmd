@echo off
rem Double-click to stop the Treasury system. Test data is kept.
title Treasury test server
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0treasury.ps1" stop
echo.
pause
