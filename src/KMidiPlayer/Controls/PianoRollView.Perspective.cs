using System;
using System.Collections.Generic;
using System.Drawing;
using KMidiPlayer.Midi;

namespace KMidiPlayer.Controls
{
    // 3D の表示: ノートが奥から手前の鍵盤へ流れてくる。
    //
    // 鍵盤の上端（再生位置）の面を奥行き 0 とし、奥行き z の物は、その面の絵を消失点（中央・地平線の高さ）へ向けて
    // 1 / (1 + z) に縮めた位置に描く（カメラが奥行き -1 にある遠近法）。時間は奥行きに比例させ、先読みの秒数が FarDepth にあたる
    public partial class PianoRollView
    {
        /// <summary>一番奥の奥行き（ここで大きさが 1 / (1 + FarDepth) になる）。</summary>
        private const float FarDepth = 6f;

        /// <summary>先読みの秒数を決める値（÷ 1 秒あたりの px）。拡大率 100 で 5 秒先まで描く。</summary>
        private const double PerspectiveDepthPixels = 500;

        private static readonly Color FloorNearColor = Color.FromArgb(36, 40, 56);
        private static readonly Color FloorCLineColor = Color.FromArgb(62, 68, 90);
        private static readonly Color FloorFLineColor = Color.FromArgb(42, 46, 62);
        private static readonly Color FloorBarColor = Color.FromArgb(80, 86, 110);
        private static readonly Color FloorLoopColor = Color.FromArgb(50, 255, 210, 80);

        private int PerspectiveKeyboardHeight => Math.Max(30, Math.Min(80, Height / 7));

        /// <summary>再生位置（鍵盤の上端）の Y 座標。</summary>
        private int PerspectiveHitY => Height - PerspectiveKeyboardHeight;

        /// <summary>地平線（消失点）の Y 座標。</summary>
        private int PerspectiveHorizonY => Height * 8 / 100;

        /// <summary>何秒先まで描くか。</summary>
        private double PerspectiveLookAheadSeconds => PerspectiveDepthPixels / _pixelsPerSecond;

        /// <summary>ノートの厚み（奥行き 0 での px）。</summary>
        private float NoteThickness => Math.Max(3, PerspectiveKeyboardHeight * 0.15f);

        private float DepthAt(TimeSpan time)
        {
            return (float)((time - _position).TotalSeconds / PerspectiveLookAheadSeconds * FarDepth);
        }

        /// <summary>奥行き 0 の面で横位置 x の点を、奥行き z・床からの高さ raise に置いたときの画面上の位置。</summary>
        private PointF Project(float x, float z, float raise)
        {
            float scale = 1 / (1 + z);
            float centerX = Width / 2f;
            int horizonY = PerspectiveHorizonY;
            return new PointF(centerX + (x - centerX) * scale, horizonY + (PerspectiveHitY - raise - horizonY) * scale);
        }

        private TimeSpan? PerspectiveTimeAt(Point point)
        {
            float scale = (point.Y - PerspectiveHorizonY) / (float)(PerspectiveHitY - PerspectiveHorizonY);
            if (scale > 1 || scale <= 1 / (1 + FarDepth))
            {
                return null;
            }
            float depth = 1 / scale - 1;
            return _position + TimeSpan.FromSeconds(depth / FarDepth * PerspectiveLookAheadSeconds);
        }

