@echo off
chcp 65001 >nul
setlocal EnableExtensions EnableDelayedExpansion

rem ============================================================================
rem  SengokuScroll 一键打包：前端静态资源 + 后端自包含单文件 exe → build\
rem  产物：build\release-<编号>\SengokuScrollGame.exe（保留旧发行版）
rem ============================================================================

set "ROOT=%~dp0"
set "BUILD_DIR=%ROOT%build\release-%RANDOM%-%RANDOM%"
set "WEBCLIENT=%ROOT%SengokuScroll.WebClient"
set "WEBAPI=%ROOT%SengokuScroll.WebApi"
set "DIST=%WEBCLIENT%\dist"

pushd "%ROOT%"

echo.
echo ===== [1/4] 安装并构建前端 =====
pushd "%WEBCLIENT%"
call npm ci
if errorlevel 1 goto :fail

call npm run build:release
if errorlevel 1 goto :fail
popd

if not exist "%DIST%\index.html" (
  echo 错误：未找到前端构建产物 %DIST%\index.html
  goto :fail
)

echo.
echo ===== [2/4] 直接嵌入前端 dist（保留现有 wwwroot）=====

echo.
echo ===== [3/4] 发布后端（win-x64 自包含单文件）=====
if exist "%BUILD_DIR%" goto :fail
mkdir "%BUILD_DIR%"

dotnet publish "%WEBAPI%\SengokuScroll.WebApi.csproj" ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  /p:EmbedWebClient=true ^
  /p:WebClientDist="%DIST%" ^
  /p:PublishSingleFile=true ^
  /p:IncludeNativeLibrariesForSelfExtract=true ^
  /p:EnableCompressionInSingleFile=true ^
  /p:DebugType=None ^
  /p:DebugSymbols=false ^
  -o "%BUILD_DIR%"
if errorlevel 1 goto :fail

if exist "%BUILD_DIR%\SengokuScroll.WebApi.exe" (
  move /y "%BUILD_DIR%\SengokuScroll.WebApi.exe" "%BUILD_DIR%\SengokuScrollGame.exe" >nul
)

echo.
echo ===== [4/4] 生成启动脚本 =====
set "PKG_DIR=%BUILD_DIR%"
copy /y "%ROOT%scripts\LaunchGame.cmd" "%BUILD_DIR%\LaunchGame.cmd" >nul
if errorlevel 1 goto :fail
powershell -NoProfile -Command ^
  "$dir=$env:PKG_DIR;" ^
  "$readme='SengokuScroll Release','','Launch:','  1. Double-click SengokuScrollGame.exe','  2. Or double-click LaunchGame.cmd','','URL: http://127.0.0.1:5100/','Close the console window to exit.','','Frontend is embedded in the exe.','Keep Maps and App_Data next to the exe.';" ^
  "Set-Content -LiteralPath (Join-Path $dir 'README.txt') -Value ($readme -join [Environment]::NewLine) -Encoding UTF8"
if errorlevel 1 goto :fail

popd

echo.
echo ===== 打包完成 =====
echo 输出目录: %BUILD_DIR%
echo 主程序:   %BUILD_DIR%\SengokuScrollGame.exe
echo.
goto :eof

:fail
popd 2>nul
echo.
echo ===== 打包失败 =====
exit /b 1
