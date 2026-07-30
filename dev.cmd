@echo off
chcp 65001 >nul
setlocal

set "ROOT=%~dp0"
cd /d "%ROOT%"

echo 启动 SengokuScroll 开发环境（WebApi 5100 + Vite 5173）...
echo.

start "SengokuScroll WebApi" cmd /k dotnet run --project "SengokuScroll.WebApi\SengokuScroll.WebApi.csproj" --launch-profile http

echo 等待 WebApi 就绪...
timeout /t 4 /nobreak >nul

start "SengokuScroll WebClient" cmd /k cd /d "%ROOT%SengokuScroll.WebClient" ^& npm run dev

echo.
echo 已打开两个窗口：
echo   WebApi   http://127.0.0.1:5100
echo   前端     http://localhost:5173
echo.
echo 请在前端窗口出现 ready 后，用浏览器访问 http://localhost:5173/
pause
