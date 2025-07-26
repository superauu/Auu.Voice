#ifndef RECORDINGOVERLAY_H
#define RECORDINGOVERLAY_H

#include <QWidget>
#include <QTimer>
#include <QPainter>
#include <QMouseEvent>
#include <QScreen>
#include <QApplication>
#include <QRandomGenerator>
#include <QPropertyAnimation>
#include <QGraphicsOpacityEffect>

class RecordingOverlay : public QWidget
{
    Q_OBJECT
    
public:
    explicit RecordingOverlay(QWidget* parent = nullptr);
    ~RecordingOverlay();
    
    void updateText(const QString& text);
    void startAnimation();
    void stopAnimation();
    
protected:
    void paintEvent(QPaintEvent* event) override;
    void mousePressEvent(QMouseEvent* event) override;
    void mouseMoveEvent(QMouseEvent* event) override;
    void mouseReleaseEvent(QMouseEvent* event) override;
    void closeEvent(QCloseEvent* event) override;
    
private slots:
    void onAnimationTimer();
    
private:
    void setupForm();
    void initializeWaveData();
    void drawWaveform(QPainter& painter);
    void drawRoundedRect(QPainter& painter, const QRect& rect, int radius);
    
    QTimer* m_animationTimer;
    QPoint m_dragStartPoint;
    bool m_isDragging;
    QRandomGenerator* m_random;
    QVector<float> m_waveHeights;
    int m_waveOffset;
    QString m_currentText;
    
    static const int WAVE_COUNT = 20;
};

#endif // RECORDINGOVERLAY_H