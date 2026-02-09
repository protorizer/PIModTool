using System;
using System.Collections.Generic;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Document;
using System.Windows;

// Disclaimer I don't know what the hell this does

namespace PIModTool.Wpf.Services
{
    public interface ITextMarker
    {
        int StartOffset { get; }
        int Length { get; }
        Color Color { get; set; }
        string ToolTip { get; set; }
        void Delete();
    }

    public sealed class SyntaxMarkerService : IBackgroundRenderer, ITextViewConnect
    {
        private readonly TextSegmentCollection<TextMarker> markers;
        private readonly TextView textView;

        public SyntaxMarkerService(TextView textView)
        {
            this.textView = textView;
            markers = new TextSegmentCollection<TextMarker>(textView.Document);
            textView.BackgroundRenderers.Add(this);
            textView.Services.AddService(typeof(SyntaxMarkerService), this);
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (markers == null || !textView.VisualLinesValid)
                return;

            foreach (var marker in markers)
            {
                foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, marker))
                {
                    var start = rect.BottomLeft;
                    var end = rect.BottomRight;

                    var pen = new Pen(new SolidColorBrush(marker.Color), 1.0);
                    pen.Freeze();
                    const double offset = 2.5;
                    var count = Math.Max((int)((end.X - start.X) / offset), 4);
                    var geometry = new StreamGeometry();

                    using (var ctx = geometry.Open())
                    {
                        ctx.BeginFigure(start, false, false);
                        bool up = true;
                        for (int i = 0; i < count; i++)
                        {
                            var x = start.X + i * offset;
                            var y = start.Y + (up ? -offset : offset);
                            ctx.LineTo(new Point(x, y), true, false);
                            up = !up;
                        }
                        ctx.LineTo(end, true, false);
                    }

                    geometry.Freeze();
                    drawingContext.DrawGeometry(null, pen, geometry);
                }
            }
        }

        public KnownLayer Layer => KnownLayer.Selection;

        public ITextMarker Create(int startOffset, int length, Color color, string tooltip = null)
        {
            var marker = new TextMarker(this, startOffset, length)
            {
                Color = color,
                ToolTip = tooltip
            };
            markers.Add(marker);
            return marker;
        }

        public void RemoveAll()
        {
            markers.Clear();
            textView.InvalidateLayer(Layer);
        }

        private sealed class TextMarker : TextSegment, ITextMarker
        {
            private readonly SyntaxMarkerService service;
            public Color Color { get; set; }
            public string ToolTip { get; set; }

            public TextMarker(SyntaxMarkerService service, int startOffset, int length)
            {
                this.service = service;
                StartOffset = startOffset;
                Length = length;
            }

            public void Delete()
            {
                service.markers.Remove(this);
                service.textView.InvalidateLayer(service.Layer);
            }
        }

        public void AddToTextView(TextView view) { }
        public void RemoveFromTextView(TextView view) { }
    }
}
