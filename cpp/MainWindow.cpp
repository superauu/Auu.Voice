#include "MainWindow.h"
#include <QApplication>
#include <QKeyEvent>
#include <QScreen>
#include <QStandardPaths>
#include <QDir>
#include <QDebug>
#include <QDateTime>
#include <QColor>

MainWindow::MainWindow(QWidget* parent)
    : QMainWindow(parent)
    , m_centralWidget(nullptr)
    , m_mainLayout(nullptr)
    , m_trayIcon(nullptr)
    , m_settings(std::make_unique<AppSettings>())
    , m_speechService(std::make_unique<SpeechRecognizerService>(this))
    , m_chatGptService(std::make_unique<ChatGptService>(this))
    , m_hotkeyService(std::make_unique<HotkeyService>(this))
    , m_outputSimulator(std::make_unique<OutputSimulator>(this))
    , m_recordingOverlay(nullptr)
    , m_isToggleRecording(false)
{
    setupUI();
    setupTrayIcon();
    loadSettings();
    
    // 连接信号和槽
    connect(m_hotkeyService.get(), &HotkeyService::hotkeyPressed, this, &MainWindow::onHotkeyPressed);
    connect(m_hotkeyService.get(), &HotkeyService::hotkeyReleased, this, &MainWindow::onHotkeyReleased);
    
    connect(m_speechService.get(), &SpeechRecognizerService::recognitionStarted, this, &MainWindow::onRecognitionStarted);
    connect(m_speechService.get(), &SpeechRecognizerService::recognitionCompleted, this, &MainWindow::onRecognitionCompleted);
    connect(m_speechService.get(), &SpeechRecognizerService::recognitionFailed, this, &MainWindow::onRecognitionFailed);
    connect(m_speechService.get(), &SpeechRecognizerService::partialResultReceived, this, &MainWindow::onPartialResultReceived);
    
    connect(m_chatGptService.get(), &ChatGptService::textProcessed, this, &MainWindow::onTextProcessed);
    connect(m_chatGptService.get(), &ChatGptService::processingFailed, this, &MainWindow::onProcessingFailed);
    
    // 启动时自动最小化到系统托盘
    QTimer::singleShot(100, this, [this]() {
        hide();
    });
}

MainWindow::~MainWindow()
{
    if (m_hotkeyService) {
        m_hotkeyService->unregisterHotkey();
    }
    
    if (m_speechService && m_speechService->isRecording()) {
        m_speechService->stopRecognition();
    }
    
    if (m_recordingOverlay) {
        m_recordingOverlay->close();
    }
    
    if (m_trayIcon) {
        m_trayIcon->hide();
    }
}

