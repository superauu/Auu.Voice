using System.ComponentModel;
using System.Text;
using Microsoft.Win32;
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
        Size = new Size(800, 700);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "语音转文字助手";
        BackColor = SystemColors.Control;

        InitializeServices();
        LoadSettings();
        SetupTrayIcon();
        
        // 确保窗口完全加载后再启用快捷键
        this.Load += MainForm_Load;
        this.Shown += MainForm_Shown;
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
    
    private void MainForm_Shown(object? sender, EventArgs e)
    {
        // 检查是否需要启动时最小化到托盘
        if (_settings?.MinimizeToTray == true)
        {
            Hide();
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
        // 主布局容器
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(15),
            BackColor = SystemColors.Control
        };
        
        // 设置行高
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // API设置
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 功能设置
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // 日志区域
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 按钮区域
        
        // API设置组
        var apiGroup = CreateApiSettingsGroup();
        mainLayout.Controls.Add(apiGroup, 0, 0);
        
        // 功能设置组
        var funcGroup = CreateFunctionSettingsGroup();
        mainLayout.Controls.Add(funcGroup, 0, 1);
        
        // 日志区域
        var logPanel = CreateLogPanel();
        mainLayout.Controls.Add(logPanel, 0, 2);
        
        // 按钮区域
        var buttonPanel = CreateButtonPanel();
        mainLayout.Controls.Add(buttonPanel, 0, 3);
        
        Controls.Add(mainLayout);
    }
    
    private GroupBox CreateApiSettingsGroup()
    {
        var group = new GroupBox
        {
            Text = "API 设置",
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            Font = SystemFonts.DefaultFont,
            AutoSize = true
        };
        
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            AutoSize = true
        };
        
        // 设置列宽
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        
        // 设置行高
        for (int i = 0; i < 4; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        
        // OpenAI API Key
        var apiKeyLabel = new Label { Text = "OpenAI API Key:", Anchor = AnchorStyles.Left, AutoSize = true, Margin = new Padding(0, 6, 10, 6) };
        _apiKeyTextBox = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true, Margin = new Padding(0, 3, 0, 3) };
        
        // GPT Model
        var modelLabel = new Label { Text = "GPT 模型:", Anchor = AnchorStyles.Left, AutoSize = true, Margin = new Padding(0, 6, 10, 6) };
        _modelCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 3, 0, 3) };
        _modelCombo.Items.AddRange("gpt-3.5-turbo", "gpt-4", "gpt-4-turbo", "gpt-4o");
        
        // Azure Speech Key
        var speechKeyLabel = new Label { Text = "Azure Speech Key:", Anchor = AnchorStyles.Left, AutoSize = true, Margin = new Padding(0, 6, 10, 6) };
        _speechKeyTextBox = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true, Margin = new Padding(0, 3, 0, 3) };
        
        // Region
        var speechRegionLabel = new Label { Text = "区域:", Anchor = AnchorStyles.Left, AutoSize = true, Margin = new Padding(0, 6, 10, 6) };
        _speechRegionTextBox = new TextBox { Dock = DockStyle.Fill, Text = "southeastasia", Margin = new Padding(0, 3, 0, 3) };
        
        // 添加控件到布局
        layout.Controls.Add(apiKeyLabel, 0, 0);
        layout.Controls.Add(_apiKeyTextBox, 1, 0);
        layout.Controls.Add(modelLabel, 0, 1);
        layout.Controls.Add(_modelCombo, 1, 1);
        layout.Controls.Add(speechKeyLabel, 0, 2);
        layout.Controls.Add(_speechKeyTextBox, 1, 2);
        layout.Controls.Add(speechRegionLabel, 0, 3);
        layout.Controls.Add(_speechRegionTextBox, 1, 3);
        
        group.Controls.Add(layout);
        return group;
    }
    
    private GroupBox CreateFunctionSettingsGroup()
    {
        var group = new GroupBox
        {
            Text = "功能设置",
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            Font = SystemFonts.DefaultFont,
            AutoSize = true
        };
        
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            AutoSize = true
        };
        
        // 设置列宽
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        
        // 设置行高
        for (int i = 0; i < 5; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        
        // 快捷键设置
        var hotkeyLabel = new Label { Text = "快捷键:", Anchor = AnchorStyles.Left, AutoSize = true, Margin = new Padding(0, 6, 10, 6) };
        _hotkeyTextBox = new TextBox { Dock = DockStyle.Fill, ReadOnly = false, Margin = new Padding(0, 3, 0, 3) };
        _hotkeyTextBox.KeyDown += HotkeyTextBox_KeyDown;
        _hotkeyTextBox.Enter += (s, e) => _hotkeyTextBox.Text = "按下要设置的快捷键...";
        _hotkeyTextBox.Leave += (s, e) =>
        {
            if (_hotkeyTextBox.Text == "按下要设置的快捷键...") _hotkeyTextBox.Text = _settings?.HotKey ?? "Ctrl+Alt+M";
        };
        
        // 处理模式
        var modeLabel = new Label { Text = "处理模式:", Anchor = AnchorStyles.Left, AutoSize = true, Margin = new Padding(0, 6, 10, 6) };
        _processingModeCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 3, 0, 3) };
        _processingModeCombo.Items.AddRange("TranslateToEnglishEmail", "TranslateToEnglish", "FormatAsEmail", "Summarize", "CustomPrompt");
        
        // 录音模式
        var recordingModeLabel = new Label { Text = "录音模式:", Anchor = AnchorStyles.Left, AutoSize = true, Margin = new Padding(0, 6, 10, 6) };
        _recordingModeCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 3, 0, 3) };
        _recordingModeCombo.Items.AddRange("按住录音 (Hold to Record)", "切换录音 (Toggle Record)");
        
        // 系统设置区域
        var systemLabel = new Label { Text = "系统设置:", Anchor = AnchorStyles.Left, AutoSize = true, Margin = new Padding(0, 6, 10, 6) };
        var systemPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = true,
            Margin = new Padding(0, 3, 0, 3)
        };
        
        // 开机自启动
        var startupCheckBox = new CheckBox
        {
            Text = "开机自动启动",
            AutoSize = true,
            Margin = new Padding(0, 0, 15, 0)
        };
        startupCheckBox.CheckedChanged += StartupCheckBox_CheckedChanged;
        
        // 最小化到托盘
        var minimizeToTrayCheckBox = new CheckBox
        {
            Text = "启动时最小化到托盘",
            AutoSize = true,
            Margin = new Padding(0, 0, 15, 0)
        };
        minimizeToTrayCheckBox.CheckedChanged += MinimizeToTrayCheckBox_CheckedChanged;
        
        systemPanel.Controls.AddRange(new Control[] { startupCheckBox, minimizeToTrayCheckBox });
        
        // 测试按钮
        var testLabel = new Label { Text = "测试:", Anchor = AnchorStyles.Left, AutoSize = true, Margin = new Padding(0, 6, 10, 6) };
        _testButton = new Button 
        { 
            Text = "测试录音", 
            Size = new Size(100, 30),
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 3, 0, 3)
        };
        _testButton.Click += TestButton_Click;
        
        // 添加控件到布局
        layout.Controls.Add(hotkeyLabel, 0, 0);
        layout.Controls.Add(_hotkeyTextBox, 1, 0);
        layout.Controls.Add(modeLabel, 0, 1);
        layout.Controls.Add(_processingModeCombo, 1, 1);
        layout.Controls.Add(recordingModeLabel, 0, 2);
        layout.Controls.Add(_recordingModeCombo, 1, 2);
        layout.Controls.Add(systemLabel, 0, 3);
        layout.Controls.Add(systemPanel, 1, 3);
        layout.Controls.Add(testLabel, 0, 4);
        layout.Controls.Add(_testButton, 1, 4);
        
        group.Controls.Add(layout);
        
        // 保存复选框引用以便后续使用
        group.Tag = new { StartupCheckBox = startupCheckBox, MinimizeToTrayCheckBox = minimizeToTrayCheckBox };
        
        return group;
    }
    

    
    private Panel CreateLogPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        
        // 状态标签
        _statusLabel = new Label
        {
            Text = "状态: 就绪",
            Dock = DockStyle.Top,
            Height = 25,
            ForeColor = SystemColors.ControlText,
            Font = SystemFonts.DefaultFont,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(5)
        };
        
        // 日志标签
        var logLabel = new Label 
        { 
            Text = "处理日志:", 
            Dock = DockStyle.Top, 
            Height = 20,
            Font = SystemFonts.DefaultFont,
            ForeColor = SystemColors.ControlText,
            TextAlign = ContentAlignment.BottomLeft,
            Padding = new Padding(5, 5, 5, 2)
        };
        
        // 日志列表
        _logListBox = new ListBox 
        { 
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 8.5F),
            BackColor = SystemColors.Window,
            BorderStyle = BorderStyle.Fixed3D
        };
        
        panel.Controls.Add(_logListBox);
        panel.Controls.Add(logLabel);
        panel.Controls.Add(_statusLabel);
        
        return panel;
    }
    
    private Panel CreateButtonPanel()
    {
        var panel = new Panel 
        { 
            Dock = DockStyle.Fill, 
            Height = 45,
            Padding = new Padding(0, 8, 0, 0)
        };
        
        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };
        
        // 退出按钮
        var exitButton = new Button 
        { 
            Text = "退出", 
            Size = new Size(80, 30),
            Font = SystemFonts.DefaultFont,
            Margin = new Padding(5, 0, 0, 0)
        };
        exitButton.Click += (s, e) => Application.Exit();
        
        // 保存设置按钮
        var saveButton = new Button 
        { 
            Text = "保存设置", 
            Size = new Size(80, 30),
            Font = SystemFonts.DefaultFont,
            Margin = new Padding(5, 0, 0, 0)
        };
        saveButton.Click += SaveButton_Click;
        
        // 最小化到托盘按钮
        var minimizeButton = new Button 
        { 
            Text = "最小化到托盘", 
            Size = new Size(100, 30),
            Font = SystemFonts.DefaultFont,
            Margin = new Padding(5, 0, 0, 0)
        };
        minimizeButton.Click += (s, e) => { Hide(); };
        
        layout.Controls.AddRange(new Control[] { exitButton, saveButton, minimizeButton });
        panel.Controls.Add(layout);
        
        return panel;
    }

    // 系统设置复选框事件处理
    private void StartupCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (sender is CheckBox checkBox && _settings != null)
        {
            _settings.StartWithWindows = checkBox.Checked;
            SetStartupRegistry(checkBox.Checked);
        }
    }
    
    private void MinimizeToTrayCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (sender is CheckBox checkBox && _settings != null)
        {
            _settings.MinimizeToTray = checkBox.Checked;
            ConfigManager.SaveSettings(_settings); // 保存设置到文件
            AddLog($"启动时最小化到托盘: {(checkBox.Checked ? "已启用" : "已禁用")}");
        }
    }
    

    
    // 设置开机自启动
    private void SetStartupRegistry(bool enable)
    {
        try
        {
            const string keyName = "Speech2TextAssistant";
            using var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            
            if (enable)
            {
                var exePath = Application.ExecutablePath;
                key?.SetValue(keyName, $"\"{exePath}\"");
                AddLog("已设置开机自动启动");
            }
            else
            {
                key?.DeleteValue(keyName, false);
                AddLog("已取消开机自动启动");
            }
        }
        catch (Exception ex)
        {
            AddLog($"设置开机自启动失败: {ex.Message}");
            UpdateStatus($"设置开机自启动失败: {ex.Message}", Color.Red);
        }
    }
    
    // 检查是否已设置开机自启动
    private bool IsStartupEnabled()
    {
        try
        {
            const string keyName = "Speech2TextAssistant";
            using var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
            return key?.GetValue(keyName) != null;
        }
        catch
        {
            return false;
        }
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
            _recordingOverlay?.ClearRecognizedText(); // 清除识别文字
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
        _recordingOverlay?.UpdateRecognizedText(partialText);
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
            
            // 保存系统设置（复选框状态已在事件处理中更新）
            ConfigManager.SaveSettings(_settings);
        }

        // 重新注册快捷键
        _hotkeyService?.UnregisterHotkey();
        RegisterHotkey();

        // 重新初始化API
        InitializeAPIs();

        MessageBox.Show("设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        UpdateStatus("设置已保存", Color.FromArgb(40, 167, 69));
        AddLog("配置设置已保存");
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
        _speechRegionTextBox.Text = _settings.AzureSpeechRegion ?? "southeastasia";
        _hotkeyTextBox.Text = _settings.HotKey ?? "Ctrl+Alt+M";
        _modelCombo.Text = _settings.ModelName ?? "gpt-3.5-turbo";
        _processingModeCombo.Text = _settings.DefaultProcessingMode ?? "TranslateToEnglish";
        _recordingModeCombo.SelectedIndex = _settings.RecordingMode == "ToggleRecord" ? 1 : 0;
        
        // 加载系统设置到复选框
        LoadSystemSettings();

        RegisterHotkey();
        InitializeAPIs();
    }
    
    private void LoadSystemSettings()
    {
        // 查找功能设置组中的复选框
        var funcGroup = Controls.OfType<TableLayoutPanel>().FirstOrDefault()?.Controls.OfType<GroupBox>()
            .FirstOrDefault(g => g.Text == "功能设置");
        
        if (funcGroup?.Tag is { } tag)
        {
            var properties = tag.GetType().GetProperties();
            var startupCheckBox = properties.FirstOrDefault(p => p.Name == "StartupCheckBox")?.GetValue(tag) as CheckBox;
            var minimizeToTrayCheckBox = properties.FirstOrDefault(p => p.Name == "MinimizeToTrayCheckBox")?.GetValue(tag) as CheckBox;
            
            if (startupCheckBox != null)
            {
                startupCheckBox.Checked = _settings?.StartWithWindows == true || IsStartupEnabled();
            }
            
            if (minimizeToTrayCheckBox != null)
            {
                minimizeToTrayCheckBox.Checked = _settings?.MinimizeToTray == true;
            }
        }
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
            return;
        }

        _hotkeyService?.UnregisterHotkey();
        if (_speechService?.IsRecording == true) _speechService.StopRecognition();
        _recordingOverlay?.Close();
        _notifyIcon?.Dispose();
        base.OnFormClosing(e);
    }
}