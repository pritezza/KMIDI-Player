using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// フラットデザインのコンボボックス（ドロップダウンリスト）。今の配色（<see cref="Theme.Current"/>）で描く。
    /// </summary>
    /// <remarks>
    /// 閉じているときの見た目（枠・選んでいる項目・▼）は、Windows が描いた後に上から描き直す
    /// （標準のコンボボックスは、平らにしても ▼ のボタンを Windows の配色で描くため）。
    /// 項目は自分で描く。DrawItem イベントを使えば、項目の描き方を差し替えられる（このクラスの描画の上に描く）。
    /// </remarks>
    public class FlatComboBox : ComboBox, IThemeable
    {
        private const int WM_PAINT = 0x000F;
        private const int WM_PRINT = 0x0317;
        private const int WM_PRINTCLIENT = 0x0318;
        private const int ChevronWidth = 20;

        private bool _hover;

        public FlatComboBox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
            FlatStyle = FlatStyle.Flat;
            ApplyColors();
        }

        public void ApplyTheme()
        {
            ApplyColors();
            Invalidate();
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index >= 0)
            {
                Theme theme = Theme.Current;
                bool isEdit = (e.State & DrawItemState.ComboBoxEdit) != 0;
                bool selected = !isEdit && (e.State & DrawItemState.Selected) != 0;
                using (var brush = new SolidBrush(selected ? theme.Selection : e.BackColor))
                {
                    e.Graphics.FillRectangle(brush, e.Bounds);
                }
                var textBounds = Rectangle.FromLTRB(e.Bounds.Left + (isEdit ? 4 : 6), e.Bounds.Top, e.Bounds.Right - 2, e.Bounds.Bottom);
                TextRenderer.DrawText(e.Graphics, GetItemText(Items[e.Index]), e.Font, textBounds, Enabled ? theme.Text : theme.DisabledText,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            }
            base.OnDrawItem(e);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (DropDownStyle != ComboBoxStyle.DropDownList)
            {
                return;
            }
            if (m.Msg == WM_PAINT)
            {
                using (Graphics g = Graphics.FromHwnd(Handle))
                {
                    PaintClosed(g);
                }
            }
            else if (m.Msg == WM_PRINTCLIENT || m.Msg == WM_PRINT)
            {
                // DrawToBitmap などで、画面の外へ描くとき
                using (Graphics g = Graphics.FromHdc(m.WParam))
                {
                    PaintClosed(g);
                }
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

        private void ApplyColors()
        {
            BackColor = Theme.Current.ControlFill;
            ForeColor = Theme.Current.Text;
        }

        /// <summary>閉じているときの見た目を、枠・選んでいる項目・▼ の順に描く。</summary>
        private void PaintClosed(Graphics g)
        {
            Theme theme = Theme.Current;
            Rectangle client = ClientRectangle;
            if (client.Width <= 0 || client.Height <= 0)
            {
                return;
            }

            // WM_PRINT で渡される描画先は、このコントロールの範囲に絞られていないので、範囲の中だけを塗る
            g.SetClip(client);
            using (var outside = new SolidBrush(Parent?.BackColor ?? theme.Background))
            {
                g.FillRectangle(outside, client);
            }
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var bounds = new RectangleF(0.5f, 0.5f, client.Width - 1, client.Height - 1);
            Color border = !Enabled ? theme.Divider : Focused ? theme.Accent : _hover ? theme.MutedText : theme.Border;
            Color fill = _hover && Enabled ? theme.ControlHover : theme.ControlFill;
            using (GraphicsPath path = FlatPainter.RoundedRectangle(bounds, FlatPainter.CornerRadius))
            using (var brush = new SolidBrush(fill))
            using (var pen = new Pen(border))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }
            g.SmoothingMode = SmoothingMode.Default;

            if (SelectedIndex >= 0)
            {
                var editBounds = Rectangle.FromLTRB(client.Left + 2, client.Top + 2, client.Right - ChevronWidth, client.Bottom - 2);
                DrawItemState state = DrawItemState.ComboBoxEdit | (Enabled ? DrawItemState.None : DrawItemState.Disabled);
                OnDrawItem(new DrawItemEventArgs(g, Font, editBounds, SelectedIndex, state, Enabled ? theme.Text : theme.DisabledText, fill));
            }

            var chevron = Rectangle.FromLTRB(client.Right - ChevronWidth, client.Top, client.Right - 2, client.Bottom);
            FlatPainter.DrawChevron(g, chevron, Enabled ? theme.MutedText : theme.DisabledText);
        }
    }
}
