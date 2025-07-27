using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Timer = System.Threading.Timer;

namespace Speech2TextAssistant;

public class HotkeyService
{
    private const int HOTKEY_ID = 9000;
    private const uint MOD_CTRL = 0x0002;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_SHIFT = 0x0004;
    private bool _isKeyPressed;
    private Timer? _keyStateTimer; // 明确指定使用 System.Threading.Timer
    private uint _modifiers;
    private uint _targetKey;

    private IntPtr _windowHandle;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public event EventHandler? HotkeyPressed;
    public event EventHandler? HotkeyReleased;

    public bool RegisterHotkey(IntPtr handle, string hotkey)
    {
        _windowHandle = handle;

        // 解析快捷键字符串
        var parts = hotkey.Split('+');
        _modifiers = 0;
        _targetKey = 0;

        foreach (var part in parts)
            switch (part.Trim().ToUpper())
            {
                case "CTRL":
                    _modifiers |= MOD_CTRL;
                    break;
                case "ALT":
                    _modifiers |= MOD_ALT;
                    break;
                case "SHIFT":
                    _modifiers |= MOD_SHIFT;
                    break;
                default:
                    if (part.Length == 1) _targetKey = part.ToUpper()[0];
                    break;
            }

        // 启动按键状态监听定时器
        _keyStateTimer = new Timer(CheckKeyState, null, 0, 50); // 明确指定类型

        return RegisterHotKey(handle, HOTKEY_ID, _modifiers, _targetKey);
    }

    private void CheckKeyState(object? state)
    {
        try
        {
            var ctrlPressed = (_modifiers & MOD_CTRL) != 0 && (GetAsyncKeyState(0x11) & 0x8000) != 0; // VK_CONTROL
            var altPressed = (_modifiers & MOD_ALT) != 0 && (GetAsyncKeyState(0x12) & 0x8000) != 0; // VK_MENU
            var shiftPressed = (_modifiers & MOD_SHIFT) != 0 && (GetAsyncKeyState(0x10) & 0x8000) != 0; // VK_SHIFT
            var targetKeyPressed = (GetAsyncKeyState((int)_targetKey) & 0x8000) != 0;

            // 检查所有修饰键和目标键是否都被按下
            var allKeysPressed = targetKeyPressed;
            if ((_modifiers & MOD_CTRL) != 0) allKeysPressed &= ctrlPressed;
            if ((_modifiers & MOD_ALT) != 0) allKeysPressed &= altPressed;
            if ((_modifiers & MOD_SHIFT) != 0) allKeysPressed &= shiftPressed;

            if (allKeysPressed && !_isKeyPressed)
            {
                _isKeyPressed = true;
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
            }
            else if (!allKeysPressed && _isKeyPressed)
            {
                _isKeyPressed = false;
                HotkeyReleased?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            // 忽略检查过程中的异常
            Debug.WriteLine($"按键状态检查异常: {ex.Message}");
        }
    }

    public void UnregisterHotkey()
    {
        _keyStateTimer?.Dispose();
        _keyStateTimer = null;

        if (_windowHandle != IntPtr.Zero) UnregisterHotKey(_windowHandle, HOTKEY_ID);
    }

    public bool ProcessHotkey(Message m)
    {
        // 保留原有的热键处理逻辑作为备用
        if (m.Msg == 0x0312 && m.WParam.ToInt32() == HOTKEY_ID) return true; // 已由定时器处理，这里只是消费消息
        return false;
    }
}