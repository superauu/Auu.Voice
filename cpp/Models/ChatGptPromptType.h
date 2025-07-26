#ifndef CHATGPTPROMPTTYPE_H
#define CHATGPTPROMPTTYPE_H

#include <QString>

enum class ChatGptPromptType
{
    TranslateToEnglish,
    TranslateToEnglishEmail,
    FormatAsEmail,
    Summarize,
    CustomPrompt
};

class PromptTemplates
{
public:
    static QString getSystemPrompt(ChatGptPromptType type);
};

#endif // CHATGPTPROMPTTYPE_H