using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using KMidiPlayer.Midi;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// 曲全体のノートを 1 本の帯に縮めて描いた全体図。ピアノロールに出ている範囲を枠で示し、
    /// クリック・ドラッグで再生位置を移動する（ドラッグ中は印だけを動かし、離したときに移動する）。
    /// </summary>
    public class PianoRollOverview : Control
    {
        private static readonly Color BackgroundColor = Color.FromArgb(32, 34, 42);
        private static readonly Color ViewFrameColor = Color.FromArgb(220, 220, 230);
        private static readonly Color ViewFillColor = Color.FromArgb(40, 255, 255, 255);
        private static readonly Color LoopColor = Color.FromArgb(70, 255, 210, 80);

        private readonly bool[] _audible = new bool[MidiSequencer.ChannelCount];
        private readonly int[] _offsets = new int[MidiSequencer.ChannelCount];

        /// <summary>ノートを描いた絵。曲・大きさ・色分け・ミュート・移調が変わったら作り直す（null なら作り直す）。</summary>
        private Bitmap _notesImage;

        private MidiFile _song;
        private PianoRollColorMode _colorMode;
        private int _velocityCurve;
        private TimeSpan _position;
        private TimeSpan _viewStart;
        private TimeSpan _viewEnd;
        private TimeSpan? _loopStart;
        private TimeSpan? _loopEnd;

        /// <summary>ドラッグ中の移動先（ドラッグしていなければ null）。</summary>
        private TimeSpan? _dragPosition;

        public PianoRollOverview()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.UserPaint,
                true);
            Cursor = Cursors.Hand;
        }

        /// <summary>クリック、またはドラッグを終えて、再生位置の移動が求められたときに発生する。</summary>
        [Category("Action")]
        public event EventHandler<SeekRequestedEventArgs> SeekRequested;

        protected override Size DefaultSize => new Size(200, 36);

        /// <summary>シーケンサーの状態と、ピアノロールに出ている範囲を取り込み、変化があれば描き直す。</summary>
        public void UpdateFrom(MidiSequencer sequencer, TimeSpan viewStart, TimeSpan viewEnd, PianoRollColorMode colorMode)
        {
            bool imageChanged = sequencer.Song != _song || colorMode != _colorMode
                || (colorMode == PianoRollColorMode.Velocity && sequencer.VelocityCurve != _velocityCurve);
            _song = sequencer.Song;
            _colorMode = colorMode;
            _velocityCurve = sequencer.VelocityCurve;
            for (int channel = 0; channel < MidiSequencer.ChannelCount; channel++)
            {
                bool audible = sequencer.IsAudible(channel);
                int offset = sequencer.GetNoteOffset(channel);
                if (_audible[channel] != audible || _offsets[channel] != offset)
                {
                    _audible[channel] = audible;
                    _offsets[channel] = offset;
                    imageChanged = true;
                }
            }
            if (imageChanged)
            {
                DiscardImage();
            }

            TimeSpan position = _song != null ? sequencer.Position : TimeSpan.Zero;
            if (imageChanged || position != _position || viewStart != _viewStart || viewEnd != _viewEnd
                || sequencer.LoopStart != _loopStart || sequencer.LoopEnd != _loopEnd)
            {
                _position = position;
                _viewStart = viewStart;
                _viewEnd = viewEnd;
                _loopStart = sequencer.LoopStart;
                _loopEnd = sequencer.LoopEnd;
                Invalidate();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DiscardImage();
            }
            base.Dispose(disposing);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            DiscardImage();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && _song != null)
            {
                _dragPosition = TimeAtX(e.X);
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragPosition.HasValue)
            {
                _dragPosition = TimeAtX(e.X);
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && _dragPosition.HasValue)
            {
                TimeSpan target = TimeAtX(e.X);
                _dragPosition = null;
                Invalidate();
                SeekRequested?.Invoke(this, new SeekRequestedEventArgs(target));
            }
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (_dragPosition.HasValue && !Capture)
            {
                _dragPosition = null; // 離したことが届かなかった
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            if (_song == null || Width <= 2 || Height <= 2 || _song.Duration <= TimeSpan.Zero)
            {
                g.Clear(BackgroundColor);
                return;
            }
            if (_notesImage == null)
            {
                _notesImage = CreateNotesImage();
            }
            g.DrawImageUnscaled(_notesImage, 0, 0);

            if (_loopStart.HasValue)
            {
                float left = XAt(_loopStart.Value);
                float right = _loopEnd.HasValue ? XAt(_loopEnd.Value) : Width;
                using (var brush = new SolidBrush(LoopColor))
                {
                    g.FillRectangle(brush, left, 0, Math.Max(1, right - left), Height);
                }
            }
            using (var markerPen = new Pen(PianoRollPalette.MarkerColor))
            {
                foreach (TimedText marker in _song.Markers)
                {
                    float x = XAt(marker.Time);
                    g.DrawLine(markerPen, x, 0, x, 4);
                }
            }

            // ピアノロールに出ている範囲の枠
            float viewLeft = XAt(_viewStart);
            float viewRight = Math.Max(viewLeft + 3, XAt(_viewEnd));
            using (var brush = new SolidBrush(ViewFillColor))
            using (var pen = new Pen(ViewFrameColor))
            {
                g.FillRectangle(brush, viewLeft, 0, viewRight - viewLeft, Height - 1);
                g.DrawRectangle(pen, viewLeft, 0, viewRight - viewLeft, Height - 1);
            }

            using (var pen = new Pen(PianoRollPalette.PlayheadColor, 2))
            {
                float x = XAt(_dragPosition ?? _position);
                g.DrawLine(pen, x, 0, x, Height);
            }
        }

        private float XAt(TimeSpan time)
        {
            return (float)(time.TotalSeconds / _song.Duration.TotalSeconds * (Width - 1));
        }

        private TimeSpan TimeAtX(int x)
        {
            double ratio = Math.Max(0, Math.Min(1, x / (double)Math.Max(1, Width - 1)));
            return TimeSpan.FromSeconds(_song.Duration.TotalSeconds * ratio);
        }

        /// <summary>曲のすべてのノートを、横に時間・縦に音の高さ（曲で使っている音域）で描いた絵を作る。</summary>
        private Bitmap CreateNotesImage()
        {
            var image = new Bitmap(Width, Height);
            using (Graphics g = Graphics.FromImage(image))
            using (var brush = new SolidBrush(Color.White))
            {
                g.Clear(BackgroundColor);
                int lowest = int.MaxValue;
                int highest = int.MinValue;
                foreach (NoteSpan note in _song.Notes)
                {
                    int pitch = note.Note + _offsets[note.Channel];
                    lowest = Math.Min(lowest, pitch);
                    highest = Math.Max(highest, pitch);
                }
                if (lowest > highest)
                {
                    return image;
                }
                float rowHeight = (Height - 2) / (float)(highest - lowest + 1);
                foreach (NoteSpan note in _song.Notes)
                {
                    int pitch = note.Note + _offsets[note.Channel];
                    float left = XAt(note.Start);
                    float y = 1 + (highest - pitch) * rowHeight;
                    brush.Color = PianoRollPalette.NoteColor(_colorMode, note.Channel, ((pitch % 12) + 12) % 12,
                        MidiSequencer.ApplyVelocityCurve(note.Velocity, _velocityCurve), _audible[note.Channel], true);
                    g.FillRectangle(brush, left, y, Math.Max(1, XAt(note.End) - left), Math.Max(1, rowHeight));
                }
            }
            return image;
        }

        private void DiscardImage()
        {
            _notesImage?.Dispose();
            _notesImage = null;
        }
    }
}
