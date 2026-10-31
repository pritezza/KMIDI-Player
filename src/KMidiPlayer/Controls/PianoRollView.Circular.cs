using System;
using System.Drawing;
using KMidiPlayer.Midi;

namespace KMidiPlayer.Controls
{
    // 円の表示: ノートが中心から外側の円い鍵盤へ広がってくる。角度が音の高さ（上の切れ目から時計回りに低い音から高い音）、
    // 中心からの距離が時間（鍵盤の内側の円が再生位置で、内側ほど先）
    public partial class PianoRollView
    {
        /// <summary>鍵盤の輪の上に空ける切れ目の角度（低い音と高い音の境目が分かるように）。</summary>
        private const float CircularGapDegrees = 30;

        private static readonly Color CircularSpokeColor = Color.FromArgb(48, 54, 72);
        private static readonly Color CircularBarColor = Color.FromArgb(62, 68, 90);
        private static readonly Color CircularWhiteKeyColor = Color.FromArgb(230, 230, 230);
        private static readonly Color CircularBlackKeyColor = Color.FromArgb(45, 45, 50);

        private PointF CircularCenter => new PointF(Width / 2f, Height / 2f);

        /// <summary>外側の鍵盤の輪の太さ。</summary>
        private float CircularRingThickness => Math.Max(12, Math.Min(30, Math.Min(Width, Height) / 14f));

        /// <summary>再生位置の円（鍵盤の輪の内側）の半径。</summary>
        private float CircularRingRadius => Math.Max(10, Math.Min(Width, Height) / 2f - 4 - CircularRingThickness);

        private float RadiusAt(TimeSpan time)
        {
            return CircularRingRadius - (float)((time - _position).TotalSeconds * _pixelsPerSecond);
        }

        private TimeSpan? CircularTimeAt(Point point)
        {
            PointF center = CircularCenter;
            double radius = Math.Sqrt((point.X - center.X) * (point.X - center.X) + (point.Y - center.Y) * (point.Y - center.Y));
            return radius <= CircularRingRadius ? _position + TimeSpan.FromSeconds((CircularRingRadius - radius) / _pixelsPerSecond) : (TimeSpan?)null;
        }

