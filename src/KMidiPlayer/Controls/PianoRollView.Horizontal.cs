using System;
using System.Collections.Generic;
using System.Drawing;
using KMidiPlayer.Midi;

namespace KMidiPlayer.Controls
{
    // 横の表示: 横軸を時間・縦軸を音の高さにして、右から左へ流す。再生位置の線は左から 1/3 に固定する
    public partial class PianoRollView
    {
        private const int KeyboardWidth = 44;
        private const int RulerHeight = 18;
        private const float PlayheadPosition = 1 / 3f;

        private static readonly Color LaneColor = Color.FromArgb(244, 244, 244);
        private static readonly Color OctaveLineColor = Color.FromArgb(215, 215, 215);
        private static readonly Color BarLineColor = Color.FromArgb(190, 190, 190);
        private static readonly Color LoopColor = Color.FromArgb(60, 255, 210, 80);

        private int PlayheadX => KeyboardWidth + (int)((Width - KeyboardWidth) * PlayheadPosition);

        private float XAt(TimeSpan time)
        {
            return PlayheadX + (float)((time - _position).TotalSeconds * _pixelsPerSecond);
        }

        private TimeSpan TimeAtX(int x)
        {
            return _position + TimeSpan.FromSeconds((x - PlayheadX) / _pixelsPerSecond);
        }

        private static float YAt(int note, int highest, float rowHeight)
        {
            return RulerHeight + (highest - note) * rowHeight;
        }

        private void PaintHorizontal(RollCanvas c, int lowest, int highest)
        {
            float rowHeight = (Height - RulerHeight) / (float)(highest - lowest + 1);
            var roll = new Rectangle(KeyboardWidth, RulerHeight, Width - KeyboardWidth, Height - RulerHeight);

            DrawLanes(c, roll, lowest, highest, rowHeight);
            DrawLoop(c, roll);
            DrawBars(c, roll);
            DrawNotes(c, roll, lowest, highest, rowHeight);

            int playheadX = PlayheadX;
            c.DrawLine(PianoRollPalette.PlayheadColor, 2, playheadX, RulerHeight, playheadX, Height);
            DrawHitEffects(c, (int pitch, out PointF point, out float size, out PointF direction) =>
            {
                point = new PointF(playheadX, YAt(pitch, highest, rowHeight) + rowHeight / 2);
                size = Math.Max(4, rowHeight);
                direction = new PointF(-1, 0);
                return pitch >= lowest && pitch <= highest;
            });
            DrawKeyboard(c, lowest, highest, rowHeight);
        }

        /// <summary>黒鍵の段を薄く塗り、オクターブ（C）ごとに線を引く。</summary>
        private static void DrawLanes(RollCanvas c, Rectangle roll, int lowest, int highest, float rowHeight)
        {
            for (int note = lowest; note <= highest; note++)
            {
                float y = YAt(note, highest, rowHeight);
                if (PianoRollPalette.IsBlack(note))
                {
                    c.FillRectangle(LaneColor, roll.Left, y, roll.Width, rowHeight);
                }
                if (note % 12 == 0)
                {
                    c.DrawLine(OctaveLineColor, 1, roll.Left, y + rowHeight, roll.Right, y + rowHeight);
                }
            }
        }

        private void DrawLoop(RollCanvas c, Rectangle roll)
        {
            if (!_loopStart.HasValue)
            {
                return;
            }
            float left = Math.Max(roll.Left, XAt(_loopStart.Value));
            float right = _loopEnd.HasValue ? Math.Min(roll.Right, XAt(_loopEnd.Value)) : roll.Right;
            if (right > left)
            {
                c.FillRectangle(LoopColor, left, 0, right - left, Height);
            }
        }

