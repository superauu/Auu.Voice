#include "ChatGptService.h"
#include <QJsonDocument>
#include <QJsonObject>
#include <QJsonArray>
#include <QNetworkRequest>
#include <QDebug>

ChatGptService::ChatGptService(QObject* parent)
    : QObject(parent)
    , m_networkManager(new QNetworkAccessManager(this))
{
    // 设置超时时间
    m_networkManager->setTransferTimeout(30000); // 30秒
}

ChatGptService::~ChatGptService()
{
}

void ChatGptService::initialize(const QString& apiKey, const QString& model)
{
    m_apiKey = apiKey;
    m_model = model;
}

void ChatGptService::processTextAsync(const QString& text, ChatGptPromptType promptType, const QString& customPrompt)
{
    try {
        QString systemPrompt;
        if (promptType == ChatGptPromptType::CustomPrompt && !customPrompt.isEmpty()) {
            systemPrompt = customPrompt;
        } else {
            systemPrompt = PromptTemplates::getSystemPrompt(promptType);
        }
        
        // 构建请求JSON
        QJsonObject requestBody;
        requestBody["model"] = m_model;
        requestBody["max_tokens"] = 1000;
        requestBody["temperature"] = 0.7;
        
        QJsonArray messages;
        
        QJsonObject systemMessage;
        systemMessage["role"] = "system";
        systemMessage["content"] = systemPrompt;
        messages.append(systemMessage);
        
        QJsonObject userMessage;
        userMessage["role"] = "user";
        userMessage["content"] = text;
        messages.append(userMessage);
        
        requestBody["messages"] = messages;
        
        QJsonDocument doc(requestBody);
        QByteArray jsonData = doc.toJson();
        
        // 构建网络请求
        QNetworkRequest request(QUrl("https://api.openai.com/v1/chat/completions"));
        request.setHeader(QNetworkRequest::ContentTypeHeader, "application/json");
        request.setRawHeader("Authorization", QString("Bearer %1").arg(m_apiKey).toUtf8());
        
        QNetworkReply* reply = m_networkManager->post(request, jsonData);
        connect(reply, &QNetworkReply::finished, this, &ChatGptService::onNetworkReplyFinished);
        
    } catch (const std::exception& e) {
        emit processingFailed(QString("ChatGPT处理失败: %1").arg(e.what()));
    }
}

void ChatGptService::onNetworkReplyFinished()
{
    QNetworkReply* reply = qobject_cast<QNetworkReply*>(sender());
    if (!reply) {
        return;
    }
    
    if (reply->error() == QNetworkReply::NoError) {
        QByteArray response = reply->readAll();
        
        try {
            QJsonDocument doc = QJsonDocument::fromJson(response);
            QJsonObject obj = doc.object();
            
            QJsonArray choices = obj["choices"].toArray();
            if (!choices.isEmpty()) {
                QJsonObject firstChoice = choices[0].toObject();
                QJsonObject message = firstChoice["message"].toObject();
                QString content = message["content"].toString();
                
                emit textProcessed(content.trimmed());
            } else {
                emit processingFailed("API返回的响应格式不正确");
            }
        } catch (const std::exception& e) {
            emit processingFailed(QString("解析API响应失败: %1").arg(e.what()));
        }
    } else {
        QString errorContent = reply->readAll();
        emit processingFailed(QString("API调用失败: %1 - %2")
                             .arg(reply->attribute(QNetworkRequest::HttpStatusCodeAttribute).toInt())
                             .arg(errorContent));
    }
    
    reply->deleteLater();
}

// 实现PromptTemplates类
QString PromptTemplates::getSystemPrompt(ChatGptPromptType type)
{
    switch (type) {
        case ChatGptPromptType::TranslateToEnglish:
            return "You are a professional translator. Translate the following text to English. Keep the meaning accurate and natural.";
            
        case ChatGptPromptType::TranslateToEnglishEmail:
            return "You are a professional translator. Translate the following text into formal, professional English. Remove informal or filler words. Do not add anything beyond the given content—no titles, sign-offs, or extra information.";
            
        case ChatGptPromptType::FormatAsEmail:
            return "You are an email writing assistant. Format the following content into a professional email format with proper greeting, body, and closing.";
            
        case ChatGptPromptType::Summarize:
            return "You are a text summarization expert. Provide a concise summary of the following content.";
            
        case ChatGptPromptType::CustomPrompt:
            return "You are a helpful assistant. Process the following text according to user requirements.";
            
        default:
            return "You are a helpful assistant.";
    }
}