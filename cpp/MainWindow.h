#ifndef MAINWINDOW_H
#define MAINWINDOW_H

#include <QMainWindow>
#include <QVBoxLayout>
#include <QHBoxLayout>
#include <QGridLayout>
#include <QLabel>
#include <QLineEdit>
#include <QComboBox>
#include <QPushButton>
#include <QListWidget>
#include <QTextEdit>
#include <QGroupBox>
#include <QSystemTrayIcon>
#include <QMenu>
#include <QAction>
#include <QCloseEvent>
#include <QTimer>
#include <QMessageBox>
#include <QDateTime>
#include <QKeyEvent>
#include <QCheckBox>
#include <QSettings>
#include <QDir>
#include <memory>

#include "Models/AppSettings.h"
#include "Models/ChatGptPromptType.h"
#include "Services/ConfigManager.h"
#include "Services/SpeechRecognizerService.h"
#include "Services/ChatGptService.h"
#include "Services/HotkeyService.h"
#include "Services/OutputSimulator.h"
#include "UI/RecordingOverlay.h"

class MainWindow : public QMainWindow
{
    Q_OBJECT
    
public:
    explicit MainWindow(QWidget* parent = nullptr);
    ~MainWindow();
    
protected:
    void closeEvent(QCloseEvent* event) override;
    bool eventFilter(QObject* obj, QEvent* event) override;
    
private slots:
    // UI事件处理
    void onSaveButtonClicked();
    void onTestButtonClicked();
    void onHotkeyButtonClicked();
    void onExitButtonClicked();
    void onAutoStartCheckBoxToggled(bool checked);
    void onTrayIconActivated(QSystemTrayIcon::ActivationReason reason);
    void onShowMainWindow();
    void onExitApplication();
    
    // 快捷键事件处理
    void onHotkeyPressed();
    void onHotkeyReleased();
    
    // 语音识别事件处理
    void onRecognitionStarted();
    void onRecognitionCompleted(const QString& text);
    void onRecognitionFailed(const QString& error);
    void onPartialResultReceived(const QString& text);
    
    // ChatGPT事件处理
    void onTextProcessed(const QString& result);
    void onProcessingFailed(const QString& error);
    
private:
    void setupUI();
    void setupTrayIcon();
    void loadSettings();
    void registerHotkey();
    void initializeAPIs();
    void updateStatus(const QString& message, const QColor& color);
    void addLog(const QString& message);
    void showRecordingOverlay();
    void hideRecordingOverlay();
    QString getKeyText(QKeyEvent* event);
    void setAutoStart(bool enabled);
    bool isAutoStartEnabled();
    
    // UI组件
    QWidget* m_centralWidget;
    QVBoxLayout* m_mainLayout;
    
    // 设置组
    QGroupBox* m_settingsGroup;
    QGridLayout* m_settingsLayout;
    QLabel* m_apiKeyLabel;
    QLineEdit* m_apiKeyLineEdit;
    QLabel* m_speechKeyLabel;
    QLineEdit* m_speechKeyLineEdit;
    QLabel* m_speechRegionLabel;
    QLineEdit* m_speechRegionLineEdit;
    QLabel* m_hotkeyLabel;
    QLineEdit* m_hotkeyLineEdit;
    QPushButton* m_hotkeyButton;
    QLabel* m_modelLabel;
    QComboBox* m_modelComboBox;
    QLabel* m_processingModeLabel;
    QComboBox* m_processingModeComboBox;
    QLabel* m_recordingModeLabel;
    QComboBox* m_recordingModeComboBox;
    
    // 控制按钮
    QHBoxLayout* m_buttonLayout;
    QPushButton* m_saveButton;
    QPushButton* m_testButton;
    QPushButton* m_exitButton;
    
    // 开机自启动设置
    QCheckBox* m_autoStartCheckBox;
    
    // 状态和日志
    QLabel* m_statusLabel;
    QListWidget* m_logListWidget;
    
    // 系统托盘
    QSystemTrayIcon* m_trayIcon;
    QMenu* m_trayMenu;
    QAction* m_showAction;
    QAction* m_exitAction;
    
    // 服务类
    std::unique_ptr<AppSettings> m_settings;
    std::unique_ptr<SpeechRecognizerService> m_speechService;
    std::unique_ptr<ChatGptService> m_chatGptService;
    std::unique_ptr<HotkeyService> m_hotkeyService;
    std::unique_ptr<OutputSimulator> m_outputSimulator;
    std::unique_ptr<RecordingOverlay> m_recordingOverlay;
    
    // 状态变量
    bool m_isToggleRecording;
};

#endif // MAINWINDOW_H