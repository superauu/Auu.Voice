using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Timer = System.Threading.Timer;

namespace Speech2TextAssistant;

public class HotkeyService
{
    private const int HOTKEY_ID_VOICE = 9000;
    private const int HOTKEY_ID_TEXT = 9001;
    private const uint MOD_CTRL = 0x0002;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_SHIFT = 0x0004;

    // 语音录音快捷键
    private bool _isVoiceKeyPressed;
    private uint _voiceModifiers;
    private uint _voiceTargetKey;

    // 文本输入快捷键
    private bool _isTextKeyPressed;
    private uint _textModifiers;
    private uint _textTargetKey;

    private Timer? _keyStateTimer;
    private IntPtr _windowHandle;
    private bool _isListeningPaused = false;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public event EventHandler? HotkeyPressed;
    public event EventHandler? HotkeyReleased;
    public event EventHandler? TextInputHotkeyPressed;

    public bool RegisterHotkey(IntPtr handle, string voiceHotkey)
    {
        _windowHandle = handle;

        // 解析语音录音快捷键字符串
        ParseHotkey(voiceHotkey, out _voiceModifiers, out _voiceTargetKey);

        // 启动按键状态监听定时器
        _keyStateTimer = new Timer(CheckKeyState, null, 0, 50);

        return RegisterHotKey(handle, HOTKEY_ID_VOICE, _voiceModifiers, _voiceTargetKey);
    }

    public bool RegisterTextInputHotkey(IntPtr handle, string textHotkey)
    {
        // 解析文本输入快捷键字符串
        ParseHotkey(textHotkey, out _textModifiers, out _textTargetKey);

        return RegisterHotKey(handle, HOTKEY_ID_TEXT, _textModifiers, _textTargetKey);
    }

    private void ParseHotkey(string hotkey, out uint modifiers, out uint targetKey)
    {
        var parts = hotkey.Split('+');
        modifiers = 0;
        targetKey = 0;

        foreach (var part in parts)
        {
            var trimmedPart = part.Trim().ToUpper();
            switch (trimmedPart)
            {
                case "CTRL":
                    modifiers |= MOD_CTRL;
                    break;
                case "ALT":
                    modifiers |= MOD_ALT;
                    break;
                case "SHIFT":
                    modifiers |= MOD_SHIFT;
                    break;
                default:
                    targetKey = GetVirtualKeyCode(trimmedPart);
                    break;
            }
        }
    }

    private uint GetVirtualKeyCode(string key)
    {
        // 处理数字键 0-9
        if (key.Length == 1 && char.IsDigit(key[0]))
        {
            return (uint)(0x30 + (key[0] - '0')); // VK_0 到 VK_9
        }

        // 处理字母键 A-Z
        if (key.Length == 1 && char.IsLetter(key[0]))
        {
            return (uint)key[0]; // A-Z 的虚拟键码就是其ASCII值
        }

        // 处理特殊键
        return key switch
        {
            "F1" => 0x70,
            "F2" => 0x71,
            "F3" => 0x72,
            "F4" => 0x73,
            "F5" => 0x74,
            "F6" => 0x75,
            "F7" => 0x76,
            "F8" => 0x77,
            "F9" => 0x78,
            "F10" => 0x79,
            "F11" => 0x7A,
            "F12" => 0x7B,
            "SPACE" => 0x20,
            "ENTER" => 0x0D,
            "TAB" => 0x09,
            "ESC" => 0x1B,
            "ESCAPE" => 0x1B,
            "UP" => 0x26,
            "DOWN" => 0x28,
            "LEFT" => 0x25,
            "RIGHT" => 0x27,
            "HOME" => 0x24,
            "END" => 0x23,
            "PAGEUP" => 0x21,
            "PAGEDOWN" => 0x22,
            "INSERT" => 0x2D,
            "DELETE" => 0x2E,
            "D1" => 0x31,
            "D2" => 0x32,
            "D3" => 0x33,
            "D4" => 0x34,
            "D5" => 0x35,
            "D6" => 0x36,
            "D7" => 0x37,
            "D8" => 0x38,
            "D9" => 0x39,
            "D0" => 0x30,
            _ => key.Length == 1 ? (uint)key[0] : 0
        };
    }