        private void PaintPerspective(RollCanvas c, int lowest, int highest)
        {
            int hitY = PerspectiveHitY;
            var layout = new KeyboardLayout(lowest, highest, 0, Width);
            Color back = PianoRollPalette.DarkBackColor;
            c.AntiAlias = true;

            // 床（奥ほど背景に溶ける）
            PointF[] floor = { Project(0, 0, 0), Project(Width, 0, 0), Project(Width, FarDepth, 0), Project(0, FarDepth, 0) };
            c.FillPolygon(floor, new PointF(0, floor[2].Y - 1), back, new PointF(0, hitY + 1), FloorNearColor);

            // 鍵盤の C と F の位置から、奥へ向かう線
            for (int note = layout.Lowest; note <= layout.Highest; note++)
            {
                if (note % 12 == 0 || note % 12 == 5)
                {
                    float x = layout.KeyLeft(note);
                    c.DrawLine(note % 12 == 0 ? FloorCLineColor : FloorFLineColor, 1, Project(x, 0, 0), Project(x, FarDepth, 0));
                }
            }

            if (_loopStart.HasValue)
            {
                float near = Math.Max(0, DepthAt(_loopStart.Value));
                float far = _loopEnd.HasValue ? Math.Min(FarDepth, DepthAt(_loopEnd.Value)) : FarDepth;
                if (far > near)
                {
                    c.FillPolygon(FloorLoopColor, new[] { Project(0, near, 0), Project(Width, near, 0), Project(Width, far, 0), Project(0, far, 0) });
                }
            }

            // 小節線（手前の方だけ番号を書く）とマーカー
            IReadOnlyList<TimeSpan> bars = _song.BarTimes;
            double nearPixelsPerSecond = (hitY - PerspectiveHorizonY) * FarDepth / PerspectiveLookAheadSeconds;
            int labelEvery = BarLabelInterval(nearPixelsPerSecond);
            for (int i = 0; i < bars.Count; i++)
            {
                float z = DepthAt(bars[i]);
                if (z < 0 || z > FarDepth)
                {
                    continue;
                }
                Color barColor = PianoRollPalette.Blend(FloorBarColor, back, z / FarDepth);
                PointF left = Project(0, z, 0);
                c.DrawLine(barColor, 1, left, Project(Width, z, 0));
                if (i % labelEvery == 0 && z < FarDepth / 2)
                {
                    c.DrawText((i + 1).ToString(), SmallFont, barColor, (int)left.X + 2, (int)left.Y - SmallFont.Height);
                }
            }
            if (_showLyrics)
            {
                foreach (TimedText marker in _markers)
                {
                    float z = DepthAt(marker.Time);
                    if (z < 0 || z > FarDepth)
                    {
                        continue;
                    }
                    Color markerColor = PianoRollPalette.Blend(PianoRollPalette.MarkerColor, back, z / FarDepth);
                    PointF right = Project(Width, z, 0);
                    c.DrawLine(markerColor, 2, Project(0, z, 0), right);
                    string text = marker.Text.Trim();
                    Size size = c.MeasureText(text, Font);
                    c.DrawText(text, Font, markerColor, (int)right.X - size.Width - 2, (int)right.Y - size.Height - 1);
                }
            }

            DrawPerspectiveNotes(c, layout, back);
            c.AntiAlias = false;

            c.DrawLine(PianoRollPalette.PlayheadColor, 2, 0, hitY, Width, hitY);
            layout.Draw(c, hitY + 1, PerspectiveKeyboardHeight - 1, _litColors, _noteNames, SmallFont);
            float thickness = NoteThickness;
            DrawHitEffects(c, (int pitch, out PointF point, out float size, out PointF direction) =>
            {
                point = new PointF(layout.Contains(pitch) ? layout.KeyCenter(pitch) : 0, hitY - thickness);
                size = layout.Contains(pitch) ? layout.WhiteWidth : 0;
                direction = new PointF(0, -1);
                return layout.Contains(pitch);
            });
        }

        /// <summary>
        /// ノートを、奥から順に（手前のノートが上に重なるように）厚みのある板として描く。
        /// 上の面は奥ほど背景の色に近づけ、手前の面は少し暗くする（鳴っている間は明るく光らせる）。
        /// </summary>
        private void DrawPerspectiveNotes(RollCanvas c, KeyboardLayout layout, Color back)
        {
            var visible = new List<int>(VisibleNotes(_position, _position + TimeSpan.FromSeconds(PerspectiveLookAheadSeconds)));
            float thickness = NoteThickness;
            var top = new PointF[4];
            for (int n = visible.Count - 1; n >= 0; n--)
            {
                NoteSpan note = _notes[visible[n]];
                int pitch = PitchOf(note);
                if (pitch < 0 || !layout.Contains(pitch))
                {
                    continue;
                }
                float near = Math.Max(0, DepthAt(note.Start));
                float far = Math.Min(FarDepth, DepthAt(note.End));
                if (near >= FarDepth || far <= 0)
                {
                    continue;
                }
                far = Math.Max(far, near + 0.002f);

                float gap = layout.KeyWidth(pitch) * 0.1f;
                float left = layout.KeyLeft(pitch) + gap;
                float right = layout.KeyLeft(pitch) + layout.KeyWidth(pitch) - gap;
                Color color = PianoRollPalette.Blend(NoteColorOf(note, pitch), back, near / FarDepth * 0.75f);
                bool active = IsActive(note);

                top[0] = Project(left, near, thickness);
                top[1] = Project(right, near, thickness);
                top[2] = Project(right, far, thickness);
                top[3] = Project(left, far, thickness);
                c.FillPolygon(active ? PianoRollPalette.Blend(color, Color.White, 0.2f) : color, top);

                PointF frontBottom = Project(right, near, 0);
                c.FillRectangle(active ? PianoRollPalette.Blend(color, Color.White, 0.6f) : PianoRollPalette.Blend(color, Color.Black, 0.35f),
                    top[0].X, top[0].Y, frontBottom.X - top[0].X, frontBottom.Y - top[0].Y);

                if (near < FarDepth * 0.35f)
                {
                    DrawNoteName(c, pitch, RectangleF.FromLTRB(top[0].X, top[3].Y, top[1].X, top[0].Y), color, true);
                }
            }
        }
    }
}