void MainWindow::setupUI()
{
    setWindowTitle("语音转文字助手");
    setWindowIcon(QIcon(":/app.ico"));
    setMinimumSize(600, 500);
    resize(800, 600);
    
    // 创建中央窗口部件
    m_centralWidget = new QWidget(this);
    setCentralWidget(m_centralWidget);
    
    m_mainLayout = new QVBoxLayout(m_centralWidget);
    
    // 设置组
    m_settingsGroup = new QGroupBox("设置", this);
    m_settingsLayout = new QGridLayout(m_settingsGroup);
    
    // API Key
    m_apiKeyLabel = new QLabel("OpenAI API Key:", this);
    m_apiKeyLineEdit = new QLineEdit(this);
    m_apiKeyLineEdit->setEchoMode(QLineEdit::Password);
    m_settingsLayout->addWidget(m_apiKeyLabel, 0, 0);
    m_settingsLayout->addWidget(m_apiKeyLineEdit, 0, 1, 1, 2);
    
    // Speech Key
    m_speechKeyLabel = new QLabel("Azure Speech Key:", this);
    m_speechKeyLineEdit = new QLineEdit(this);
    m_speechKeyLineEdit->setEchoMode(QLineEdit::Password);
    m_settingsLayout->addWidget(m_speechKeyLabel, 1, 0);
    m_settingsLayout->addWidget(m_speechKeyLineEdit, 1, 1, 1, 2);
    
    // Speech Region
    m_speechRegionLabel = new QLabel("Azure Speech Region:", this);
    m_speechRegionLineEdit = new QLineEdit(this);
    m_speechRegionLineEdit->setText("eastus");
    m_settingsLayout->addWidget(m_speechRegionLabel, 2, 0);
    m_settingsLayout->addWidget(m_speechRegionLineEdit, 2, 1, 1, 2);
    
    // Hotkey
    m_hotkeyLabel = new QLabel("快捷键:", this);
    m_hotkeyLineEdit = new QLineEdit(this);
    m_hotkeyLineEdit->setReadOnly(true);
    m_hotkeyButton = new QPushButton("设置快捷键", this);
    m_settingsLayout->addWidget(m_hotkeyLabel, 3, 0);
    m_settingsLayout->addWidget(m_hotkeyLineEdit, 3, 1);
    m_settingsLayout->addWidget(m_hotkeyButton, 3, 2);
    
    // Model
    m_modelLabel = new QLabel("GPT模型:", this);
    m_modelComboBox = new QComboBox(this);
    m_modelComboBox->addItems({"gpt-3.5-turbo", "gpt-4", "gpt-4-turbo", "gpt-4o"});
    m_settingsLayout->addWidget(m_modelLabel, 4, 0);
    m_settingsLayout->addWidget(m_modelComboBox, 4, 1, 1, 2);
    
    // Processing Mode
    m_processingModeLabel = new QLabel("处理模式:", this);
    m_processingModeComboBox = new QComboBox(this);
    m_processingModeComboBox->addItems({
        "TranslateToEnglish",
        "TranslateToEnglishEmail", 
        "FormatAsEmail",
        "Summarize",
        "CustomPrompt"
    });
    m_settingsLayout->addWidget(m_processingModeLabel, 5, 0);
    m_settingsLayout->addWidget(m_processingModeComboBox, 5, 1, 1, 2);
    
    // Recording Mode
    m_recordingModeLabel = new QLabel("录音模式:", this);
    m_recordingModeComboBox = new QComboBox(this);
    m_recordingModeComboBox->addItems({"按住录音 (Hold to Record)", "切换录音 (Toggle Record)"});
    m_settingsLayout->addWidget(m_recordingModeLabel, 6, 0);
    m_settingsLayout->addWidget(m_recordingModeComboBox, 6, 1, 1, 2);
    
    // Auto Start
    m_autoStartCheckBox = new QCheckBox("开机自动启动", this);
    m_settingsLayout->addWidget(m_autoStartCheckBox, 7, 0, 1, 3);
    
    m_mainLayout->addWidget(m_settingsGroup);
    
    // 控制按钮
    m_buttonLayout = new QHBoxLayout();
    m_saveButton = new QPushButton("保存设置", this);
    m_testButton = new QPushButton("测试录音", this);
    m_exitButton = new QPushButton("退出程序", this);
    m_exitButton->setStyleSheet("QPushButton { background-color: #ff6b6b; color: white; font-weight: bold; }");
    m_buttonLayout->addWidget(m_saveButton);
    m_buttonLayout->addWidget(m_testButton);
    m_buttonLayout->addStretch();
    m_buttonLayout->addWidget(m_exitButton);
    m_mainLayout->addLayout(m_buttonLayout);
    
    // 状态标签
    m_statusLabel = new QLabel("状态: 就绪", this);
    m_statusLabel->setStyleSheet("QLabel { color: green; font-weight: bold; }");
    m_mainLayout->addWidget(m_statusLabel);
    
    // 日志列表
    QLabel* logLabel = new QLabel("日志:", this);
    m_mainLayout->addWidget(logLabel);
    m_logListWidget = new QListWidget(this);
    m_logListWidget->setMaximumHeight(150);
    m_mainLayout->addWidget(m_logListWidget);
    
    // 连接信号
    connect(m_saveButton, &QPushButton::clicked, this, &MainWindow::onSaveButtonClicked);
    connect(m_testButton, &QPushButton::clicked, this, &MainWindow::onTestButtonClicked);
    connect(m_hotkeyButton, &QPushButton::clicked, this, &MainWindow::onHotkeyButtonClicked);
    connect(m_exitButton, &QPushButton::clicked, this, &MainWindow::onExitButtonClicked);
    connect(m_autoStartCheckBox, &QCheckBox::toggled, this, &MainWindow::onAutoStartCheckBoxToggled);
    
    // 安装事件过滤器用于快捷键设置
    m_hotkeyLineEdit->installEventFilter(this);
}

