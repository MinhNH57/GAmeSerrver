@echo off
TITLE KV1 Event Configurator - Startup Script
echo ======================================================
echo   DANG KHOI DONG KV1 EVENT CONFIGURATOR...
echo   TIEN TRINH SE TU DONG BAT SERVER VA MO TRINH DUYET
echo ======================================================

:: Chuyen vao thu muc tool
cd /d "%~dp0tools\EventConfigurator"

:: Kiem tra node_modules
if not exist "node_modules\" (
    echo [ERROR] Khong tim thay node_modules. Dang chay npm install...
    call npm install
)

:: Khoi chay server Node.js trong mot cua so moi
echo [1/2] Dang chay server tai http://localhost:3000 ...
start "KV1 EventConfigurator Server" cmd /c "npm start"

:: Doi server khoi dong (3 giay)
timeout /t 3 /nobreak >nul

:: Mo trinh duyet
echo [2/2] Dang mo trinh duyet...
start http://localhost:3000

echo ======================================================
echo   HOAN TAT! Vui long khong tat cua so Server dang chay.
echo ======================================================
timeout /t 2 >nul
exit
