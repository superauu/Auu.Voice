using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Speech2TextAssistant.Models;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using Hardcodet.Wpf.TaskbarNotification;
using DrawingColor = System.Drawing.Color;
using MediaColor = System.Windows.Media.Color;

namespace Speech2TextAssistant
{
    public partial class MainWindow : Window
    {
        // Services
        private ChatGptService _chatGptService = null!;
        private HotkeyService _hotkeyService = null!;
        private bool _isToggleRecording = false;
        private RecordingOverlayWpf? _recordingOverlay;
        private AppSettings? _settings;
        private SpeechRecognizerService _speechService = null!;
        private TaskbarIcon? _notifyIcon;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                AddLog("InitializeComponent 完成");
                
                InitializeServices();
                AddLog("InitializeServices 完成");
                
                LoadSettings();
                AddLog("LoadSettings 完成");
                
                SetupTrayIcon();
                AddLog("SetupTrayIcon 完成");
                
                // 确保窗口完全加载后再启用快捷键
                this.Loaded += MainWindow_Loaded;
                this.StateChanged += MainWindow_StateChanged;
                
                AddLog("MainWindow 构造函数完成");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"MainWindow 初始化失败: {ex.Message}\n\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }
        
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 窗口加载完成后，确保所有服务都已正确初始化
            if (!string.IsNullOrEmpty(_settings?.HotKey))
            {
                _hotkeyService?.UnregisterHotkey();
                var windowInteropHelper = new System.Windows.Interop.WindowInteropHelper(this);
                _hotkeyService?.RegisterHotkey(windowInteropHelper.Handle, _settings.HotKey);
                _hotkeyService?.RegisterTextInputHotkey(windowInteropHelper.Handle, _settings.TextInputHotKey ?? "Ctrl+Alt+T");
                UpdateStatus("监听已启动，按快捷键开始录音或文本输入", Colors.Green);
                AddLog("快捷键监听已自动启动");
            }
            
            // 检查是否需要启动时最小化到托盘
            if (_settings?.MinimizeToTray == true)
            {
                this.WindowState = WindowState.Minimized;
                this.Hide();
            }
        }
        
        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void InitializeServices()
        {
            _hotkeyService = new HotkeyService();
            _hotkeyService.HotkeyPressed += OnHotkeyPressed;
            _hotkeyService.HotkeyReleased += OnHotkeyReleased;
            _hotkeyService.TextInputHotkeyPressed += OnTextInputHotkeyPressed;

            _speechService = new SpeechRecognizerService();
            _speechService.RecognitionStarted += OnRecognitionStarted;
            _speechService.RecognitionCompleted += OnRecognitionCompleted;
            _speechService.RecognitionFailed += OnRecognitionFailed;
            _speechService.PartialResultReceived += OnPartialResultReceived;

            _chatGptService = new ChatGptService();
        }

        private void LoadSettings()
        {
            _settings = ConfigManager.LoadSettings();
            
            if (_settings != null)
            {
                // 设置API Key (PasswordBox需要特殊处理)
                ApiKeyPasswordBox.Password = _settings.OpenAIApiKey ?? "";
                
                // 设置模型
                for (int i = 0; i < ModelComboBox.Items.Count; i++)
                {
                    if (((ComboBoxItem)ModelComboBox.Items[i]).Content.ToString() == _settings.ModelName)
                    {
                        ModelComboBox.SelectedIndex = i;
                        break;
                    }
                }
                
                // 设置Speech Key
                SpeechKeyPasswordBox.Password = _settings.AzureSpeechKey ?? "";
                SpeechRegionTextBox.Text = _settings.AzureSpeechRegion ?? "southeastasia";
                
                // 设置快捷键
                HotkeyTextBox.Text = _settings.HotKey ?? "Ctrl+Alt+M";
                TextInputHotkeyTextBox.Text = _settings.TextInputHotKey ?? "Ctrl+Alt+T";
                
                // 加载处理模式
                LoadProcessingModes();
                
                // 设置录音模式
                var recordingModeText = _settings.RecordingMode == "HoldToRecord" ? "按住录音 (Hold to Record)" : "切换录音 (Toggle Record)";
                for (int i = 0; i < RecordingModeComboBox.Items.Count; i++)
                {
                    if (((ComboBoxItem)RecordingModeComboBox.Items[i]).Content.ToString() == recordingModeText)
                    {
                        RecordingModeComboBox.SelectedIndex = i;
                        break;
                    }
                }
                
                // 设置系统设置
                StartupCheckBox.IsChecked = IsStartupEnabled();
                MinimizeToTrayCheckBox.IsChecked = _settings.MinimizeToTray;
            }
        }

