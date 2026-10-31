using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KMidiPlayer.Controls
{
    /// <summary>ボタンの見た目の種類。</summary>
    public enum FlatButtonKind
    {
        /// <summary>枠のある、普通のボタン。</summary>
        Standard,

        /// <summary>枠も地の色も無く、マウスを乗せたときだけ地の色が付くボタン（アイコンのボタン用）。</summary>
        Subtle,

        /// <summary>強調の色で塗ったボタン（再生ボタンなど）。</summary>
        Accent,
    }

    /// <summary>フラットデザインのコントロールの、共通の描き方。</summary>
    internal static class FlatPainter
    {
        /// <summary>ボタン・入力欄の角の丸み（px）。</summary>
        public const float CornerRadius = 4f;

        public static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
        {
            var path = new GraphicsPath();
            float diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            if (diameter <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// ボタン（チェックボックスのボタン表示を含む）を描く。
        /// </summary>
        /// <param name="isChecked">押した状態（オン）のトグルボタンか。強調の色で塗る。</param>
        /// <param name="round">円いボタンにするか（短い辺を直径にする）。</param>
        /// <param name="showFocus">キーボードのフォーカスの枠を描くか。</param>
        public static void PaintButton(Graphics g, ButtonBase button, FlatButtonKind kind, bool isChecked, bool round,
            bool hover, bool pressed, bool showFocus)
        {
            Theme theme = Theme.Current;
            g.Clear(button.Parent?.BackColor ?? theme.Background);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool enabled = button.Enabled;
            Color fill;
            Color border = Color.Empty;
            Color text;
            if (kind == FlatButtonKind.Accent || isChecked)
            {
                fill = !enabled ? theme.Hover : pressed ? theme.AccentPressed : hover ? theme.AccentHover : theme.Accent;
                text = enabled ? theme.AccentText : theme.DisabledText;
            }
            else if (kind == FlatButtonKind.Standard)
            {
                fill = pressed ? theme.Pressed : hover ? theme.ControlHover : theme.ControlFill;
                border = theme.Border;
                text = enabled ? theme.Text : theme.DisabledText;
            }
            else
            {
                fill = pressed ? theme.Pressed : hover ? theme.Hover : Color.Empty;
                text = enabled ? theme.Text : theme.DisabledText;
            }

            RectangleF bounds = new RectangleF(0.5f, 0.5f, button.Width - 1, button.Height - 1);
            if (round)
            {
                float diameter = Math.Min(bounds.Width, bounds.Height);
                bounds = new RectangleF(bounds.Left + (bounds.Width - diameter) / 2, bounds.Top + (bounds.Height - diameter) / 2, diameter, diameter);
            }
            using (GraphicsPath path = round ? Ellipse(bounds) : RoundedRectangle(bounds, CornerRadius))
            {
                if (!fill.IsEmpty)
                {
                    using (var brush = new SolidBrush(fill))
                    {
                        g.FillPath(brush, path);
                    }
                }
                if (!border.IsEmpty)
                {
                    using (var pen = new Pen(border))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }

            if (showFocus)
            {
                RectangleF ring = RectangleF.Inflate(bounds, -1.5f, -1.5f);
                using (GraphicsPath path = round ? Ellipse(ring) : RoundedRectangle(ring, CornerRadius - 1))
                using (var pen = new Pen(kind == FlatButtonKind.Accent || isChecked ? theme.AccentText : theme.Accent, 1.5f))
                {
                    g.DrawPath(pen, path);
                }
            }

            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(g, button.Text, button.Font, button.ClientRectangle, text, flags);
        }

        /// <summary>
        /// 入力欄の地と枠を描く（角の丸い四角。フォーカスがあれば枠を強調の色にする）。
        /// </summary>
        /// <param name="outside">角の丸みの外側を塗る色（親の背景色）。</param>
        public static void PaintField(Graphics g, Rectangle bounds, Color outside, bool focused)
        {
            Theme theme = Theme.Current;
            using (var outsideBrush = new SolidBrush(outside))
            {
                g.FillRectangle(outsideBrush, bounds);
            }
            SmoothingMode oldMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var field = new RectangleF(bounds.Left + 0.5f, bounds.Top + 0.5f, bounds.Width - 1, bounds.Height - 1);
            using (GraphicsPath path = RoundedRectangle(field, CornerRadius))
            using (var brush = new SolidBrush(theme.ControlFill))
            using (var pen = new Pen(focused ? theme.Accent : theme.Border))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }
            g.SmoothingMode = oldMode;
        }

        /// <summary>下向きの山形（コンボボックスの印）を描く。</summary>
        public static void DrawChevron(Graphics g, Rectangle area, Color color)
        {
            SmoothingMode oldMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float centerX = area.Left + area.Width / 2f;
            float centerY = area.Top + area.Height / 2f;
            using (var pen = new Pen(color, 1.3f))
            {
                g.DrawLines(pen, new[]
                {
                    new PointF(centerX - 4, centerY - 2),
                    new PointF(centerX, centerY + 2),
                    new PointF(centerX + 4, centerY - 2),
                });
            }
            g.SmoothingMode = oldMode;
        }

        private static GraphicsPath Ellipse(RectangleF bounds)
        {
            var path = new GraphicsPath();
            path.AddEllipse(bounds);
            return path;
        }
    }
}
