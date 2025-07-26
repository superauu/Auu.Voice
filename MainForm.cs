using System.ComponentModel;
using System.Text;
using Speech2TextAssistant.Models;

namespace Speech2TextAssistant;

public class MainForm : Form
{
    // UI Controls
    private TextBox _apiKeyTextBox = null!;
    private ChatGptService _chatGptService = null!;
    private HotkeyService _hotkeyService = null!;
    private TextBox _hotkeyTextBox = null!;
    private ListBox _logListBox = null!;
    private ComboBox _modelCombo = null!;
    private NotifyIcon? _notifyIcon;
    private ComboBox _processingModeCombo = null!;
    private ComboBox _recordingModeCombo = null!;
    private bool _isToggleRecording = false; // 用于跟踪切换模式下的录音状态
    private RecordingOverlay? _recordingOverlay;
    private AppSettings? _settings;
    private TextBox _speechKeyTextBox = null!;
    private TextBox _speechRegionTextBox = null!;
    private SpeechRecognizerService _speechService = null!;
    private Label _statusLabel = null!;
    private Button _testButton = null!;

    public MainForm()
    {
        InitializeComponent();
        CreateControls();
        Size = new Size(600, 560);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "语音转文字助手";

        InitializeServices();
        LoadSettings();
        SetupTrayIcon();
        
        // 确保窗口完全加载后再启用快捷键
        this.Load += MainForm_Load;
    }
    
    private void MainForm_Load(object? sender, EventArgs e)
    {
        // 窗口加载完成后，确保所有服务都已正确初始化
        if (!string.IsNullOrEmpty(_settings?.HotKey))
        {
            _hotkeyService?.UnregisterHotkey();
            _hotkeyService?.RegisterHotkey(Handle, _settings.HotKey);
        }
    }

