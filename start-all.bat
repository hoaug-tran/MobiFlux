@echo off
setlocal enabledelayedexpansion
title MobiFlux All-In-One Runner
cd /d "%~dp0"

echo ===================================================
echo             MobiFlux All-In-One Runner             
echo ===================================================
echo.

:: 1. Kiem tra ADB
where adb >nul 2>&1
if errorlevel 1 (
    echo [LOI] Khong tim thay ADB trong PATH. Vui long cai dat hoac them ADB vao PATH.
    pause
    exit /b 1
)

:: 2. Kiem tra thiet bi Android
echo [1/5] Kiem tra thiet bi Android qua ADB...
set "DEVICE_COUNT=0"
for /f "tokens=1,2" %%A in ('adb devices ^| findstr /v "List of devices"') do (
    if "%%B"=="device" (
        set /a DEVICE_COUNT+=1
        echo   - Thiet bi %%A: San sang.
        
        :: Kiem tra va cai dat Agent neu chua co
        adb -s %%A shell pm list packages | findstr "com.mobiflux.agent" >nul 2>&1
        if errorlevel 1 (
            if exist "android\app\build\outputs\apk\debug\app-debug.apk" (
                echo     + Dang cai dat Agent APK len %%A...
                adb -s %%A install -r "android\app\build\outputs\apk\debug\app-debug.apk" >nul 2>&1
            )
        )
        
        :: Bat Agent tren dien thoai
        echo     + Khoi dong MobiFlux Agent...
        adb -s %%A shell am start -n com.mobiflux.agent/.MainActivity >nul 2>&1
    )
    if "%%B"=="unauthorized" (
        echo   - [CANH BAO] Thiet bi %%A chua xac thuc USB Debugging!
        echo     Mo khoa man hinh dien thoai va tich 'Luon cho phep' roi bam OK.
    )
)

if %DEVICE_COUNT%==0 (
    echo   - [CANH BAO] Chua co thiet bi nao o trang thai 'device'.
)

:: 3. Tat tien trinh cu neu dang chay
echo.
echo [2/5] Don dep tien trinh MobiFlux cu neu co...
taskkill /F /IM MobiFlux.Service.exe /IM MobiFlux.Desktop.exe >nul 2>&1

:: 4. Kiem tra file thuc thi
echo.
echo [3/5] Kiem tra ban build...
if not exist "src\MobiFlux.Service\bin\Debug\net10.0\MobiFlux.Service.exe" (
    echo   - Dang build solution lan dau...
    dotnet build -c Debug
    if errorlevel 1 (
        echo [LOI] Build that bai.
        pause
        exit /b 1
    )
)

:: 5. Khoi dong Service
echo.
echo [4/5] Khoi dong MobiFlux Service (API + Web Server)...
start "MobiFlux Service" /d "%~dp0src\MobiFlux.Service" "%~dp0src\MobiFlux.Service\bin\Debug\net10.0\MobiFlux.Service.exe"

:: Cho Service san sang
echo   - Dang doi Service san sang tai cong 5080...
set "READY=0"
for /l %%i in (1,1,15) do (
    if !READY!==0 (
        curl.exe -s http://127.0.0.1:5080/health >nul 2>&1
        if not errorlevel 1 (
            set "READY=1"
        ) else (
            timeout /t 1 /nobreak >nul
        )
    )
)

if !READY!==1 (
    echo   - Service da san sang!
    :: Goi discovery quet thiet bi
    echo   - Quet va nhan dien thiet bi vao he thong...
    curl.exe -s -X POST "http://127.0.0.1:5080/api/v1/mobiflux/device-discovery-runs" >nul 2>&1
) else (
    echo   - [Luu y] Service khoi dong cham hon du kien.
)

:: 6. Khoi dong Desktop UI va mo trinh duyet Web
echo.
echo [5/5] Khoi dong MobiFlux Desktop UI va mo Web...
start "MobiFlux Desktop" /d "%~dp0src\MobiFlux.Desktop" "%~dp0src\MobiFlux.Desktop\bin\Debug\net10.0-windows\MobiFlux.Desktop.exe"
start http://127.0.0.1:5080

echo.
echo ===================================================
echo      He thong da khoi dong thanh cong!             
echo      - Web Dashboard:  http://127.0.0.1:5080       
echo      - Desktop App:    Cua so dang mo              
echo ===================================================
echo.
pause