void MainWindow::setupTrayIcon()
{
    if (!QSystemTrayIcon::isSystemTrayAvailable()) {
        QMessageBox::critical(this, "系统托盘", "系统托盘不可用");
        return;
    }
    
    m_trayIcon = new QSystemTrayIcon(this);
    m_trayIcon->setIcon(QIcon(":/app.ico"));
    m_trayIcon->setToolTip("语音转文字助手");
    
    // 创建托盘菜单
    m_trayMenu = new QMenu(this);
    m_showAction = new QAction("显示主窗口", this);
    m_exitAction = new QAction("退出", this);
    
    m_trayMenu->addAction(m_showAction);
    m_trayMenu->addSeparator();
    m_trayMenu->addAction(m_exitAction);
    
    m_trayIcon->setContextMenu(m_trayMenu);
    
    // 连接信号
    connect(m_trayIcon, &QSystemTrayIcon::activated, this, &MainWindow::onTrayIconActivated);
    connect(m_showAction, &QAction::triggered, this, &MainWindow::onShowMainWindow);
    connect(m_exitAction, &QAction::triggered, this, &MainWindow::onExitApplication);
    
    m_trayIcon->show();
}

void MainWindow::loadSettings()
{
    *m_settings = ConfigManager::loadSettings();
    
    m_apiKeyLineEdit->setText(m_settings->openAIApiKey);
    m_speechKeyLineEdit->setText(m_settings->azureSpeechKey);
    m_speechRegionLineEdit->setText(m_settings->azureSpeechRegion.isEmpty() ? "eastus" : m_settings->azureSpeechRegion);
    m_hotkeyLineEdit->setText(m_settings->hotKey.isEmpty() ? "F2" : m_settings->hotKey);
    m_modelComboBox->setCurrentText(m_settings->modelName.isEmpty() ? "gpt-3.5-turbo" : m_settings->modelName);
    m_processingModeComboBox->setCurrentText(m_settings->defaultProcessingMode.isEmpty() ? "TranslateToEnglish" : m_settings->defaultProcessingMode);
    m_recordingModeComboBox->setCurrentIndex(m_settings->recordingMode == "ToggleRecord" ? 1 : 0);
    m_autoStartCheckBox->setChecked(m_settings->autoStart);
    
    registerHotkey();
    initializeAPIs();
}

void MainWindow::registerHotkey()
{
    if (!m_settings->hotKey.isEmpty()) {
        m_hotkeyService->registerHotkey(winId(), m_settings->hotKey);
    }
}

void MainWindow::initializeAPIs()
{
    if (!m_settings->azureSpeechKey.isEmpty()) {
        m_speechService->initialize(m_settings->azureSpeechKey, 
                                   m_settings->azureSpeechRegion.isEmpty() ? "eastus" : m_settings->azureSpeechRegion);
    }
    
    if (!m_settings->openAIApiKey.isEmpty()) {
        m_chatGptService->initialize(m_settings->openAIApiKey, 
                                    m_settings->modelName.isEmpty() ? "gpt-3.5-turbo" : m_settings->modelName);
    }
}

void MainWindow::updateStatus(const QString& message, const QColor& color)
{
    m_statusLabel->setText(QString("状态: %1").arg(message));
    m_statusLabel->setStyleSheet(QString("QLabel { color: %1; font-weight: bold; }").arg(color.name()));
}

void MainWindow::addLog(const QString& message)
{
    QString timestamp = QDateTime::currentDateTime().toString("hh:mm:ss");
    QString logEntry = QString("[%1] %2").arg(timestamp, message);
    
    m_logListWidget->insertItem(0, logEntry);
    
    // 限制日志条目数量
    if (m_logListWidget->count() > 100) {
        delete m_logListWidget->takeItem(m_logListWidget->count() - 1);
    }
}

void MainWindow::showRecordingOverlay()
{
    if (!m_recordingOverlay) {
        m_recordingOverlay = std::make_unique<RecordingOverlay>();
    }
    
    m_recordingOverlay->show();
    m_recordingOverlay->startAnimation();
}

void MainWindow::hideRecordingOverlay()
{
    if (m_recordingOverlay) {
        m_recordingOverlay->stopAnimation();
        m_recordingOverlay->hide();
    }
}

void MainWindow::closeEvent(QCloseEvent* event)
{
    if (m_trayIcon && m_trayIcon->isVisible()) {
        hide();
        event->ignore();
    } else {
        event->accept();
    }
}

bool MainWindow::eventFilter(QObject* obj, QEvent* event)
{
    if (obj == m_hotkeyLineEdit && event->type() == QEvent::KeyPress) {
        QKeyEvent* keyEvent = static_cast<QKeyEvent*>(event);
        QString keyText = getKeyText(keyEvent);
        if (!keyText.isEmpty()) {
            m_hotkeyLineEdit->setText(keyText);
            return true;
        }
    }
    return QMainWindow::eventFilter(obj, event);
}

