#include "OutputSimulator.h"
#include <QMessageBox>
#include <QThread>
#include <QDebug>

#ifdef Q_OS_WIN
#include <windows.h>
#endif

OutputSimulator::OutputSimulator(QObject* parent)
    : QObject(parent)
{
}

void OutputSimulator::sendTextToActiveWindow(const QString& text)
{
    try {
#ifdef Q_OS_WIN
        // 获取当前活动窗口
        HWND activeWindow = GetForegroundWindow();
        if (!activeWindow) {
            return;
        }
        
        // 短暂延迟确保窗口准备就绪
        QThread::msleep(100);
        
        // 处理特殊字符并发送文本
        QString escapedText = escapeSpecialCharacters(text);
        sendKeySequence(escapedText);
#else
        Q_UNUSED(text)
#endif
    } catch (const std::exception& e) {
        QMessageBox::critical(nullptr, "错误", QString("输出文本失败: %1").arg(e.what()));
    }
}

void OutputSimulator::sendTextWithClipboard(const QString& text)
{
    try {
        setClipboardWithRetry(text);
    } catch (const std::exception& e) {
        QMessageBox::critical(nullptr, "错误", QString("粘贴文本失败: %1").arg(e.what()));
    }
}

void OutputSimulator::sendTextDirect(const QString& text)
{
    try {
#ifdef Q_OS_WIN
        HWND activeWindow = GetForegroundWindow();
        if (!activeWindow) {
            return;
        }
        
        // 确保窗口获得焦点
        SetForegroundWindow(activeWindow);
        QThread::msleep(100);
        
        // 逐字符发送，避免特殊字符问题
        for (const QChar& c : text) {
            if (c.category() >= QChar::Other_Control && c.category() <= QChar::Other_NotAssigned) {
                // 处理控制字符
                if (c == '\n') {
                    keybd_event(VK_RETURN, 0, 0, 0);
                    keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP, 0);
                } else if (c == '\t') {
                    keybd_event(VK_TAB, 0, 0, 0);
                    keybd_event(VK_TAB, 0, KEYEVENTF_KEYUP, 0);
                }
            } else {
                // 发送普通字符
                INPUT input = {0};
                input.type = INPUT_KEYBOARD;
                input.ki.wVk = 0;
                input.ki.wScan = c.unicode();
                input.ki.dwFlags = KEYEVENTF_UNICODE;
                SendInput(1, &input, sizeof(INPUT));
            }
            
            // 小延迟确保字符正确发送
            QThread::msleep(1);
        }
#else
        Q_UNUSED(text)
#endif
    } catch (const std::exception& e) {
        QMessageBox::critical(nullptr, "错误", QString("直接输入文本失败: %1").arg(e.what()));
    }
}

void OutputSimulator::sendTextToActiveWindowAsync(const QString& text)
{
    QTimer::singleShot(0, [text]() {
        sendTextToActiveWindow(text);
    });
}

void OutputSimulator::sendTextWithClipboardAsync(const QString& text)
{
    QTimer::singleShot(0, [this, text]() {
        try {
            setClipboardWithRetry(text);
            emit textSent();
        } catch (const std::exception& e) {
            emit sendingFailed(QString("粘贴文本失败: %1").arg(e.what()));
        }
    });
}

QString OutputSimulator::escapeSpecialCharacters(const QString& text)
{
    QString escaped = text;
    escaped.replace("{", "{{}")
           .replace("}", "{}}") 
           .replace("+", "{+}")
           .replace("^", "{^}")
           .replace("%", "{%}")
           .replace("~", "{~}")
           .replace("(", "{(}")
           .replace(")", "{)}")
           .replace("[", "{[}")
           .replace("]", "{]}");
    return escaped;
}

void OutputSimulator::setClipboardWithRetry(const QString& text)
{
    const int maxRetries = 3;
    QString originalClipboard;
    
    QClipboard* clipboard = QApplication::clipboard();
    if (!clipboard) {
        throw std::runtime_error("无法访问剪贴板");
    }
    
    for (int i = 0; i < maxRetries; i++) {
        try {
            // 备份当前剪贴板内容
            if (clipboard->mimeData()->hasText()) {
                originalClipboard = clipboard->text();
            }
            
            // 清空剪贴板
            clipboard->clear();
            QThread::msleep(10);
            
            // 将文本放入剪贴板
            clipboard->setText(text);
            QThread::msleep(50);
            
            // 验证剪贴板内容
            if (clipboard->text() == text) {
#ifdef Q_OS_WIN
                // 获取当前活动窗口并确保获得焦点
                HWND activeWindow = GetForegroundWindow();
                if (activeWindow) {
                    SetForegroundWindow(activeWindow);
                    QThread::msleep(50);
                }
                
                // 发送Ctrl+V粘贴
                keybd_event(VK_CONTROL, 0, 0, 0);
                keybd_event('V', 0, 0, 0);
                keybd_event('V', 0, KEYEVENTF_KEYUP, 0);
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
#endif
                QThread::msleep(100);
                
                // 延迟后恢复原剪贴板内容
                QThread::msleep(400);
                if (!originalClipboard.isEmpty()) {
                    clipboard->setText(originalClipboard);
                }
                return; // 成功，退出重试循环
            }
        } catch (...) {
            // 剪贴板被其他进程占用，等待后重试
            QThread::msleep(100);
            if (i == maxRetries - 1) {
                throw std::runtime_error("无法访问剪贴板，请稍后重试");
            }
        }
    }
    
    throw std::runtime_error("无法访问剪贴板，请稍后重试");
}

#ifdef Q_OS_WIN
HWND OutputSimulator::getForegroundWindow()
{
    return GetForegroundWindow();
}

bool OutputSimulator::setForegroundWindow(HWND hWnd)
{
    return SetForegroundWindow(hWnd);
}

void OutputSimulator::sendKeySequence(const QString& sequence)
{
    // 简化的键盘输入模拟
    for (const QChar& c : sequence) {
        if (!(c.category() >= QChar::Other_Control && c.category() <= QChar::Other_NotAssigned)) {
            INPUT input = {0};
            input.type = INPUT_KEYBOARD;
            input.ki.wVk = 0;
            input.ki.wScan = c.unicode();
            input.ki.dwFlags = KEYEVENTF_UNICODE;
            SendInput(1, &input, sizeof(INPUT));
            QThread::msleep(1);
        }
    }
}
#endif