using System;
using System.Drawing;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// ピアノロールと、その全体図で使う色と音名。
    /// </summary>
    internal static class PianoRollPalette
    {
        private static readonly bool[] KeyIsBlack = { false, true, false, true, false, false, true, false, true, false, true, false };

        private static readonly string[] LetterNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        // トラック（曲データのチャンネル）ごとの色
        private static readonly Color[] TrackColors =
        {
            Color.FromArgb(230, 80, 70), Color.FromArgb(240, 150, 40), Color.FromArgb(210, 190, 30), Color.FromArgb(120, 190, 50),
            Color.FromArgb(40, 170, 100), Color.FromArgb(30, 170, 170), Color.FromArgb(40, 140, 220), Color.FromArgb(80, 90, 220),
            Color.FromArgb(140, 80, 210), Color.FromArgb(60, 60, 60), Color.FromArgb(210, 70, 160), Color.FromArgb(160, 110, 60),
            Color.FromArgb(100, 160, 140), Color.FromArgb(180, 140, 200), Color.FromArgb(90, 130, 60), Color.FromArgb(200, 120, 120),
        };

        /// <summary>暗い背景で、ドラム（10ch）の代わりに使う色（黒に近い色だと見えないため）。</summary>
        private static readonly Color DarkDrumColor = Color.FromArgb(170, 170, 170);

        /// <summary>ミュート中のトラックのノートの色（明るい背景用）。</summary>
        public static readonly Color MutedColor = Color.FromArgb(200, 200, 200);

        /// <summary>ミュート中のトラックのノートの色（暗い背景用）。</summary>
        public static readonly Color DarkMutedColor = Color.FromArgb(75, 75, 80);

        /// <summary>暗い背景の表示（3D・円）の背景色。</summary>
        public static readonly Color DarkBackColor = Color.FromArgb(14, 16, 22);

        public static readonly Color PlayheadColor = Color.FromArgb(220, 40, 40);
        public static readonly Color MarkerColor = Color.FromArgb(230, 140, 20);
        public static readonly Color LyricSungColor = Color.FromArgb(255, 200, 60);

        public static bool IsBlack(int note) => KeyIsBlack[note % 12];

        /// <summary>ノートの色。ミュート中のトラックは灰色。</summary>
        /// <param name="velocity">カーブをかけた後のベロシティ（1～127）。</param>
        /// <param name="dark">暗い背景に描くか。</param>
        public static Color NoteColor(PianoRollColorMode mode, int channel, int pitch, int velocity, bool audible, bool dark)
        {
            if (!audible)
            {
                return dark ? DarkMutedColor : MutedColor;
            }
            switch (mode)
            {
                case PianoRollColorMode.Velocity:
                    // 弱い = 青（色相 240°）から、強い = 赤（0°）まで
                    double strength = Math.Max(0, Math.Min(1, (velocity - 1) / 126.0));
                    return FromHsv(240 * (1 - strength), 0.8, 0.95);

                case PianoRollColorMode.PitchClass:
                    // C を赤にして、半音ごとに色相を 30° ずつ回す
                    return FromHsv((pitch % 12) * 30, 0.7, 0.92);

                default:
                    return dark && channel == 9 ? DarkDrumColor : TrackColors[channel];
            }
        }

        /// <summary>2 つの色を混ぜる（amount = 0 なら a、1 なら b）。</summary>
        public static Color Blend(Color a, Color b, float amount)
        {
            amount = Math.Max(0, Math.Min(1, amount));
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * amount),
                (int)(a.R + (b.R - a.R) * amount),
                (int)(a.G + (b.G - a.G) * amount),
                (int)(a.B + (b.B - a.B) * amount));
        }

        /// <summary>その色の上に書く文字の色（明るい色なら黒、暗い色なら白）。</summary>
        public static Color TextColorOn(Color back)
        {
            return back.R * 299 + back.G * 587 + back.B * 114 > 150000 ? Color.Black : Color.White;
        }

        /// <summary>音名。オクターブ付きなら C4（中央のド）のように書く。</summary>
        public static string NoteName(int pitch, PianoRollNoteNames names, bool withOctave)
        {
            string name = names == PianoRollNoteNames.Solfege ? Texts.SolfegeNames[pitch % 12] : LetterNames[pitch % 12];
            return withOctave ? name + (pitch / 12 - 1) : name;
        }

        private static Color FromHsv(double hue, double saturation, double value)
        {
            hue = ((hue % 360) + 360) % 360 / 60;
            int sector = (int)hue;
            double fraction = hue - sector;
            double p = value * (1 - saturation);
            double q = value * (1 - saturation * fraction);
            double t = value * (1 - saturation * (1 - fraction));
            double r, g, b;
            switch (sector)
            {
                case 0: r = value; g = t; b = p; break;
                case 1: r = q; g = value; b = p; break;
                case 2: r = p; g = value; b = t; break;
                case 3: r = p; g = q; b = value; break;
                case 4: r = t; g = p; b = value; break;
                default: r = value; g = p; b = q; break;
            }
            return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }
    }
}
