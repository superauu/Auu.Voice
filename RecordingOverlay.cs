using System.Drawing.Drawing2D;
using Timer = System.Windows.Forms.Timer;

namespace Speech2TextAssistant;

public class RecordingOverlay : Form
{
    private Timer? _animationTimer;
    private Point _dragStartPoint;
    private bool _isDragging;
    private readonly Random _random = new();
    private readonly float[] _waveHeights = new float[20];
    private int _waveOffset;
    private string _currentText = "正在录音中...";
    private string _recognizedText = "";

    public RecordingOverlay()
    {
        InitializeComponent();
        InitializeWaveData();
        StartAnimation();
    }

    /// <summary>
    ///     Required method for Designer support - do not modify
    ///     the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        SuspendLayout();
        // 
        // RecordingOverlay
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.Black;
        ClientSize = new Size(400, 120);
        FormBorderStyle = FormBorderStyle.None;
        Margin = new Padding(4, 4, 4, 4);
        ShowInTaskbar = false;
        TopMost = true;
        TransparencyKey = Color.Black;
        Opacity = 0.9;
        Load += RecordingOverlay_Load;
        ResumeLayout(false);
    }

    private void SetupForm()
    {
        // 设置窗体位置到屏幕正中间下部
        var screen = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        Location = new Point(
            (screen.Width - Width) / 2,
            screen.Height - Height - 100
        );

        // 设置双缓冲以减少闪烁
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.DoubleBuffer, true);

        // 添加鼠标事件处理器以支持拖动
        MouseDown += RecordingOverlay_MouseDown;
        MouseMove += RecordingOverlay_MouseMove;
        MouseUp += RecordingOverlay_MouseUp;
    }

    private void InitializeWaveData()
    {
        for (var i = 0; i < _waveHeights.Length; i++) _waveHeights[i] = _random.Next(10, 50);
    }

    private void StartAnimation()
    {
        _animationTimer = new Timer();
        _animationTimer.Interval = 50; // 20 FPS
        _animationTimer.Tick += AnimationTimer_Tick;
        _animationTimer.Start();
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        _waveOffset += 2;

        // 随机更新波形高度
        for (var i = 0; i < _waveHeights.Length; i++)
            if (_random.Next(0, 10) < 3) // 30%概率更新
                _waveHeights[i] = _random.Next(10, 50);

        Invalidate(); // 触发重绘
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // 绘制半透明背景
        using (var brush = new SolidBrush(Color.FromArgb(220, 30, 30, 30)))
        {
            g.FillRoundedRectangle(brush, new Rectangle(10, 10, Width - 20, Height - 20), 15);
        }

        // 绘制"正在录音"文字（如果太长则截取最新内容）
        using (var font = new Font("微软雅黑", 12, FontStyle.Bold))
        using (var brush = new SolidBrush(Color.White))
        {
            var displayText = !string.IsNullOrEmpty(_recognizedText) ? _recognizedText : _currentText;
            
            // 如果文字太长，只保留最新的内容
            var maxWidth = Width - 40;
            var textSize = g.MeasureString(displayText, font);
            
            if (textSize.Width > maxWidth)
            {
                // 从后往前截取文字，确保显示最新内容
                var words = displayText.Split(' ');
                var truncatedText = "";
                
                for (int i = words.Length - 1; i >= 0; i--)
                {
                    var testText = words[i] + (string.IsNullOrEmpty(truncatedText) ? "" : " " + truncatedText);
                    var testSize = g.MeasureString(testText, font);
                    
                    if (testSize.Width <= maxWidth)
                    {
                        truncatedText = testText;
                    }
                    else
                    {
                        break;
                    }
                }
                
                // 如果截取后仍然为空或太长，则直接截取字符
                if (string.IsNullOrEmpty(truncatedText) || g.MeasureString(truncatedText, font).Width > maxWidth)
                {
                    truncatedText = displayText;
                    while (g.MeasureString(truncatedText, font).Width > maxWidth && truncatedText.Length > 1)
                    {
                        truncatedText = truncatedText.Substring(1);
                    }
                }
                
                displayText = truncatedText;
            }
            
            var finalTextSize = g.MeasureString(displayText, font);
            var textX = (Width - finalTextSize.Width) / 2;
            
            g.DrawString(displayText, font, brush, textX, 15);
        }

        // 绘制音波
        DrawWaveform(g);
    }

    private void DrawWaveform(Graphics g)
    {
        var waveY = Height - 35;
        var waveWidth = Width - 40;
        var barWidth = waveWidth / _waveHeights.Length;

        using (var brush = new SolidBrush(Color.FromArgb(100, 0, 255, 0)))
        {
            for (var i = 0; i < _waveHeights.Length; i++)
            {
                var x = 20 + i * barWidth;
                var height = _waveHeights[i] * (0.5f + 0.5f * (float)Math.Sin((_waveOffset + i * 10) * Math.PI / 180));
                var y = waveY - height / 2;

                g.FillRectangle(brush, x, y, barWidth - 2, height);
            }
        }
    }

    public void UpdateText(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            _recognizedText = text;
            Invalidate(); // 触发重绘
        }
    }

    public void UpdateRecognizedText(string text)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<string>(UpdateRecognizedText), text);
            return;
        }
        
        if (!string.IsNullOrWhiteSpace(text))
        {
            _recognizedText = text;
            Invalidate(); // 触发重绘
        }
    }
    
    // 清除识别文字，恢复默认显示
    public void ClearRecognizedText()
    {
        if (InvokeRequired)
        {
            Invoke(ClearRecognizedText);
            return;
        }
        
        _recognizedText = "";
        Invalidate();
    }

    public void StopAnimation()
    {
        _animationTimer?.Stop();
        _animationTimer?.Dispose();
        _animationTimer = null;
    }

    private void RecordingOverlay_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = true;
            _dragStartPoint = e.Location;
            Cursor = Cursors.SizeAll;
        }
    }

    private void RecordingOverlay_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            var newLocation = new Point(
                Location.X + e.X - _dragStartPoint.X,
                Location.Y + e.Y - _dragStartPoint.Y
            );
            Location = newLocation;
        }
    }

    private void RecordingOverlay_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = false;
            Cursor = Cursors.Default;
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        StopAnimation();
        base.OnFormClosed(e);
    }

    private void RecordingOverlay_Load(object? sender, EventArgs e)
    {
        SetupForm();
    }
}

// 扩展方法用于绘制圆角矩形
public static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle rect, int radius)
    {
        using (var path = new GraphicsPath())
        {
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.X + rect.Width - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.X + rect.Width - radius, rect.Y + rect.Height - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Y + rect.Height - radius, radius, radius, 90, 90);
            path.CloseFigure();
            g.FillPath(brush, path);
        }
    }
}