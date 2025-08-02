# AuuVoice Quick Start Script
# AuuVoice 快速启动脚本

Write-Host "=== AuuVoice Quick Start ===" -ForegroundColor Green
Write-Host "=== AuuVoice 快速启动 ===" -ForegroundColor Green
Write-Host ""

# Check if the application is built
# 检查应用程序是否已构建
if (Test-Path "./bin/Release/net9.0-windows/Speech2TextAssistant.exe") {
    Write-Host "Running from Release build..." -ForegroundColor Yellow
    Write-Host "从 Release 构建运行..." -ForegroundColor Yellow
    Start-Process "./bin/Release/net9.0-windows/Speech2TextAssistant.exe"
}
elseif (Test-Path "./publish/Speech2TextAssistant.exe") {
    Write-Host "Running from published version..." -ForegroundColor Yellow
    Write-Host "从发布版本运行..." -ForegroundColor Yellow
    Start-Process "./publish/Speech2TextAssistant.exe"
}
else {
    Write-Host "Application not found. Building and running..." -ForegroundColor Yellow
    Write-Host "未找到应用程序。正在构建并运行..." -ForegroundColor Yellow
    
    # Check if .NET is available
    # 检查 .NET 是否可用
    try {
        $dotnetVersion = dotnet --version
        Write-Host "Using .NET version: $dotnetVersion" -ForegroundColor Green
        Write-Host "使用 .NET 版本: $dotnetVersion" -ForegroundColor Green
    }
    catch {
        Write-Host "Error: .NET is not installed or not found in PATH." -ForegroundColor Red
        Write-Host "错误: 未安装 .NET 或在 PATH 中找不到。" -ForegroundColor Red
        Write-Host "Please run install.ps1 first or install .NET 9.0." -ForegroundColor Yellow
        Write-Host "请先运行 install.ps1 或安装 .NET 9.0。" -ForegroundColor Yellow
        exit 1
    }
    
    # Build and run
    # 构建并运行
    Write-Host "Building application..." -ForegroundColor Yellow
    Write-Host "构建应用程序..." -ForegroundColor Yellow
    
    dotnet build --configuration Release
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Build successful. Starting application..." -ForegroundColor Green
        Write-Host "✅ 构建成功。启动应用程序..." -ForegroundColor Green
        dotnet run --configuration Release
    } else {
        Write-Host "❌ Build failed. Please check the error messages above." -ForegroundColor Red
        Write-Host "❌ 构建失败。请检查上面的错误消息。" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "AuuVoice is starting..." -ForegroundColor Green
Write-Host "AuuVoice 正在启动..." -ForegroundColor Green
Write-Host ""
Write-Host "First-time setup reminders:" -ForegroundColor Yellow
Write-Host "首次设置提醒:" -ForegroundColor Yellow
Write-Host "1. Configure OpenAI API key in settings" -ForegroundColor White
Write-Host "   在设置中配置 OpenAI API 密钥" -ForegroundColor White
Write-Host "2. Configure Azure Speech Services" -ForegroundColor White
Write-Host "   配置 Azure 语音服务" -ForegroundColor White
Write-Host "3. Test your hotkeys" -ForegroundColor White
Write-Host "   测试您的快捷键" -ForegroundColor White
Write-Host ""
Write-Host "Enjoy using AuuVoice! 🎤✨" -ForegroundColor Cyan
Write-Host "享受使用 AuuVoice! 🎤✨" -ForegroundColor Cyan