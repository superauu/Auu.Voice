using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Speech2TextAssistant.Models;

namespace Speech2TextAssistant;

public partial class ProcessingModeManagerWindow : Window
{
    public ObservableCollection<ProcessingMode> ProcessingModes { get; set; }
    private ProcessingMode? _editingMode;
    private bool _isEditing;

    public ProcessingModeManagerWindow(List<ProcessingMode> modes)
    {
        InitializeComponent();
        ProcessingModes = new ObservableCollection<ProcessingMode>(modes);
        ProcessingModesDataGrid.ItemsSource = ProcessingModes;

        // 更新默认标记
        UpdateDefaultFlags();
    }

    private void UpdateDefaultFlags()
    {
        foreach (var mode in ProcessingModes)
        {
            mode.IsDefault = false;
        }

        // 这里需要从外部传入当前默认模式名称
        // 暂时先不设置，由调用方处理
    }

    public void SetDefaultMode(string defaultModeName)
    {
        foreach (var mode in ProcessingModes)
        {
            mode.IsDefault = mode.Name == defaultModeName;
        }
        ProcessingModesDataGrid.Items.Refresh();
    }

    private void ProcessingModesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedMode = ProcessingModesDataGrid.SelectedItem as ProcessingMode;
        bool hasSelection = selectedMode != null;

        EditModeButton.IsEnabled = hasSelection;
        DeleteModeButton.IsEnabled = hasSelection && !selectedMode?.IsBuiltIn == true;
        SetDefaultButton.IsEnabled = hasSelection && !selectedMode?.IsDefault == true;

        if (hasSelection && !_isEditing)
        {
            LoadModeForDisplay(selectedMode!);
        }
    }

    private void LoadModeForDisplay(ProcessingMode mode)
    {
        ModeNameTextBox.Text = mode.Name;
        ModeDisplayNameTextBox.Text = mode.DisplayName;
        ModeSystemPromptTextBox.Text = mode.SystemPrompt;

        // 显示模式，不允许编辑
        ModeNameTextBox.IsReadOnly = true;
        ModeDisplayNameTextBox.IsReadOnly = true;
        ModeSystemPromptTextBox.IsReadOnly = true;
    }

    private void AddModeButton_Click(object sender, RoutedEventArgs e)
    {
        StartEditing(new ProcessingMode());
    }

    private void EditModeButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedMode = ProcessingModesDataGrid.SelectedItem as ProcessingMode;
        if (selectedMode != null)
        {
            StartEditing(new ProcessingMode
            {
                Name = selectedMode.Name,
                DisplayName = selectedMode.DisplayName,
                SystemPrompt = selectedMode.SystemPrompt,
                IsBuiltIn = selectedMode.IsBuiltIn,
                IsDefault = selectedMode.IsDefault
            });
        }
    }

    private void StartEditing(ProcessingMode mode)
    {
        _editingMode = mode;
        _isEditing = true;

        ModeNameTextBox.Text = mode.Name;
        ModeDisplayNameTextBox.Text = mode.DisplayName;
        ModeSystemPromptTextBox.Text = mode.SystemPrompt;

        // 编辑模式，允许编辑
        ModeNameTextBox.IsReadOnly = false;
        ModeDisplayNameTextBox.IsReadOnly = false;
        ModeSystemPromptTextBox.IsReadOnly = false;

        // 内置模式不允许修改名称
        if (mode.IsBuiltIn)
        {
            ModeNameTextBox.IsReadOnly = true;
        }

        SaveModeButton.IsEnabled = true;
        CancelEditButton.IsEnabled = true;

        // 禁用列表操作
        AddModeButton.IsEnabled = false;
        EditModeButton.IsEnabled = false;
        DeleteModeButton.IsEnabled = false;
        SetDefaultButton.IsEnabled = false;
    }

    private void SaveModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_editingMode == null) return;

        // 验证输入
        if (string.IsNullOrWhiteSpace(ModeNameTextBox.Text))
        {
            MessageBox.Show("请输入模式名称", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(ModeDisplayNameTextBox.Text))
        {
            MessageBox.Show("请输入显示名称", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(ModeSystemPromptTextBox.Text))
        {
            MessageBox.Show("请输入系统提示词", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 检查名称是否重复（编辑现有模式时排除自己）
        var existingMode = ProcessingModes.FirstOrDefault(m => m.Name == ModeNameTextBox.Text);
        if (existingMode != null && existingMode != ProcessingModesDataGrid.SelectedItem)
        {
            MessageBox.Show("模式名称已存在", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 更新模式信息
        _editingMode.Name = ModeNameTextBox.Text.Trim();
        _editingMode.DisplayName = ModeDisplayNameTextBox.Text.Trim();
        _editingMode.SystemPrompt = ModeSystemPromptTextBox.Text.Trim();

        // 如果是新模式，添加到列表
        if (!ProcessingModes.Contains(_editingMode))
        {
            ProcessingModes.Add(_editingMode);
        }
        else
        {
            // 如果是编辑现有模式，更新列表中的对应项
            var selectedMode = ProcessingModesDataGrid.SelectedItem as ProcessingMode;
            if (selectedMode != null)
            {
                selectedMode.Name = _editingMode.Name;
                selectedMode.DisplayName = _editingMode.DisplayName;
                selectedMode.SystemPrompt = _editingMode.SystemPrompt;
            }
        }

        ProcessingModesDataGrid.Items.Refresh();
        StopEditing();
    }

    private void CancelEditButton_Click(object sender, RoutedEventArgs e)
    {
        StopEditing();
    }

    private void StopEditing()
    {
        _editingMode = null;
        _isEditing = false;

        // 清空编辑区域
        ModeNameTextBox.Text = "";
        ModeDisplayNameTextBox.Text = "";
        ModeSystemPromptTextBox.Text = "";

        ModeNameTextBox.IsReadOnly = true;
        ModeDisplayNameTextBox.IsReadOnly = true;
        ModeSystemPromptTextBox.IsReadOnly = true;

        SaveModeButton.IsEnabled = false;
        CancelEditButton.IsEnabled = false;

        // 重新启用列表操作
        AddModeButton.IsEnabled = true;

        // 重新检查选择状态
        ProcessingModesDataGrid_SelectionChanged(ProcessingModesDataGrid, new SelectionChangedEventArgs(Selector.SelectionChangedEvent, new List<object>(), new List<object>()));
    }

    private void DeleteModeButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedMode = ProcessingModesDataGrid.SelectedItem as ProcessingMode;
        if (selectedMode != null && !selectedMode.IsBuiltIn)
        {
            var result = MessageBox.Show($"确定要删除处理模式 '{selectedMode.DisplayName}' 吗？",
                "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ProcessingModes.Remove(selectedMode);

                // 清空编辑区域
                ModeNameTextBox.Text = "";
                ModeDisplayNameTextBox.Text = "";
                ModeSystemPromptTextBox.Text = "";
            }
        }
    }

    private void SetDefaultButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedMode = ProcessingModesDataGrid.SelectedItem as ProcessingMode;
        if (selectedMode != null)
        {
            foreach (var mode in ProcessingModes)
            {
                mode.IsDefault = mode == selectedMode;
            }
            ProcessingModesDataGrid.Items.Refresh();
            SetDefaultButton.IsEnabled = false;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    public string? GetDefaultModeName()
    {
        return ProcessingModes.FirstOrDefault(m => m.IsDefault)?.Name;
    }

    public List<ProcessingMode> GetProcessingModes()
    {
        return ProcessingModes.ToList();
    }
}