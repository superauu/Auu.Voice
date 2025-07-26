using System.Runtime.InteropServices;

namespace Speech2TextAssistant;

public class OutputSimulator
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

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
            SendKeys.SendWait(text);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"输出文本失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public static async Task SendTextWithClipboardAsync(string text)
    {
        try
        {
            // 备份当前剪贴板内容
            var originalClipboard = "";
            if (Clipboard.ContainsText()) originalClipboard = Clipboard.GetText();

            // 将文本放入剪贴板
            Clipboard.SetText(text);

            // 短暂延迟确保剪贴板操作完成
            await Task.Delay(50);

            // 发送Ctrl+V粘贴
            SendKeys.SendWait("^v");

            // 延迟后恢复原剪贴板内容
            await Task.Delay(500);
            if (!string.IsNullOrEmpty(originalClipboard)) Clipboard.SetText(originalClipboard);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"粘贴文本失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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
    ///     更强大的文本输入方法，使用 Windows API 直接发送字符
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

            // 逐字符发送，避免特殊字符问题
            foreach (var c in text)
            {
                if (char.IsControl(c))
                {
                    // 处理控制字符（如换行符）
                    if (c == '\n')
                        SendKeys.SendWait("{ENTER}");
                    else if (c == '\t') SendKeys.SendWait("{TAB}");
                }
                else
                {
                    // 发送普通字符
                    SendKeys.SendWait(c.ToString());
                }

                // 小延迟确保字符正确发送
                Thread.Sleep(1);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"直接输入文本失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}