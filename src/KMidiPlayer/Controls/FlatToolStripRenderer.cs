using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// メニュー・ツールバー・ステータスバー・右クリックメニューを、フラットデザインで描く。
    /// グラデーションや立体の枠を描かず、マウスを乗せた項目だけを角の丸い地の色で示す。
    /// </summary>
    internal sealed class FlatToolStripRenderer : ToolStripProfessionalRenderer
    {
        private readonly Theme _theme;

        public FlatToolStripRenderer(Theme theme)
            : base(new FlatColorTable(theme))
        {
            _theme = theme;
            RoundedEdges = false;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(e.ToolStrip is ToolStripDropDown ? _theme.Surface : _theme.Background))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            Rectangle bounds = new Rectangle(Point.Empty, e.ToolStrip.Size);
            if (e.ToolStrip is ToolStripDropDown)
            {
                using (var pen = new Pen(_theme.Border))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, bounds.Width - 1, bounds.Height - 1);
                }
            }
            else if (e.ToolStrip is StatusStrip)
            {
                using (var pen = new Pen(_theme.Divider))
                {
                    e.Graphics.DrawLine(pen, 0, 0, bounds.Width, 0);
                }
            }
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(_theme.Surface))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            ToolStripItem item = e.Item;
            bool open = item is ToolStripMenuItem menuItem && !item.IsOnDropDown && menuItem.DropDown.Visible;
            if (!(item.Selected && item.Enabled) && !open)
            {
                return;
            }
            Rectangle bounds = item.IsOnDropDown
                ? new Rectangle(4, 1, item.Width - 6, item.Height - 2)
                : new Rectangle(1, 1, item.Width - 2, item.Height - 2);
            FillRounded(e.Graphics, bounds, open ? _theme.Pressed : _theme.Hover);
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var button = e.Item as ToolStripButton;
            var bounds = new Rectangle(1, 1, e.Item.Width - 2, e.Item.Height - 2);
            if (button != null && button.Checked)
            {
                FillRounded(e.Graphics, bounds, e.Item.Selected ? _theme.AccentHover : _theme.Accent);
            }
            else if (e.Item.Pressed)
            {
                FillRounded(e.Graphics, bounds, _theme.Pressed);
            }
            else if (e.Item.Selected)
            {
                FillRounded(e.Graphics, bounds, _theme.Hover);
            }
        }

        protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var bounds = new Rectangle(1, 1, e.Item.Width - 2, e.Item.Height - 2);
            if (e.Item.Pressed)
            {
                FillRounded(e.Graphics, bounds, _theme.Pressed);
            }
            else if (e.Item.Selected)
            {
                FillRounded(e.Graphics, bounds, _theme.Hover);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (!e.Item.Enabled)
            {
                e.TextColor = _theme.DisabledText;
            }
            else if (e.Item is ToolStripButton button && button.Checked)
            {
                e.TextColor = _theme.AccentText;
            }
            else if (e.Item is ToolStripStatusLabel)
            {
                e.TextColor = _theme.MutedText;
            }
            else
            {
                e.TextColor = _theme.Text;
            }
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = e.Item == null || e.Item.Enabled ? _theme.MutedText : _theme.DisabledText;
            base.OnRenderArrow(e);
        }

        /// <summary>チェックの印：強調の色の角丸の四角に、チェックの線を描く。</summary>
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            Rectangle box = Rectangle.Inflate(e.ImageRectangle, -1, -1);
            Graphics g = e.Graphics;
            SmoothingMode oldMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color fill = e.Item.Enabled ? _theme.Accent : _theme.DisabledText;
            using (GraphicsPath path = FlatPainter.RoundedRectangle(box, 3))
            using (var brush = new SolidBrush(fill))
            {
                g.FillPath(brush, path);
            }
            if (e.Image == null)
            {
                using (var pen = new Pen(_theme.AccentText, 1.6f))
                {
                    float left = box.Left + box.Width * 0.25f;
                    float middle = box.Left + box.Width * 0.43f;
                    float right = box.Left + box.Width * 0.77f;
                    g.DrawLines(pen, new[]
                    {
                        new PointF(left, box.Top + box.Height * 0.52f),
                        new PointF(middle, box.Top + box.Height * 0.70f),
                        new PointF(right, box.Top + box.Height * 0.32f),
                    });
                }
            }
            g.SmoothingMode = oldMode;
            if (e.Image != null)
            {
                g.DrawImage(e.Image, e.ImageRectangle);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using (var pen = new Pen(_theme.Divider))
            {
                if (e.Vertical)
                {
                    int x = e.Item.Width / 2;
                    e.Graphics.DrawLine(pen, x, 4, x, e.Item.Height - 4);
                }
                else
                {
                    int y = e.Item.Height / 2;
                    e.Graphics.DrawLine(pen, 4, y, e.Item.Width - 4, y);
                }
            }
        }

        private static void FillRounded(Graphics g, Rectangle bounds, Color color)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }
            SmoothingMode oldMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = FlatPainter.RoundedRectangle(bounds, FlatPainter.CornerRadius))
            using (var brush = new SolidBrush(color))
            {
                g.FillPath(brush, path);
            }
            g.SmoothingMode = oldMode;
        }

        /// <summary>ToolStripProfessionalRenderer が使う色（グラデーションは同じ色にして、平らにする）。</summary>
        private sealed class FlatColorTable : ProfessionalColorTable
        {
            private readonly Theme _theme;

            public FlatColorTable(Theme theme)
            {
                _theme = theme;
                UseSystemColors = false;
            }

            public override Color MenuBorder => _theme.Border;
            public override Color MenuItemBorder => Color.Transparent;
            public override Color MenuItemSelected => _theme.Hover;
            public override Color MenuStripGradientBegin => _theme.Background;
            public override Color MenuStripGradientEnd => _theme.Background;
            public override Color ToolStripDropDownBackground => _theme.Surface;
            public override Color ImageMarginGradientBegin => _theme.Surface;
            public override Color ImageMarginGradientMiddle => _theme.Surface;
            public override Color ImageMarginGradientEnd => _theme.Surface;
            public override Color ToolStripGradientBegin => _theme.Background;
            public override Color ToolStripGradientMiddle => _theme.Background;
            public override Color ToolStripGradientEnd => _theme.Background;
            public override Color ToolStripBorder => _theme.Background;
            public override Color StatusStripGradientBegin => _theme.Background;
            public override Color StatusStripGradientEnd => _theme.Background;
            public override Color SeparatorDark => _theme.Divider;
            public override Color SeparatorLight => _theme.Divider;
            public override Color GripDark => _theme.Border;
            public override Color GripLight => _theme.Background;
            public override Color CheckBackground => _theme.Accent;
            public override Color CheckSelectedBackground => _theme.AccentHover;
            public override Color CheckPressedBackground => _theme.AccentPressed;
            public override Color ButtonSelectedBorder => Color.Transparent;
            public override Color ButtonSelectedHighlight => _theme.Hover;
            public override Color ButtonPressedHighlight => _theme.Pressed;
            public override Color ButtonCheckedHighlight => _theme.Accent;
            public override Color MenuItemPressedGradientBegin => _theme.Pressed;
            public override Color MenuItemPressedGradientEnd => _theme.Pressed;
            public override Color MenuItemSelectedGradientBegin => _theme.Hover;
            public override Color MenuItemSelectedGradientEnd => _theme.Hover;
        }
    }
}
