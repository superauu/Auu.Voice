namespace Speech2TextAssistant.Models;

public class AppSettings
{
    public string HotKey { get; set; } = "Ctrl+Alt+M";
    public string TextInputHotKey { get; set; } = "Ctrl+Alt+T";
    public string OpenAIApiKey { get; set; } = "";
    public string DefaultProcessingMode { get; set; } = "TranslateToEnglishEmail";
    public string ModelName { get; set; } = "gpt-3.5-turbo";
    public string AzureSpeechKey { get; set; } = "";
    public string AzureSpeechRegion { get; set; } = "";
    public bool PlaySounds { get; set; } = true;
    public int RecordingTimeoutSeconds { get; set; } = 30;
    public string RecordingMode { get; set; } = "HoldToRecord"; // HoldToRecord 或 ToggleRecord
    public bool StartWithWindows { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool ShowTrayNotifications { get; set; } = false;
    public List<ProcessingMode> ProcessingModes { get; set; } = new List<ProcessingMode>();
    public string CustomPrompt { get; set; } = "";

    public AppSettings()
    {
        // 初始化默认处理模式
        if (ProcessingModes.Count == 0)
        {
            ProcessingModes = ProcessingMode.GetDefaultModes();
        }
    }

    public ProcessingMode? GetProcessingMode(string name)
    {
        return ProcessingModes.FirstOrDefault(m => m.Name == name);
    }

    public ProcessingMode GetDefaultProcessingModeObject()
    {
        var mode = GetProcessingMode(DefaultProcessingMode);
        return mode ?? ProcessingModes.FirstOrDefault() ?? ProcessingMode.GetDefaultModes().First();
    }
}