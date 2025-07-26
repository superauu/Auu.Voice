#ifndef SPEECHRECOGNIZERSERVICE_H
#define SPEECHRECOGNIZERSERVICE_H

#include <QObject>
#include <QString>
#include <QTimer>
#include <QNetworkAccessManager>
#include <QNetworkReply>
#include <QAudioSource>
#include <QAudioFormat>
#include <QBuffer>
#include <QIODevice>
#include <memory>

class SpeechRecognizerService : public QObject
{
    Q_OBJECT
    
public:
    explicit SpeechRecognizerService(QObject* parent = nullptr);
    ~SpeechRecognizerService();
    
    void initialize(const QString& subscriptionKey, const QString& region);
    bool isRecording() const { return m_isRecording; }
    
public slots:
    void startContinuousRecognition();
    void stopContinuousRecognition();
    void startRecognition(int timeoutSeconds = 30);
    void stopRecognition();
    
signals:
    void recognitionStarted();
    void recognitionCompleted(const QString& text);
    void recognitionFailed(const QString& error);
    void partialResultReceived(const QString& text);
    
private slots:
    void onAudioDataReady();
    void onRecognitionTimeout();
    void onNetworkReplyFinished();
    
private:
    void setupAudioInput();
    void sendAudioToAzure();
    void processRecognitionResponse(const QByteArray& response);
    void cleanup();
    
    QString m_subscriptionKey;
    QString m_region;
    bool m_isRecording;
    
    std::unique_ptr<QAudioSource> m_audioSource;
    std::unique_ptr<QBuffer> m_audioBuffer;
    QByteArray m_audioData;
    
    QNetworkAccessManager* m_networkManager;
    QTimer* m_timeoutTimer;
    
    QString m_recognizedText;
};

#endif // SPEECHRECOGNIZERSERVICE_H