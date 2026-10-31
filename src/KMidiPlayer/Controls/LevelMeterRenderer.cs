using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// レベルメーターの中身（レベルとピークホールドの印）を、表示タイプに合わせて描く。
    /// チャンネルパネルと、設定画面の見本で使う。
    /// </summary>
    public static class LevelMeterRenderer
    {
        // 3 色の表示（緑 → 黄 → 赤）。黄は左端から 70% の位置
        private static readonly Color LowColor = Color.FromArgb(60, 180, 75);
        private static readonly Color MiddleColor = Color.FromArgb(235, 200, 0);
        private static readonly Color HighColor = Color.FromArgb(220, 50, 40);
        private const float MiddlePosition = 0.7f;

        // 虹色（左端 → 右端）
        private static readonly Color[] RainbowColors =
        {
            Color.FromArgb(140, 60, 200), Color.FromArgb(40, 90, 230), Color.FromArgb(0, 180, 200),
            Color.FromArgb(50, 180, 60), Color.FromArgb(240, 200, 0), Color.FromArgb(245, 130, 20), Color.FromArgb(220, 40, 40),
        };

        private static readonly Color DarkBackColor = Color.FromArgb(24, 24, 24);

        private const int SegmentWidth = 4;
        private const int SegmentGap = 1;
        private const int DarkSegmentWidth = 3;
        private const int BlockCount = 8;
        private const int BlockGap = 2;
        private const int NeedleWidth = 3;
        private const int PeakWidth = 2;

        /// <summary>
        /// メーターの枠の内側（<paramref name="inner"/>）に、レベルとピークの印を描く。
        /// </summary>
        /// <param name="level">レベル（0～1）。</param>
        /// <param name="peak">ピークの印の位置（0～1）。0 なら描かない。</param>
        /// <param name="audible">音を出すチャンネルか。false なら灰色で描く。</param>
        public static void Draw(Graphics g, Rectangle inner, LevelMeterStyle style, float level, float peak, bool audible)
        {
            if (inner.Width <= 0 || inner.Height <= 0)
            {
                return;
            }
            level = Math.Max(0f, Math.Min(1f, level));
            int levelWidth = (int)Math.Round(inner.Width * level);

            switch (style)
            {
                case LevelMeterStyle.Gradient:
                    FillBar(g, inner, levelWidth, audible, ThreeColorBlend());
                    break;

                case LevelMeterStyle.Rainbow:
                    FillBar(g, inner, levelWidth, audible, RainbowBlend());
                    break;

                case LevelMeterStyle.Segments:
                    DrawSegments(g, inner, levelWidth, SegmentWidth, SegmentGap, audible, ThreeColorAt, Theme.Current.MeterOff, false);
                    break;

                case LevelMeterStyle.SegmentsMono:
                    DrawSegments(g, inner, levelWidth, SegmentWidth, SegmentGap, audible, position => Theme.Current.Accent, Theme.Current.MeterOff, false);
                    break;

                case LevelMeterStyle.Blocks:
                    int blockWidth = Math.Max(1, (inner.Width - BlockGap * (BlockCount - 1)) / BlockCount);
                    DrawSegments(g, inner, levelWidth, blockWidth, BlockGap, audible, ThreeColorAt, Theme.Current.MeterOff, false);
                    break;

                case LevelMeterStyle.Dots:
                    int diameter = Math.Max(3, Math.Min(inner.Height, 6));
                    DrawSegments(g, inner, levelWidth, diameter, 2, audible, ThreeColorAt, Theme.Current.MeterOff, true);
                    break;

                case LevelMeterStyle.Hardware:
                    using (var back = new SolidBrush(DarkBackColor))
                    {
                        g.FillRectangle(back, inner);
                    }
                    DrawSegments(g, Rectangle.Inflate(inner, 0, -1), levelWidth, DarkSegmentWidth, SegmentGap, audible,
                        ThreeColorAt, Color.Empty, false);
                    break;

                case LevelMeterStyle.Center:
                    if (levelWidth > 0)
                    {
                        var bar = new Rectangle(inner.Left + (inner.Width - levelWidth) / 2, inner.Top, levelWidth, inner.Height);
                        if (audible)
                        {
                            // 中央が緑、両端が赤になるよう、メーター全体に左右対称のグラデーションをかける
                            using (var brush = new LinearGradientBrush(inner, LowColor, HighColor, LinearGradientMode.Horizontal))
                            {
                                brush.InterpolationColors = new ColorBlend
                                {
                                    Colors = new[] { HighColor, MiddleColor, LowColor, MiddleColor, HighColor },
                                    Positions = new[] { 0f, (1 - MiddlePosition) / 2, 0.5f, 1 - (1 - MiddlePosition) / 2, 1f },
                                };
                                g.FillRectangle(brush, bar);
                            }
                        }
                        else
                        {
                            FillSolid(g, Theme.Current.DisabledText, bar);
                        }
                    }
                    break;

                case LevelMeterStyle.Needle:
                    if (levelWidth > 0)
                    {
                        int x = inner.Left + Math.Max(0, levelWidth - NeedleWidth);
                        using (var brush = new SolidBrush(audible ? ThreeColorAt(level) : Theme.Current.DisabledText))
                        {
                            g.FillRectangle(brush, x, inner.Top, NeedleWidth, inner.Height);
                        }
                    }
                    break;

                default:
                    if (levelWidth > 0)
                    {
                        FillSolid(g, audible ? Theme.Current.Accent : Theme.Current.DisabledText, new Rectangle(inner.Left, inner.Top, levelWidth, inner.Height));
                    }
                    break;
            }

            if (peak > 0)
            {
                DrawPeak(g, inner, style, Math.Min(1f, peak), audible);
            }
        }

        /// <summary>メーターの左端を 0、右端を 1 としたときの、3 色表示の色（緑 → 黄 → 赤）。</summary>
        public static Color ThreeColorAt(float position)
        {
            return position < MiddlePosition
                ? Blend(LowColor, MiddleColor, position / MiddlePosition)
                : Blend(MiddleColor, HighColor, (position - MiddlePosition) / (1 - MiddlePosition));
        }

        private static Color RainbowAt(float position)
        {
            float scaled = Math.Max(0f, Math.Min(1f, position)) * (RainbowColors.Length - 1);
            int index = Math.Min(RainbowColors.Length - 2, (int)scaled);
            return Blend(RainbowColors[index], RainbowColors[index + 1], scaled - index);
        }

        /// <summary>ピークホールドの印（2px の縦線）。中央から左右のタイプでは、左右に 1 本ずつ描く。</summary>
        private static void DrawPeak(Graphics g, Rectangle inner, LevelMeterStyle style, float peak, bool audible)
        {
            Color color;
            if (!audible)
            {
                color = Theme.Current.DisabledText;
            }
            else
            {
                switch (style)
                {
                    case LevelMeterStyle.Solid:
                    case LevelMeterStyle.SegmentsMono:
                        color = Theme.Current.Accent;
                        break;
                    case LevelMeterStyle.Rainbow:
                        color = RainbowAt(peak);
                        break;
                    case LevelMeterStyle.Hardware:
                        color = Color.White;
                        break;
                    default:
                        color = ThreeColorAt(peak);
                        break;
                }
            }

            using (var brush = new SolidBrush(color))
            {
                int width = (int)Math.Round(inner.Width * peak);
                if (style == LevelMeterStyle.Center)
                {
                    int left = inner.Left + (inner.Width - width) / 2;
                    g.FillRectangle(brush, left, inner.Top, PeakWidth, inner.Height);
                    g.FillRectangle(brush, Math.Max(inner.Left, left + width - PeakWidth), inner.Top, PeakWidth, inner.Height);
                }
                else
                {
                    int x = inner.Left + Math.Max(0, Math.Min(inner.Width - PeakWidth, width - PeakWidth));
                    g.FillRectangle(brush, x, inner.Top, PeakWidth, inner.Height);
                }
            }
        }

        /// <summary>メーター全体にかけたグラデーションのうち、レベルまでの部分を塗る。</summary>
        private static void FillBar(Graphics g, Rectangle inner, int levelWidth, bool audible, ColorBlend blend)
        {
            if (levelWidth <= 0)
            {
                return;
            }
            if (!audible)
            {
                FillSolid(g, Theme.Current.DisabledText, new Rectangle(inner.Left, inner.Top, levelWidth, inner.Height));
                return;
            }
            using (var brush = new LinearGradientBrush(inner, blend.Colors[0], blend.Colors[blend.Colors.Length - 1], LinearGradientMode.Horizontal))
            {
                brush.InterpolationColors = blend;
                g.FillRectangle(brush, inner.Left, inner.Top, levelWidth, inner.Height);
            }
        }

        /// <summary>
        /// 区切ったセグメント（または丸）を並べ、レベルまでを点灯させる。
        /// </summary>
        /// <param name="colorAt">点灯したセグメントの色（位置 0～1 から）。</param>
        /// <param name="offColor">消灯したセグメントの色。Color.Empty なら、点灯時の色を暗くして使う。</param>
        private static void DrawSegments(Graphics g, Rectangle inner, int levelWidth, int segmentWidth, int gap, bool audible,
            Func<float, Color> colorAt, Color offColor, bool round)
        {
            SmoothingMode oldMode = g.SmoothingMode;
            if (round)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
            }
            for (int x = 0; x + segmentWidth <= inner.Width; x += segmentWidth + gap)
            {
                bool lit = x < levelWidth;
                Color onColor = audible ? colorAt((x + segmentWidth / 2f) / inner.Width) : Theme.Current.DisabledText;
                Color color = lit ? onColor : offColor.IsEmpty ? Blend(DarkBackColor, onColor, 0.25f) : offColor;
                using (var brush = new SolidBrush(color))
                {
                    if (round)
                    {
                        g.FillEllipse(brush, inner.Left + x, inner.Top + (inner.Height - segmentWidth) / 2, segmentWidth, segmentWidth);
                    }
                    else
                    {
                        g.FillRectangle(brush, inner.Left + x, inner.Top, segmentWidth, inner.Height);
                    }
                }
            }
            g.SmoothingMode = oldMode;
        }

        private static ColorBlend ThreeColorBlend()
        {
            return new ColorBlend
            {
                Colors = new[] { LowColor, MiddleColor, HighColor },
                Positions = new[] { 0f, MiddlePosition, 1f },
            };
        }

        private static ColorBlend RainbowBlend()
        {
            var positions = new float[RainbowColors.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = (float)i / (positions.Length - 1);
            }
            return new ColorBlend { Colors = RainbowColors, Positions = positions };
        }

        private static void FillSolid(Graphics g, Color color, Rectangle bounds)
        {
            using (var brush = new SolidBrush(color))
            {
                g.FillRectangle(brush, bounds);
            }
        }

        private static Color Blend(Color from, Color to, float amount)
        {
            amount = Math.Max(0, Math.Min(1, amount));
            return Color.FromArgb(
                (int)Math.Round(from.R + (to.R - from.R) * amount),
                (int)Math.Round(from.G + (to.G - from.G) * amount),
                (int)Math.Round(from.B + (to.B - from.B) * amount));
        }
    }
}
