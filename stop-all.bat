@echo off
title MobiFlux Stop All
echo Dang dung toan bo tien trinh MobiFlux...
taskkill /F /IM MobiFlux.Service.exe /IM MobiFlux.Desktop.exe >nul 2>&1
echo Da tat MobiFlux Service va MobiFlux Desktop.
timeout /t 2 /nobreak >nul
