using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using Size = System.Drawing.Size;
using WpfColor = System.Windows.Media.Color;
using WpfPoint = System.Windows.Point;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// WPF の描画（GPU）で描く描き先。描いた内容は WPF が GPU で画面に描く。
    /// </summary>
    /// <remarks>
    /// 座標は画素単位で受け取り、WPF の座標（96 DPI 基準）へは最初に縮小の変換をかけて合わせる。
    /// 毎回作ると重いもの（ブラシ・ペン・文字）は <see cref="WpfRollResources"/> に取っておいて使い回す。
    /// </remarks>
    internal sealed class WpfRollCanvas : RollCanvas
    {
        private readonly DrawingContext _dc;
        private readonly WpfRollResources _resources;
        private readonly double _width;
        private readonly double _height;
        private int _pushes;

        /// <param name="pixelsPerDip">画面の DPI ÷ 96。</param>
        public WpfRollCanvas(DrawingContext dc, WpfRollResources resources, double pixelsPerDip, double width, double height)
        {
            _dc = dc;
            _resources = resources;
            _width = width;
            _height = height;
            resources.PixelsPerDip = pixelsPerDip;
            if (pixelsPerDip != 1)
            {
                _dc.PushTransform(new ScaleTransform(1 / pixelsPerDip, 1 / pixelsPerDip));
                _pushes++;
            }
        }

        public override bool IsGpu => true;

        /// <summary>描き終わったときに呼ぶ（変換やクリップを戻す）。</summary>
        public void Finish()
        {
            for (; _pushes > 0; _pushes--)
            {
                _dc.Pop();
            }
            _resources.Trim();
        }

        public override void Clear(Color color)
        {
            _dc.DrawRectangle(_resources.Brush(color), null, new Rect(0, 0, _width, _height));
        }

        public override void FillRectangle(Color color, float x, float y, float width, float height)
        {
            if (width <= 0 || height <= 0)
            {
                return;
            }
            _dc.DrawRectangle(_resources.Brush(color), null, Snap(x, y, width, height));
        }

        public override void DrawRectangle(Color color, float x, float y, float width, float height)
        {
            // GDI+ の 1px の枠は、x～x + width の画素の中央を通る
            Rect rect = AntiAlias ? new Rect(x, y, Math.Max(0, width), Math.Max(0, height))
                : new Rect(Math.Round(x) + 0.5, Math.Round(y) + 0.5, Math.Max(0, Math.Round(width)), Math.Max(0, Math.Round(height)));
            _dc.DrawRectangle(null, _resources.Pen(color, 1), rect);
        }

        public override void DrawLine(Color color, float width, float x1, float y1, float x2, float y2)
        {
            var from = new WpfPoint(x1, y1);
            var to = new WpfPoint(x2, y2);
            if (!AntiAlias)
            {
                // 縦・横の線は、画素にそろえる（奇数の太さは画素の中央を通す）
                double offset = Math.Round(width) % 2 == 1 ? 0.5 : 0;
                if (x1 == x2)
                {
                    from.X = to.X = Math.Round(x1) + offset;
                }
                if (y1 == y2)
                {
                    from.Y = to.Y = Math.Round(y1) + offset;
                }
            }
            _dc.DrawLine(_resources.Pen(color, width), from, to);
        }

        public override void FillPolygon(Color color, PointF[] points)
        {
            _dc.DrawGeometry(_resources.Brush(color), null, Polygon(points));
        }

        public override void FillPolygon(PointF[] points, PointF gradientStart, Color startColor, PointF gradientEnd, Color endColor)
        {
            var brush = new LinearGradientBrush(ToWpf(startColor), ToWpf(endColor),
                new WpfPoint(gradientStart.X, gradientStart.Y), new WpfPoint(gradientEnd.X, gradientEnd.Y))
            {
                MappingMode = BrushMappingMode.Absolute,
            };
            brush.Freeze();
            _dc.DrawGeometry(brush, null, Polygon(points));
        }

        public override void FillEllipse(Color color, float x, float y, float width, float height)
        {
            _dc.DrawEllipse(_resources.Brush(color), null, new WpfPoint(x + width / 2, y + height / 2), width / 2, height / 2);
        }

        public override void DrawEllipse(Color color, float penWidth, float x, float y, float width, float height)
        {
            _dc.DrawEllipse(null, _resources.Pen(color, penWidth), new WpfPoint(x + width / 2, y + height / 2), width / 2, height / 2);
        }

        public override void FillRing(Color color, PointF center, float inner, float outer)
        {
            var geometry = new StreamGeometry { FillRule = FillRule.EvenOdd };
            using (StreamGeometryContext context = geometry.Open())
            {
                AddCircle(context, center, outer);
                if (inner > 0.5f)
                {
                    AddCircle(context, center, inner);
                }
            }
            geometry.Freeze();
            _dc.DrawGeometry(_resources.Brush(color), null, geometry);
        }

        public override void FillSector(Color color, Color outline, PointF center, float inner, float outer, float start, float sweep)
        {
            var geometry = new StreamGeometry();
            using (StreamGeometryContext context = geometry.Open())
            {
                bool isLarge = Math.Abs(sweep) > 180;
                context.BeginFigure(Polar(center, outer, start), true, true);
                context.ArcTo(Polar(center, outer, start + sweep), new System.Windows.Size(outer, outer), 0, isLarge,
                    sweep >= 0 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise, true, false);
                if (inner < 0.5f)
                {
                    context.LineTo(new WpfPoint(center.X, center.Y), true, false);
                }
                else
                {
                    context.LineTo(Polar(center, inner, start + sweep), true, false);
                    context.ArcTo(Polar(center, inner, start), new System.Windows.Size(inner, inner), 0, isLarge,
                        sweep >= 0 ? SweepDirection.Counterclockwise : SweepDirection.Clockwise, true, false);
                }
            }
            geometry.Freeze();
            _dc.DrawGeometry(_resources.Brush(color), outline.IsEmpty ? null : _resources.Pen(outline, 1), geometry);
        }

        public override Size MeasureText(string text, Font font)
        {
            FormattedText formatted = _resources.Text(text, font, Color.Black);
            return new Size((int)Math.Ceiling(formatted.WidthIncludingTrailingWhitespace), (int)Math.Ceiling(formatted.Height));
        }

        public override void DrawText(string text, Font font, Color color, float x, float y)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            _dc.DrawText(_resources.Text(text, font, color), new WpfPoint(x, y));
        }

        public override void PushClip(RectangleF rect)
        {
            _dc.PushClip(new RectangleGeometry(new Rect(rect.X, rect.Y, Math.Max(0, rect.Width), Math.Max(0, rect.Height))));
            _pushes++;
        }

        public override void PopClip()
        {
            _dc.Pop();
            _pushes--;
        }

        internal static WpfColor ToWpf(Color color) => WpfColor.FromArgb(color.A, color.R, color.G, color.B);

        /// <summary>四角を、なめらかに描かないときは画素の境目にそろえる（縁がにじまないように）。</summary>
        private Rect Snap(float x, float y, float width, float height)
        {
            if (AntiAlias)
            {
                return new Rect(x, y, width, height);
            }
            double left = Math.Round(x);
            double top = Math.Round(y);
            double right = Math.Max(left + 1, Math.Round(x + width));
            double bottom = Math.Max(top + 1, Math.Round(y + height));
            return new Rect(left, top, right - left, bottom - top);
        }

        private static StreamGeometry Polygon(PointF[] points)
        {
            var geometry = new StreamGeometry();
            using (StreamGeometryContext context = geometry.Open())
            {
                context.BeginFigure(new WpfPoint(points[0].X, points[0].Y), true, true);
                for (int i = 1; i < points.Length; i++)
                {
                    context.LineTo(new WpfPoint(points[i].X, points[i].Y), false, false);
                }
            }
            geometry.Freeze();
            return geometry;
        }

        private static void AddCircle(StreamGeometryContext context, PointF center, float radius)
        {
            var size = new System.Windows.Size(radius, radius);
            context.BeginFigure(new WpfPoint(center.X + radius, center.Y), true, true);
            context.ArcTo(new WpfPoint(center.X - radius, center.Y), size, 0, false, SweepDirection.Clockwise, true, false);
            context.ArcTo(new WpfPoint(center.X + radius, center.Y), size, 0, false, SweepDirection.Clockwise, true, false);
        }

        private static WpfPoint Polar(PointF center, float radius, float degrees)
        {
            double radians = degrees * Math.PI / 180;
            return new WpfPoint(center.X + radius * Math.Cos(radians), center.Y + radius * Math.Sin(radians));
        }
    }

    /// <summary>
    /// WPF で描くときに使い回すブラシ・ペン・文字（凍結して、描くたびに作らないようにする）。数が増えすぎたら捨てて作り直す。
    /// </summary>
    internal sealed class WpfRollResources
    {
        /// <summary>取っておく数の上限（色を混ぜて作るので、曲や位置によって種類が増え続けることがある）。</summary>
        private const int MaxEntries = 4096;

        private readonly Dictionary<int, SolidColorBrush> _brushes = new Dictionary<int, SolidColorBrush>();
        private readonly Dictionary<(int, float), Pen> _pens = new Dictionary<(int, float), Pen>();
        private readonly Dictionary<(string, Font, int), FormattedText> _texts = new Dictionary<(string, Font, int), FormattedText>();
        private readonly Dictionary<Font, Typeface> _typefaces = new Dictionary<Font, Typeface>();
        private double _pixelsPerDip = 1;

        /// <summary>画面の DPI ÷ 96。変わったら文字を作り直す。</summary>
        public double PixelsPerDip
        {
            get => _pixelsPerDip;
            set
            {
                if (value != _pixelsPerDip)
                {
                    _pixelsPerDip = value;
                    _texts.Clear();
                }
            }
        }

        public SolidColorBrush Brush(Color color)
        {
            int key = color.ToArgb();
            if (!_brushes.TryGetValue(key, out SolidColorBrush brush))
            {
                brush = new SolidColorBrush(WpfRollCanvas.ToWpf(color));
                brush.Freeze();
                _brushes.Add(key, brush);
            }
            return brush;
        }

        public Pen Pen(Color color, float width)
        {
            var key = (color.ToArgb(), width);
            if (!_pens.TryGetValue(key, out Pen pen))
            {
                pen = new Pen(Brush(color), width);
                pen.Freeze();
                _pens.Add(key, pen);
            }
            return pen;
        }

        /// <summary>文字（フォントの大きさは、画素単位に直す）。</summary>
        public FormattedText Text(string text, Font font, Color color)
        {
            var key = (text, font, color.ToArgb());
            if (!_texts.TryGetValue(key, out FormattedText formatted))
            {
                if (!_typefaces.TryGetValue(font, out Typeface typeface))
                {
                    typeface = new Typeface(new FontFamily(font.FontFamily.Name),
                        font.Italic ? FontStyles.Italic : FontStyles.Normal,
                        font.Bold ? FontWeights.Bold : FontWeights.Normal,
                        FontStretches.Normal);
                    _typefaces.Add(font, typeface);
                }
                // 描く座標は画素単位（1 が 1 画素）なので、大きさも画素で渡し、1 画素 = 1 単位として組む
                double emSize = font.SizeInPoints / 72 * 96 * _pixelsPerDip;
                formatted = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, emSize, Brush(color), 1.0);
                _texts.Add(key, formatted);
            }
            return formatted;
        }

        /// <summary>取っておいた物が多すぎたら捨てる（描き終わるたびに呼ぶ）。</summary>
        public void Trim()
        {
            if (_brushes.Count + _pens.Count > MaxEntries)
            {
                _brushes.Clear();
                _pens.Clear();
            }
            if (_texts.Count > MaxEntries)
            {
                _texts.Clear();
            }
        }
    }
}
