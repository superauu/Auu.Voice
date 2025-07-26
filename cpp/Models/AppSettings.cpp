#include "AppSettings.h"

AppSettings::AppSettings()
{
    // 使用默认值初始化
}

void AppSettings::reset()
{
    hotKey = "Ctrl+Alt+M";
    openAIApiKey = "";
    defaultProcessingMode = "TranslateToEnglish";
    modelName = "gpt-3.5-turbo";
    azureSpeechKey = "";
    azureSpeechRegion = "";
    playSounds = true;
    recordingTimeoutSeconds = 30;
    recordingMode = "HoldToRecord";
}

bool AppSettings::isValid() const
{
    return !hotKey.isEmpty() && 
           !openAIApiKey.isEmpty() && 
           !azureSpeechKey.isEmpty() &&
           !azureSpeechRegion.isEmpty();
}