        private void SetupTrayIcon()
        {
            try
            {
                _notifyIcon = new TaskbarIcon
                {
                    ToolTipText = "语音转文字助手"
                };
                
                // 尝试加载图标文件
                try
                {
                    // 获取程序所在目录的绝对路径
                    var exeDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    var iconPath = System.IO.Path.Combine(exeDirectory ?? "", "app.ico");
                    
                    if (System.IO.File.Exists(iconPath))
                    {
                        _notifyIcon.Icon = new System.Drawing.Icon(iconPath);
                    }
                    else
                    {
                        // 使用默认系统图标
                        _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                    }
                }
                catch
                {
                    // 如果加载图标失败，使用默认图标
                    _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }
            
            var contextMenu = new ContextMenu();
            
            var showMenuItem = new MenuItem
            {
                Header = "显示主窗口"
            };
            showMenuItem.Click += (s, e) => {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
            };
            
            var exitMenuItem = new MenuItem
            {
                Header = "退出"
            };
            exitMenuItem.Click += (s, e) => Application.Current.Shutdown();
            
            contextMenu.Items.Add(showMenuItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(exitMenuItem);
            
            _notifyIcon.ContextMenu = contextMenu;
            _notifyIcon.TrayLeftMouseUp += (s, e) => {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
            };
            }
            catch (Exception ex)
            {
                // 如果托盘图标初始化失败，记录错误但不阻止程序启动
                AddLog($"托盘图标初始化失败: {ex.Message}");
            }
        }

        // 快捷键文本框事件
        private void HotkeyTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var keyText = GetKeyText(e.Key, Keyboard.Modifiers);
            if (!string.IsNullOrEmpty(keyText))
            {
                HotkeyTextBox.Text = keyText;
                e.Handled = true;
            }
        }
        
