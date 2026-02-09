using PIModTool.Core.Utilities;
using System.Windows;
using System.Windows.Media;

namespace PIModTool.Wpf.Views.Components
{
    public class AudioWaveformView: FrameworkElement
    {
        public static readonly DependencyProperty PeaksProperty = DependencyProperty.Register(
            nameof(Peaks), 
            typeof(WaveformPeak[]), 
            typeof(AudioWaveformView), 
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty PlayheadPositionProperty =
        DependencyProperty.Register(
            nameof(PlayheadPosition),
            typeof(double),
            typeof(AudioWaveformView),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty IsPlayingProperty =
        DependencyProperty.Register(
            nameof(IsPlaying),
            typeof(bool),
            typeof(AudioWaveformView),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public bool IsPlaying {
            get
            {
                return (bool)GetValue(IsPlayingProperty);
            }
            set
            {
                SetValue(IsPlayingProperty, value);
            }
        }

        public WaveformPeak[] Peaks
        {
            get
            {
                return (WaveformPeak[])GetValue(PeaksProperty);
            }
            set
            {
                SetValue(PeaksProperty, value);
            }
        }

        public double PlayheadPosition
        {
            get
            {
                return (double)GetValue(PlayheadPositionProperty);
            }
            set
            {
                SetValue(PlayheadPositionProperty, value);
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (Peaks == null || Peaks.Length == 0)
            {
                return;
            }

            double width = ActualWidth;
            double height = ActualHeight;
            double midHeight = height / 2;

            Pen waveformPen = new Pen(IsPlaying ? Brushes.DarkOrange : Brushes.DodgerBlue, 1);
            waveformPen.Freeze();

            double xStep = width / Peaks.Length;

            // Draw peaks
            for (int i = 0; i < Peaks.Length; i++)
            {
                double xPos = i * xStep;
                double minPos = midHeight - (Peaks[i].Max * midHeight);
                double maxPos = midHeight - (Peaks[i].Min * midHeight);
                drawingContext.DrawLine(waveformPen, new Point(xPos, minPos), new Point(xPos, maxPos));
            }

            // TODO: add playhead?
        }
    }
}
