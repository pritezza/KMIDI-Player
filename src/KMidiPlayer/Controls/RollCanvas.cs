using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// ピアノロールの描き先。描き方（GDI+ か、WPF による GPU か）を隠して、同じ描画のコードで描けるようにする。
    /// 座標は画素単位（コントロールの左上が 0, 0）。
    /// </summary>
    internal abstract class RollCanvas
    {
        /// <summary>
        /// 斜めの線や図形の縁をなめらかに描くか（初期値 false）。
        /// false のとき、軸に平行な四角と線は画素の境目にそろえて描く（GPU で描くときに、にじまないように）。
        /// </summary>
        public bool AntiAlias { get; set; }

        /// <summary>GPU で描いているか（文字の大きさの測り方が違うので、測った結果を取っておくときの区別に使う）。</summary>
        public abstract bool IsGpu { get; }

        public abstract void Clear(Color color);

        public abstract void FillRectangle(Color color, float x, float y, float width, float height);

        public void FillRectangle(Color color, RectangleF rect) => FillRectangle(color, rect.X, rect.Y, rect.Width, rect.Height);

        /// <summary>1px の線で四角の枠を描く（右と下の線は x + width、y + height の画素に引く。GDI+ の DrawRectangle と同じ）。</summary>
        public abstract void DrawRectangle(Color color, float x, float y, float width, float height);

        public abstract void DrawLine(Color color, float width, float x1, float y1, float x2, float y2);

        public void DrawLine(Color color, float width, PointF from, PointF to) => DrawLine(color, width, from.X, from.Y, to.X, to.Y);

        public abstract void FillPolygon(Color color, PointF[] points);

        /// <summary>多角形を、gradientStart（startColor）から gradientEnd（endColor）へのグラデーションで塗る。</summary>
        public abstract void FillPolygon(PointF[] points, PointF gradientStart, Color startColor, PointF gradientEnd, Color endColor);

        public abstract void FillEllipse(Color color, float x, float y, float width, float height);

        public abstract void DrawEllipse(Color color, float penWidth, float x, float y, float width, float height);

        /// <summary>中心のまわりの、半径 inner～outer の輪を塗る（inner が 0 なら円）。</summary>
        public abstract void FillRing(Color color, PointF center, float inner, float outer);

        /// <summary>
        /// 中心のまわりの、半径 inner～outer・角度 start（度、右が 0 で時計回り）から sweep 分の扇形の帯を塗る。
        /// outline が <see cref="Color.Empty"/> でなければ、1px の線で縁取る。
        /// </summary>
        public abstract void FillSector(Color color, Color outline, PointF center, float inner, float outer, float start, float sweep);

        /// <summary>文字の大きさ（余白なし）。</summary>
        public abstract Size MeasureText(string text, Font font);

        /// <summary>文字を、左上を (x, y) に合わせて書く（余白なし）。</summary>
        public abstract void DrawText(string text, Font font, Color color, float x, float y);

        /// <summary>文字を、範囲の中に寄せて書く（範囲からはみ出す部分は切る）。</summary>
        public void DrawText(string text, Font font, Color color, RectangleF bounds, ContentAlignment alignment)
        {
            Size size = MeasureText(text, font);
            float x;
            switch (alignment)
            {
                case ContentAlignment.TopCenter:
                case ContentAlignment.MiddleCenter:
                case ContentAlignment.BottomCenter:
                    x = bounds.Left + (bounds.Width - size.Width) / 2;
                    break;
                case ContentAlignment.TopRight:
                case ContentAlignment.MiddleRight:
                case ContentAlignment.BottomRight:
                    x = bounds.Right - size.Width;
                    break;
                default:
                    x = bounds.Left;
                    break;
            }
            float y;
            switch (alignment)
            {
                case ContentAlignment.MiddleLeft:
                case ContentAlignment.MiddleCenter:
                case ContentAlignment.MiddleRight:
                    y = bounds.Top + (bounds.Height - size.Height) / 2;
                    break;
                case ContentAlignment.BottomLeft:
                case ContentAlignment.BottomCenter:
                case ContentAlignment.BottomRight:
                    y = bounds.Bottom - size.Height;
                    break;
                default:
                    y = bounds.Top;
                    break;
            }

            bool overflows = size.Width > bounds.Width || size.Height > bounds.Height;
            if (overflows)
            {
                PushClip(bounds);
            }
            DrawText(text, font, color, (float)Math.Round(x), (float)Math.Round(y));
            if (overflows)
            {
                PopClip();
            }
        }

        /// <summary>これから描く範囲を rect の中に限る。<see cref="PopClip"/> で戻す。</summary>
        public abstract void PushClip(RectangleF rect);

        public abstract void PopClip();
    }

    /// <summary>GDI+（CPU）で描く描き先。今までと同じ描き方。</summary>
    internal sealed class GdiRollCanvas : RollCanvas, IDisposable
    {
        private readonly Graphics _g;
        private readonly Stack<Region> _clips = new Stack<Region>();

        // 描く間に使う筆とペン（色・太さごと）。描き終わったら破棄する
        private readonly Dictionary<int, SolidBrush> _brushes = new Dictionary<int, SolidBrush>();
        private readonly Dictionary<(int, float), Pen> _pens = new Dictionary<(int, float), Pen>();
        private readonly GraphicsPath _path = new GraphicsPath();

        public GdiRollCanvas(Graphics g)
        {
            _g = g;
        }

        public override bool IsGpu => false;

        public void Dispose()
        {
            foreach (SolidBrush brush in _brushes.Values)
            {
                brush.Dispose();
            }
            foreach (Pen pen in _pens.Values)
            {
                pen.Dispose();
            }
            _path.Dispose();
            foreach (Region region in _clips)
            {
                region.Dispose();
            }
        }

        public override void Clear(Color color) => _g.Clear(color);

        public override void FillRectangle(Color color, float x, float y, float width, float height)
        {
            Prepare();
            _g.FillRectangle(Brush(color), x, y, width, height);
        }

        public override void DrawRectangle(Color color, float x, float y, float width, float height)
        {
            Prepare();
            _g.DrawRectangle(Pen(color, 1), x, y, width, height);
        }

        public override void DrawLine(Color color, float width, float x1, float y1, float x2, float y2)
        {
            Prepare();
            _g.DrawLine(Pen(color, width), x1, y1, x2, y2);
        }

        public override void FillPolygon(Color color, PointF[] points)
        {
            Prepare();
            _g.FillPolygon(Brush(color), points);
        }

        public override void FillPolygon(PointF[] points, PointF gradientStart, Color startColor, PointF gradientEnd, Color endColor)
        {
            Prepare();
            using (var brush = new LinearGradientBrush(gradientStart, gradientEnd, startColor, endColor))
            {
                _g.FillPolygon(brush, points);
            }
        }

        public override void FillEllipse(Color color, float x, float y, float width, float height)
        {
            Prepare();
            _g.FillEllipse(Brush(color), x, y, width, height);
        }

        public override void DrawEllipse(Color color, float penWidth, float x, float y, float width, float height)
        {
            Prepare();
            _g.DrawEllipse(Pen(color, penWidth), x, y, width, height);
        }

        public override void FillRing(Color color, PointF center, float inner, float outer)
        {
            Prepare();
            _path.Reset();
            _path.AddEllipse(center.X - outer, center.Y - outer, outer * 2, outer * 2);
            if (inner > 0.5f)
            {
                _path.AddEllipse(center.X - inner, center.Y - inner, inner * 2, inner * 2); // 内側は塗らない（交互塗り）
            }
            _g.FillPath(Brush(color), _path);
        }

        public override void FillSector(Color color, Color outline, PointF center, float inner, float outer, float start, float sweep)
        {
            Prepare();
            _path.Reset();
            _path.AddArc(center.X - outer, center.Y - outer, outer * 2, outer * 2, start, sweep);
            if (inner < 0.5f)
            {
                double radians = (start + sweep) * Math.PI / 180;
                _path.AddLine(center.X + outer * (float)Math.Cos(radians), center.Y + outer * (float)Math.Sin(radians), center.X, center.Y);
            }
            else
            {
                _path.AddArc(center.X - inner, center.Y - inner, inner * 2, inner * 2, start + sweep, -sweep);
            }
            _path.CloseFigure();
            _g.FillPath(Brush(color), _path);
            if (!outline.IsEmpty)
            {
                _g.DrawPath(Pen(outline, 1), _path);
            }
        }

        public override Size MeasureText(string text, Font font)
        {
            return TextRenderer.MeasureText(_g, text, font, Size.Empty, TextFormatFlags.NoPadding);
        }

        public override void DrawText(string text, Font font, Color color, float x, float y)
        {
            TextRenderer.DrawText(_g, text, font, new Point((int)x, (int)y), color, TextFormatFlags.NoPadding);
        }

        public override void PushClip(RectangleF rect)
        {
            _clips.Push(_g.Clip);
            _g.SetClip(rect, CombineMode.Intersect);
        }

        public override void PopClip()
        {
            using (Region region = _clips.Pop())
            {
                _g.Clip = region;
            }
        }

        private void Prepare()
        {
            SmoothingMode mode = AntiAlias ? SmoothingMode.AntiAlias : SmoothingMode.Default;
            if (_g.SmoothingMode != mode)
            {
                _g.SmoothingMode = mode;
            }
        }

        private SolidBrush Brush(Color color)
        {
            int key = color.ToArgb();
            if (!_brushes.TryGetValue(key, out SolidBrush brush))
            {
                brush = new SolidBrush(color);
                _brushes.Add(key, brush);
            }
            return brush;
        }

        private Pen Pen(Color color, float width)
        {
            var key = (color.ToArgb(), width);
            if (!_pens.TryGetValue(key, out Pen pen))
            {
                pen = new Pen(color, width);
                _pens.Add(key, pen);
            }
            return pen;
        }
    }
}
