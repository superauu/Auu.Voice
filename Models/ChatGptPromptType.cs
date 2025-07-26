namespace Speech2TextAssistant.Models;

public enum ChatGptPromptType
{
    TranslateToEnglish,
    FormatAsEmail,
    Summarize,
    CustomPrompt
}

public static class PromptTemplates
{
    public static string GetSystemPrompt(ChatGptPromptType type)
    {
        return type switch
        {
            ChatGptPromptType.TranslateToEnglish =>
                "You are a professional translator. Translate the following text to English. Keep the meaning accurate and natural.",
            ChatGptPromptType.FormatAsEmail =>
                "You are an email writing assistant. Format the following content into a professional email format with proper greeting, body, and closing.",
            ChatGptPromptType.Summarize =>
                "You are a text summarization expert. Provide a concise summary of the following content.",
            ChatGptPromptType.CustomPrompt =>
                "You are a helpful assistant. Process the following text according to user requirements.",
            _ => "You are a helpful assistant."
        };
    }
}