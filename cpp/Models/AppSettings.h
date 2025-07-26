#ifndef APPSETTINGS_H
#define APPSETTINGS_H

#include <QString>

class AppSettings
{
public:
    AppSettings();
    
    // Properties
    QString hotKey = "Ctrl+Alt+M";
    QString openAIApiKey = "";
    QString defaultProcessingMode = "TranslateToEnglish";
    QString modelName = "gpt-3.5-turbo";
    QString azureSpeechKey = "";
    QString azureSpeechRegion = "";
    bool playSounds = true;
    int recordingTimeoutSeconds = 30;
    QString recordingMode = "HoldToRecord"; // HoldToRecord 或 ToggleRecord
    
    // Methods
    void reset();
    bool isValid() const;
};

#endif // APPSETTINGS_H