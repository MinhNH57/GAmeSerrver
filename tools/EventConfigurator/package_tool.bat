@echo off
setlocal

echo [*] Dang tao thu muc Release...
set RELEASE_DIR=..\..\Release_EventConfigurator
if not exist "%RELEASE_DIR%" mkdir "%RELEASE_DIR%"

echo [*] Dang copy cac file can thiet...
xcopy /E /I /Y "public" "%RELEASE_DIR%\public"
copy "server.js" "%RELEASE_DIR%\"
copy "package.json" "%RELEASE_DIR%\"
copy ".env.example" "%RELEASE_DIR%\"
copy "README.md" "%RELEASE_DIR%\"



echo [*] Tao file bat khoi dong cho Release...
(
echo @echo off
echo if not exist ".env" ^(
echo     echo [!] File .env khong ton tai. Dang tao tu .env.example...
echo     copy .env.example .env
echo     echo [!] Da tao file .env. Vui long mo file .env va chinh lai duong dan KV1_CONFIG_PATH cho dung.
echo     pause
echo ^)
echo if not exist "node_modules" ^(
echo     echo [*] Dang cai dat thu vien...
echo     npm install
echo ^)
echo echo [*] Dang khoi dong Event Configurator...
echo start http://localhost:3000
echo npm start
) > "%RELEASE_DIR%\Start_Tool.bat"

echo [OK] Da dong goi xong tai: %RELEASE_DIR%
echo [!] Luu y: Ban chi can copy thu muc nay sang may khac.
pause
