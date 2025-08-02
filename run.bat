@echo off
chcp 65001 >nul
echo === AuuVoice Quick Start ===
echo === AuuVoice 快速启动 ===
echo.

REM Check if the application is built
REM 检查应用程序是否已构建
if exist "bin\Release\net9.0-windows\Speech2TextAssistant.exe" (
    echo Running from Release build...
    echo 从 Release 构建运行...
    start "" "bin\Release\net9.0-windows\Speech2TextAssistant.exe"
    goto :end
)

if exist "publish\Speech2TextAssistant.exe" (
    echo Running from published version...
    echo 从发布版本运行...
    start "" "publish\Speech2TextAssistant.exe"
    goto :end
)

echo Application not found. Attempting to build and run...
echo 未找到应用程序。尝试构建并运行...
echo.

REM Check if .NET is available
REM 检查 .NET 是否可用
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo Error: .NET is not installed or not found in PATH.
    echo 错误: 未安装 .NET 或在 PATH 中找不到。
    echo Please install .NET 9.0 from: https://dotnet.microsoft.com/download
    echo 请从以下地址安装 .NET 9.0: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo Building application...
echo 构建应用程序...
dotnet build --configuration Release

if errorlevel 1 (
    echo Build failed. Please check the error messages above.
    echo 构建失败。请检查上面的错误消息。
    pause
    exit /b 1
)

echo Build successful. Starting application...
echo 构建成功。启动应用程序...
dotnet run --configuration Release

:end
echo.
echo AuuVoice is starting...
echo AuuVoice 正在启动...
echo.
echo First-time setup reminders:
echo 首次设置提醒:
echo 1. Configure OpenAI API key in settings
echo    在设置中配置 OpenAI API 密钥
echo 2. Configure Azure Speech Services
echo    配置 Azure 语音服务
echo 3. Test your hotkeys
echo    测试您的快捷键
echo.
echo Enjoy using AuuVoice! 🎤✨
echo 享受使用 AuuVoice! 🎤✨
echo.
echo Press any key to close this window...
echo 按任意键关闭此窗口...
pause >nul