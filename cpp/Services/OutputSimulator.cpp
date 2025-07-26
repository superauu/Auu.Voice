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
    const int maxRetries = 5;
    QString originalClipboard;
    
    QClipboard* clipboard = QApplication::clipboard();
    if (!clipboard) {
        throw std::runtime_error("无法访问剪贴板");
    }
    
    for (int i = 0; i < maxRetries; i++) {
        try {
            // 备份当前剪贴板内容
            if (clipboard->mimeData() && clipboard->mimeData()->hasText()) {
                originalClipboard = clipboard->text();
            }
            
            // 将文本放入剪贴板
            clipboard->setText(text, QClipboard::Clipboard);
            
            // 等待剪贴板操作完成
            QThread::msleep(100);
            
            // 验证剪贴板内容
            QString clipboardText = clipboard->text(QClipboard::Clipboard);
            if (clipboardText == text) {
#ifdef Q_OS_WIN
                // 获取当前活动窗口
                HWND activeWindow = GetForegroundWindow();
                if (!activeWindow) {
                    // 如果没有活动窗口，尝试获取桌面窗口
                    activeWindow = GetDesktopWindow();
                }
                
                if (activeWindow) {
                    // 确保窗口获得焦点
                    SetForegroundWindow(activeWindow);
                    SetFocus(activeWindow);
                    QThread::msleep(100);
                    
                    // 使用更可靠的方式发送Ctrl+V
                    INPUT inputs[4] = {};
                    
                    // 按下Ctrl
                    inputs[0].type = INPUT_KEYBOARD;
                    inputs[0].ki.wVk = VK_CONTROL;
                    inputs[0].ki.dwFlags = 0;
                    
                    // 按下V
                    inputs[1].type = INPUT_KEYBOARD;
                    inputs[1].ki.wVk = 'V';
                    inputs[1].ki.dwFlags = 0;
                    
                    // 释放V
                    inputs[2].type = INPUT_KEYBOARD;
                    inputs[2].ki.wVk = 'V';
                    inputs[2].ki.dwFlags = KEYEVENTF_KEYUP;
                    
                    // 释放Ctrl
                    inputs[3].type = INPUT_KEYBOARD;
                    inputs[3].ki.wVk = VK_CONTROL;
                    inputs[3].ki.dwFlags = KEYEVENTF_KEYUP;
                    
                    SendInput(4, inputs, sizeof(INPUT));
                }
#endif
                
                // 等待粘贴操作完成
                QThread::msleep(200);
                
                // 延迟后恢复原剪贴板内容
                QTimer::singleShot(1000, [clipboard, originalClipboard]() {
                    if (!originalClipboard.isEmpty()) {
                        clipboard->setText(originalClipboard, QClipboard::Clipboard);
                    }
                });
                
                return; // 成功，退出重试循环
            }
        } catch (...) {
            // 剪贴板被其他进程占用，等待后重试
            QThread::msleep(200);
        }
        
        if (i < maxRetries - 1) {
            QThread::msleep(100 * (i + 1)); // 递增延迟
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