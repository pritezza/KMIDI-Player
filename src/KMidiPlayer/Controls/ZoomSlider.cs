using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// 拡大率を選ぶスライダー（ピアノロールの時間方向の倍率用）。目盛りは対数で、どの倍率でも同じ感覚で動かせる。
    /// </summary>
    /// <remarks>
    /// クリック・ドラッグ、マウスホイール・← → ↑ ↓ キー（1 段ずつ）で変え、ダブルクリックと Home キーで <see cref="ResetValue"/> に戻す。
    /// ドラッグ中は <see cref="ResetValue"/> の近くで吸い付く。操作で値が変わったときに <see cref="ValueChanged"/> が発生する。
    /// </remarks>
    public class ZoomSlider : Control, IThemeable
    {
        private const int TrackHeight = 4;
        private const int ThumbRadius = 7;

        /// <summary>ドラッグ中、既定値の目盛りにこれより近ければ、既定値にそろえる（px）。</summary>
        private const int SnapDistance = 4;

        private double _minimum = 20;
        private double _maximum = 800;
        private double _value = 100;
        private bool _dragging;
        private bool _hover;
        private int _wheelDelta;

        public ZoomSlider()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint | ControlStyles.Selectable | ControlStyles.StandardDoubleClick, true);
            TabStop = true;
        }

        /// <summary>操作で値が変わったときに発生する（<see cref="Value"/> に代入したときは発生しない）。</summary>
        [Category("Action")]
        public event EventHandler ValueChanged;

        [Category("Behavior")]
        [DefaultValue(20.0)]
        public double Minimum
        {
            get => _minimum;
            set
            {
                _minimum = value;
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(800.0)]
        public double Maximum
        {
            get => _maximum;
            set
            {
                _maximum = value;
                Invalidate();
            }
        }

        /// <summary>目盛りの印を付け、ダブルクリックで戻す値。</summary>
        [Category("Behavior")]
        [DefaultValue(100.0)]
        public double ResetValue { get; set; } = 100;

        /// <summary>ホイール・矢印キー 1 回で、値に掛ける（割る）倍率。</summary>
        [Category("Behavior")]
        [DefaultValue(1.25)]
        public double Step { get; set; } = 1.25;

        [Category("Behavior")]
        [DefaultValue(100.0)]
        public double Value
        {
            get => _value;
            set
            {
                value = Clamp(value);
                if (value != _value)
                {
                    _value = value;
                    Invalidate();
                }
            }
        }

        protected override Size DefaultSize => new Size(120, 22);

        public void ApplyTheme()
        {
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Theme theme = Theme.Current;
            Graphics g = e.Graphics;
            g.Clear(Parent?.BackColor ?? theme.Background);
            int left = ThumbRadius + 1;
            int width = ClientSize.Width - left * 2;
            if (width <= 0)
            {
                return;
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;
            float centerY = ClientSize.Height / 2f;
            float x = left + width * PositionOf(_value);
            Color accent = Enabled ? theme.Accent : theme.DisabledText;
            using (var trackBrush = new SolidBrush(theme.TrackBack))
            using (var accentBrush = new SolidBrush(accent))
            using (var tickPen = new Pen(theme.MutedText))
            using (var thumbBrush = new SolidBrush(theme.ThumbBack))
            using (var thumbPen = new Pen(Focused && ShowFocusCues ? theme.Accent : theme.Border))
            {
                FillTrack(g, trackBrush, new RectangleF(left, centerY - TrackHeight / 2f, width, TrackHeight));
                FillTrack(g, accentBrush, new RectangleF(left, centerY - TrackHeight / 2f, x - left, TrackHeight));

                // 既定値（100%）の目盛り
                float tick = left + width * PositionOf(ResetValue);
                g.DrawLine(tickPen, tick, centerY + TrackHeight / 2f + 2, tick, centerY + TrackHeight / 2f + 5);

                var thumb = new RectangleF(x - ThumbRadius, centerY - ThumbRadius, ThumbRadius * 2, ThumbRadius * 2);
                g.FillEllipse(thumbBrush, thumb);
                g.DrawEllipse(thumbPen, thumb);
                float inner = _dragging || _hover ? ThumbRadius * 0.62f : ThumbRadius * 0.5f;
                g.FillEllipse(accentBrush, x - inner, centerY - inner, inner * 2, inner * 2);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }
            Focus();
            _dragging = true;
            Capture = true;
            SetValueByUser(ValueAtX(e.X));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging)
            {
                SetValueByUser(ValueAtX(e.X));
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                EndDrag();
            }
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (_dragging && !Capture)
            {
                EndDrag();
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (e.Button == MouseButtons.Left)
            {
                SetValueByUser(ResetValue);
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

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (e is HandledMouseEventArgs handled)
            {
                handled.Handled = true;
            }
            _wheelDelta += e.Delta;
            int steps = _wheelDelta / 120;
            _wheelDelta -= steps * 120;
            if (steps != 0)
            {
                SetValueByUser(_value * Math.Pow(Step, steps)); // 上に回すと拡大
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                case Keys.Home:
                    return true;
                default:
                    return base.IsInputKey(keyData);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyData)
            {
                case Keys.Right:
                case Keys.Up:
                    SetValueByUser(_value * Step);
                    e.Handled = true;
                    break;
                case Keys.Left:
                case Keys.Down:
                    SetValueByUser(_value / Step);
                    e.Handled = true;
                    break;
                case Keys.Home:
                    SetValueByUser(ResetValue);
                    e.Handled = true;
                    break;
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        private void SetValueByUser(double value)
        {
            value = Clamp(value);
            if (value == _value)
            {
                return;
            }
            _value = value;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        private void EndDrag()
        {
            if (!_dragging)
            {
                return;
            }
            _dragging = false;
            Capture = false;
            Invalidate();
        }

        /// <summary>値の、スライダーの左端（0）から右端（1）までの位置（対数の目盛り）。</summary>
        private float PositionOf(double value)
        {
            if (_maximum <= _minimum)
            {
                return 0;
            }
            return (float)(Math.Log(Clamp(value) / _minimum) / Math.Log(_maximum / _minimum));
        }

        /// <summary>横位置の値。既定値の目盛りの近くなら、既定値にそろえる。</summary>
        private double ValueAtX(int x)
        {
            int left = ThumbRadius + 1;
            int width = Math.Max(1, ClientSize.Width - left * 2);
            float tick = left + width * PositionOf(ResetValue);
            if (Math.Abs(x - tick) <= SnapDistance)
            {
                return ResetValue;
            }
            double position = Math.Max(0, Math.Min(1, (x - left) / (double)width));
            return _minimum * Math.Pow(_maximum / _minimum, position);
        }

        private double Clamp(double value)
        {
            return double.IsNaN(value) ? ResetValue : Math.Max(_minimum, Math.Min(_maximum, value));
        }

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
    }
}
