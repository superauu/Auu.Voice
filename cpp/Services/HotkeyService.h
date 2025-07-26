#ifndef HOTKEYSERVICE_H
#define HOTKEYSERVICE_H

#include <QObject>
#include <QString>
#include <QTimer>
#include <QAbstractNativeEventFilter>
#include <QWidget>

#ifdef Q_OS_WIN
#include <windows.h>
#endif

class HotkeyService : public QObject, public QAbstractNativeEventFilter
{
    Q_OBJECT
    
public:
    explicit HotkeyService(QObject* parent = nullptr);
    ~HotkeyService();
    
    bool registerHotkey(WId windowHandle, const QString& hotkey);
    void unregisterHotkey();
    
    // QAbstractNativeEventFilter interface
    bool nativeEventFilter(const QByteArray& eventType, void* message, qintptr* result) override;
    
signals:
    void hotkeyPressed();
    void hotkeyReleased();
    
private slots:
    void checkKeyState();
    
private:
    void parseHotkey(const QString& hotkey);
    bool isKeyPressed(int virtualKey) const;
    
    static const int HOTKEY_ID = 9000;
    
#ifdef Q_OS_WIN
    UINT m_modifiers;
    UINT m_targetKey;
    HWND m_windowHandle;
#endif
    
    bool m_isKeyPressed;
    QTimer* m_keyStateTimer;
};

#endif // HOTKEYSERVICE_H