using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// マウスホイール 1 目盛りで、値を <see cref="NumericUpDown.Increment"/> 1 回分だけ増減する NumericUpDown。
    /// 数値の欄を押したまま上下にドラッグしても、値を増減できる。
    /// </summary>
    /// <remarks>
    /// 標準の NumericUpDown は、Windows の「一度にスクロールする行数」（初期値 3）回分ずつ増減する。
    /// ドラッグは、押してから少し動かしたときに始める（動かさずに離せば、普通のクリックとして文字の入力ができる）。
    /// ドラッグ中はマウスをこのコントロール自身が受け取り、数値の欄で文字が選択されないようにする。
    /// </remarks>
    public class StepNumericUpDown : NumericUpDown, IThemeable
    {
        private const int WheelDelta = 120; // ホイール 1 目盛り分

        /// <summary>ドラッグで値が 1 段変わる移動量（px）。</summary>
        private const int DragStep = 4;

        /// <summary>これだけ動かしたら、クリックではなくドラッグとして扱う（px）。</summary>
        private const int DragThreshold = 3;

        /// <summary>数値の欄（NumericUpDown の中の TextBox）。</summary>
        private readonly TextBox _edit;

        // 高精度のタッチパッドなどで、1 目盛りに満たない回転が続けて届いた分
        private int _wheelRemainder;

        // 数値の欄でボタンを押したときの Y 座標（このコントロールの座標）。押していなければ null
        private int? _pressY;
        private bool _dragging;

        /// <summary>ドラッグを始めたときの値。</summary>
        private decimal _dragStartValue;

        private UpDownButtonsPainter _buttonsPainter;

        public StepNumericUpDown()
        {
            // 数値の欄の MouseDown・MouseUp は、欄自身では発生せず、このコントロールの OnMouseDown・OnMouseUp に
            // （このコントロールの座標で）回ってくる。MouseMove だけは欄自身で発生する
            _edit = Controls.OfType<TextBox>().FirstOrDefault();
            if (_edit != null)
            {
                _edit.MouseMove += Edit_MouseMove;
                _edit.MouseCaptureChanged += Edit_MouseCaptureChanged;
                _edit.GotFocus += (sender, e) => Invalidate();
                _edit.LostFocus += (sender, e) => Invalidate();
            }

            // ▲▼ のボタン（数値の欄でない方の子）は、Windows の配色で描かれるので、描いた後に上から描き直す
            Control buttons = Controls.Cast<Control>().FirstOrDefault(control => control != _edit);
            if (buttons != null)
            {
                _buttonsPainter = new UpDownButtonsPainter(buttons);
            }
            BorderStyle = BorderStyle.FixedSingle;
            ApplyTheme();
        }

        public void ApplyTheme()
        {
            BackColor = Theme.Current.ControlFill; // 数値の欄にも引き継がれる
            ForeColor = Theme.Current.Text;
            Invalidate(true);
        }

        /// <summary>枠を、配色の 1px の線で描き直す（標準の枠は Windows の配色で描かれるため）。</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Theme theme = Theme.Current;
            Color border = !Enabled ? theme.Divider : ContainsFocus ? theme.Accent : theme.Border;
            using (var pen = new Pen(border))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _buttonsPainter?.ReleaseHandle();
                _buttonsPainter = null;
            }
            base.Dispose(disposing);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            bool onEdit = _edit != null && _edit.Bounds.Contains(e.Location);
            _pressY = e.Button == MouseButtons.Left && onEdit ? e.Y : (int?)null;
            _dragging = false;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (e is HandledMouseEventArgs handled)
            {
                handled.Handled = true;
            }

            _wheelRemainder += e.Delta;
            while (_wheelRemainder >= WheelDelta)
            {
                UpButton();
                _wheelRemainder -= WheelDelta;
            }
            while (_wheelRemainder <= -WheelDelta)
            {
                DownButton();
                _wheelRemainder += WheelDelta;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging)
            {
                StepTo(e.Y);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_dragging)
                {
                    EndDrag();
                }
                _pressY = null;
            }
            base.OnMouseUp(e);
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (_dragging && !Capture)
            {
                EndDrag(); // ウィンドウの切り替えなどで、離したことが届かなかった
            }
        }

        /// <summary>
        /// 押している間は数値の欄がマウスを取り込んでいる。ドラッグを始めずに取り込みが外れたら（ウィンドウの切り替えなど）、押していないことにする。
        /// </summary>
        private void Edit_MouseCaptureChanged(object sender, EventArgs e)
        {
            if (!_dragging && !_edit.Capture)
            {
                _pressY = null;
            }
        }

        private void Edit_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_pressY.HasValue || _dragging)
            {
                return;
            }
            int y = e.Y + _edit.Top;
            if (Math.Abs(y - _pressY.Value) < DragThreshold)
            {
                return;
            }

            // ここからドラッグ。マウスをこのコントロールで受け取り、数値の欄での文字の選択を終わらせる
            _dragging = true;
            _dragStartValue = Value; // 入力中の文字があれば、ここで確定する
            _edit.SelectionLength = 0;
            Cursor = Cursors.SizeNS;
            Capture = true;
            StepTo(y);
        }

        /// <summary>
        /// 押した位置からの上下の移動量に合わせて、値を増減する（上が増加）。
        /// 最大・最小で止めても、戻せばすぐに反応するよう、ドラッグを始めたときの値から計算する。
        /// </summary>
        private void StepTo(int y)
        {
            int steps = (_pressY.Value - y) / DragStep;
            decimal value = _dragStartValue + steps * Increment;
            Value = Math.Max(Minimum, Math.Min(Maximum, value));
        }

        private void EndDrag()
        {
            _dragging = false;
            _pressY = null;
            Cursor = Cursors.Default;
            Capture = false;
        }

        /// <summary>
        /// ▲▼ のボタンを、Windows が描いた後に、配色に合わせて上から描き直す（ボタンのウィンドウのメッセージを横取りする）。
        /// </summary>
        private sealed class UpDownButtonsPainter : NativeWindow
        {
            private const int WM_PAINT = 0x000F;
            private const int WM_MOUSEMOVE = 0x0200;
            private const int WM_MOUSELEAVE = 0x02A3;
            private const int WM_PRINTCLIENT = 0x0318;

            private readonly Control _buttons;

            /// <summary>前に描いたときの、マウスの下のボタン（-1 = なし、0 = ▲、1 = ▼）。変わったら描き直す。</summary>
            private int _hotPart = -1;

            public UpDownButtonsPainter(Control buttons)
            {
                _buttons = buttons;
                buttons.HandleCreated += (sender, e) => AssignHandle(_buttons.Handle);
                buttons.HandleDestroyed += (sender, e) => ReleaseHandle();
                if (buttons.IsHandleCreated)
                {
                    AssignHandle(buttons.Handle);
                }
            }

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);
                switch (m.Msg)
                {
                    case WM_PAINT:
                        using (Graphics g = Graphics.FromHwnd(Handle))
                        {
                            Paint(g);
                        }
                        break;
                    case WM_PRINTCLIENT:
                        // DrawToBitmap などで、画面の外へ描くとき
                        using (Graphics g = Graphics.FromHdc(m.WParam))
                        {
                            Paint(g);
                        }
                        break;
                    case WM_MOUSEMOVE:
                    case WM_MOUSELEAVE:
                        if (HotPart(m.Msg == WM_MOUSELEAVE) != _hotPart)
                        {
                            _buttons.Invalidate();
                        }
                        break;
                }
            }

            private int HotPart(bool leaving)
            {
                Point mouse = _buttons.PointToClient(Cursor.Position);
                if (leaving || !_buttons.Enabled || !_buttons.ClientRectangle.Contains(mouse))
                {
                    return -1;
                }
                return mouse.Y < _buttons.ClientSize.Height / 2 ? 0 : 1;
            }

            private void Paint(Graphics g)
            {
                Size size = _buttons.ClientSize;
                if (size.Width <= 0 || size.Height <= 0)
                {
                    return;
                }

                Theme theme = Theme.Current;
                bool enabled = _buttons.Enabled;
                _hotPart = HotPart(false);
                bool inside = _hotPart >= 0;
                bool pressed = inside && (Control.MouseButtons & MouseButtons.Left) != 0;
                int half = size.Height / 2;
                using (var back = new SolidBrush(theme.ControlFill))
                using (var hot = new SolidBrush(pressed ? theme.Pressed : theme.Hover))
                using (var pen = new Pen(enabled ? theme.MutedText : theme.DisabledText, 1.2f))
                {
                    g.FillRectangle(back, 0, 0, size.Width, size.Height);
                    if (inside)
                    {
                        g.FillRectangle(hot, 0, _hotPart == 0 ? 0 : half, size.Width, _hotPart == 0 ? half : size.Height - half);
                    }

                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    float centerX = size.Width / 2f;
                    float upY = half / 2f;
                    float downY = half + (size.Height - half) / 2f;
                    g.DrawLines(pen, new[] { new PointF(centerX - 3, upY + 1.5f), new PointF(centerX, upY - 1.5f), new PointF(centerX + 3, upY + 1.5f) });
                    g.DrawLines(pen, new[] { new PointF(centerX - 3, downY - 1.5f), new PointF(centerX, downY + 1.5f), new PointF(centerX + 3, downY - 1.5f) });
                }
            }
        }
    }
}
