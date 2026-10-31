using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// 再生位置の表示と変更を行うシークバー。
    /// クリックした位置へ直接移動でき、ドラッグ中は表示だけが動き、マウスを離したときに
    /// <see cref="SeekRequested"/> が発生する。
    /// </summary>
    public class SeekBar : Control
    {
        private const int TrackHeight = 4;
        private const int ThumbRadius = 8;

        private static readonly Color LoopColor = Color.FromArgb(224, 112, 32);

        private int _maximum;
        private int _value;
        private int _loopStart = -1;
        private int _loopEnd = -1;
        private bool _dragging;

        /// <summary>マウスが乗っているか（つまみの中の円を少し大きくする）。</summary>
        private bool _hover;

        public SeekBar()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.UserPaint,
                true);
            SetStyle(ControlStyles.Selectable, false);
        }

        /// <summary>ユーザーが再生位置の変更を確定したときに発生する。</summary>
        [Category("Action")]
        [Description("ユーザーが再生位置の変更を確定したときに発生します。")]
        public event EventHandler SeekRequested;

        /// <summary>最大値（ミリ秒）。</summary>
        [Category("Behavior")]
        [DefaultValue(0)]
        public int Maximum
        {
            get => _maximum;
            set
            {
                value = Math.Max(0, value);
                if (_maximum == value)
                {
                    return;
                }
                _maximum = value;
                _value = Math.Min(_value, _maximum);
                Invalidate();
            }
        }

        /// <summary>現在の値（ミリ秒）。</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Value
        {
            get => _value;
            set
            {
                value = Math.Max(0, Math.Min(_maximum, value));
                if (_value == value)
                {
                    return;
                }
                _value = value;
                Invalidate();
            }
        }

        /// <summary>A-B 区間ループの開始位置（ミリ秒）。設定していなければ -1。</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int LoopStart
        {
            get => _loopStart;
            set
            {
                if (_loopStart != value)
                {
                    _loopStart = value;
                    Invalidate();
                }
            }
        }

        /// <summary>A-B 区間ループの終了位置（ミリ秒）。設定していなければ -1。</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int LoopEnd
        {
            get => _loopEnd;
            set
            {
                if (_loopEnd != value)
                {
                    _loopEnd = value;
                    Invalidate();
                }
            }
        }

        /// <summary>ユーザーがつまみをドラッグしている最中かどうか。</summary>
        [Browsable(false)]
        public bool IsDragging => _dragging;

        protected override Size DefaultSize => new Size(200, 20);

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || _maximum <= 0)
            {
                return;
            }
            _dragging = true;
            Capture = true;
            Value = ValueFromX(e.X);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging)
            {
                Value = ValueFromX(e.X);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_dragging && e.Button == MouseButtons.Left)
            {
                EndDrag();
            }
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (_dragging)
            {
                EndDrag();
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int left = ThumbRadius;
            int width = ClientSize.Width - ThumbRadius * 2;
            if (width <= 0)
            {
                return;
            }

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int centerY = ClientSize.Height / 2;
            Theme theme = Theme.Current;
            var track = new RectangleF(left, centerY - TrackHeight / 2f, width, TrackHeight);
            using (var trackBrush = new SolidBrush(theme.TrackBack))
            {
                FillTrack(g, trackBrush, track);
            }
            if (_maximum <= 0)
            {
                return;
            }

            // 溝の再生済みの部分を強調の色で塗り、つまみは外側の円の中に強調の色の円を描く（Windows 11 のスライダーに近い見た目）
            float x = XFromValue(_value, left, width);
            Color accent = Enabled ? theme.Accent : theme.DisabledText;
            using (var accentBrush = new SolidBrush(accent))
            using (var thumbBrush = new SolidBrush(theme.ThumbBack))
            using (var thumbPen = new Pen(theme.Border))
            {
                FillTrack(g, accentBrush, new RectangleF(left, track.Top, x - left, TrackHeight));
                DrawLoopRange(g, left, width, centerY);
                var thumb = new RectangleF(x - ThumbRadius, centerY - ThumbRadius, ThumbRadius * 2, ThumbRadius * 2);
                g.FillEllipse(thumbBrush, thumb);
                g.DrawEllipse(thumbPen, thumb);
                float inner = _dragging || _hover ? ThumbRadius * 0.62f : ThumbRadius * 0.5f;
                g.FillEllipse(accentBrush, x - inner, centerY - inner, inner * 2, inner * 2);
            }
        }

        /// <summary>溝（両端の丸い横棒）を塗る。</summary>
        private static void FillTrack(Graphics g, Brush brush, RectangleF bounds)
        {
            if (bounds.Width <= 0)
            {
                return;
            }
            using (GraphicsPath path = FlatPainter.RoundedRectangle(bounds, bounds.Height / 2))
            {
                g.FillPath(brush, path);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            Invalidate();
        }

        /// <summary>
        /// A-B 区間を、つまみの上下にはみ出す帯で示す。B が未設定なら A の位置に線だけを引く。
        /// </summary>
        private void DrawLoopRange(Graphics g, int left, int width, int centerY)
        {
            if (_loopStart < 0)
            {
                return;
            }

            Color color = Color.FromArgb(Enabled ? 255 : 128, LoopColor);
            float start = XFromValue(_loopStart, left, width);
            int top = centerY - ThumbRadius;
            int height = ThumbRadius * 2;
            using (var pen = new Pen(color, 2))
            {
                g.DrawLine(pen, start, top, start, top + height);
                if (_loopEnd < 0)
                {
                    return;
                }
                float end = XFromValue(_loopEnd, left, width);
                g.DrawLine(pen, end, top, end, top + height);
                using (var brush = new SolidBrush(Color.FromArgb(70, color)))
                {
                    g.FillRectangle(brush, start, top, end - start, height);
                }
            }
        }

        private float XFromValue(int value, int left, int width)
        {
            return left + width * (float)Math.Min(value, _maximum) / _maximum;
        }

        private void EndDrag()
        {
            _dragging = false;
            Capture = false;
            SeekRequested?.Invoke(this, EventArgs.Empty);
        }

        private int ValueFromX(int x)
        {
            int width = Math.Max(1, ClientSize.Width - ThumbRadius * 2);
            double ratio = (x - ThumbRadius) / (double)width;
            ratio = Math.Max(0, Math.Min(1, ratio));
            return (int)Math.Round(ratio * _maximum);
        }
    }
}