        private void PaintCircular(RollCanvas c, int lowest, int highest)
        {
            PointF center = CircularCenter;
            float ringRadius = CircularRingRadius;
            float ringThickness = CircularRingThickness;
            float step = (360 - CircularGapDegrees) / (highest - lowest + 1);
            float startAngle = -90 + CircularGapDegrees / 2;
            float AngleOf(int pitch) => startAngle + (pitch - lowest) * step;
            Color back = PianoRollPalette.DarkBackColor;

            c.AntiAlias = true;

            // C の位置の放射線
            for (int pitch = lowest; pitch <= highest; pitch++)
            {
                if (pitch % 12 == 0)
                {
                    c.DrawLine(CircularSpokeColor, 1, center, Polar(center, ringRadius, AngleOf(pitch)));
                }
            }

            if (_loopStart.HasValue)
            {
                float outer = Math.Min(ringRadius, RadiusAt(_loopStart.Value));
                float inner = _loopEnd.HasValue ? Math.Max(0, RadiusAt(_loopEnd.Value)) : 0;
                if (outer > inner)
                {
                    c.FillRing(FloorLoopColor, center, inner, outer);
                }
            }

            // 小節の円とマーカーの円
            foreach (TimeSpan bar in _song.BarTimes)
            {
                float radius = RadiusAt(bar);
                if (radius > 1 && radius <= ringRadius)
                {
                    c.DrawEllipse(PianoRollPalette.Blend(back, CircularBarColor, radius / ringRadius), 1,
                        center.X - radius, center.Y - radius, radius * 2, radius * 2);
                }
            }
            if (_showLyrics)
            {
                foreach (TimedText marker in _markers)
                {
                    float radius = RadiusAt(marker.Time);
                    if (radius > 1 && radius <= ringRadius)
                    {
                        c.DrawEllipse(PianoRollPalette.MarkerColor, 1, center.X - radius, center.Y - radius, radius * 2, radius * 2);
                        c.DrawText(marker.Text.Trim(), SmallFont, PianoRollPalette.MarkerColor, (int)center.X + 3, (int)(center.Y - radius) - SmallFont.Height);
                    }
                }
            }

            // ノート（中心に近いほど背景に溶ける）
            TimeSpan viewEnd = _position + TimeSpan.FromSeconds(ringRadius / _pixelsPerSecond);
            foreach (int i in VisibleNotes(_position, viewEnd))
            {
                NoteSpan note = _notes[i];
                int pitch = PitchOf(note);
                if (pitch < lowest || pitch > highest)
                {
                    continue;
                }
                float outer = Math.Min(ringRadius, RadiusAt(note.Start));
                float inner = Math.Max(0, RadiusAt(note.End));
                if (outer <= 1)
                {
                    continue;
                }
                inner = Math.Min(inner, outer - 2);
                Color color = PianoRollPalette.Blend(back, NoteColorOf(note, pitch), 0.4f + 0.6f * outer / ringRadius);
                c.FillSector(color, IsActive(note) ? Color.White : Color.Empty, center, inner, outer, AngleOf(pitch) + step * 0.06f, step * 0.88f);
            }

            // 再生位置の円と、外側の鍵盤の輪
            c.DrawEllipse(PianoRollPalette.PlayheadColor, 2, center.X - ringRadius, center.Y - ringRadius, ringRadius * 2, ringRadius * 2);
            for (int pitch = lowest; pitch <= highest; pitch++)
            {
                bool isBlack = PianoRollPalette.IsBlack(pitch);
                Color color = !_litColors[pitch].IsEmpty ? _litColors[pitch] : isBlack ? CircularBlackKeyColor : CircularWhiteKeyColor;
                c.FillSector(color, Color.Empty, center, ringRadius + 1, ringRadius + (isBlack ? ringThickness * 0.7f : ringThickness),
                    AngleOf(pitch) + step * 0.04f, step * 0.92f);
            }
            c.AntiAlias = false;

            // 鍵盤の輪に音名（音名を出す設定なら白鍵すべて、そうでなければ C にだけ C4 などを書く）
            float arcLength = (float)(ringRadius * step * Math.PI / 180);
            for (int pitch = lowest; pitch <= highest; pitch++)
            {
                bool isC = pitch % 12 == 0;
                if (PianoRollPalette.IsBlack(pitch) || (!isC && _noteNames == PianoRollNoteNames.None))
                {
                    continue;
                }
                string text = PianoRollPalette.NoteName(pitch, _noteNames == PianoRollNoteNames.None ? PianoRollNoteNames.Letters : _noteNames, isC);
                Size size = c.MeasureText(text, SmallFont);
                if (size.Width > arcLength * 2.2f && !isC)
                {
                    continue; // 詰まっていて収まらない
                }
                PointF point = Polar(center, ringRadius + ringThickness * 0.82f, AngleOf(pitch) + step / 2);
                c.DrawText(text, SmallFont, Color.FromArgb(110, 110, 110), (int)(point.X - size.Width / 2f), (int)(point.Y - size.Height / 2f));
            }

            DrawHitEffects(c, (int pitch, out PointF point, out float size, out PointF direction) =>
            {
                float angle = AngleOf(pitch) + step / 2;
                point = Polar(center, ringRadius, angle);
                size = Math.Max(4, arcLength);
                double radians = angle * Math.PI / 180;
                direction = new PointF((float)Math.Cos(radians), (float)Math.Sin(radians));
                return pitch >= lowest && pitch <= highest;
            });
        }

        private static PointF Polar(PointF center, float radius, float degrees)
        {
            double radians = degrees * Math.PI / 180;
            return new PointF(center.X + radius * (float)Math.Cos(radians), center.Y + radius * (float)Math.Sin(radians));
        }
    }
}
