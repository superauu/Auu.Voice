#ifndef OUTPUTSIMULATOR_H
#define OUTPUTSIMULATOR_H

#include <QObject>
#include <QString>
#include <QClipboard>
#include <QApplication>
#include <QTimer>
#include <QMimeData>

#ifdef Q_OS_WIN
#include <windows.h>
#endif

class OutputSimulator : public QObject
{
    Q_OBJECT
    
public:
    explicit OutputSimulator(QObject* parent = nullptr);
    
    static void sendTextToActiveWindow(const QString& text);
    static void sendTextWithClipboard(const QString& text);
    static void sendTextDirect(const QString& text);
    
public slots:
    void sendTextToActiveWindowAsync(const QString& text);
    void sendTextWithClipboardAsync(const QString& text);
    
signals:
    void textSent();
    void sendingFailed(const QString& error);
    
private:
    static QString escapeSpecialCharacters(const QString& text);
    static void setClipboardWithRetry(const QString& text);
    
#ifdef Q_OS_WIN
    static HWND getForegroundWindow();
    static bool setForegroundWindow(HWND hWnd);
    static void sendKeySequence(const QString& sequence);
#endif
};

#endif // OUTPUTSIMULATOR_H