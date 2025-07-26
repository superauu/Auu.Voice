#include "ConfigManager.h"
#include <QJsonDocument>
#include <QJsonObject>
#include <QFile>
#include <QMessageBox>
#include <QCryptographicHash>
#include <QByteArray>

AppSettings ConfigManager::loadSettings()
{
    AppSettings settings;
    
    try {
        QString configPath = getConfigPath();
        QFile file(configPath);
        
        if (file.exists() && file.open(QIODevice::ReadOnly)) {
            QByteArray data = file.readAll();
            QJsonDocument doc = QJsonDocument::fromJson(data);
            QJsonObject obj = doc.object();
            
            settings.hotKey = obj["hotKey"].toString("Ctrl+Alt+M");
            settings.openAIApiKey = decryptString(obj["openAIApiKey"].toString());
            settings.defaultProcessingMode = obj["defaultProcessingMode"].toString("TranslateToEnglish");
            settings.modelName = obj["modelName"].toString("gpt-3.5-turbo");
            settings.azureSpeechKey = decryptString(obj["azureSpeechKey"].toString());
            settings.azureSpeechRegion = obj["azureSpeechRegion"].toString();
            settings.playSounds = obj["playSounds"].toBool(true);
            settings.recordingTimeoutSeconds = obj["recordingTimeoutSeconds"].toInt(30);
            settings.recordingMode = obj["recordingMode"].toString("HoldToRecord");
            settings.autoStart = obj["autoStart"].toBool(false);
            
            file.close();
        }
    } catch (const std::exception& e) {
        QMessageBox::critical(nullptr, "错误", QString("加载配置失败: %1").arg(e.what()));
    }
    
    return settings;
}

void ConfigManager::saveSettings(const AppSettings& settings)
{
    try {
        QString configPath = getConfigPath();
        QDir configDir = QFileInfo(configPath).dir();
        
        if (!configDir.exists()) {
            configDir.mkpath(".");
        }
        
        QJsonObject obj;
        obj["hotKey"] = settings.hotKey;
        obj["openAIApiKey"] = settings.openAIApiKey.isEmpty() ? "" : encryptString(settings.openAIApiKey);
        obj["defaultProcessingMode"] = settings.defaultProcessingMode;
        obj["modelName"] = settings.modelName;
        obj["azureSpeechKey"] = settings.azureSpeechKey.isEmpty() ? "" : encryptString(settings.azureSpeechKey);
        obj["azureSpeechRegion"] = settings.azureSpeechRegion;
        obj["playSounds"] = settings.playSounds;
        obj["recordingTimeoutSeconds"] = settings.recordingTimeoutSeconds;
        obj["recordingMode"] = settings.recordingMode;
        obj["autoStart"] = settings.autoStart;
        
        QJsonDocument doc(obj);
        
        QFile file(configPath);
        if (file.open(QIODevice::WriteOnly)) {
            file.write(doc.toJson());
            file.close();
        }
    } catch (const std::exception& e) {
        QMessageBox::critical(nullptr, "错误", QString("保存配置失败: %1").arg(e.what()));
    }
}

QString ConfigManager::getConfigPath()
{
    QString appDataPath = QStandardPaths::writableLocation(QStandardPaths::AppDataLocation);
    return QDir(appDataPath).filePath("config.json");
}

QString ConfigManager::encryptString(const QString& plainText)
{
    // 简单的Base64编码作为加密（实际项目中应使用更强的加密）
    QByteArray data = plainText.toUtf8();
    return data.toBase64();
}

QString ConfigManager::decryptString(const QString& encryptedText)
{
    // 简单的Base64解码作为解密
    try {
        QByteArray data = QByteArray::fromBase64(encryptedText.toUtf8());
        return QString::fromUtf8(data);
    } catch (...) {
        return encryptedText; // 如果解密失败，返回原文
    }
}