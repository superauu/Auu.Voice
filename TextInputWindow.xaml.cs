using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace Speech2TextAssistant
{
    public partial class TextInputWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);
        
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }
        
        public string? InputText { get; private set; }
        public bool IsConfirmed { get; private set; }
        
        public TextInputWindow()
        {
            InitializeComponent();
            
            // 设置窗口位置在鼠标附近
            SetWindowPositionNearMouse();
            
            // 设置焦点到文本框
            Loaded += (s, e) => 
            {
                // 确保窗口激活并获得焦点
                this.Activate();
                this.Focus();
                // 延迟一点时间确保窗口完全加载后再设置文本框焦点
                this.Dispatcher.BeginInvoke(new Action(() => 
                {
                    InputTextBox.Focus();
                    InputTextBox.SelectAll();
                }), System.Windows.Threading.DispatcherPriority.Input);
            };
            
            // 支持ESC键取消
            KeyDown += TextInputWindow_KeyDown;
        }
        
        private void SetWindowPositionNearMouse()
        {
            if (GetCursorPos(out POINT mousePos))
            {
                // 获取屏幕工作区域
                var workingArea = SystemParameters.WorkArea;
                
                // 计算窗口位置，确保不超出屏幕边界
                double left = mousePos.X + 10;
                double top = mousePos.Y + 10;
                
                // 检查右边界
                if (left + Width > workingArea.Right)
                {
                    left = mousePos.X - Width - 10;
                }
                
                // 检查下边界
                if (top + Height > workingArea.Bottom)
                {
                    top = mousePos.Y - Height - 10;
                }
                
                // 确保不超出左上边界
                left = Math.Max(workingArea.Left, left);
                top = Math.Max(workingArea.Top, top);
                
                Left = left;
                Top = top;
            }
        }
        
        private void TextInputWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CancelButton_Click(sender, new RoutedEventArgs());
            }
            else if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ConfirmButton_Click(sender, new RoutedEventArgs());
            }
        }
        
        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            InputText = InputTextBox.Text;
            IsConfirmed = true;
            DialogResult = true;
            Close();
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            InputText = null;
            IsConfirmed = false;
            DialogResult = false;
            Close();
        }
        
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}