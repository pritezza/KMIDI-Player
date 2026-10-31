using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using KMidiPlayer.Midi;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// ベロシティカーブ（-12～+12）を、カーブのグラフで表示し、上下のドラッグで変えるコントロール。
    /// </summary>
    /// <remarks>
    /// 上にドラッグすると +（弱い音を強めに）、下にドラッグすると -（弱い音をより弱く）。
    /// マウスホイールと ↑ ↓ キーで 1 ずつ、ダブルクリックと Home キーで 0 に戻る。
    /// </remarks>
    public class VelocityCurveBox : Control
    {
        /// <summary>ドラッグで値が 1 変わる移動量（px）。</summary>
        private const int DragStep = 3;

        /// <summary>右側の数値の欄の幅。</summary>
        private const int ValueWidth = 26;

        private int _value;

        // ドラッグを始めたときのマウスの Y 座標と値（ドラッグしていなければ _dragStartY は null）
        private int? _dragStartY;
        private int _dragStartValue;

        /// <summary>マウスホイールの、1 目盛りに満たない分。</summary>
        private int _wheelDelta;

        public VelocityCurveBox()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.Selectable,
                true);
            Cursor = Cursors.SizeNS;
            TabStop = true;
        }

        /// <summary>値が操作で変わったときに発生する。</summary>
        [Category("Action")]
        public event EventHandler ValueChanged;

        /// <summary>ベロシティカーブ（<see cref="MidiSequencer.MinVelocityCurve"/>～<see cref="MidiSequencer.MaxVelocityCurve"/>、0 で曲のまま）。</summary>
        [Category("Behavior")]
        [DefaultValue(0)]
        public int Value
        {
            get => _value;
            set
            {
                value = Math.Max(MidiSequencer.MinVelocityCurve, Math.Min(MidiSequencer.MaxVelocityCurve, value));
                if (value != _value)
                {
                    _value = value;
                    Invalidate();
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        protected override Size DefaultSize => new Size(72, 24);

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }
            Focus();
            if (e.Clicks >= 2)
            {
                _dragStartY = null;
                Value = 0;
                return;
            }
            _dragStartY = e.Y;
            _dragStartValue = _value;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragStartY.HasValue)
            {
                // 上に動かすほど +。端数は 0 の方へ切り捨てる（少し動いただけで変わらないように）
                Value = _dragStartValue + (_dragStartY.Value - e.Y) / DragStep;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _dragStartY = null;
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            _dragStartY = null;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            _wheelDelta += e.Delta;
            int steps = _wheelDelta / 120;
            _wheelDelta -= steps * 120;
            Value += steps;
            if (e is HandledMouseEventArgs handled)
            {
                handled.Handled = true; // 親（フォーム）にスクロールさせない
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            return keyData == Keys.Up || keyData == Keys.Down || base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyCode)
            {
                case Keys.Up:
                    Value++;
                    e.Handled = true;
                    break;
                case Keys.Down:
                    Value--;
                    e.Handled = true;
                    break;
                case Keys.Home:
                    Value = 0;
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.Clear(BackColor);

            Theme theme = Theme.Current;
            var graph = new Rectangle(0, 0, Math.Max(1, Width - ValueWidth), Height);
            FlatPainter.PaintField(g, graph, BackColor, Focused);

            // 内側の描画範囲（左下が入力 1・出力 1、右上が入力 127・出力 127）
            Rectangle plot = Rectangle.Inflate(graph, -3, -3);
            if (plot.Width > 1 && plot.Height > 1)
            {
                SmoothingMode oldMode = g.SmoothingMode;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var referencePen = new Pen(theme.Border) { DashStyle = DashStyle.Dot })
                {
                    g.DrawLine(referencePen, plot.Left, plot.Bottom, plot.Right, plot.Top);
                }

                var points = new PointF[plot.Width + 1];
                for (int x = 0; x <= plot.Width; x++)
                {
                    int input = 1 + (int)Math.Round(x * 126.0 / plot.Width);
                    int output = MidiSequencer.ApplyVelocityCurve(input, _value);
                    points[x] = new PointF(plot.Left + x, plot.Bottom - (output - 1) * plot.Height / 126f);
                }
                Color curveColor = Enabled ? theme.Accent : theme.DisabledText;
                using (var curvePen = new Pen(curveColor, 2f))
                {
                    g.DrawLines(curvePen, points);
                }
                g.SmoothingMode = oldMode;
            }

            string text = _value > 0 ? "+" + _value : _value.ToString();
            var valueBounds = new Rectangle(graph.Right, 0, Width - graph.Right, Height);
            TextRenderer.DrawText(g, text, Font, valueBounds, Enabled ? theme.Text : theme.DisabledText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }
}