        /// <summary>小節線と、上の目盛りに小節番号を描く（詰まるときは番号を間引く）。歌詞を出すなら、マーカーも目盛りに書く。</summary>
        private void DrawBars(RollCanvas c, Rectangle roll)
        {
            c.FillRectangle(SystemColors.Control, KeyboardWidth, 0, Width - KeyboardWidth, RulerHeight);
            IReadOnlyList<TimeSpan> bars = _song.BarTimes;
            int labelEvery = BarLabelInterval(_pixelsPerSecond);
            var barLabels = new List<Rectangle>();
            for (int i = 0; i < bars.Count; i++)
            {
                float x = XAt(bars[i]);
                if (x < roll.Left || x > roll.Right)
                {
                    continue;
                }
                c.DrawLine(BarLineColor, 1, x, RulerHeight, x, Height);
                c.DrawLine(SystemColors.ControlDark, 1, x, RulerHeight - 5, x, RulerHeight);
                if (i % labelEvery == 0)
                {
                    string text = (i + 1).ToString();
                    var bounds = new Rectangle(new Point((int)x + 2, 2), c.MeasureText(text, Font));
                    c.DrawText(text, Font, SystemColors.ControlText, bounds.X, bounds.Y);
                    barLabels.Add(bounds);
                }
            }

            if (!_showLyrics)
            {
                return;
            }
            foreach (TimedText marker in _markers)
            {
                float x = XAt(marker.Time);
                if (x < roll.Left || x > roll.Right)
                {
                    continue;
                }
                c.DrawLine(PianoRollPalette.MarkerColor, 2, x, 0, x, RulerHeight);

                // 同じ位置の小節番号と重なるなら、その右に書く
                int textX = (int)x + 3;
                foreach (Rectangle label in barLabels)
                {
                    if (label.Left <= textX + 4 && textX < label.Right)
                    {
                        textX = label.Right + 3;
                    }
                }
                c.DrawText(marker.Text.Trim(), Font, PianoRollPalette.MarkerColor, textX, 2);
            }
        }

        private void DrawNotes(RollCanvas c, Rectangle roll, int lowest, int highest, float rowHeight)
        {
            c.PushClip(roll);
            float noteHeight = Math.Max(1, rowHeight - 1);
            foreach (int i in VisibleNotes(TimeAtX(roll.Left), TimeAtX(roll.Right)))
            {
                NoteSpan note = _notes[i];
                int pitch = PitchOf(note);
                if (pitch < lowest || pitch > highest)
                {
                    continue;
                }

                float left = XAt(note.Start);
                var rect = new RectangleF(left, YAt(pitch, highest, rowHeight), Math.Max(2, XAt(note.End) - left), noteHeight);
                bool active = IsActive(note);
                Color color = NoteColorOf(note, pitch);
                c.FillRectangle(active ? color : Color.FromArgb(150, color), rect);
                if (active)
                {
                    c.DrawRectangle(SystemColors.ControlText, rect.X, rect.Y, rect.Width, rect.Height);
                }
                DrawNoteName(c, pitch, rect, active ? color : PianoRollPalette.Blend(Color.White, color, 150 / 255f), false);
            }
            c.PopClip();
        }

        /// <summary>左の縦向きの鍵盤。鳴っている鍵は、そのノートの色で塗る。C には音名（C4 など）を書く。</summary>
        private void DrawKeyboard(RollCanvas c, int lowest, int highest, float rowHeight)
        {
            Color borderColor = Color.FromArgb(180, 180, 180);
            c.FillRectangle(Color.White, 0, RulerHeight, KeyboardWidth, Height - RulerHeight);
            c.FillRectangle(SystemColors.Control, 0, 0, KeyboardWidth, RulerHeight);
            for (int note = lowest; note <= highest; note++)
            {
                float y = YAt(note, highest, rowHeight);
                bool isBlack = PianoRollPalette.IsBlack(note);
                float width = isBlack ? KeyboardWidth * 0.6f : KeyboardWidth;
                if (!_litColors[note].IsEmpty || isBlack)
                {
                    c.FillRectangle(_litColors[note].IsEmpty ? Color.FromArgb(40, 40, 40) : _litColors[note], 0, y, width, rowHeight);
                }

                if (!isBlack && (note % 12 == 4 || note % 12 == 11))
                {
                    c.DrawLine(borderColor, 1, 0, y, KeyboardWidth, y); // E と F、B と C の境目
                }

                // 音名を出す設定なら白鍵すべてに、そうでなければ C にだけ書く
                bool isC = note % 12 == 0;
                bool labeled = isC ? rowHeight >= 6 : !isBlack && _noteNames != PianoRollNoteNames.None && rowHeight >= 9;
                if (labeled)
                {
                    PianoRollNoteNames names = _noteNames == PianoRollNoteNames.None ? PianoRollNoteNames.Letters : _noteNames;
                    c.DrawText(PianoRollPalette.NoteName(note, names, isC), isC ? Font : SmallFont, SystemColors.GrayText,
                        new RectangleF(0, (int)y, KeyboardWidth - 2, (int)Math.Ceiling(rowHeight)), ContentAlignment.MiddleRight);
                }
            }
            c.DrawLine(borderColor, 1, KeyboardWidth - 1, 0, KeyboardWidth - 1, Height);
        }
    }
}
