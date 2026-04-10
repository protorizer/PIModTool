using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace PIModTool.Wpf.Views.Components
{
    /// <summary>
    /// Interaction logic for TextureView.xaml
    /// </summary>
    public partial class TextureView : UserControl
    {
        // Constants: Maybe turn these into parameters later?
        private const double ZoomStep = 1.15;
        private const double MinZoom = 0.1;
        private const double MaxZoom = 32.0;

        private Point _panOrigin; // This is where the mouse is at the start of the pan
        private Point _panStartPos; // This is where the texture itself is at the start of the pan
        private bool _isPanning;

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source),
            typeof(ImageSource),
            typeof(TextureView),
            new FrameworkPropertyMetadata(null, (d, _) => ((TextureView)d).ResetView())
        );

        public ImageSource Source
        {
            get
            {
                return (ImageSource)GetValue(SourceProperty);
            }
            set
            {
                SetValue(SourceProperty, value);
            }
        }

        public TextureView()
        {
            InitializeComponent();
            ImageTexture.SetBinding(Image.SourceProperty, new Binding(nameof(Source)) { Source = this });
        }

        public void ResetView()
        {
            TextureScale.ScaleX = 1;
            TextureScale.ScaleY = 1;
            TextureTranslation.X = 0;
            TextureTranslation.Y = 0;
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoomFactor = e.Delta > 0 ? ZoomStep : 1.0 / ZoomStep;
            double newScale = Math.Clamp(TextureScale.ScaleX * zoomFactor, MinZoom, MaxZoom);

            Point mousePos = e.GetPosition(ImageTexture);
            double scaleRatio = newScale / TextureScale.ScaleX;

            TextureTranslation.X = mousePos.X - scaleRatio * (mousePos.X - TextureTranslation.X);
            TextureTranslation.Y = mousePos.Y - scaleRatio * (mousePos.Y - TextureTranslation.Y);

            TextureScale.ScaleX = newScale;
            TextureScale.ScaleY = newScale;
            e.Handled = true;
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isPanning = true;
            _panOrigin = e.GetPosition((IInputElement)sender);
            _panStartPos = new Point(TextureTranslation.X, TextureTranslation.Y);
            ((UIElement)sender).CaptureMouse();
            Cursor = Cursors.SizeAll;
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isPanning = false;
            ((UIElement)sender).ReleaseMouseCapture();
            Cursor = Cursors.Arrow;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPanning)
            {
                return;
            }
            Point currentPos = e.GetPosition((IInputElement)sender);
            TextureTranslation.X = _panStartPos.X + (currentPos.X - _panOrigin.X);
            TextureTranslation.Y = _panStartPos.Y + (currentPos.Y - _panOrigin.Y);
        }
    }
}
