#include "HotkeyService.h"
#include <QApplication>
#include <QDebug>

#ifdef Q_OS_WIN
#include <windows.h>
#endif

HotkeyService::HotkeyService(QObject* parent)
    : QObject(parent)
    , m_isKeyPressed(false)
    , m_keyStateTimer(new QTimer(this))
#ifdef Q_OS_WIN
    , m_modifiers(0)
    , m_targetKey(0)
    , m_windowHandle(nullptr)
#endif
{
    m_keyStateTimer->setInterval(50); // 50ms检查一次
    connect(m_keyStateTimer, &QTimer::timeout, this, &HotkeyService::checkKeyState);
    
    // 安装本地事件过滤器
    QApplication::instance()->installNativeEventFilter(this);
}

HotkeyService::~HotkeyService()
{
    unregisterHotkey();
    QApplication::instance()->removeNativeEventFilter(this);
}

bool HotkeyService::registerHotkey(WId windowHandle, const QString& hotkey)
{
#ifdef Q_OS_WIN
    m_windowHandle = reinterpret_cast<HWND>(windowHandle);
    
    // 解析快捷键字符串
    parseHotkey(hotkey);
    
    // 启动按键状态监听定时器
    m_keyStateTimer->start();
    
    // 注册系统热键
    return RegisterHotKey(m_windowHandle, HOTKEY_ID, m_modifiers, m_targetKey);
#else
    Q_UNUSED(windowHandle)
    Q_UNUSED(hotkey)
    return false;
#endif
}

void HotkeyService::unregisterHotkey()
{
    m_keyStateTimer->stop();
    
#ifdef Q_OS_WIN
    if (m_windowHandle) {
        UnregisterHotKey(m_windowHandle, HOTKEY_ID);
        m_windowHandle = nullptr;
    }
#endif
}

bool HotkeyService::nativeEventFilter(const QByteArray& eventType, void* message, qintptr* result)
{
    Q_UNUSED(result)
    
#ifdef Q_OS_WIN
    if (eventType == "windows_generic_MSG") {
        MSG* msg = static_cast<MSG*>(message);
        if (msg->message == WM_HOTKEY && msg->wParam == HOTKEY_ID) {
            // 热键消息已由定时器处理，这里只是消费消息
            return true;
        }
    }
#else
    Q_UNUSED(eventType)
    Q_UNUSED(message)
#endif
    
    return false;
}

void HotkeyService::checkKeyState()
{
#ifdef Q_OS_WIN
    try {
        bool ctrlPressed = (m_modifiers & MOD_CONTROL) != 0 && isKeyPressed(VK_CONTROL);
        bool altPressed = (m_modifiers & MOD_ALT) != 0 && isKeyPressed(VK_MENU);
        bool shiftPressed = (m_modifiers & MOD_SHIFT) != 0 && isKeyPressed(VK_SHIFT);
        bool targetKeyPressed = isKeyPressed(m_targetKey);
        
        // 检查所有修饰键和目标键是否都被按下
        bool allKeysPressed = targetKeyPressed;
        if (m_modifiers & MOD_CONTROL) allKeysPressed &= ctrlPressed;
        if (m_modifiers & MOD_ALT) allKeysPressed &= altPressed;
        if (m_modifiers & MOD_SHIFT) allKeysPressed &= shiftPressed;
        
        if (allKeysPressed && !m_isKeyPressed) {
            m_isKeyPressed = true;
            emit hotkeyPressed();
        } else if (!allKeysPressed && m_isKeyPressed) {
            m_isKeyPressed = false;
            emit hotkeyReleased();
        }
    } catch (...) {
        // 忽略检查过程中的异常
        qDebug() << "按键状态检查异常";
    }
#endif
}

void HotkeyService::parseHotkey(const QString& hotkey)
{
#ifdef Q_OS_WIN
    QStringList parts = hotkey.split('+');
    m_modifiers = 0;
    m_targetKey = 0;
    
    for (const QString& part : parts) {
        QString trimmedPart = part.trimmed().toUpper();
        
        if (trimmedPart == "CTRL") {
            m_modifiers |= MOD_CONTROL;
        } else if (trimmedPart == "ALT") {
            m_modifiers |= MOD_ALT;
        } else if (trimmedPart == "SHIFT") {
            m_modifiers |= MOD_SHIFT;
        } else if (trimmedPart.length() == 1) {
            // 单个字符键
            m_targetKey = trimmedPart.at(0).unicode();
        } else {
            // 功能键等
            if (trimmedPart == "F1") m_targetKey = VK_F1;
            else if (trimmedPart == "F2") m_targetKey = VK_F2;
            else if (trimmedPart == "F3") m_targetKey = VK_F3;
            else if (trimmedPart == "F4") m_targetKey = VK_F4;
            else if (trimmedPart == "F5") m_targetKey = VK_F5;
            else if (trimmedPart == "F6") m_targetKey = VK_F6;
            else if (trimmedPart == "F7") m_targetKey = VK_F7;
            else if (trimmedPart == "F8") m_targetKey = VK_F8;
            else if (trimmedPart == "F9") m_targetKey = VK_F9;
            else if (trimmedPart == "F10") m_targetKey = VK_F10;
            else if (trimmedPart == "F11") m_targetKey = VK_F11;
            else if (trimmedPart == "F12") m_targetKey = VK_F12;
            else if (trimmedPart == "SPACE") m_targetKey = VK_SPACE;
            else if (trimmedPart == "ENTER") m_targetKey = VK_RETURN;
            else if (trimmedPart == "TAB") m_targetKey = VK_TAB;
            else if (trimmedPart == "ESC") m_targetKey = VK_ESCAPE;
            else if (trimmedPart == "BACKSPACE") m_targetKey = VK_BACK;
            else if (trimmedPart == "DELETE") m_targetKey = VK_DELETE;
            else if (trimmedPart == "INSERT") m_targetKey = VK_INSERT;
            else if (trimmedPart == "HOME") m_targetKey = VK_HOME;
            else if (trimmedPart == "END") m_targetKey = VK_END;
            else if (trimmedPart == "PAGEUP") m_targetKey = VK_PRIOR;
            else if (trimmedPart == "PAGEDOWN") m_targetKey = VK_NEXT;
            else if (trimmedPart == "UP") m_targetKey = VK_UP;
            else if (trimmedPart == "DOWN") m_targetKey = VK_DOWN;
            else if (trimmedPart == "LEFT") m_targetKey = VK_LEFT;
            else if (trimmedPart == "RIGHT") m_targetKey = VK_RIGHT;
        }
    }
#else
    Q_UNUSED(hotkey)
#endif
}

bool HotkeyService::isKeyPressed(int virtualKey) const
{
#ifdef Q_OS_WIN
    return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
#else
    Q_UNUSED(virtualKey)
    return false;
#endif
}