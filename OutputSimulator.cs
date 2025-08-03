using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Speech2TextAssistant;

public class OutputSimulator
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    /// <summary>
    /// 获取当前前台窗口句柄
    /// </summary>
    public static IntPtr GetCurrentForegroundWindow()
    {
        return GetForegroundWindow();
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    private const int KEYEVENTF_KEYDOWN = 0x0000;
    private const int KEYEVENTF_KEYUP = 0x0002;
    private const int KEYEVENTF_UNICODE = 0x0004;
    private const int VK_CONTROL = 0x11;
    private const int VK_V = 0x56;
    private const int VK_RETURN = 0x0D;
    private const int VK_TAB = 0x09;
    private const int INPUT_KEYBOARD = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    public static async Task SendTextToActiveWindowAsync(string text)
    {
        try
        {
            // 获取当前活动窗口
            var activeWindow = GetForegroundWindow();

            // 短暂延迟确保窗口准备就绪
            await Task.Delay(100);

            // 处理特殊字符
            text = EscapeSpecialCharacters(text);

            // 发送文本
            SendTextDirect(text);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"输出文本失败: {ex.Message}", "错误");
        }
    }

    /// <summary>
    /// 向指定窗口发送文本
    /// </summary>
    public static async Task SendTextToSpecificWindowAsync(string text, IntPtr targetWindow)
    {
        try
        {
            if (targetWindow == IntPtr.Zero)
            {
                await SendTextToActiveWindowAsync(text);
                return;
            }

            // 短暂延迟确保窗口准备就绪
            await Task.Delay(100);

            // 处理特殊字符
            text = EscapeSpecialCharacters(text);

            // 发送文本到指定窗口
            SendTextToSpecificWindow(text, targetWindow);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"输出文本失败: {ex.Message}", "错误");
        }
    }

    public static async Task SendTextWithClipboardAsync(string text)
    {
        try
        {
            await Task.Run(() =>
            {
                // 在STA线程中执行剪贴板操作
                var staThread = new Thread(() =>
                {
                    try
                    {
                        SetClipboardWithRetry(text);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"粘贴文本失败: {ex.Message}", "错误");
                    }
                });

                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();
                staThread.Join();
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"粘贴文本失败: {ex.Message}", "错误");
        }
    }

    private static void SetClipboardWithRetry(string text)
    {
        const int maxRetries = 3;
        var originalClipboard = "";

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                // 备份当前剪贴板内容
                if (Clipboard.ContainsText())
                {
                    originalClipboard = Clipboard.GetText();
                }

                // 清空剪贴板
                Clipboard.Clear();
                Thread.Sleep(10);

                // 将文本放入剪贴板
                Clipboard.SetText(text);
                Thread.Sleep(50);

                // 验证剪贴板内容
                if (Clipboard.ContainsText() && Clipboard.GetText() == text)
                {
                    // 发送Ctrl+V粘贴
                    SendCtrlV();
                    Thread.Sleep(100);

                    // 延迟后恢复原剪贴板内容
                    Thread.Sleep(400);
                    if (!string.IsNullOrEmpty(originalClipboard))
                    {
                        Clipboard.SetText(originalClipboard);
                    }
                    return; // 成功，退出重试循环
                }
            }
            catch (ExternalException)
            {
                // 剪贴板被其他进程占用，等待后重试
                Thread.Sleep(100);
                if (i == maxRetries - 1) throw;
            }
        }

        throw new InvalidOperationException("无法访问剪贴板，请稍后重试");
    }

    // 保持向后兼容的同步方法
    public static void SendTextToActiveWindow(string text)
    {
        SendTextToActiveWindowAsync(text).GetAwaiter().GetResult();
    }

    public static void SendTextWithClipboard(string text)
    {
        SendTextWithClipboardAsync(text).GetAwaiter().GetResult();
    }

    private static string EscapeSpecialCharacters(string text)
    {
        return text.Replace("{", "{{}")
            .Replace("}", "{}}")
            .Replace("+", "{+}")
            .Replace("^", "{^}")
            .Replace("%", "{%}")
            .Replace("~", "{~}")
            .Replace("(", "{(}")
            .Replace(")", "{)}")
            .Replace("[", "{[}")
            .Replace("]", "{]}");
    }

    /// <summary>
    ///     更强大的文本输入方法，使用 Unicode 输入绕过输入法影响
    /// </summary>
    public static void SendTextDirect(string text)
    {
        try
        {
            var activeWindow = GetForegroundWindow();
            if (activeWindow == IntPtr.Zero) return;

            // 确保窗口获得焦点
            SetForegroundWindow(activeWindow);
            Thread.Sleep(100);

            // 逐字符发送，使用Unicode方式避免输入法影响
            foreach (var c in text)
            {
                if (char.IsControl(c))
                {
                    // 处理控制字符（如换行符）
                    if (c == '\n')
                        SendKey(VK_RETURN);
                    else if (c == '\t')
                        SendKey(VK_TAB);
                }
                else
                {
                    // 使用Unicode方式发送普通字符
                    SendCharUnicode(c);
                }

                // 小延迟确保字符正确发送
                Thread.Sleep(2);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"直接输入文本失败: {ex.Message}", "错误");
        }
    }

    /// <summary>
    /// 向指定窗口发送文本
    /// </summary>
    private static void SendTextToSpecificWindow(string text, IntPtr targetWindow)
    {
        try
        {
            if (targetWindow == IntPtr.Zero) return;

            // 确保目标窗口获得焦点
            SetForegroundWindow(targetWindow);
            Thread.Sleep(150); // 稍微增加延迟确保窗口切换完成

            // 逐字符发送，使用Unicode方式避免输入法影响
            foreach (var c in text)
            {
                if (char.IsControl(c))
                {
                    // 处理控制字符（如换行符）
                    if (c == '\n')
                        SendKey(VK_RETURN);
                    else if (c == '\t')
                        SendKey(VK_TAB);
                }
                else
                {
                    // 使用Unicode方式发送普通字符
                    SendCharUnicode(c);
                }

                // 小延迟确保字符正确发送
                Thread.Sleep(2);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"向指定窗口输入文本失败: {ex.Message}", "错误");
        }
    }

    private static void SendCtrlV()
    {
        // 按下Ctrl
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        // 按下V
        keybd_event(VK_V, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        // 释放V
        keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        // 释放Ctrl
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private static void SendKey(int vkCode)
    {
        keybd_event((byte)vkCode, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        keybd_event((byte)vkCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private static void SendCharUnicode(char c)
    {
        // 使用Unicode输入方法，绕过输入法影响
        var inputs = new INPUT[2];

        // Key down
        inputs[0] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = c,
                    dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYDOWN,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        // Key up
        inputs[1] = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = c,
                    dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    [DllImport("user32.dll")]
    private static extern short VkKeyScan(char ch);

    // 输入法相关API
    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    private static extern bool ActivateKeyboardLayout(IntPtr hkl, uint flags);

    [DllImport("user32.dll")]
    private static extern int GetKeyboardLayoutList(int nBuff, IntPtr[] lpList);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

    private const uint KLF_ACTIVATE = 0x00000001;
    private const int LOCALE_SENGLANGUAGE = 0x1001;

    /// <summary>
    /// 获取当前输入法布局
    /// </summary>
    private static IntPtr GetCurrentInputMethod()
    {
        var foregroundWindow = GetForegroundWindow();
        var threadId = GetWindowThreadProcessId(foregroundWindow, IntPtr.Zero);
        return GetKeyboardLayout(threadId);
    }

    /// <summary>
    /// 切换到英文输入法
    /// </summary>
    private static IntPtr SwitchToEnglishInputMethod()
    {
        var currentLayout = GetCurrentInputMethod();

        // 获取所有可用的键盘布局
        var layoutCount = GetKeyboardLayoutList(0, Array.Empty<IntPtr>());
        if (layoutCount > 0)
        {
            var layouts = new IntPtr[layoutCount];
            GetKeyboardLayoutList(layoutCount, layouts);

            // 查找英文布局 (通常是0x04090409 for US English)
            foreach (var layout in layouts)
            {
                var layoutId = layout.ToInt64() & 0xFFFF;
                // 英文布局的语言ID通常是0x0409 (US English) 或 0x0809 (UK English)
                if (layoutId == 0x0409 || layoutId == 0x0809)
                {
                    ActivateKeyboardLayout(layout, KLF_ACTIVATE);
                    return currentLayout; // 返回原来的布局以便恢复
                }
            }
        }

        return currentLayout;
    }

    /// <summary>
    /// 恢复输入法布局
    /// </summary>
    private static void RestoreInputMethod(IntPtr originalLayout)
    {
        if (originalLayout != IntPtr.Zero)
        {
            ActivateKeyboardLayout(originalLayout, KLF_ACTIVATE);
        }
    }
}