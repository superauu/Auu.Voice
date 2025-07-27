using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Globalization;
namespace Speech2TextAssistant
{
    public partial class RecordingOverlayWpf : Window
    {
        private DispatcherTimer? _animationTimer;
        private readonly Random _random = new();
        private readonly float[] _waveHeights = new float[20];
        private int _waveOffset;
        private string _currentText = "正在录音中...";
        private string _recognizedText = "";

        public RecordingOverlayWpf()
        {
            InitializeComponent();
            SetupWindow();
            InitializeWaveData();
            StartAnimation();
        }

        private void SetupWindow()
        {
            // 设置窗体位置到屏幕正中间下部
            var workingArea = SystemParameters.WorkArea;
            this.Left = (workingArea.Width - this.Width) / 2;
            this.Top = workingArea.Height - this.Height - 100;
        }

        private void InitializeWaveData()
        {
            for (var i = 0; i < _waveHeights.Length; i++)
            {
                _waveHeights[i] = _random.Next(10, 50);
            }
        }

        private void StartAnimation()
        {
            _animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50) // 20 FPS
            };
            _animationTimer.Tick += AnimationTimer_Tick;
            _animationTimer.Start();
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            _waveOffset += 2;

            // 随机更新波形高度
            for (var i = 0; i < _waveHeights.Length; i++)
            {
                if (_random.Next(0, 10) < 3) // 30%概率更新
                {
                    _waveHeights[i] = _random.Next(10, 50);
                }
            }

            UpdateWaveform();
        }

        private void UpdateWaveform()
        {
            WaveformCanvas.Children.Clear();

            var canvasWidth = WaveformCanvas.ActualWidth > 0 ? WaveformCanvas.ActualWidth : 360; // 默认宽度
            var canvasHeight = WaveformCanvas.Height;
            var barWidth = canvasWidth / _waveHeights.Length;

            for (var i = 0; i < _waveHeights.Length; i++)
            {
                var x = i * barWidth;
                var height = _waveHeights[i] * (0.5f + 0.5f * (float)Math.Sin((_waveOffset + i * 10) * Math.PI / 180));
                var y = (canvasHeight - height) / 2;

                var rectangle = new Rectangle
                {
                    Width = barWidth - 2,
                    Height = height,
                    Fill = new SolidColorBrush(Color.FromArgb(100, 0, 255, 0))
                };

                Canvas.SetLeft(rectangle, x);
                Canvas.SetTop(rectangle, y);
                WaveformCanvas.Children.Add(rectangle);
            }
        }

        public void UpdateText(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                this.Dispatcher.Invoke(() =>
                {
                    _recognizedText = text;
                    UpdateDisplayText();
                });
            }
        }

        public void UpdateRecognizedText(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                this.Dispatcher.Invoke(() =>
                {
                    _recognizedText = text;
                    UpdateDisplayText();
                });
            }
        }

        public void ClearRecognizedText()
        {
            this.Dispatcher.Invoke(() =>
            {
                _recognizedText = "";
                UpdateDisplayText();
            });
        }

        private void UpdateDisplayText()
        {
            var displayText = !string.IsNullOrEmpty(_recognizedText) ? _recognizedText : _currentText;
            
            // 如果文字太长，只保留最新的内容
            var maxWidth = this.Width - 80; // 考虑边距
            var formattedText = new FormattedText(
                displayText,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(StatusTextBlock.FontFamily, StatusTextBlock.FontStyle, StatusTextBlock.FontWeight, StatusTextBlock.FontStretch),
                StatusTextBlock.FontSize,
                Brushes.White,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            
            if (formattedText.Width > maxWidth)
            {
                // 从后往前截取文字，确保显示最新内容
                var words = displayText.Split(' ');
                var truncatedText = "";
                
                for (int i = words.Length - 1; i >= 0; i--)
                {
                    var testText = words[i] + (string.IsNullOrEmpty(truncatedText) ? "" : " " + truncatedText);
                    var testFormattedText = new FormattedText(
                        testText,
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface(StatusTextBlock.FontFamily, StatusTextBlock.FontStyle, StatusTextBlock.FontWeight, StatusTextBlock.FontStretch),
                        StatusTextBlock.FontSize,
                        Brushes.White,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);
                    
                    if (testFormattedText.Width <= maxWidth)
                    {
                        truncatedText = testText;
                    }
                    else
                    {
                        break;
                    }
                }
                
                // 如果截取后仍然为空或太长，则直接截取字符
                if (string.IsNullOrEmpty(truncatedText))
                {
                    truncatedText = displayText;
                    while (truncatedText.Length > 1)
                    {
                        var testFormattedText = new FormattedText(
                            truncatedText,
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface(StatusTextBlock.FontFamily, StatusTextBlock.FontStyle, StatusTextBlock.FontWeight, StatusTextBlock.FontStretch),
                            StatusTextBlock.FontSize,
                            Brushes.White,
                            VisualTreeHelper.GetDpi(this).PixelsPerDip);
                        
                        if (testFormattedText.Width <= maxWidth)
                        {
                            break;
                        }
                        
                        truncatedText = truncatedText.Substring(1);
                    }
                }
                
                displayText = truncatedText;
            }
            
            StatusTextBlock.Text = displayText;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _animationTimer?.Stop();
            _animationTimer = null;
            base.OnClosed(e);
        }
    }
}