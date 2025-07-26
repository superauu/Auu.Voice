using System.Text;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace Speech2TextAssistant;

public class SpeechRecognizerService
{
    private AudioConfig? _audioConfig;
    private readonly StringBuilder _recognizedText = new();
    private SpeechRecognizer? _recognizer;
    private SpeechConfig? _speechConfig;

    public bool IsRecording { get; private set; }

    public event EventHandler<string>? RecognitionCompleted;
    public event EventHandler<string>? RecognitionFailed;
    public event EventHandler? RecognitionStarted;
    public event EventHandler<string>? PartialResultReceived;

    public void Initialize(string subscriptionKey, string region)
    {
        _speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);
        _speechConfig.SpeechRecognitionLanguage = "zh-CN"; // 默认中文，可配置
    }

    public async Task StartContinuousRecognitionAsync()
    {
        if (IsRecording) return;
        
        if (_speechConfig == null)
        {
            RecognitionFailed?.Invoke(this, "语音服务未初始化，请先设置Azure Speech Key");
            return;
        }

        try
        {
            _audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            _recognizer = new SpeechRecognizer(_speechConfig, _audioConfig);

            // 清空之前的识别结果
            _recognizedText.Clear();

            // 设置事件处理器
            _recognizer.Recognizing += OnRecognizing;
            _recognizer.Recognized += OnRecognized;
            _recognizer.Canceled += OnCanceled;
            _recognizer.SessionStopped += OnSessionStopped;

            IsRecording = true;
            RecognitionStarted?.Invoke(this, EventArgs.Empty);

            // 开始连续识别
            await _recognizer.StartContinuousRecognitionAsync();
        }
        catch (Exception ex)
        {
            IsRecording = false;
            RecognitionFailed?.Invoke(this, $"开始录音失败: {ex.Message}");
        }
    }

    public async Task StopContinuousRecognitionAsync()
    {
        if (!IsRecording || _recognizer == null) return;

        try
        {
            await _recognizer.StopContinuousRecognitionAsync();

            // 等待一小段时间确保最后的识别结果被处理
            await Task.Delay(500);

            var finalText = _recognizedText.ToString().Trim();
            if (!string.IsNullOrEmpty(finalText))
                RecognitionCompleted?.Invoke(this, finalText);
            else
                RecognitionFailed?.Invoke(this, "未识别到语音内容");
        }
        catch (Exception ex)
        {
            RecognitionFailed?.Invoke(this, $"停止录音失败: {ex.Message}");
        }
        finally
        {
            IsRecording = false;
            CleanupRecognizer();
        }
    }

    private void OnRecognizing(object? sender, SpeechRecognitionEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Result.Text)) PartialResultReceived?.Invoke(this, e.Result.Text);
    }

    private void OnRecognized(object? sender, SpeechRecognitionEventArgs e)
    {
        if (e.Result.Reason == ResultReason.RecognizedSpeech && !string.IsNullOrEmpty(e.Result.Text))
            _recognizedText.AppendLine(e.Result.Text);
    }

    private void OnCanceled(object? sender, SpeechRecognitionCanceledEventArgs e)
    {
        IsRecording = false;
        if (e.Reason == CancellationReason.Error) RecognitionFailed?.Invoke(this, $"识别被取消: {e.ErrorDetails}");
    }

    private void OnSessionStopped(object? sender, SessionEventArgs e)
    {
        IsRecording = false;
    }

    private void CleanupRecognizer()
    {
        if (_recognizer != null)
        {
            _recognizer.Recognizing -= OnRecognizing;
            _recognizer.Recognized -= OnRecognized;
            _recognizer.Canceled -= OnCanceled;
            _recognizer.SessionStopped -= OnSessionStopped;

            _recognizer.Dispose();
            _recognizer = null;
        }

        _audioConfig?.Dispose();
        _audioConfig = null;
    }

    // 保留原有方法以向后兼容
    public async Task StartRecognitionAsync(int timeoutSeconds = 30)
    {
        await StartContinuousRecognitionAsync();

        // 自动超时停止
        _ = Task.Delay(timeoutSeconds * 1000).ContinueWith(async _ =>
        {
            if (IsRecording) await StopContinuousRecognitionAsync();
        });
    }

    public void StopRecognition()
    {
        _ = StopContinuousRecognitionAsync();
    }
}