QString MainWindow::getKeyText(QKeyEvent* event)
{
    QString keyText;
    
    // 修饰键
    if (event->modifiers() & Qt::ControlModifier) {
        keyText += "Ctrl+";
    }
    if (event->modifiers() & Qt::AltModifier) {
        keyText += "Alt+";
    }
    if (event->modifiers() & Qt::ShiftModifier) {
        keyText += "Shift+";
    }
    
    // 主键
    int key = event->key();
    if (key >= Qt::Key_F1 && key <= Qt::Key_F35) {
        keyText += QString("F%1").arg(key - Qt::Key_F1 + 1);
    } else if (key >= Qt::Key_A && key <= Qt::Key_Z) {
        keyText += QChar('A' + key - Qt::Key_A);
    } else if (key >= Qt::Key_0 && key <= Qt::Key_9) {
        keyText += QChar('0' + key - Qt::Key_0);
    } else {
        // 其他特殊键
        switch (key) {
            case Qt::Key_Space: keyText += "Space"; break;
            case Qt::Key_Tab: keyText += "Tab"; break;
            case Qt::Key_Return: keyText += "Enter"; break;
            case Qt::Key_Escape: keyText += "Esc"; break;
            case Qt::Key_Backspace: keyText += "Backspace"; break;
            case Qt::Key_Delete: keyText += "Delete"; break;
            case Qt::Key_Insert: keyText += "Insert"; break;
            case Qt::Key_Home: keyText += "Home"; break;
            case Qt::Key_End: keyText += "End"; break;
            case Qt::Key_PageUp: keyText += "PageUp"; break;
            case Qt::Key_PageDown: keyText += "PageDown"; break;
            case Qt::Key_Up: keyText += "Up"; break;
            case Qt::Key_Down: keyText += "Down"; break;
            case Qt::Key_Left: keyText += "Left"; break;
            case Qt::Key_Right: keyText += "Right"; break;
            default:
                if (event->text().length() == 1) {
                    keyText += event->text().toUpper();
                }
                break;
        }
    }
    
    return keyText;
}

// 槽函数实现
void MainWindow::onSaveButtonClicked()
{
    m_settings->openAIApiKey = m_apiKeyLineEdit->text();
    m_settings->azureSpeechKey = m_speechKeyLineEdit->text();
    m_settings->azureSpeechRegion = m_speechRegionLineEdit->text();
    m_settings->hotKey = m_hotkeyLineEdit->text();
    m_settings->modelName = m_modelComboBox->currentText();
    m_settings->defaultProcessingMode = m_processingModeComboBox->currentText();
    m_settings->recordingMode = m_recordingModeComboBox->currentIndex() == 0 ? "HoldToRecord" : "ToggleRecord";
    m_settings->autoStart = m_autoStartCheckBox->isChecked();
    
    ConfigManager::saveSettings(*m_settings);
    
    // 重新注册快捷键
    m_hotkeyService->unregisterHotkey();
    registerHotkey();
    
    // 重新初始化API
    initializeAPIs();
    
    QMessageBox::information(this, "提示", "设置已保存");
}

void MainWindow::onTestButtonClicked()
{
    if (m_settings->azureSpeechKey.isEmpty()) {
        QMessageBox::warning(this, "警告", "请先设置Azure Speech Key");
        return;
    }
    
    if (!m_speechService->isRecording()) {
        m_testButton->setText("停止录音");
        showRecordingOverlay();
        m_speechService->startContinuousRecognition();
    } else {
        m_testButton->setText("测试录音");
        hideRecordingOverlay();
        m_speechService->stopContinuousRecognition();
    }
}

void MainWindow::onHotkeyButtonClicked()
{
    QMessageBox::information(this, "设置快捷键", 
        "请在快捷键文本框中按下您想要设置的快捷键组合。\n\n"
        "支持的修饰键：Ctrl、Alt、Shift\n"
        "支持的主键：字母、数字、功能键等");
    m_hotkeyLineEdit->setFocus();
}

void MainWindow::onTrayIconActivated(QSystemTrayIcon::ActivationReason reason)
{
    if (reason == QSystemTrayIcon::DoubleClick) {
        onShowMainWindow();
    }
}

void MainWindow::onShowMainWindow()
{
    show();
    setWindowState(Qt::WindowActive);
    raise();
    activateWindow();
}

void MainWindow::onExitApplication()
{
    if (m_trayIcon) {
        m_trayIcon->hide();
    }
    QApplication::quit();
}

