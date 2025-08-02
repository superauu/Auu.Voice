# AuuVoice Installation Script
# AuuVoice 安装脚本

Write-Host "=== AuuVoice Installation Script ===" -ForegroundColor Green
Write-Host "=== AuuVoice 安装脚本 ===" -ForegroundColor Green
Write-Host ""

# Check if .NET 9.0 is installed
# 检查是否安装了 .NET 9.0
Write-Host "Checking .NET installation..." -ForegroundColor Yellow
Write-Host "检查 .NET 安装..." -ForegroundColor Yellow

try {
    $dotnetVersion = dotnet --version
    Write-Host "Found .NET version: $dotnetVersion" -ForegroundColor Green
    Write-Host "找到 .NET 版本: $dotnetVersion" -ForegroundColor Green
    
    if ($dotnetVersion -notmatch "^9\.0") {
        Write-Host "Warning: .NET 9.0 is recommended for this application." -ForegroundColor Yellow
        Write-Host "警告: 建议为此应用程序使用 .NET 9.0。" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "Error: .NET is not installed or not found in PATH." -ForegroundColor Red
    Write-Host "错误: 未安装 .NET 或在 PATH 中找不到。" -ForegroundColor Red
    Write-Host "Please download and install .NET 9.0 from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    Write-Host "请从以下地址下载并安装 .NET 9.0: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Restore NuGet packages
# 恢复 NuGet 包
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
Write-Host "恢复 NuGet 包..." -ForegroundColor Yellow

try {
    dotnet restore
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ NuGet packages restored successfully." -ForegroundColor Green
        Write-Host "✅ NuGet 包恢复成功。" -ForegroundColor Green
    } else {
        throw "dotnet restore failed"
    }
}
catch {
    Write-Host "❌ Failed to restore NuGet packages." -ForegroundColor Red
    Write-Host "❌ 恢复 NuGet 包失败。" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Build the application
# 构建应用程序
Write-Host "Building the application..." -ForegroundColor Yellow
Write-Host "构建应用程序..." -ForegroundColor Yellow

try {
    dotnet build --configuration Release
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Application built successfully." -ForegroundColor Green
        Write-Host "✅ 应用程序构建成功。" -ForegroundColor Green
    } else {
        throw "dotnet build failed"
    }
}
catch {
    Write-Host "❌ Failed to build the application." -ForegroundColor Red
    Write-Host "❌ 构建应用程序失败。" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Publish the application
# 发布应用程序
Write-Host "Publishing the application..." -ForegroundColor Yellow
Write-Host "发布应用程序..." -ForegroundColor Yellow

try {
    dotnet publish --configuration Release --output ./publish
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Application published successfully." -ForegroundColor Green
        Write-Host "✅ 应用程序发布成功。" -ForegroundColor Green
    } else {
        throw "dotnet publish failed"
    }
}
catch {
    Write-Host "❌ Failed to publish the application." -ForegroundColor Red
    Write-Host "❌ 发布应用程序失败。" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== Installation Complete! ===" -ForegroundColor Green
Write-Host "=== 安装完成! ===" -ForegroundColor Green
Write-Host ""
Write-Host "The application has been published to: ./publish/" -ForegroundColor Cyan
Write-Host "应用程序已发布到: ./publish/" -ForegroundColor Cyan
Write-Host ""
Write-Host "To run the application:" -ForegroundColor Yellow
Write-Host "要运行应用程序:" -ForegroundColor Yellow
Write-Host "  cd publish" -ForegroundColor White
Write-Host "  .\Speech2TextAssistant.exe" -ForegroundColor White
Write-Host ""
Write-Host "Or run directly with:" -ForegroundColor Yellow
Write-Host "或直接运行:" -ForegroundColor Yellow
Write-Host "  dotnet run" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "下一步:" -ForegroundColor Yellow
Write-Host "1. Configure your OpenAI API key" -ForegroundColor White
Write-Host "   配置您的 OpenAI API 密钥" -ForegroundColor White
Write-Host "2. Configure your Azure Speech Services key and region" -ForegroundColor White
Write-Host "   配置您的 Azure 语音服务密钥和区域" -ForegroundColor White
Write-Host "3. Set up your preferred hotkeys" -ForegroundColor White
Write-Host "   设置您首选的快捷键" -ForegroundColor White
Write-Host "4. Choose your default processing mode" -ForegroundColor White
Write-Host "   选择您的默认处理模式" -ForegroundColor White
Write-Host ""
Write-Host "For more information, please read the README.md file." -ForegroundColor Cyan
Write-Host "更多信息，请阅读 README.md 文件。" -ForegroundColor Cyan