    private void CheckKeyState(object? state)
    {
        try
        {
            // 如果监听被暂停，则不检查按键状态
            if (_isListeningPaused)
                return;

            // 检查语音录音快捷键
            if (_voiceTargetKey != 0)
            {
                var voiceAllKeysPressed = CheckHotkeyPressed(_voiceModifiers, _voiceTargetKey);

                if (voiceAllKeysPressed && !_isVoiceKeyPressed)
                {
                    _isVoiceKeyPressed = true;
                    HotkeyPressed?.Invoke(this, EventArgs.Empty);
                }
                else if (!voiceAllKeysPressed && _isVoiceKeyPressed)
                {
                    _isVoiceKeyPressed = false;
                    HotkeyReleased?.Invoke(this, EventArgs.Empty);
                }
            }

            // 检查文本输入快捷键
            if (_textTargetKey != 0)
            {
                var textAllKeysPressed = CheckHotkeyPressed(_textModifiers, _textTargetKey);

                if (textAllKeysPressed && !_isTextKeyPressed)
                {
                    _isTextKeyPressed = true;
                    TextInputHotkeyPressed?.Invoke(this, EventArgs.Empty);
                }
                else if (!textAllKeysPressed && _isTextKeyPressed)
                {
                    _isTextKeyPressed = false;
                }
            }
        }
        catch (Exception ex)
        {
            // 忽略检查过程中的异常
            Debug.WriteLine($"按键状态检查异常: {ex.Message}");
        }
    }

    private bool CheckHotkeyPressed(uint modifiers, uint targetKey)
    {
        var targetKeyPressed = (GetAsyncKeyState((int)targetKey) & 0x8000) != 0;
        if (!targetKeyPressed) return false;

        // 检查Ctrl键
        if ((modifiers & MOD_CTRL) != 0)
        {
            if ((GetAsyncKeyState(0x11) & 0x8000) == 0) return false; // VK_CONTROL
        }
        else
        {
            if ((GetAsyncKeyState(0x11) & 0x8000) != 0) return false; // 不应该按下Ctrl
        }

        // 检查Alt键
        if ((modifiers & MOD_ALT) != 0)
        {
            if ((GetAsyncKeyState(0x12) & 0x8000) == 0) return false; // VK_MENU
        }
        else
        {
            if ((GetAsyncKeyState(0x12) & 0x8000) != 0) return false; // 不应该按下Alt
        }

        // 检查Shift键
        if ((modifiers & MOD_SHIFT) != 0)
        {
            if ((GetAsyncKeyState(0x10) & 0x8000) == 0) return false; // VK_SHIFT
        }
        else
        {
            if ((GetAsyncKeyState(0x10) & 0x8000) != 0) return false; // 不应该按下Shift
        }

        return true;
    }

    /// <summary>
    /// 暂停快捷键监听
    /// </summary>
    public void PauseListening()
    {
        _isListeningPaused = true;
    }

    /// <summary>
    /// 恢复快捷键监听
    /// </summary>
    public void ResumeListening()
    {
        _isListeningPaused = false;
    }

    public void UnregisterHotkey()
    {
        _keyStateTimer?.Dispose();
        _keyStateTimer = null;

        if (_windowHandle != IntPtr.Zero)
        {
            UnregisterHotKey(_windowHandle, HOTKEY_ID_VOICE);
            UnregisterHotKey(_windowHandle, HOTKEY_ID_TEXT);
        }
    }

    public bool ProcessHotkey(Message m)
    {
        // 保留原有的热键处理逻辑作为备用
        if (m.Msg == 0x0312 && (m.WParam.ToInt32() == HOTKEY_ID_VOICE || m.WParam.ToInt32() == HOTKEY_ID_TEXT))
            return true; // 已由定时器处理，这里只是消费消息
        return false;
    }
}