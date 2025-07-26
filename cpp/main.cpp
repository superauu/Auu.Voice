#include <QApplication>
#include <QMessageBox>
#include <QDir>
#include <QStandardPaths>
#include <QStyleFactory>
#include <QFont>
#include "MainWindow.h"

int main(int argc, char *argv[])
{
    QApplication app(argc, argv);
    
    // 设置应用程序信息
    app.setApplicationName("Speech2TextAssistant");
    app.setApplicationVersion("1.0.0");
    app.setOrganizationName("AuuVoice");
    app.setOrganizationDomain("auuvoice.com");
    
    // 设置应用程序样式
    app.setStyle(QStyleFactory::create("Fusion"));
    
    // 设置字体
    QFont font = app.font();
    font.setPointSize(9);
    app.setFont(font);
    
    // 设置应用程序图标
    app.setWindowIcon(QIcon(":/icon.png"));
    
    // 确保配置目录存在
    QString configDir = QStandardPaths::writableLocation(QStandardPaths::AppDataLocation);
    QDir().mkpath(configDir);
    
    // 检查是否已有实例在运行（简单检查）
    QStringList arguments = app.arguments();
    if (arguments.contains("--single-instance")) {
        // 这里可以添加单实例检查逻辑
        // 为了简化，暂时跳过
    }
    
    try {
        // 创建主窗口
        MainWindow window;
        
        // 显示主窗口
        window.show();
        
        // 运行应用程序事件循环
        return app.exec();
    }
    catch (const std::exception& e) {
        QMessageBox::critical(nullptr, "错误", 
            QString("应用程序启动失败: %1").arg(e.what()));
        return -1;
    }
    catch (...) {
        QMessageBox::critical(nullptr, "错误", "应用程序启动失败: 未知错误");
        return -1;
    }
}