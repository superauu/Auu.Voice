#ifndef CONFIGMANAGER_H
#define CONFIGMANAGER_H

#include <QString>
#include <QStandardPaths>
#include <QDir>
#include "AppSettings.h"

class ConfigManager
{
public:
    static AppSettings loadSettings();
    static void saveSettings(const AppSettings& settings);
    
private:
    static QString getConfigPath();
    static QString encryptString(const QString& plainText);
    static QString decryptString(const QString& encryptedText);
};

#endif // CONFIGMANAGER_H