    /// <summary>
    ///     Required method for Designer support - do not modify
    ///     the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        var resources = new ComponentResourceManager(typeof(MainForm));
        SuspendLayout();
        // 
        // MainForm
        // 
        ClientSize = new Size(434, 561);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        Icon = (Icon?)resources.GetObject("$this.Icon");
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Speech2Text Assistant";
        ResumeLayout(false);
    }

    private void CreateControls()
    {
        // API设置组
        var apiGroup = new GroupBox { Text = "API 设置", Location = new Point(10, 10), Size = new Size(560, 120) };

        var apiKeyLabel = new Label
            { Text = "OpenAI API Key:", Location = new Point(10, 25), Size = new Size(100, 20) };
        _apiKeyTextBox = new TextBox
            { Location = new Point(120, 23), Size = new Size(420, 20), UseSystemPasswordChar = true };

        var speechKeyLabel = new Label
            { Text = "Azure Speech Key:", Location = new Point(10, 55), Size = new Size(100, 20) };
        _speechKeyTextBox = new TextBox
            { Location = new Point(120, 53), Size = new Size(200, 20), UseSystemPasswordChar = true };

        var speechRegionLabel = new Label { Text = "Region:", Location = new Point(330, 55), Size = new Size(50, 20) };
        _speechRegionTextBox = new TextBox { Location = new Point(380, 53), Size = new Size(100, 20), Text = "eastus" };

        var modelLabel = new Label { Text = "GPT Model:", Location = new Point(10, 85), Size = new Size(100, 20) };
        _modelCombo = new ComboBox
        {
            Location = new Point(120, 83),
            Size = new Size(150, 20),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _modelCombo.Items.AddRange("gpt-3.5-turbo", "gpt-4", "gpt-4-turbo", "gpt-4o");

        apiGroup.Controls.AddRange(apiKeyLabel, _apiKeyTextBox, speechKeyLabel, _speechKeyTextBox, speechRegionLabel,
            _speechRegionTextBox, modelLabel, _modelCombo);

        // 功能设置组
        var funcGroup = new GroupBox { Text = "功能设置", Location = new Point(10, 140), Size = new Size(560, 110) };

        var hotkeyLabel = new Label { Text = "快捷键:", Location = new Point(10, 25), Size = new Size(60, 20) };
        _hotkeyTextBox = new TextBox { Location = new Point(80, 23), Size = new Size(120, 20), ReadOnly = false };
        _hotkeyTextBox.KeyDown += HotkeyTextBox_KeyDown;
        _hotkeyTextBox.Enter += (s, e) => _hotkeyTextBox.Text = "按下要设置的快捷键...";
        _hotkeyTextBox.Leave += (s, e) =>
        {
            if (_hotkeyTextBox.Text == "按下要设置的快捷键...") _hotkeyTextBox.Text = _settings?.HotKey ?? "F2";
        };

        var hotkeyButton = new Button { Text = "设置", Location = new Point(205, 22), Size = new Size(50, 22) };
        hotkeyButton.Click += HotkeyButton_Click;

        var modeLabel = new Label { Text = "处理模式:", Location = new Point(265, 25), Size = new Size(70, 20) };
        _processingModeCombo = new ComboBox
        {
            Location = new Point(335, 23),
            Size = new Size(170, 20),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _processingModeCombo.Items.AddRange("TranslateToEnglishEmail","TranslateToEnglish", "FormatAsEmail", "Summarize", "CustomPrompt");

        var recordingModeLabel = new Label { Text = "录音模式:", Location = new Point(10, 55), Size = new Size(70, 20) };
        _recordingModeCombo = new ComboBox
        {
            Location = new Point(80, 53),
            Size = new Size(120, 20),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _recordingModeCombo.Items.AddRange("按住录音", "切换录音");

        _testButton = new Button { Text = "测试录音", Location = new Point(10, 80), Size = new Size(80, 25) };
        _testButton.Click += TestButton_Click;

        funcGroup.Controls.AddRange(hotkeyLabel, _hotkeyTextBox, hotkeyButton, modeLabel, _processingModeCombo,
            recordingModeLabel, _recordingModeCombo, _testButton);

        // 状态显示
        _statusLabel = new Label
        {
            Text = "状态: 就绪",
            Location = new Point(10, 260),
            Size = new Size(560, 20),
            ForeColor = Color.Green
        };

        // 日志显示
        var logLabel = new Label { Text = "处理日志:", Location = new Point(10, 290), Size = new Size(100, 20) };
        _logListBox = new ListBox { Location = new Point(10, 315), Size = new Size(560, 150) };

        // 按钮区域
        var buttonPanel = new Panel { Location = new Point(10, 475), Size = new Size(560, 40) };

        var saveButton = new Button { Text = "保存设置", Location = new Point(280, 5), Size = new Size(80, 30) };
        saveButton.Click += SaveButton_Click;

        var exitButton = new Button { Text = "退出", Location = new Point(370, 5), Size = new Size(80, 30) };
        exitButton.Click += (s, e) => Application.Exit();

        var minimizeButton = new Button { Text = "最小化到托盘", Location = new Point(160, 5), Size = new Size(110, 30) };
        minimizeButton.Click += (s, e) => { Hide(); };

        buttonPanel.Controls.AddRange(minimizeButton, saveButton, exitButton);

        Controls.AddRange(apiGroup, funcGroup, _statusLabel, logLabel, _logListBox, buttonPanel);
    }

    private void LayoutControls()
    {
        // 控件布局已在CreateControls中完成
    }

    // 在 InitializeServices 方法中
    private void InitializeServices()
    {
        _hotkeyService = new HotkeyService();
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        _hotkeyService.HotkeyReleased += OnHotkeyReleased; // 新增松开事件

        _speechService = new SpeechRecognizerService();
        _speechService.RecognitionStarted += OnRecognitionStarted;
        _speechService.RecognitionCompleted += OnRecognitionCompleted;
        _speechService.RecognitionFailed += OnRecognitionFailed;
        _speechService.PartialResultReceived += OnPartialResultReceived; // 新增实时结果事件

        _chatGptService = new ChatGptService();
    }

    // 修改按键按下事件处理
    private async void OnHotkeyPressed(object? sender, EventArgs e)
    {
        try
        {
            // 检查窗口是否已完全加载
            if (!this.IsHandleCreated || this.IsDisposed)
            {
                return;
            }
            
            // 检查设置是否已加载
            if (_settings == null)
            {
                UpdateStatus("设置未加载，请稍后再试", Color.Red);
                return;
            }
            
            // 检查语音服务是否已初始化
            if (string.IsNullOrEmpty(_settings.AzureSpeechKey))
            {
                UpdateStatus("请先设置Azure Speech Key", Color.Red);
                return;
            }
            
            // 确保语音服务已初始化
            if (_speechService == null)
            {
                UpdateStatus("语音服务未初始化", Color.Red);
                return;
            }
            
            // 根据录音模式处理
            if (_settings.RecordingMode == "ToggleRecord")
            {
                // 切换模式：按一次开始，再按一次停止
                if (!_isToggleRecording && _speechService?.IsRecording != true)
                {
                    // 开始录音
                    _isToggleRecording = true;
                    _speechService?.Initialize(_settings.AzureSpeechKey, _settings.AzureSpeechRegion ?? "eastus");
                    UpdateStatus("开始录音...", Color.Orange);
                    ShowRecordingOverlay();
                    await (_speechService?.StartContinuousRecognitionAsync() ?? Task.CompletedTask);
                }
                else if (_isToggleRecording && _speechService?.IsRecording == true)
                {
                    // 停止录音
                    _isToggleRecording = false;
                    UpdateStatus("录音结束，正在处理...", Color.Blue);
                    HideRecordingOverlay();
                    await (_speechService?.StopContinuousRecognitionAsync() ?? Task.CompletedTask);
                }
            }
            else
            {
                // 按住模式：只在按下时开始录音
                if (_speechService?.IsRecording == true)
                {
                    return;
                }
                
                _speechService?.Initialize(_settings.AzureSpeechKey, _settings.AzureSpeechRegion ?? "eastus");
                UpdateStatus("开始录音...", Color.Orange);
                ShowRecordingOverlay();
                await (_speechService?.StartContinuousRecognitionAsync() ?? Task.CompletedTask);
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"启动录音失败: {ex.Message}", Color.Red);
            AddLog($"快捷键录音错误: {ex.Message}");
            _isToggleRecording = false;
        }
    }

    // 新增按键松开事件处理
    private async void OnHotkeyReleased(object? sender, EventArgs e)
    {
        try
        {
            // 检查窗口是否已完全加载
            if (!this.IsHandleCreated || this.IsDisposed)
            {
                return;
            }
            
            // 只在按住模式下响应松开事件
            if (_settings?.RecordingMode != "HoldToRecord")
            {
                return;
            }
            
            // 检查语音服务状态
            if (_speechService?.IsRecording == true)
            {
                UpdateStatus("录音结束，正在处理...", Color.Blue);
                HideRecordingOverlay();
                await (_speechService?.StopContinuousRecognitionAsync() ?? Task.CompletedTask);
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"停止录音失败: {ex.Message}", Color.Red);
            AddLog($"快捷键停止录音错误: {ex.Message}");
            HideRecordingOverlay(); // 确保覆盖层被隐藏
        }
    }

    private void ShowRecordingOverlay()
    {
        if (InvokeRequired)
        {
            Invoke(ShowRecordingOverlay);
            return;
        }
        
        try
        {
            if (_recordingOverlay == null || _recordingOverlay.IsDisposed) 
            {
                _recordingOverlay = new RecordingOverlay();
            }
            _recordingOverlay.Show();
        }
        catch (Exception ex)
        {
            UpdateStatus($"显示录音窗口失败: {ex.Message}", Color.Red);
            AddLog($"显示录音窗口错误: {ex.Message}");
        }
    }

    private void HideRecordingOverlay()
    {
        if (InvokeRequired)
        {
            Invoke(HideRecordingOverlay);
            return;
        }
        
        try
        {
            _recordingOverlay?.Hide();
        }
        catch (Exception ex)
        {
            AddLog($"隐藏录音窗口错误: {ex.Message}");
        }
    }

    // 新增实时识别结果显示
    private void OnPartialResultReceived(object? sender, string partialText)
    {
        Invoke(() => { UpdateStatus($"识别中: {partialText}", Color.Orange); });
        _recordingOverlay?.UpdateText(partialText);
    }

    // 修改识别完成事件处理
    private async void OnRecognitionCompleted(object? sender, string recognizedText)
    {
        if (string.IsNullOrWhiteSpace(recognizedText)) return;

        Invoke(() =>
        {
            UpdateStatus("正在处理文本...", Color.Blue);
            AddLog($"识别结果: {recognizedText}");
        });

        await ProcessRecognizedTextAsync(recognizedText);
    }

    private async Task ProcessRecognizedTextAsync(string recognizedText)
    {
        try
        {
            var promptType = (ChatGptPromptType)Enum.Parse(typeof(ChatGptPromptType), _settings?.DefaultProcessingMode ?? "TranslateToEnglish");
            var processedText = _chatGptService != null ? await _chatGptService.ProcessTextAsync(recognizedText, promptType) : recognizedText;

            Invoke(() =>
            {
                AddLog($"处理结果: {processedText}");
                UpdateStatus("正在输出文本...", Color.Blue);
            });

            // 输出到当前光标位置
            await OutputSimulator.SendTextWithClipboardAsync(processedText);

            Invoke(() => UpdateStatus("处理完成", Color.Green));
        }
        catch (Exception ex)
        {
            Invoke(() =>
            {
                UpdateStatus($"处理失败: {ex.Message}", Color.Red);
                AddLog($"处理错误: {ex.Message}");
            });
        }
    }

    // 修改测试按钮事件
    private async void TestButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_speechKeyTextBox.Text))
        {
            MessageBox.Show("请先设置Azure Speech Key", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_speechService?.IsRecording != true)
        {
            _testButton.Text = "停止录音";
            ShowRecordingOverlay();
            await (_speechService?.StartContinuousRecognitionAsync() ?? Task.CompletedTask);
        }
        else
        {
            _testButton.Text = "测试录音";
            HideRecordingOverlay();
            await (_speechService?.StopContinuousRecognitionAsync() ?? Task.CompletedTask);
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        if (_settings != null)
        {
            _settings.OpenAIApiKey = _apiKeyTextBox.Text;
            _settings.AzureSpeechKey = _speechKeyTextBox.Text;
            _settings.AzureSpeechRegion = _speechRegionTextBox.Text;
            _settings.HotKey = _hotkeyTextBox.Text;
            _settings.ModelName = _modelCombo.Text;
            _settings.DefaultProcessingMode = _processingModeCombo.Text;
            _settings.RecordingMode = _recordingModeCombo.SelectedIndex == 0 ? "HoldToRecord" : "ToggleRecord";
        }

        if (_settings != null)
            ConfigManager.SaveSettings(_settings);

        // 重新注册快捷键
        _hotkeyService?.UnregisterHotkey();
        RegisterHotkey();

        // 重新初始化API
        InitializeAPIs();

        MessageBox.Show("设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void SetupTrayIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = this.Icon,
            Text = "语音转文字助手",
            Visible = true
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("显示主窗口", null, (s, e) =>
        {
            Show();
            WindowState = FormWindowState.Normal;
        });
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("退出", null, (s, e) =>
        {
            if (_notifyIcon != null)
                _notifyIcon.Visible = false;
            Application.Exit();
        });

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) =>
        {
            Show();
            WindowState = FormWindowState.Normal;
        };
    }

    protected override void WndProc(ref Message m)
    {
        if (_hotkeyService?.ProcessHotkey(m) == true) return;
        base.WndProc(ref m);
    }

    protected override void SetVisibleCore(bool value)
    {
        base.SetVisibleCore(value);
        if (value && WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
    }

    private void LoadSettings()
    {
        _settings = ConfigManager.LoadSettings();

        _apiKeyTextBox.Text = _settings.OpenAIApiKey ?? "";
        _speechKeyTextBox.Text = _settings.AzureSpeechKey ?? "";
        _speechRegionTextBox.Text = _settings.AzureSpeechRegion ?? "eastus";
        _hotkeyTextBox.Text = _settings.HotKey ?? "F2";
        _modelCombo.Text = _settings.ModelName ?? "gpt-3.5-turbo";
        _processingModeCombo.Text = _settings.DefaultProcessingMode ?? "TranslateToEnglish";
        _recordingModeCombo.SelectedIndex = _settings.RecordingMode == "ToggleRecord" ? 1 : 0;

        RegisterHotkey();
        InitializeAPIs();
    }

    private void RegisterHotkey()
    {
        if (!string.IsNullOrEmpty(_settings?.HotKey)) _hotkeyService?.RegisterHotkey(Handle, _settings.HotKey);
    }

    private void InitializeAPIs()
    {
        if (!string.IsNullOrEmpty(_settings?.AzureSpeechKey))
            _speechService?.Initialize(_settings.AzureSpeechKey, _settings.AzureSpeechRegion ?? "eastus");

        if (!string.IsNullOrEmpty(_settings?.OpenAIApiKey))
            _chatGptService?.Initialize(_settings.OpenAIApiKey, _settings.ModelName ?? "gpt-3.5-turbo");
    }

    private void UpdateStatus(string message, Color color)
    {
        if (_statusLabel?.InvokeRequired == true)
        {
            _statusLabel.Invoke(() =>
            {
                _statusLabel.Text = $"状态: {message}";
                _statusLabel.ForeColor = color;
            });
        }
        else if (_statusLabel != null)
        {
            _statusLabel.Text = $"状态: {message}";
            _statusLabel.ForeColor = color;
        }
    }

    private void AddLog(string message)
    {
        if (_logListBox?.InvokeRequired == true)
        {
            _logListBox.Invoke(() =>
            {
                _logListBox.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
                if (_logListBox.Items.Count > 100) _logListBox.Items.RemoveAt(_logListBox.Items.Count - 1);
            });
        }
        else if (_logListBox != null)
        {
            _logListBox.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
            if (_logListBox.Items.Count > 100) _logListBox.Items.RemoveAt(_logListBox.Items.Count - 1);
        }
    }

    private void OnRecognitionStarted(object? sender, EventArgs e)
    {
        Invoke(() =>
        {
            UpdateStatus("开始语音识别...", Color.Orange);
            AddLog("语音识别已开始");
        });
    }

    private void OnRecognitionFailed(object? sender, string error)
    {
        Invoke(() =>
        {
            UpdateStatus($"识别失败: {error}", Color.Red);
            AddLog($"识别失败: {error}");
        });
    }

    private void HotkeyTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        e.Handled = true;
        e.SuppressKeyPress = true;

        var keyText = GetKeyText(e);
        if (!string.IsNullOrEmpty(keyText)) _hotkeyTextBox.Text = keyText;
    }

    private void HotkeyButton_Click(object? sender, EventArgs e)
    {
        MessageBox.Show("请在快捷键文本框中按下您想要设置的快捷键组合。\n\n支持的修饰键：Ctrl、Alt、Shift\n支持的主键：字母、数字、功能键等", "设置快捷键",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
        _hotkeyTextBox?.Focus();
    }

    private string GetKeyText(KeyEventArgs e)
    {
        var keyText = new StringBuilder();

        if (e.Control) keyText.Append("Ctrl+");
        if (e.Alt) keyText.Append("Alt+");
        if (e.Shift) keyText.Append("Shift+");

        // 获取主键
        var key = e.KeyCode;
        if (key >= Keys.A && key <= Keys.Z)
            keyText.Append(key.ToString());
        else if (key >= Keys.F1 && key <= Keys.F12)
            keyText.Append(key.ToString());
        else if (key >= Keys.D0 && key <= Keys.D9)
            keyText.Append(key.ToString().Replace("D", ""));
        else
            switch (key)
            {
                case Keys.Space:
                    keyText.Append("Space");
                    break;
                case Keys.Enter:
                    keyText.Append("Enter");
                    break;
                case Keys.Tab:
                    keyText.Append("Tab");
                    break;
                case Keys.Escape:
                    keyText.Append("Esc");
                    break;
                case Keys.Back:
                    keyText.Append("Backspace");
                    break;
                case Keys.Delete:
                    keyText.Append("Delete");
                    break;
                case Keys.Insert:
                    keyText.Append("Insert");
                    break;
                case Keys.Home:
                    keyText.Append("Home");
                    break;
                case Keys.End:
                    keyText.Append("End");
                    break;
                case Keys.PageUp:
                    keyText.Append("PageUp");
                    break;
                case Keys.PageDown:
                    keyText.Append("PageDown");
                    break;
                case Keys.Up:
                    keyText.Append("Up");
                    break;
                case Keys.Down:
                    keyText.Append("Down");
                    break;
                case Keys.Left:
                    keyText.Append("Left");
                    break;
                case Keys.Right:
                    keyText.Append("Right");
                    break;
                default:
                    if (key != Keys.Control && key != Keys.Alt && key != Keys.Shift) keyText.Append(key.ToString());
                    break;
            }

        return keyText.ToString();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            _notifyIcon?.ShowBalloonTip(2000, "语音转文字助手", "程序已最小化到系统托盘", ToolTipIcon.Info);
            return;
        }

        _hotkeyService?.UnregisterHotkey();
        if (_speechService?.IsRecording == true) _speechService.StopRecognition();
        _recordingOverlay?.Close();
        _notifyIcon?.Dispose();
        base.OnFormClosing(e);
    }
}