namespace Speech2TextAssistant.Models;

public class ProcessingMode
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string SystemPrompt { get; set; } = "";
    public bool IsBuiltIn { get; set; } = false;
    public bool IsDefault { get; set; } = false;

    public ProcessingMode() { }

    public ProcessingMode(string name, string displayName, string systemPrompt, bool isBuiltIn = false)
    {
        Name = name;
        DisplayName = displayName;
        SystemPrompt = systemPrompt;
        IsBuiltIn = isBuiltIn;
    }

    public static List<ProcessingMode> GetDefaultModes()
    {
        return new List<ProcessingMode>
        {
            new ProcessingMode(
                "TranslateToEnglish",
                "翻译为英文",
                "You are a professional translator. Translate the following text to English. Keep the meaning accurate and natural.",
                true),
            new ProcessingMode(
                "TranslateToEnglishEmail",
                "翻译为英文邮件",
                "You are a professional translator. Translate the following email content to English. Keep the meaning accurate and natural. Since the content I provided is quite colloquial, please remove any informal expressions such as filler words (e.g., \"um,\" \"ah\") and make the email more formal and professional. No need to add extra information, just process the text I provide.",
                true),
            new ProcessingMode(
                "FormatAsEmail",
                "格式化为邮件",
                "You are an email writing assistant. Format the following content into a professional email format with proper greeting, body, and closing.",
                true),
            new ProcessingMode(
                "Summarize",
                "总结文本",
                "You are a text summarization expert. Provide a concise summary of the following content.",
                true),
            new ProcessingMode(
                "CustomPrompt",
                "自定义处理",
                "You are a helpful assistant. Process the following text according to user requirements.",
                true)
        };
    }
}