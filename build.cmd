@echo off
chcp 65001 >nul
setlocal EnableExtensions EnableDelayedExpansion

rem ============================================================================
rem  SengokuScroll 一键打包：前端静态资源 + 后端自包含单文件 exe → build\
rem  产物：build\SengokuScrollGame.exe（前端内嵌，无需 wwwroot 文件夹）
rem ============================================================================

set "ROOT=%~dp0"
set "BUILD_DIR=%ROOT%build"
set "WEBCLIENT=%ROOT%SengokuScroll.WebClient"
set "WEBAPI=%ROOT%SengokuScroll.WebApi"
set "WWWROOT=%WEBAPI%\wwwroot"
set "DIST=%WEBCLIENT%\dist"

pushd "%ROOT%"

echo.
echo ===== [1/4] 安装并构建前端 =====
pushd "%WEBCLIENT%"
if not exist "node_modules\" (
  call npm ci
  if errorlevel 1 goto :fail
) else (
  echo node_modules 已存在，跳过 npm ci
)

call npm run build:release
if errorlevel 1 goto :fail
popd

if not exist "%DIST%\index.html" (
  echo 错误：未找到前端构建产物 %DIST%\index.html
  goto :fail
)

echo.
echo ===== [2/4] 复制前端到 WebApi\wwwroot =====
if exist "%WWWROOT%" rmdir /s /q "%WWWROOT%"
mkdir "%WWWROOT%"
xcopy /e /i /y /q "%DIST%\*" "%WWWROOT%\" >nul
if errorlevel 1 goto :fail

echo.
echo ===== [3/4] 发布后端（win-x64 自包含单文件）=====
taskkill /IM SengokuScrollGame.exe /F >nul 2>&1
if exist "%BUILD_DIR%" rmdir /s /q "%BUILD_DIR%"
mkdir "%BUILD_DIR%"

dotnet publish "%WEBAPI%\SengokuScroll.WebApi.csproj" ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  /p:EmbedWebClient=true ^
  /p:PublishSingleFile=true ^
  /p:IncludeNativeLibrariesForSelfExtract=true ^
  /p:EnableCompressionInSingleFile=true ^
  /p:DebugType=None ^
  /p:DebugSymbols=false ^
  -o "%BUILD_DIR%"
if errorlevel 1 goto :fail

if exist "%WWWROOT%" rmdir /s /q "%WWWROOT%"

if exist "%BUILD_DIR%\SengokuScroll.WebApi.exe" (
  move /y "%BUILD_DIR%\SengokuScroll.WebApi.exe" "%BUILD_DIR%\SengokuScrollGame.exe" >nul
)

echo.
echo ===== [4/4] 生成启动脚本 =====
set "PKG_DIR=%BUILD_DIR%"
powershell -NoProfile -Command ^
  "$dir=$env:PKG_DIR;" ^
  "$launcher='@echo off','chcp 65001 >nul','cd /d \"\"%%~dp0\"\"','echo Starting SengokuScroll...','start \"\" \"\"%%~dp0SengokuScrollGame.exe\"\"','echo Open http://127.0.0.1:5100/ if browser does not launch.','pause';" ^
  "Set-Content -LiteralPath (Join-Path $dir 'LaunchGame.cmd') -Value ($launcher -join [Environment]::NewLine) -Encoding ASCII;" ^
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
