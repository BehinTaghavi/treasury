@echo off
rem Double-click to build (if needed) and start the Treasury system, then open it in the browser.
title Treasury test server
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0treasury.ps1" start
echo.
pause