void MainWindow::onHotkeyPressed()
{
    if (m_settings->azureSpeechKey.isEmpty()) {
        updateStatus("请先设置Azure Speech Key", QColor("red"));
        return;
    }
    
    if (m_settings->recordingMode == "ToggleRecord") {
        // 切换录音模式
        if (!m_speechService->isRecording()) {
            m_isToggleRecording = true;
            showRecordingOverlay();
            m_speechService->initialize(m_settings->azureSpeechKey, m_settings->azureSpeechRegion);
            m_speechService->startContinuousRecognition();
        } else {
            m_isToggleRecording = false;
            hideRecordingOverlay();
            m_speechService->stopContinuousRecognition();
        }
    } else {
        // 按住录音模式
        if (!m_speechService->isRecording()) {
            showRecordingOverlay();
            m_speechService->initialize(m_settings->azureSpeechKey, m_settings->azureSpeechRegion);
            m_speechService->startContinuousRecognition();
        }
    }
}

void MainWindow::onHotkeyReleased()
{
    // 只有在按住录音模式下才处理释放事件
    if (m_settings->recordingMode == "HoldToRecord" && m_speechService->isRecording()) {
        hideRecordingOverlay();
        m_speechService->stopContinuousRecognition();
    }
}

void MainWindow::onRecognitionStarted()
{
    updateStatus("开始语音识别...", QColor("orange"));
    addLog("语音识别已开始");
}

void MainWindow::onRecognitionCompleted(const QString& text)
{
    updateStatus("识别完成，正在处理...", QColor("blue"));
    addLog(QString("识别结果: %1").arg(text));
    
    hideRecordingOverlay();
    
    // 使用ChatGPT处理文本
    if (!m_settings->openAIApiKey.isEmpty()) {
        ChatGptPromptType promptType = ChatGptPromptType::TranslateToEnglish;
        QString processingMode = m_settings->defaultProcessingMode;
        
        if (processingMode == "TranslateToEnglishEmail") {
            promptType = ChatGptPromptType::TranslateToEnglishEmail;
        } else if (processingMode == "FormatAsEmail") {
            promptType = ChatGptPromptType::FormatAsEmail;
        } else if (processingMode == "Summarize") {
            promptType = ChatGptPromptType::Summarize;
        } else if (processingMode == "CustomPrompt") {
            promptType = ChatGptPromptType::CustomPrompt;
        }
        
        m_chatGptService->processTextAsync(text, promptType);
    } else {
        // 直接输出原始文本
        OutputSimulator::sendTextWithClipboard(text);
        updateStatus("文本已输出", QColor("green"));
        addLog("文本已输出到剪贴板");
    }
}

void MainWindow::onRecognitionFailed(const QString& error)
{
    updateStatus(QString("识别失败: %1").arg(error), QColor("red"));
    addLog(QString("识别失败: %1").arg(error));
    hideRecordingOverlay();
}

void MainWindow::onPartialResultReceived(const QString& text)
{
    if (m_recordingOverlay) {
        m_recordingOverlay->updateText(text);
    }
}

void MainWindow::onTextProcessed(const QString& result)
{
    OutputSimulator::sendTextWithClipboard(result);
    updateStatus("处理完成，文本已输出", QColor("green"));
    addLog(QString("处理结果: %1").arg(result));
}

void MainWindow::onProcessingFailed(const QString& error)
{
    updateStatus(QString("处理失败: %1").arg(error), QColor("red"));
    addLog(QString("处理失败: %1").arg(error));
}

void MainWindow::onExitButtonClicked()
{
    if (QMessageBox::question(this, "确认退出", "确定要退出程序吗？", 
                             QMessageBox::Yes | QMessageBox::No, 
                             QMessageBox::No) == QMessageBox::Yes) {
        onExitApplication();
    }
}

void MainWindow::onAutoStartCheckBoxToggled(bool checked)
{
    setAutoStart(checked);
    if (checked) {
        addLog("已启用开机自动启动");
    } else {
        addLog("已禁用开机自动启动");
    }
}

void MainWindow::setAutoStart(bool enabled)
{
#ifdef Q_OS_WIN
    QSettings settings("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Run", QSettings::NativeFormat);
    QString appName = "Speech2TextAssistant";
    
    if (enabled) {
        QString appPath = QApplication::applicationFilePath();
        appPath = QDir::toNativeSeparators(appPath);
        settings.setValue(appName, QString("\"%1\"").arg(appPath));
    } else {
        settings.remove(appName);
    }
#endif
}

bool MainWindow::isAutoStartEnabled()
{
#ifdef Q_OS_WIN
    QSettings settings("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Run", QSettings::NativeFormat);
    return settings.contains("Speech2TextAssistant");
#else
    return false;
#endif
}