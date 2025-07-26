namespace Speech2TextAssistant.Models;

public class AppSettings
{
    public string HotKey { get; set; } = "Ctrl+Alt+M";
    public string OpenAIApiKey { get; set; } = "";
    public string DefaultProcessingMode { get; set; } = "TranslateToEnglish";
    public string ModelName { get; set; } = "gpt-3.5-turbo";
    public string AzureSpeechKey { get; set; } = "";
    public string AzureSpeechRegion { get; set; } = "";
    public bool PlaySounds { get; set; } = true;
    public int RecordingTimeoutSeconds { get; set; } = 30;
    public string RecordingMode { get; set; } = "HoldToRecord"; // HoldToRecord 或 ToggleRecord
    public bool StartWithWindows { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool ShowTrayNotifications { get; set; } = false;
}