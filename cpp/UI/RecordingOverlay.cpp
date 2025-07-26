#include "RecordingOverlay.h"
#include <QPainterPath>
#include <QBrush>
#include <QPen>
#include <QFont>
#include <QFontMetrics>
#include <QDebug>

RecordingOverlay::RecordingOverlay(QWidget* parent)
    : QWidget(parent)
    , m_animationTimer(new QTimer(this))
    , m_isDragging(false)
    , m_random(QRandomGenerator::global())
    , m_waveHeights(WAVE_COUNT)
    , m_waveOffset(0)
    , m_currentText("正在录音中...")
{
    setupForm();
    initializeWaveData();
    
    connect(m_animationTimer, &QTimer::timeout, this, &RecordingOverlay::onAnimationTimer);
}

RecordingOverlay::~RecordingOverlay()
{
    stopAnimation();
}

void RecordingOverlay::setupForm()
{
    // 设置窗体属性
    setWindowFlags(Qt::FramelessWindowHint | Qt::WindowStaysOnTopHint | Qt::Tool);
    setAttribute(Qt::WA_TranslucentBackground);
    setAttribute(Qt::WA_ShowWithoutActivating);
    
    // 设置窗体大小
    resize(350, 131);
    
    // 设置窗体位置到屏幕正中间下部
    QScreen* screen = QApplication::primaryScreen();
    if (screen) {
        QRect screenGeometry = screen->availableGeometry();
        move((screenGeometry.width() - width()) / 2, 
             screenGeometry.height() - height() - 100);
    }
    
    // 设置鼠标跟踪
    setMouseTracking(true);
}

void RecordingOverlay::initializeWaveData()
{
    for (int i = 0; i < WAVE_COUNT; i++) {
        m_waveHeights[i] = m_random->bounded(10, 50);
    }
}

void RecordingOverlay::startAnimation()
{
    m_animationTimer->start(50); // 20 FPS
}

void RecordingOverlay::stopAnimation()
{
    if (m_animationTimer) {
        m_animationTimer->stop();
    }
}

void RecordingOverlay::updateText(const QString& text)
{
    if (!text.trimmed().isEmpty()) {
        m_currentText = text;
        update(); // 触发重绘
    }
}

void RecordingOverlay::paintEvent(QPaintEvent* event)
{
    Q_UNUSED(event)
    
    QPainter painter(this);
    painter.setRenderHint(QPainter::Antialiasing);
    
    // 绘制背景
    QBrush backgroundBrush(QColor(0, 0, 0, 180));
    painter.setBrush(backgroundBrush);
    painter.setPen(Qt::NoPen);
    drawRoundedRect(painter, QRect(10, 10, width() - 20, height() - 20), 15);
    
    // 绘制"正在录音"文字
    QFont font("Microsoft YaHei", 12, QFont::Bold);
    painter.setFont(font);
    painter.setPen(QColor(255, 255, 255));
    
    QFontMetrics fontMetrics(font);
    QRect textRect = fontMetrics.boundingRect(m_currentText);
    int textX = (width() - textRect.width()) / 2;
    painter.drawText(textX, 30, m_currentText);
    
    // 绘制音波
    drawWaveform(painter);
}

void RecordingOverlay::drawWaveform(QPainter& painter)
{
    int waveY = height() - 35;
    int waveWidth = width() - 40;
    float barWidth = static_cast<float>(waveWidth) / WAVE_COUNT;
    
    QBrush waveBrush(QColor(0, 255, 0, 100));
    painter.setBrush(waveBrush);
    painter.setPen(Qt::NoPen);
    
    for (int i = 0; i < WAVE_COUNT; i++) {
        float x = 20 + i * barWidth;
        float height = m_waveHeights[i] * (0.5f + 0.5f * qSin(qDegreesToRadians(static_cast<float>(m_waveOffset + i * 10))));
        float y = waveY - height / 2;
        
        QRectF barRect(x, y, barWidth - 2, height);
        painter.drawRect(barRect);
    }
}

void RecordingOverlay::drawRoundedRect(QPainter& painter, const QRect& rect, int radius)
{
    QPainterPath path;
    path.addRoundedRect(rect, radius, radius);
    painter.fillPath(path, painter.brush());
}

void RecordingOverlay::mousePressEvent(QMouseEvent* event)
{
    if (event->button() == Qt::LeftButton) {
        m_isDragging = true;
        m_dragStartPoint = event->pos();
        setCursor(Qt::SizeAllCursor);
    }
    QWidget::mousePressEvent(event);
}

void RecordingOverlay::mouseMoveEvent(QMouseEvent* event)
{
    if (m_isDragging && (event->buttons() & Qt::LeftButton)) {
        QPoint newPosition = pos() + event->pos() - m_dragStartPoint;
        move(newPosition);
    }
    QWidget::mouseMoveEvent(event);
}

void RecordingOverlay::mouseReleaseEvent(QMouseEvent* event)
{
    if (event->button() == Qt::LeftButton) {
        m_isDragging = false;
        setCursor(Qt::ArrowCursor);
    }
    QWidget::mouseReleaseEvent(event);
}

void RecordingOverlay::closeEvent(QCloseEvent* event)
{
    stopAnimation();
    QWidget::closeEvent(event);
}

void RecordingOverlay::onAnimationTimer()
{
    m_waveOffset += 2;
    
    // 随机更新波形高度
    for (int i = 0; i < WAVE_COUNT; i++) {
        if (m_random->bounded(0, 10) < 3) { // 30%概率更新
            m_waveHeights[i] = m_random->bounded(10, 50);
        }
    }
    
    update(); // 触发重绘
}