        private void HotkeyTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            HotkeyTextBox.Text = "按下要设置的快捷键...";
            // 暂停快捷键监听，避免在设置快捷键时触发
            _hotkeyService?.PauseListening();
        }
        
        private void HotkeyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (HotkeyTextBox.Text == "按下要设置的快捷键...")
            {
                HotkeyTextBox.Text = _settings?.HotKey ?? "Ctrl+Alt+M";
            }
            // 恢复快捷键监听
            _hotkeyService?.ResumeListening();
        }
        
        // 文本输入快捷键文本框事件
        private void TextInputHotkeyTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var keyText = GetKeyText(e.Key, Keyboard.Modifiers);
            if (!string.IsNullOrEmpty(keyText))
            {
                TextInputHotkeyTextBox.Text = keyText;
                e.Handled = true;
            }
        }
        
        private void TextInputHotkeyTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextInputHotkeyTextBox.Text = "按下要设置的快捷键...";
            // 暂停快捷键监听，避免在设置快捷键时触发
            _hotkeyService?.PauseListening();
        }
        
        private void TextInputHotkeyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (TextInputHotkeyTextBox.Text == "按下要设置的快捷键...")
            {
                TextInputHotkeyTextBox.Text = _settings?.TextInputHotKey ?? "Ctrl+Alt+T";
            }
            // 恢复快捷键监听
            _hotkeyService?.ResumeListening();
        }

        private string GetKeyText(Key key, ModifierKeys modifiers)
        {
            var result = new StringBuilder();
            
            if (modifiers.HasFlag(ModifierKeys.Control))
                result.Append("Ctrl+");
            if (modifiers.HasFlag(ModifierKeys.Alt))
                result.Append("Alt+");
            if (modifiers.HasFlag(ModifierKeys.Shift))
                result.Append("Shift+");
            if (modifiers.HasFlag(ModifierKeys.Windows))
                result.Append("Win+");
                
            // 处理数字键，将D1-D9转换为1-9
            var keyString = key.ToString();
            if (keyString.StartsWith("D") && keyString.Length == 2 && char.IsDigit(keyString[1]))
            {
                result.Append(keyString[1]); // 只取数字部分
            }
            else
            {
                result.Append(keyString);
            }
            
            return result.ToString();
        }

        // 系统设置复选框事件
        private void StartupCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_settings != null)
            {
                _settings.StartWithWindows = true;
                SetStartupRegistry(true);
            }
        }
        
        private void StartupCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_settings != null)
            {
                _settings.StartWithWindows = false;
                SetStartupRegistry(false);
            }
        }
        
        private void MinimizeToTrayCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_settings != null)
            {
                _settings.MinimizeToTray = true;
                ConfigManager.SaveSettings(_settings);
                AddLog("启动时最小化到托盘: 已启用");
            }
        }
        
        private void MinimizeToTrayCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_settings != null)
            {
                _settings.MinimizeToTray = false;
                ConfigManager.SaveSettings(_settings);
                AddLog("启动时最小化到托盘: 已禁用");
            }
        }

        // 按钮事件
        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_speechService?.IsRecording == true)
                {
                    UpdateStatus("录音结束，正在处理...", Colors.Blue);
                    HideRecordingOverlay();
                    await (_speechService?.StopContinuousRecognitionAsync() ?? Task.CompletedTask);
                    TestButton.Content = "测试录音";
                }
                else
                {
                    if (string.IsNullOrEmpty(SpeechKeyPasswordBox.Password))
                    {
                        MessageBox.Show("请先设置Azure Speech Key", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    _speechService?.Initialize(SpeechKeyPasswordBox.Password, SpeechRegionTextBox.Text);
                    UpdateStatus("开始测试录音...", Colors.Orange);
                    ShowRecordingOverlay();
                    await (_speechService?.StartContinuousRecognitionAsync() ?? Task.CompletedTask);
                    TestButton.Content = "停止录音";
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"测试录音失败: {ex.Message}", Colors.Red);
                AddLog($"测试录音错误: {ex.Message}");
                TestButton.Content = "测试录音";
            }
        }
        
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }
        
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
        
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void SaveSettings()
        {
            try
            {
                if (_settings == null) _settings = new AppSettings();
                
                _settings.OpenAIApiKey = ApiKeyPasswordBox.Password;
                _settings.ModelName = ((ComboBoxItem)ModelComboBox.SelectedItem)?.Content?.ToString() ?? "gpt-3.5-turbo";
                _settings.AzureSpeechKey = SpeechKeyPasswordBox.Password;
                _settings.AzureSpeechRegion = SpeechRegionTextBox.Text;
                _settings.HotKey = HotkeyTextBox.Text;
                _settings.TextInputHotKey = TextInputHotkeyTextBox.Text;
                _settings.DefaultProcessingMode = ((ComboBoxItem)ProcessingModeComboBox.SelectedItem)?.Tag?.ToString() ?? "TranslateToEnglishEmail";
                
                var recordingModeText = ((ComboBoxItem)RecordingModeComboBox.SelectedItem)?.Content?.ToString();
                _settings.RecordingMode = recordingModeText?.Contains("Hold") == true ? "HoldToRecord" : "ToggleRecord";
                
                _settings.StartWithWindows = StartupCheckBox.IsChecked == true;
                _settings.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked == true;
                
                ConfigManager.SaveSettings(_settings);
                
                // 重新注册快捷键
                _hotkeyService?.UnregisterHotkey();
                var windowInteropHelper = new System.Windows.Interop.WindowInteropHelper(this);
                _hotkeyService?.RegisterHotkey(windowInteropHelper.Handle, _settings.HotKey);
                _hotkeyService?.RegisterTextInputHotkey(windowInteropHelper.Handle, _settings.TextInputHotKey);
                
                UpdateStatus("设置已保存", Colors.Green);
                AddLog("设置已保存到配置文件");
            }
            catch (Exception ex)
            {
                UpdateStatus($"保存设置失败: {ex.Message}", Colors.Red);
                AddLog($"保存设置错误: {ex.Message}");
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
                    var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
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
                UpdateStatus($"设置开机自启动失败: {ex.Message}", Colors.Red);
            }
        }
        
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

        // 快捷键事件处理
        private async void OnHotkeyPressed(object? sender, EventArgs e)
        {
            try
            {
                if (_settings == null)
                {
                    UpdateStatus("设置未加载，请稍后再试", Colors.Red);
                    return;
                }
                
                if (string.IsNullOrEmpty(_settings.AzureSpeechKey))
                {
                    UpdateStatus("请先设置Azure Speech Key", Colors.Red);
                    return;
                }
                
                if (_speechService == null)
                {
                    UpdateStatus("语音服务未初始化", Colors.Red);
                    return;
                }
                
                if (_settings.RecordingMode == "ToggleRecord")
                {
                    if (!_isToggleRecording && _speechService?.IsRecording != true)
                    {
                        _isToggleRecording = true;
                        _speechService?.Initialize(_settings.AzureSpeechKey, _settings.AzureSpeechRegion ?? "eastus");
                        UpdateStatus("开始录音...", Colors.Orange);
                        ShowRecordingOverlay();
                        await (_speechService?.StartContinuousRecognitionAsync() ?? Task.CompletedTask);
                    }
                    else if (_isToggleRecording && _speechService?.IsRecording == true)
                    {
                        _isToggleRecording = false;
                        UpdateStatus("录音结束，正在处理...", Colors.Blue);
                        HideRecordingOverlay();
                        await (_speechService?.StopContinuousRecognitionAsync() ?? Task.CompletedTask);
                    }
                }
                else
                {
                    if (_speechService?.IsRecording == true)
                    {
                        return;
                    }
                    
                    _speechService?.Initialize(_settings.AzureSpeechKey, _settings.AzureSpeechRegion ?? "eastus");
                    UpdateStatus("开始录音...", Colors.Orange);
                    ShowRecordingOverlay();
                    await (_speechService?.StartContinuousRecognitionAsync() ?? Task.CompletedTask);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"启动录音失败: {ex.Message}", Colors.Red);
                AddLog($"快捷键录音错误: {ex.Message}");
                _isToggleRecording = false;
            }
        }

        private async void OnHotkeyReleased(object? sender, EventArgs e)
        {
            try
            {
                if (_settings?.RecordingMode != "HoldToRecord")
                {
                    return;
                }
                
                if (_speechService?.IsRecording == true)
                {
                    UpdateStatus("录音结束，正在处理...", Colors.Blue);
                    HideRecordingOverlay();
                    await (_speechService?.StopContinuousRecognitionAsync() ?? Task.CompletedTask);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"停止录音失败: {ex.Message}", Colors.Red);
                AddLog($"快捷键停止录音错误: {ex.Message}");
                HideRecordingOverlay();
            }
        }
        
        // 文本输入快捷键事件处理
        private void OnTextInputHotkeyPressed(object? sender, EventArgs e)
        {
            try
            {
                // 在显示输入窗口之前保存当前活动窗口
                var targetWindow = OutputSimulator.GetCurrentForegroundWindow();
                
                this.Dispatcher.Invoke(() =>
                {
                    var textInputWindow = new TextInputWindow();
                    var result = textInputWindow.ShowDialog();
                    
                    if (result == true && !string.IsNullOrWhiteSpace(textInputWindow.InputText))
                    {
                        // 异步处理文本输入，传递目标窗口句柄
                        _ = Task.Run(async () => await ProcessTextInputAsync(textInputWindow.InputText, targetWindow));
                    }
                });
            }
            catch (Exception ex)
            {
                UpdateStatus($"打开文本输入窗口失败: {ex.Message}", Colors.Red);
                AddLog($"文本输入快捷键错误: {ex.Message}");
            }
        }
        
        private async Task ProcessTextInputAsync(string inputText, IntPtr targetWindow = default)
        {
            try
            {
                this.Dispatcher.Invoke(() => UpdateStatus("正在处理文本...", Colors.Blue));
                
                if (_settings == null)
                {
                    this.Dispatcher.Invoke(() => UpdateStatus("设置未加载", Colors.Red));
                    return;
                }
                
                if (string.IsNullOrEmpty(_settings.OpenAIApiKey))
                {
                    this.Dispatcher.Invoke(() => UpdateStatus("请先设置OpenAI API Key", Colors.Red));
                    return;
                }
                
                // 获取当前选择的处理模式
                var processingMode = _settings.DefaultProcessingMode ?? "TranslateToEnglishEmail";
                var mode = _settings.GetProcessingMode(processingMode);
                
                if (mode == null)
                {
                    mode = _settings.GetDefaultProcessingModeObject();
                }
                
                // 使用ChatGPT处理文本
                _chatGptService?.Initialize(_settings.OpenAIApiKey, _settings.ModelName ?? "gpt-3.5-turbo");
                var processedText = await (_chatGptService?.ProcessTextAsync(inputText, mode) ?? Task.FromResult(inputText));
                
                if (!string.IsNullOrEmpty(processedText))
                {
                    // 发送处理后的文本到指定窗口或当前活动窗口
                    if (targetWindow != IntPtr.Zero)
                    {
                        await OutputSimulator.SendTextToSpecificWindowAsync(processedText, targetWindow);
                    }
                    else
                    {
                        await OutputSimulator.SendTextToActiveWindowAsync(processedText);
                    }
                    
                    this.Dispatcher.Invoke(() =>
                    {
                        UpdateStatus("文本处理完成", Colors.Green);
                        AddLog($"文本输入处理完成: {inputText} -> {processedText}");
                    });
                }
                else
                {
                    this.Dispatcher.Invoke(() => UpdateStatus("文本处理失败", Colors.Red));
                }
            }
            catch (Exception ex)
            {
                this.Dispatcher.Invoke(() =>
                {
                    UpdateStatus($"文本处理失败: {ex.Message}", Colors.Red);
                    AddLog($"文本处理错误: {ex.Message}");
                });
            }
        }

        // 录音覆盖层管理
        private void ShowRecordingOverlay()
        {
            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (_recordingOverlay == null || !_recordingOverlay.IsLoaded)
                    {
                        _recordingOverlay = new RecordingOverlayWpf();
                    }
                    _recordingOverlay.Show();
                }
                catch (Exception ex)
                {
                    AddLog($"显示录音覆盖层失败: {ex.Message}");
                }
            });
        }

        private void HideRecordingOverlay()
        {
            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    _recordingOverlay?.Hide();
                }
                catch (Exception ex)
                {
                    AddLog($"隐藏录音覆盖层失败: {ex.Message}");
                }
            });
        }

        // 处理模式管理
        private void LoadProcessingModes()
        {
            ProcessingModeComboBox.Items.Clear();
            
            foreach (var mode in _settings?.ProcessingModes ?? new List<ProcessingMode>())
            {
                ProcessingModeComboBox.Items.Add(new ComboBoxItem
                {
                    Content = mode.DisplayName,
                    Tag = mode.Name
                });
            }
            
            // 设置默认选中项
            for (int i = 0; i < ProcessingModeComboBox.Items.Count; i++)
            {
                var item = (ComboBoxItem)ProcessingModeComboBox.Items[i];
                if (item.Tag?.ToString() == _settings?.DefaultProcessingMode)
                {
                    ProcessingModeComboBox.SelectedIndex = i;
                    break;
                }
            }
        }
        
        private void ManageModesButton_Click(object sender, RoutedEventArgs e)
        {
            var managerWindow = new ProcessingModeManagerWindow(_settings?.ProcessingModes ?? new List<ProcessingMode>());
            managerWindow.SetDefaultMode(_settings?.DefaultProcessingMode ?? string.Empty);
            
            if (managerWindow.ShowDialog() == true)
            {
                // 更新设置中的处理模式列表
                if (_settings != null)
                {
                    _settings.ProcessingModes = managerWindow.GetProcessingModes() ?? new List<ProcessingMode>();
                    
                    // 更新默认模式
                    var defaultModeName = managerWindow.GetDefaultModeName();
                    if (!string.IsNullOrEmpty(defaultModeName))
                    {
                        _settings.DefaultProcessingMode = defaultModeName;
                    }
                }
                
                // 重新加载处理模式
                LoadProcessingModes();
                
                // 保存设置
                SaveSettings();
            }
        }
        
        private void ProcessingModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_settings != null && ProcessingModeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _settings.DefaultProcessingMode = selectedItem.Tag?.ToString() ?? "TranslateToEnglishEmail";
            }
        }

        // 语音识别事件处理
        private void OnRecognitionStarted(object? sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                UpdateStatus("正在录音...", Colors.Orange);
                AddLog("开始语音识别");
            });
        }

        private void OnRecognitionCompleted(object? sender, string recognizedText)
        {
            this.Dispatcher.Invoke(async () =>
            {
                try
                {
                    HideRecordingOverlay();
                    
                    if (string.IsNullOrWhiteSpace(recognizedText))
                    {
                        UpdateStatus("未识别到语音内容", Colors.Orange);
                        AddLog("语音识别完成，但未识别到内容");
                        return;
                    }
                    
                    UpdateStatus("正在处理文本...", Colors.Blue);
                    AddLog($"识别结果: {recognizedText}");
                    
                    if (!string.IsNullOrEmpty(_settings?.OpenAIApiKey))
                    {
                        _chatGptService.Initialize(_settings.OpenAIApiKey!, _settings.ModelName ?? "gpt-3.5-turbo");
                        
                        // 获取当前选中的处理模式
                        var selectedMode = _settings.GetProcessingMode(_settings.DefaultProcessingMode ?? string.Empty);
                        if (selectedMode == null)
                        {
                            selectedMode = _settings.GetDefaultProcessingModeObject();
                        }
                        
                        var processedText = await _chatGptService.ProcessTextAsync(recognizedText, selectedMode);
                        
                        if (!string.IsNullOrEmpty(processedText))
                        {
                            System.Windows.Clipboard.SetText(processedText);
                            await OutputSimulator.SendTextWithClipboardAsync(processedText);
                            UpdateStatus("处理完成，结果已输出到光标位置", Colors.Green);
                            AddLog($"处理结果: {processedText}");
                        }
                        else
                        {
                            System.Windows.Clipboard.SetText(recognizedText);
                            await OutputSimulator.SendTextWithClipboardAsync(recognizedText);
                            UpdateStatus("AI处理失败，原文已输出到光标位置", Colors.Orange);
                            AddLog("AI处理失败，使用原始识别结果");
                        }
                    }
                    else
                    {
                        System.Windows.Clipboard.SetText(recognizedText);
                        await OutputSimulator.SendTextWithClipboardAsync(recognizedText);
                        UpdateStatus("识别完成，结果已输出到光标位置", Colors.Green);
                        AddLog("未设置OpenAI API Key，直接输出识别结果");
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus($"处理失败: {ex.Message}", Colors.Red);
                    AddLog($"处理错误: {ex.Message}");
                }
            });
        }

        private void OnRecognitionFailed(object? sender, string error)
        {
            this.Dispatcher.Invoke(() =>
            {
                HideRecordingOverlay();
                UpdateStatus($"识别失败: {error}", Colors.Red);
                AddLog($"语音识别失败: {error}");
            });
        }

        private void OnPartialResultReceived(object? sender, string partialText)
        {
            this.Dispatcher.Invoke(() =>
            {
                _recordingOverlay?.UpdateRecognizedText(partialText);
            });
        }

        // UI更新方法
        private void UpdateStatus(string message, MediaColor color)
        {
            this.Dispatcher.Invoke(() =>
            {
                StatusLabel.Text = $"状态: {message}";
                StatusLabel.Foreground = new SolidColorBrush(color);
            });
        }

        private void AddLog(string message)
        {
            this.Dispatcher.Invoke(() =>
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                var logEntry = $"[{timestamp}] {message}\n";
                
                // 添加到日志文本块
                LogTextBlock.Text += logEntry;
                
                // 限制日志长度（保留最后1000行）
                var lines = LogTextBlock.Text.Split('\n');
                if (lines.Length > 1000)
                {
                    var recentLines = lines.Skip(lines.Length - 1000).ToArray();
                    LogTextBlock.Text = string.Join("\n", recentLines);
                }
            });
        }

        // 快捷键监听现在始终启用，无需手动控制

        protected override void OnClosing(CancelEventArgs e)
        {
            // 清理资源
            _hotkeyService?.UnregisterHotkey();
            _speechService?.StopRecognition();
            _notifyIcon?.Dispose();
            _recordingOverlay?.Close();
            
            base.OnClosing(e);
        }
    }
}