#include "SpeechRecognizerService.h"
#include <QJsonDocument>
#include <QJsonObject>
#include <QJsonArray>
#include <QNetworkRequest>
#include <QAudioDevice>
#include <QMediaDevices>
#include <QDebug>

SpeechRecognizerService::SpeechRecognizerService(QObject* parent)
    : QObject(parent)
    , m_isRecording(false)
    , m_networkManager(new QNetworkAccessManager(this))
    , m_timeoutTimer(new QTimer(this))
{
    m_timeoutTimer->setSingleShot(true);
    connect(m_timeoutTimer, &QTimer::timeout, this, &SpeechRecognizerService::onRecognitionTimeout);
}

SpeechRecognizerService::~SpeechRecognizerService()
{
    cleanup();
}

void SpeechRecognizerService::initialize(const QString& subscriptionKey, const QString& region)
{
    m_subscriptionKey = subscriptionKey;
    m_region = region;
}

void SpeechRecognizerService::startContinuousRecognition()
{
    if (m_isRecording) {
        return;
    }
    
    if (m_subscriptionKey.isEmpty()) {
        emit recognitionFailed("语音服务未初始化，请先设置Azure Speech Key");
        return;
    }
    
    try {
        setupAudioInput();
        m_isRecording = true;
        m_recognizedText.clear();
        
        emit recognitionStarted();
        
        // 开始录音
        m_audioSource->start(m_audioBuffer.get());
        
    } catch (const std::exception& e) {
        m_isRecording = false;
        emit recognitionFailed(QString("开始录音失败: %1").arg(e.what()));
    }
}

void SpeechRecognizerService::stopContinuousRecognition()
{
    if (!m_isRecording) {
        return;
    }
    
    try {
        // 停止录音
        if (m_audioSource) {
            m_audioSource->stop();
        }
        
        // 发送音频数据到Azure进行识别
        sendAudioToAzure();
        
    } catch (const std::exception& e) {
        emit recognitionFailed(QString("停止录音失败: %1").arg(e.what()));
    }
    
    m_isRecording = false;
}

void SpeechRecognizerService::startRecognition(int timeoutSeconds)
{
    startContinuousRecognition();
    
    // 设置超时
    m_timeoutTimer->start(timeoutSeconds * 1000);
}

void SpeechRecognizerService::stopRecognition()
{
    stopContinuousRecognition();
}

void SpeechRecognizerService::setupAudioInput()
{
    // 设置音频格式
    QAudioFormat format;
    format.setSampleRate(16000);
    format.setChannelCount(1);
    format.setSampleFormat(QAudioFormat::Int16);
    
    // 获取默认音频输入设备
    QAudioDevice audioDevice = QMediaDevices::defaultAudioInput();
    
    if (audioDevice.isNull()) {
        throw std::runtime_error("未找到音频输入设备");
    }
    
    // 创建音频源
    m_audioSource = std::make_unique<QAudioSource>(format, this);
    m_audioBuffer = std::make_unique<QBuffer>();
    m_audioBuffer->open(QIODevice::WriteOnly);
    
    connect(m_audioSource.get(), &QAudioSource::stateChanged, this, &SpeechRecognizerService::onAudioDataReady);
}

void SpeechRecognizerService::sendAudioToAzure()
{
    if (m_audioBuffer) {
        m_audioData = m_audioBuffer->data();
        m_audioBuffer->close();
    }
    
    if (m_audioData.isEmpty()) {
        emit recognitionFailed("未录制到音频数据");
        return;
    }
    
    // 构建Azure Speech API请求
    QString url = QString("https://%1.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1")
                  .arg(m_region);
    url += "?language=zh-CN&format=detailed";
    
    QNetworkRequest request(url);
    request.setHeader(QNetworkRequest::ContentTypeHeader, "audio/wav");
    request.setRawHeader("Ocp-Apim-Subscription-Key", m_subscriptionKey.toUtf8());
    
    QNetworkReply* reply = m_networkManager->post(request, m_audioData);
    connect(reply, &QNetworkReply::finished, this, &SpeechRecognizerService::onNetworkReplyFinished);
}

void SpeechRecognizerService::onAudioDataReady()
{
    // 处理音频数据就绪事件
    if (m_isRecording) {
        // 可以在这里处理实时音频数据
        emit partialResultReceived("正在录音...");
    }
}

void SpeechRecognizerService::onRecognitionTimeout()
{
    if (m_isRecording) {
        stopContinuousRecognition();
    }
}

void SpeechRecognizerService::onNetworkReplyFinished()
{
    QNetworkReply* reply = qobject_cast<QNetworkReply*>(sender());
    if (!reply) {
        return;
    }
    
    if (reply->error() == QNetworkReply::NoError) {
        QByteArray response = reply->readAll();
        processRecognitionResponse(response);
    } else {
        emit recognitionFailed(QString("网络请求失败: %1").arg(reply->errorString()));
    }
    
    reply->deleteLater();
    cleanup();
}

void SpeechRecognizerService::processRecognitionResponse(const QByteArray& response)
{
    try {
        QJsonDocument doc = QJsonDocument::fromJson(response);
        QJsonObject obj = doc.object();
        
        QString recognitionStatus = obj["RecognitionStatus"].toString();
        
        if (recognitionStatus == "Success") {
            QString displayText = obj["DisplayText"].toString();
            if (!displayText.isEmpty()) {
                emit recognitionCompleted(displayText.trimmed());
            } else {
                emit recognitionFailed("未识别到语音内容");
            }
        } else {
            emit recognitionFailed(QString("识别失败: %1").arg(recognitionStatus));
        }
    } catch (const std::exception& e) {
        emit recognitionFailed(QString("解析识别结果失败: %1").arg(e.what()));
    }
}

void SpeechRecognizerService::cleanup()
{
    if (m_audioSource) {
        m_audioSource->stop();
        m_audioSource.reset();
    }
    
    if (m_audioBuffer) {
        m_audioBuffer->close();
        m_audioBuffer.reset();
    }
    
    m_audioData.clear();
    m_timeoutTimer->stop();
}