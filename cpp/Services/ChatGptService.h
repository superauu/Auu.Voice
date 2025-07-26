#ifndef CHATGPTSERVICE_H
#define CHATGPTSERVICE_H

#include <QObject>
#include <QString>
#include <QNetworkAccessManager>
#include <QNetworkReply>
#include "ChatGptPromptType.h"

class ChatGptService : public QObject
{
    Q_OBJECT
    
public:
    explicit ChatGptService(QObject* parent = nullptr);
    ~ChatGptService();
    
    void initialize(const QString& apiKey, const QString& model = "gpt-3.5-turbo");
    void processTextAsync(const QString& text, ChatGptPromptType promptType, const QString& customPrompt = "");
    
signals:
    void textProcessed(const QString& result);
    void processingFailed(const QString& error);
    
private slots:
    void onNetworkReplyFinished();
    
private:
    QString m_apiKey;
    QString m_model;
    QNetworkAccessManager* m_networkManager;
};

#endif // CHATGPTSERVICE_H