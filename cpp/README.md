# Speech2TextAssistant - Qt C++ Version

这是语音转文字助手的Qt C++版本，提供与C#版本相同的功能。

## 功能特性

- 🎤 全局快捷键语音录制
- 🔄 支持按住录音和切换录音两种模式
- 🌐 Azure Speech Services语音识别
- 🤖 OpenAI ChatGPT文本处理
- 📋 自动输出到剪贴板
- 🎨 录音状态可视化界面
- 🔧 系统托盘集成

## 系统要求

- Windows 10/11
- Qt 6.5 或更高版本
- CMake 3.16 或更高版本
- Visual Studio 2019/2022 或 MinGW
- Azure Speech Services 订阅
- OpenAI API 密钥

## 构建说明

### 1. 安装依赖

确保已安装以下组件：
- Qt 6.5+ (包含 Core, Widgets, Network 模块)
- CMake 3.16+
- 支持C++17的编译器

### 2. 构建项目

```bash
# 创建构建目录
mkdir build
cd build

# 配置项目
cmake .. -DCMAKE_PREFIX_PATH="C:/Qt/6.5.0/msvc2019_64"

# 构建项目
cmake --build . --config Release
```

### 3. 运行程序

```bash
# 在build目录下
./Release/Speech2TextAssistant.exe
```

## 配置说明

首次运行时需要配置以下信息：

1. **Azure Speech Services**
   - Speech Key: 您的Azure Speech Services密钥
   - Speech Region: 服务区域（如：eastus）

2. **OpenAI API**
   - API Key: 您的OpenAI API密钥
   - Model: GPT模型选择（gpt-3.5-turbo, gpt-4等）

3. **快捷键设置**
   - 默认快捷键：F2
   - 支持组合键：Ctrl+Alt+字母/数字/功能键

4. **录音模式**
   - 按住录音：按住快捷键期间录音
   - 切换录音：按一次开始，再按一次停止

## 项目结构

```
cpp/
├── CMakeLists.txt          # CMake构建配置
├── main.cpp                # 程序入口点
├── MainWindow.h/cpp        # 主窗口界面
├── resources.qrc           # Qt资源文件
├── icon.png               # 应用程序图标
├── Models/                 # 数据模型
│   ├── AppSettings.h/cpp   # 应用设置
│   └── ChatGptPromptType.h # ChatGPT提示类型
├── Services/               # 服务层
│   ├── ConfigManager.h/cpp        # 配置管理
│   ├── SpeechRecognizerService.h/cpp # 语音识别服务
│   ├── ChatGptService.h/cpp        # ChatGPT服务
│   ├── HotkeyService.h/cpp         # 快捷键服务
│   └── OutputSimulator.h/cpp       # 输出模拟器
└── UI/                     # 用户界面
    └── RecordingOverlay.h/cpp      # 录音状态覆盖层
```

## 使用说明

1. **启动程序**：运行可执行文件
2. **配置设置**：在主界面中填入必要的API密钥
3. **保存设置**：点击"保存设置"按钮
4. **开始使用**：按下设置的快捷键开始录音
5. **查看结果**：处理后的文本会自动复制到剪贴板

## 故障排除

### 常见问题

1. **程序无法启动**
   - 检查Qt库是否正确安装
   - 确认所有DLL文件在程序目录中

2. **快捷键不工作**
   - 检查是否有其他程序占用相同快捷键
   - 尝试以管理员权限运行

3. **语音识别失败**
   - 验证Azure Speech Services密钥和区域
   - 检查网络连接
   - 确认麦克风权限

4. **文本处理失败**
   - 验证OpenAI API密钥
   - 检查API配额和余额

### 日志查看

程序运行时会在主界面底部显示日志信息，包括：
- 语音识别状态
- API调用结果
- 错误信息

## 开发说明

### 添加新功能

1. **新的处理模式**：在`ChatGptPromptType.h`中添加新的枚举值
2. **新的服务**：在`Services/`目录下创建新的服务类
3. **UI组件**：在`UI/`目录下添加新的界面组件

### 代码规范

- 使用Qt的信号槽机制进行组件通信
- 遵循RAII原则管理资源
- 使用智能指针管理内存
- 异步处理网络请求

## 许可证

本项目采用MIT许可证，详见LICENSE文件。

## 贡献

欢迎提交Issue和Pull Request来改进项目。