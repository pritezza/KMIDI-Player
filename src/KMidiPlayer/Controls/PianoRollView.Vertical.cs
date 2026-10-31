using System;
using System.Collections.Generic;
using System.Drawing;
using KMidiPlayer.Midi;

namespace KMidiPlayer.Controls
{
    // 縦の表示: ノートが上から下の鍵盤へ落ちてくる。鍵盤の上端が再生位置
    public partial class PianoRollView
    {
        private static readonly Color VerticalLaneColor = Color.FromArgb(246, 246, 246);
        private static readonly Color VerticalFLineColor = Color.FromArgb(232, 232, 232);

        /// <summary>下の鍵盤の高さ。</summary>
        private int VerticalKeyboardHeight => Math.Max(36, Math.Min(90, Height / 6));

        /// <summary>再生位置（鍵盤の上端）の Y 座標。</summary>
        private int VerticalHitY => Height - VerticalKeyboardHeight;

        private float VerticalYAt(TimeSpan time)
        {
            return VerticalHitY - (float)((time - _position).TotalSeconds * _pixelsPerSecond);
        }

        private TimeSpan? VerticalTimeAt(Point point)
        {
            return point.Y < VerticalHitY ? _position + TimeSpan.FromSeconds((VerticalHitY - point.Y) / _pixelsPerSecond) : (TimeSpan?)null;
        }

        private void PaintVertical(RollCanvas c, int lowest, int highest)
        {
            int hitY = VerticalHitY;
            var layout = new KeyboardLayout(lowest, highest, 0, Width);
            var roll = new Rectangle(0, 0, Width, hitY);
            TimeSpan viewEnd = _position + TimeSpan.FromSeconds(hitY / _pixelsPerSecond);

            // 黒鍵の列を薄く塗り、B と C（濃く）、E と F（薄く）の境目に線を引く
            for (int note = layout.Lowest; note <= layout.Highest; note++)
            {
                if (PianoRollPalette.IsBlack(note))
                {
                    c.FillRectangle(VerticalLaneColor, layout.KeyLeft(note), 0, layout.KeyWidth(note), hitY);
                }
                else if (note % 12 == 0 || note % 12 == 5)
                {
                    float x = layout.KeyLeft(note);
                    c.DrawLine(note % 12 == 0 ? OctaveLineColor : VerticalFLineColor, 1, x, 0, x, hitY);
                }
            }

            if (_loopStart.HasValue)
            {
                float bottom = Math.Min(hitY, VerticalYAt(_loopStart.Value));
                float top = _loopEnd.HasValue ? Math.Max(0, VerticalYAt(_loopEnd.Value)) : 0;
                if (bottom > top)
                {
                    c.FillRectangle(LoopColor, 0, top, Width, bottom - top);
                }
            }

            // 小節線（左端に小節番号）とマーカー（右端に名前）
            IReadOnlyList<TimeSpan> bars = _song.BarTimes;
            int labelEvery = BarLabelInterval(_pixelsPerSecond);
            for (int i = 0; i < bars.Count; i++)
            {
                float y = VerticalYAt(bars[i]);
                if (y < 0 || y > hitY)
                {
                    continue;
                }
                c.DrawLine(BarLineColor, 1, 0, y, Width, y);
                if (i % labelEvery == 0)
                {
                    c.DrawText((i + 1).ToString(), SmallFont, SystemColors.GrayText, 2, (int)y - SmallFont.Height);
                }
            }
            if (_showLyrics)
            {
                foreach (TimedText marker in _markers)
                {
                    float y = VerticalYAt(marker.Time);
                    if (y < 0 || y > hitY)
                    {
                        continue;
                    }
                    c.DrawLine(PianoRollPalette.MarkerColor, 2, 0, y, Width, y);
                    string text = marker.Text.Trim();
                    Size size = c.MeasureText(text, Font);
                    c.DrawText(text, Font, PianoRollPalette.MarkerColor, Width - size.Width - 4, (int)y - size.Height - 1);
                }
            }

            // ノート（鍵盤より下に過ぎた部分は描かない）
            c.PushClip(roll);
            foreach (int i in VisibleNotes(_position, viewEnd))
            {
                NoteSpan note = _notes[i];
                int pitch = PitchOf(note);
                if (pitch < 0 || !layout.Contains(pitch))
                {
                    continue;
                }
                float gap = Math.Max(1, layout.KeyWidth(pitch) * 0.08f);
                float top = VerticalYAt(note.End);
                float bottom = VerticalYAt(note.Start);
                var rect = new RectangleF(layout.KeyLeft(pitch) + gap, top, layout.KeyWidth(pitch) - gap * 2, Math.Max(2, bottom - top));
                Color color = NoteColorOf(note, pitch);
                c.FillRectangle(color, rect);
                c.DrawRectangle(IsActive(note) ? Color.Black : PianoRollPalette.Blend(color, Color.Black, 0.35f), rect.X, rect.Y, rect.Width, rect.Height);
                DrawNoteName(c, pitch, rect.Bottom > hitY ? RectangleF.FromLTRB(rect.Left, rect.Top, rect.Right, hitY) : rect, color, true);
            }
            c.PopClip();

            c.DrawLine(PianoRollPalette.PlayheadColor, 2, 0, hitY, Width, hitY);
            layout.Draw(c, hitY + 1, VerticalKeyboardHeight - 1, _litColors, _noteNames, SmallFont);
            DrawHitEffects(c, (int pitch, out PointF point, out float size, out PointF direction) =>
            {
                point = new PointF(layout.Contains(pitch) ? layout.KeyCenter(pitch) : 0, hitY);
                size = layout.Contains(pitch) ? layout.WhiteWidth : 0;
                direction = new PointF(0, -1);
                return layout.Contains(pitch);
            });
        }
    }
}
