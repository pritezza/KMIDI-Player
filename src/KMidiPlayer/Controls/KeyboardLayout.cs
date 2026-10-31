using System;
using System.Drawing;
using KMidiPlayer.Midi;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// 横向きの鍵盤（本物と同じく、白鍵を同じ幅で並べ、黒鍵をその境目に置いたもの）の、各鍵の位置。
    /// 縦・3D のピアノロールで、鍵盤とノートの横位置に使う。
    /// </summary>
    internal sealed class KeyboardLayout
    {
        /// <summary>黒鍵の幅（白鍵の幅に対する割合）。</summary>
        private const float BlackWidthRatio = 0.6f;

        /// <summary>黒鍵の長さ（鍵盤の高さに対する割合）。</summary>
        private const float BlackLengthRatio = 0.62f;

        private readonly float[] _left = new float[MidiSequencer.NoteCount];
        private readonly float[] _width = new float[MidiSequencer.NoteCount];

        /// <summary>音域の両端が黒鍵なら、隣の白鍵まで広げて並べる。</summary>
        public KeyboardLayout(int lowest, int highest, float left, float width)
        {
            if (PianoRollPalette.IsBlack(lowest))
            {
                lowest--;
            }
            if (PianoRollPalette.IsBlack(highest))
            {
                highest++;
            }
            Lowest = lowest;
            Highest = highest;

            int whiteCount = 0;
            for (int note = lowest; note <= highest; note++)
            {
                if (!PianoRollPalette.IsBlack(note))
                {
                    whiteCount++;
                }
            }
            WhiteWidth = width / Math.Max(1, whiteCount);

            int whiteIndex = 0;
            for (int note = lowest; note <= highest; note++)
            {
                if (PianoRollPalette.IsBlack(note))
                {
                    _width[note] = WhiteWidth * BlackWidthRatio;
                    _left[note] = left + whiteIndex * WhiteWidth - _width[note] / 2;
                }
                else
                {
                    _width[note] = WhiteWidth;
                    _left[note] = left + whiteIndex * WhiteWidth;
                    whiteIndex++;
                }
            }
        }

        public int Lowest { get; }

        public int Highest { get; }

        public float WhiteWidth { get; }

        public bool Contains(int note) => note >= Lowest && note <= Highest;

        public float KeyLeft(int note) => _left[note];

        public float KeyWidth(int note) => _width[note];

        public float KeyCenter(int note) => _left[note] + _width[note] / 2;

        /// <summary>鍵盤を描く。鳴っている鍵は <paramref name="litColors"/> の色（<see cref="Color.Empty"/> なら鳴っていない）で塗る。</summary>
        /// <param name="names">白鍵の下に書く音名。<see cref="PianoRollNoteNames.None"/> なら C にだけ C4 などを書く。</param>
        public void Draw(RollCanvas c, float top, float height, Color[] litColors, PianoRollNoteNames names, Font font)
        {
            Color borderColor = Color.FromArgb(150, 150, 150);
            c.FillRectangle(Color.White, _left[Lowest], top, _left[Highest] + _width[Highest] - _left[Lowest], height);
            for (int note = Lowest; note <= Highest; note++)
            {
                if (PianoRollPalette.IsBlack(note))
                {
                    continue;
                }
                if (!litColors[note].IsEmpty)
                {
                    c.FillRectangle(litColors[note], _left[note], top, _width[note], height);
                }
                c.DrawLine(borderColor, 1, _left[note], top, _left[note], top + height);

                // 鍵の幅に収まる音名だけ書く（C だけは、はみ出しても書く）
                bool isC = note % 12 == 0;
                if (names != PianoRollNoteNames.None || isC)
                {
                    string text = PianoRollPalette.NoteName(note, names == PianoRollNoteNames.None ? PianoRollNoteNames.Letters : names, isC);
                    Size size = c.MeasureText(text, font);
                    if (isC || size.Width <= WhiteWidth)
                    {
                        float x = _left[note] + (_width[note] - size.Width) / 2;
                        c.DrawText(text, font, Color.FromArgb(120, 120, 120), (int)Math.Round(x), (int)(top + height) - font.Height - 2);
                    }
                }
            }

            float blackHeight = height * BlackLengthRatio;
            for (int note = Lowest; note <= Highest; note++)
            {
                if (!PianoRollPalette.IsBlack(note))
                {
                    continue;
                }
                c.FillRectangle(litColors[note].IsEmpty ? Color.FromArgb(35, 35, 35) : litColors[note], _left[note], top, _width[note], blackHeight);
            }
            c.DrawLine(borderColor, 1, _left[Lowest], top, _left[Highest] + _width[Highest], top);
        }
    }
}
