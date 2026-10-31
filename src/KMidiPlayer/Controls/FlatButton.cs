using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// フラットデザインのボタン。今の配色（<see cref="Theme.Current"/>）で描く。
    /// </summary>
    public class FlatButton : Button, IThemeable
    {
        private FlatButtonKind _kind;
        private bool _round;
        private bool _hover;
        private bool _pressed;

        public FlatButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
        }

        /// <summary>見た目の種類。</summary>
        [DefaultValue(FlatButtonKind.Standard)]
        public FlatButtonKind Kind
        {
            get => _kind;
            set
            {
                _kind = value;
                Invalidate();
            }
        }

        /// <summary>円いボタンにするか（短い辺を直径にする）。</summary>
        [DefaultValue(false)]
        public bool Round
        {
            get => _round;
            set
            {
                _round = value;
                Invalidate();
            }
        }

        public void ApplyTheme()
        {
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            FlatPainter.PaintButton(e.Graphics, this, _kind, false, _round, _hover, _pressed, Focused && ShowFocusCues);
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
            _pressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _pressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _pressed = false;
            Invalidate();
        }
    }

    /// <summary>
    /// フラットデザインのトグルボタン（オンの間は強調の色で塗る）。<see cref="CheckBox.Checked"/> でオン / オフを表す。
    /// </summary>
    public class FlatToggleButton : CheckBox, IThemeable
    {
        private FlatButtonKind _kind;
        private bool _hover;
        private bool _pressed;

        public FlatToggleButton()
        {
            Appearance = Appearance.Button;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
        }

        /// <summary>オフのときの見た目の種類。</summary>
        [DefaultValue(FlatButtonKind.Standard)]
        public FlatButtonKind Kind
        {
            get => _kind;
            set
            {
                _kind = value;
                Invalidate();
            }
        }

        public void ApplyTheme()
        {
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            FlatPainter.PaintButton(e.Graphics, this, _kind, Checked, false, _hover, _pressed, Focused && ShowFocusCues);
        }

        protected override void OnCheckedChanged(EventArgs e)
        {
            base.OnCheckedChanged(e);
            Invalidate();
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
            _pressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _pressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _pressed = false;
            Invalidate();
        }
    }
}
