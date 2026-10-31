using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KMidiPlayer.Midi;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// ピアノロール。曲のノートを、再生に合わせて流して描く。流し方は <see cref="Style"/> で選ぶ
    /// （横・縦・3D・円。描き方はそれぞれ PianoRollView.*.cs）。
    /// </summary>
    /// <remarks>
    /// 音域は、曲で使っている音域（移調・オクターブを反映）に合わせる（<see cref="FullKeyboard"/> なら 88 鍵）。
    /// クリックでその位置へ移動（<see cref="SeekRequested"/>）、押したまま動かすとノートをつかんで位置を変える
    /// （表示だけを動かし、離したときに移動する）。マウスホイールで時間方向に拡大・縮小する。
    /// </remarks>
    public partial class PianoRollView : Control
    {
        public const double MinPixelsPerSecond = 20;
        public const double MaxPixelsPerSecond = 800;
        public const double DefaultPixelsPerSecond = 100;

        private const int ChannelCount = MidiSequencer.ChannelCount;
        private const int NoteCount = MidiSequencer.NoteCount;

        /// <summary>音域の上下に足す余白（半音）。</summary>
        private const int RangeMargin = 2;

        /// <summary>表示する音域の最小の幅（半音）。音の少ない曲で、ノートが太くなりすぎないように。</summary>
        private const int MinRange = 24;

        // ピアノの 88 鍵の両端（A0 と C8）
        private const int LowestPianoKey = 21;
        private const int HighestPianoKey = 108;

        /// <summary>ホイール 1 目盛りでの拡大率。</summary>
        public const double ZoomStep = 1.25;

        /// <summary>これだけ動かしたら、クリックではなくドラッグとして扱う（px）。</summary>
        private const int DragThreshold = 3;

        /// <summary>小節番号を書く最小の間隔（px）。これより詰まるときは間引く。</summary>
        private const int MinBarLabelSpacing = 36;

        // エフェクトの強さ（0 = なし、1～5）ごとの、光と火花を出しておく時間（曲の上の秒）、1 つのノートで飛ばす火花の数、大きさの倍率
        private static readonly double[] EffectSecondsByLevel = { 0, 0.4, 0.5, 0.6, 0.7, 0.85 };
        private static readonly int[] SparkCountByLevel = { 0, 6, 10, 16, 24, 36 };
        private static readonly float[] EffectScaleByLevel = { 0, 1, 1.25f, 1.5f, 1.8f, 2.2f };

        /// <summary>1 回の描画で描く火花の総数の上限（これを超えるときは、1 つのノートあたりの数を減らす）。</summary>
        private const int MaxSparksPerFrame = 300;

        private readonly bool[] _audible = new bool[ChannelCount];
        private readonly int[] _offsets = new int[ChannelCount];

        /// <summary>音高（移調後）ごとの、鳴らしているチャンネル（鳴っていなければ -1）。鍵盤を光らせるのに使う。</summary>
        private readonly int[] _keyChannels = new int[NoteCount];

        /// <summary>音高（移調後）ごとの、鍵盤を光らせる色（鳴っていなければ <see cref="Color.Empty"/>）。描くたびに求める。</summary>
        private readonly Color[] _litColors = new Color[NoteCount];

        // チャンネルごとの、曲で使っている音高の範囲（移調する前。ノートが無ければ -1）
        private readonly int[] _channelLowest = new int[ChannelCount];
        private readonly int[] _channelHighest = new int[ChannelCount];

        // ノートに書く音名（オクターブ無し、12 音分）とその大きさ。音名の種類・言語・フォントが変わったら作り直す
        private readonly string[] _nameTexts = new string[12];
        private readonly Size[] _nameSizes = new Size[12];
        private PianoRollNoteNames _nameTextsKind = PianoRollNoteNames.None;
        private UiLanguage _nameTextsLanguage;
        private bool _nameTextsOnGpu;

        private MidiFile _song;
        private IReadOnlyList<NoteSpan> _notes = new NoteSpan[0];
        private TimeSpan _longestNote;
        private List<LyricLine> _lyricLines = new List<LyricLine>();
        private IReadOnlyList<TimedText> _markers = new TimedText[0];
        private TimeSpan _position;
        private TimeSpan? _loopStart;
        private TimeSpan? _loopEnd;
        private int _velocityCurve;
        private double _pixelsPerSecond = DefaultPixelsPerSecond;
        private int _wheelDelta;

        private PianoRollStyle _style;
        private PianoRollColorMode _colorMode;
        private PianoRollNoteNames _noteNames;
        private int _effectLevel = 1;
        private bool _fullKeyboard;
        private bool _showLyrics = true;

        private Font _smallFont;
        private Font _lyricFont;

        // ボタンを押した位置（押していなければ null）、ドラッグ中か、ドラッグを始めたときの再生位置
        private Point? _pressPoint;
        private bool _dragging;
        private TimeSpan _dragStartPosition;

        public PianoRollView()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.UserPaint,
                true);
            BackColor = SystemColors.Window;
            for (int note = 0; note < NoteCount; note++)
            {
                _keyChannels[note] = -1;
            }
            SetSong(null);
        }

        /// <summary>クリック、またはドラッグを終えて、再生位置の移動が求められたときに発生する。</summary>
        [Category("Action")]
        public event EventHandler<SeekRequestedEventArgs> SeekRequested;

        /// <summary>時間方向の拡大率（1 秒あたりの px。3D では奥行きの長さを決める）。</summary>
        [Category("Behavior")]
        [DefaultValue(DefaultPixelsPerSecond)]
        public double PixelsPerSecond
        {
            get => _pixelsPerSecond;
            set
            {
                value = double.IsNaN(value) ? DefaultPixelsPerSecond : Math.Max(MinPixelsPerSecond, Math.Min(MaxPixelsPerSecond, value));
                if (value != _pixelsPerSecond)
                {
                    _pixelsPerSecond = value;
                    Invalidate();
                    PixelsPerSecondChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary><see cref="PixelsPerSecond"/> が変わったときに発生する（マウスホイールでの拡大・縮小を含む）。</summary>
        [Category("PropertyChanged")]
        public event EventHandler PixelsPerSecondChanged;

        /// <summary>表示の種類。</summary>
        [Category("Appearance")]
        [DefaultValue(PianoRollStyle.Horizontal)]
        public PianoRollStyle Style
        {
            get => _style;
            set
            {
                if (value != _style)
                {
                    EndDrag();
                    _style = value;
                    Invalidate();
                }
            }
        }

        /// <summary>ノートの色分けの方法。</summary>
        [Category("Appearance")]
        [DefaultValue(PianoRollColorMode.Track)]
        public PianoRollColorMode ColorMode
        {
            get => _colorMode;
            set => SetAndInvalidate(ref _colorMode, value);
        }

        /// <summary>ノートと鍵盤に書く音名。</summary>
        [Category("Appearance")]
        [DefaultValue(PianoRollNoteNames.None)]
        public PianoRollNoteNames NoteNames
        {
            get => _noteNames;
            set => SetAndInvalidate(ref _noteNames, value);
        }

        /// <summary>ノートが鍵盤に届いたときのエフェクトの強さ（0 = なし、1～<see cref="PianoRollOptions.MaxEffectLevel"/>、大きいほど派手）。</summary>
        [Category("Appearance")]
        [DefaultValue(1)]
        public int EffectLevel
        {
            get => _effectLevel;
            set => SetAndInvalidate(ref _effectLevel, Math.Max(0, Math.Min(PianoRollOptions.MaxEffectLevel, value)));
        }

        /// <summary>音域を曲に合わせず、ピアノの 88 鍵の幅で表示するかどうか。</summary>
        [Category("Appearance")]
        [DefaultValue(false)]
        public bool FullKeyboard
        {
            get => _fullKeyboard;
            set => SetAndInvalidate(ref _fullKeyboard, value);
        }

        /// <summary>曲に入っている歌詞とマーカーを表示するかどうか。</summary>
        [Category("Appearance")]
        [DefaultValue(true)]
        public bool ShowLyrics
        {
            get => _showLyrics;
            set => SetAndInvalidate(ref _showLyrics, value);
        }

        /// <summary>暗い背景で描く表示か（3D・円）。</summary>
        private bool IsDark => _style == PianoRollStyle.Perspective || _style == PianoRollStyle.Circular;

        private Font SmallFont => _smallFont ?? (_smallFont = new Font(Font.FontFamily, Math.Max(6.5f, Font.SizeInPoints - 1.5f)));

        private Font LyricFont => _lyricFont ?? (_lyricFont = new Font(Font.FontFamily, Font.SizeInPoints * 1.6f, FontStyle.Bold));

        /// <summary>表示の設定をまとめて変える。</summary>
        public void ApplyOptions(PianoRollOptions options)
        {
            Style = options.Style;
            ColorMode = options.ColorMode;
            NoteNames = options.NoteNames;
            EffectLevel = options.EffectLevel;
            FullKeyboard = options.FullKeyboard;
            ShowLyrics = options.ShowLyrics;
            UseGpu = options.UseGpu;
        }

        /// <summary>
        /// シーケンサーの状態（曲、再生位置、ミュート、移調、鳴っている音、A-B ループ、ベロシティカーブ）を取り込み、変化があれば描き直す。
        /// </summary>
        public void UpdateFrom(MidiSequencer sequencer)
        {
            bool changed = false;
            if (sequencer.Song != _song)
            {
                SetSong(sequencer.Song);
                changed = true;
            }

            // ドラッグ中は、ドラッグで動かした位置を表示し続ける
            TimeSpan position = _dragging ? _position : _song != null ? sequencer.Position : TimeSpan.Zero;
            if (position != _position)
            {
                _position = position;
                changed = true;
            }
            if (sequencer.LoopStart != _loopStart || sequencer.LoopEnd != _loopEnd)
            {
                _loopStart = sequencer.LoopStart;
                _loopEnd = sequencer.LoopEnd;
                changed = true;
            }
            if (sequencer.VelocityCurve != _velocityCurve)
            {
                _velocityCurve = sequencer.VelocityCurve;
                changed |= _colorMode == PianoRollColorMode.Velocity;
            }

            for (int channel = 0; channel < ChannelCount; channel++)
            {
                bool audible = sequencer.IsAudible(channel);
                int offset = sequencer.GetNoteOffset(channel);
                if (_audible[channel] != audible || _offsets[channel] != offset)
                {
                    _audible[channel] = audible;
                    _offsets[channel] = offset;
                    changed = true;
                }
            }

            for (int note = 0; note < NoteCount; note++)
            {
                int keyChannel = -1;
                for (int channel = 0; channel < ChannelCount; channel++)
                {
                    if (sequencer.IsNoteOn(channel, note))
                    {
                        keyChannel = channel;
                        break;
                    }
                }
                if (_keyChannels[note] != keyChannel)
                {
                    _keyChannels[note] = keyChannel;
                    changed = true;
                }
            }

            if (changed)
            {
                Invalidate();
            }
        }

        /// <summary>今、画面に出ている時間の範囲（全体図に枠を出すのに使う）。</summary>
        public void GetVisibleRange(out TimeSpan start, out TimeSpan end)
        {
            double before;
            double after;
            switch (_style)
            {
                case PianoRollStyle.Vertical:
                    before = 0;
                    after = VerticalHitY / _pixelsPerSecond;
                    break;
                case PianoRollStyle.Perspective:
                    before = 0;
                    after = PerspectiveLookAheadSeconds;
                    break;
                case PianoRollStyle.Circular:
                    before = 0;
                    after = CircularRingRadius / _pixelsPerSecond;
                    break;
                default:
                    before = (PlayheadX - KeyboardWidth) / _pixelsPerSecond;
                    after = (Width - PlayheadX) / _pixelsPerSecond;
                    break;
            }
            start = _position - TimeSpan.FromSeconds(Math.Max(0, before));
            end = _position + TimeSpan.FromSeconds(Math.Max(0, after));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _smallFont?.Dispose();
                _lyricFont?.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            _smallFont?.Dispose();
            _smallFont = null;
            _lyricFont?.Dispose();
            _lyricFont = null;
            _nameTextsKind = PianoRollNoteNames.None; // 音名の大きさを測り直す
        }

        // ---- マウス ----

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || _song == null || !IsInRoll(e.Location))
            {
                return;
            }
            _pressPoint = e.Location;
            _dragging = false;
            _dragStartPosition = _position;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_pressPoint.HasValue)
            {
                return;
            }
            bool horizontal = _style == PianoRollStyle.Horizontal;
            int delta = horizontal ? e.X - _pressPoint.Value.X : e.Y - _pressPoint.Value.Y;
            if (!_dragging && Math.Abs(delta) < DragThreshold)
            {
                return;
            }
            if (!_dragging)
            {
                _dragging = true;
                Cursor = horizontal ? Cursors.SizeWE : Cursors.SizeNS;
            }

            // ノートをつかんで動かす（流れる向きに引くと先へ、逆に引くと前へ）。表示だけを動かし、離したときに移動する
            _position = ClampToSong(_dragStartPosition + TimeSpan.FromSeconds(delta * SecondsPerDragPixel));
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left || !_pressPoint.HasValue)
            {
                return;
            }
            TimeSpan? target = _dragging ? _position : TimeAtPoint(e.Location); // 動かさなければ、クリックした位置へ
            EndDrag();
            if (target.HasValue)
            {
                SeekRequested?.Invoke(this, new SeekRequestedEventArgs(ClampToSong(target.Value)));
            }
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (_pressPoint.HasValue && !Capture)
            {
                EndDrag(); // ウィンドウの切り替えなどで、離したことが届かなかった。位置は次の更新で元に戻る
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            _wheelDelta += e.Delta;
            int steps = _wheelDelta / 120;
            _wheelDelta -= steps * 120;
            if (steps != 0)
            {
                PixelsPerSecond = _pixelsPerSecond * Math.Pow(ZoomStep, steps); // 上に回すと拡大
            }
        }

        private void EndDrag()
        {
            _pressPoint = null;
            if (_dragging)
            {
                _dragging = false;
                Cursor = Cursors.Default;
                Invalidate();
            }
        }

        /// <summary>ドラッグを始められる場所か（鍵盤の上は除く）。</summary>
        private bool IsInRoll(Point point)
        {
            switch (_style)
            {
                case PianoRollStyle.Vertical:
                    return point.Y < VerticalHitY;
                case PianoRollStyle.Perspective:
                    return point.Y < PerspectiveHitY;
                case PianoRollStyle.Circular:
                    return true;
                default:
                    return point.X >= KeyboardWidth;
            }
        }

        /// <summary>ドラッグで 1px 動かしたときに、再生位置を動かす秒数。</summary>
        private double SecondsPerDragPixel
        {
            get
            {
                switch (_style)
                {
                    case PianoRollStyle.Perspective:
                        return PerspectiveLookAheadSeconds / Math.Max(1, 2 * (PerspectiveHitY - PerspectiveHorizonY));
                    case PianoRollStyle.Horizontal:
                        return -1 / _pixelsPerSecond; // 右へ引くと前へ
                    default:
                        return 1 / _pixelsPerSecond; // 下へ引くと先へ
                }
            }
        }

        /// <summary>その点にあたる曲の上の時間。時間にあたらない場所なら null。</summary>
        private TimeSpan? TimeAtPoint(Point point)
        {
            switch (_style)
            {
                case PianoRollStyle.Vertical:
                    return VerticalTimeAt(point);
                case PianoRollStyle.Perspective:
                    return PerspectiveTimeAt(point);
                case PianoRollStyle.Circular:
                    return CircularTimeAt(point);
                default:
                    return TimeAtX(point.X);
            }
        }

        private TimeSpan ClampToSong(TimeSpan time)
        {
            if (time < TimeSpan.Zero)
            {
                return TimeSpan.Zero;
            }
            return _song != null && time > _song.Duration ? _song.Duration : time;
        }

        // ---- 描画 ----

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_gpuSource != null)
            {
                return; // GPU で描く子ウィンドウが上に重なっていて、そちらで描く
            }
            using (var canvas = new GdiRollCanvas(e.Graphics))
            {
                Render(canvas);
            }
        }

        /// <summary>ピアノロール全体を描く（GDI+ で描くときも、GPU で描くときも、ここで描く）。</summary>
        private void Render(RollCanvas c)
        {
            c.AntiAlias = false;
            c.Clear(IsDark ? PianoRollPalette.DarkBackColor : BackColor);
            if (_song == null || Width < 60 || Height < 60)
            {
                c.DrawText(Texts.PianoRollNoSong, Font, IsDark ? Color.Gray : SystemColors.GrayText, ClientRectangle, ContentAlignment.MiddleCenter);
                return;
            }

            GetPitchRange(out int lowest, out int highest);
            UpdateLitColors();
            UpdateNameTexts(c);

            Rectangle lyricArea;
            Point timeLabel;
            switch (_style)
            {
                case PianoRollStyle.Vertical:
                    PaintVertical(c, lowest, highest);
                    lyricArea = new Rectangle(0, 0, Width, VerticalHitY);
                    timeLabel = new Point(Width / 2, VerticalHitY - 12);
                    break;
                case PianoRollStyle.Perspective:
                    PaintPerspective(c, lowest, highest);
                    lyricArea = new Rectangle(0, 0, Width, PerspectiveHitY);
                    timeLabel = new Point(Width / 2, PerspectiveHitY - 14);
                    break;
                case PianoRollStyle.Circular:
                    PaintCircular(c, lowest, highest);
                    lyricArea = ClientRectangle;
                    timeLabel = Point.Round(CircularCenter);
                    break;
                default:
                    PaintHorizontal(c, lowest, highest);
                    lyricArea = new Rectangle(KeyboardWidth, RulerHeight, Width - KeyboardWidth, Height - RulerHeight);
                    timeLabel = new Point(PlayheadX, RulerHeight / 2);
                    break;
            }

            c.AntiAlias = false;
            if (_showLyrics)
            {
                DrawLyrics(c, lyricArea);
            }
            if (_dragging)
            {
                DrawTimeLabel(c, timeLabel);
            }
        }

        private void SetSong(MidiFile song)
        {
            _song = song;
            _notes = song?.Notes ?? (IReadOnlyList<NoteSpan>)new NoteSpan[0];
            _lyricLines = song != null ? LyricLine.Build(song.Lyrics) : new List<LyricLine>();
            _markers = song?.Markers ?? (IReadOnlyList<TimedText>)new TimedText[0];
            _longestNote = TimeSpan.Zero;
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                _channelLowest[channel] = -1;
                _channelHighest[channel] = -1;
            }
            foreach (NoteSpan note in _notes)
            {
                TimeSpan length = note.End - note.Start;
                if (length > _longestNote)
                {
                    _longestNote = length;
                }
                int channel = note.Channel;
                if (_channelLowest[channel] < 0 || note.Note < _channelLowest[channel])
                {
                    _channelLowest[channel] = note.Note;
                }
                if (note.Note > _channelHighest[channel])
                {
                    _channelHighest[channel] = note.Note;
                }
            }
        }

        /// <summary>
        /// 表示する音域。曲で使っている音域（移調・オクターブを反映）に余白を足し、最小の幅に広げたもの。
        /// 88 鍵の表示なら、88 鍵の範囲（曲がはみ出すなら、そこまで広げる）。
        /// </summary>
        private void GetPitchRange(out int lowest, out int highest)
        {
            lowest = NoteCount;
            highest = -1;
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                if (_channelLowest[channel] >= 0)
                {
                    lowest = Math.Min(lowest, _channelLowest[channel] + _offsets[channel]);
                    highest = Math.Max(highest, _channelHighest[channel] + _offsets[channel]);
                }
            }
            if (_fullKeyboard)
            {
                lowest = Math.Max(0, Math.Min(lowest, LowestPianoKey));
                highest = Math.Min(NoteCount - 1, Math.Max(highest, HighestPianoKey));
                return;
            }
            if (highest < 0)
            {
                lowest = 48;
                highest = 72;
            }
            lowest -= RangeMargin;
            highest += RangeMargin;
            int shortage = MinRange - (highest - lowest + 1);
            if (shortage > 0)
            {
                lowest -= shortage / 2;
                highest += shortage - shortage / 2;
            }
            lowest = Math.Max(0, lowest);
            highest = Math.Min(NoteCount - 1, Math.Max(highest, lowest + 1));
        }

        /// <summary>移調後の音高（範囲外なら -1）。</summary>
        private int PitchOf(NoteSpan note)
        {
            int pitch = note.Note + _offsets[note.Channel];
            return pitch >= 0 && pitch < NoteCount ? pitch : -1;
        }

        private Color NoteColorOf(NoteSpan note, int pitch)
        {
            return PianoRollPalette.NoteColor(_colorMode, note.Channel, pitch,
                MidiSequencer.ApplyVelocityCurve(note.Velocity, _velocityCurve), _audible[note.Channel], IsDark);
        }

        private bool IsActive(NoteSpan note) => note.Start <= _position && _position < note.End;

        /// <summary>鳴っている時間が start～end に重なるノートの番号を、鳴り始めの順に返す。</summary>
        private IEnumerable<int> VisibleNotes(TimeSpan start, TimeSpan end)
        {
            for (int i = FirstNoteIndex(start - _longestNote); i < _notes.Count; i++)
            {
                NoteSpan note = _notes[i];
                if (note.Start > end)
                {
                    yield break;
                }
                if (note.End >= start)
                {
                    yield return i;
                }
            }
        }

        /// <summary>鳴っている鍵を光らせる色を求める。強さ別の色分けでは、今鳴っているノートのベロシティを使う。</summary>
        private void UpdateLitColors()
        {
            for (int pitch = 0; pitch < NoteCount; pitch++)
            {
                int channel = _keyChannels[pitch];
                _litColors[pitch] = channel < 0 ? Color.Empty
                    : PianoRollPalette.NoteColor(_colorMode, channel, pitch, 100, _audible[channel], IsDark);
            }
            if (_colorMode != PianoRollColorMode.Velocity)
            {
                return;
            }
            foreach (int i in VisibleNotes(_position, _position))
            {
                NoteSpan note = _notes[i];
                int pitch = PitchOf(note);
                if (pitch >= 0 && _keyChannels[pitch] == note.Channel && IsActive(note))
                {
                    _litColors[pitch] = NoteColorOf(note, pitch);
                }
            }
        }

        private void UpdateNameTexts(RollCanvas c)
        {
            // GDI+ と GPU では文字の大きさの測り方が違うので、描き方が変わったときも測り直す
            if (_noteNames == _nameTextsKind && Texts.Current == _nameTextsLanguage && c.IsGpu == _nameTextsOnGpu)
            {
                return;
            }
            _nameTextsKind = _noteNames;
            _nameTextsLanguage = Texts.Current;
            _nameTextsOnGpu = c.IsGpu;
            if (_noteNames == PianoRollNoteNames.None)
            {
                return;
            }
            for (int pitchClass = 0; pitchClass < 12; pitchClass++)
            {
                _nameTexts[pitchClass] = PianoRollPalette.NoteName(pitchClass, _noteNames, false);
                _nameSizes[pitchClass] = c.MeasureText(_nameTexts[pitchClass], SmallFont);
            }
        }

        /// <summary>ノートの中に音名を書く（音名を出す設定で、ノートに収まるときだけ）。</summary>
        /// <param name="atBottom">ノートの下端に寄せるか（縦・3D では鍵盤に近い側）。false なら左端に寄せる。</param>
        private void DrawNoteName(RollCanvas c, int pitch, RectangleF rect, Color noteColor, bool atBottom)
        {
            if (_noteNames == PianoRollNoteNames.None)
            {
                return;
            }
            Size size = _nameSizes[pitch % 12];
            if (rect.Width < size.Width + 2 || rect.Height < size.Height)
            {
                return;
            }
            var point = atBottom
                ? new Point((int)(rect.Left + (rect.Width - size.Width) / 2), (int)(rect.Bottom - size.Height - 1))
                : new Point((int)rect.Left + 2, (int)(rect.Top + (rect.Height - size.Height) / 2));
            c.DrawText(_nameTexts[pitch % 12], SmallFont, PianoRollPalette.TextColorOn(noteColor), point.X, point.Y);
        }

        /// <summary>ノートが鍵盤に届く位置と、光の大きさ、火花を飛ばす向きを求める。届く位置が画面に無ければ false。</summary>
        private delegate bool HitPointFunc(int pitch, out PointF point, out float size, out PointF direction);

        /// <summary>
        /// 鳴り始めたばかりのノートに、光と火花を描く（強さ 3 からは衝撃の輪、4 からは鳴っている間の光の柱と白い火花、
        /// 5 は火花の尾も足す）。曲の上の時間で決めるので、一時停止中は止まって見える。
        /// </summary>
        private void DrawHitEffects(RollCanvas c, HitPointFunc hitPoint)
        {
            int level = _effectLevel;
            if (level <= 0)
            {
                return;
            }
            double seconds = EffectSecondsByLevel[level];
            float scale = EffectScaleByLevel[level];
            bool oldAntiAlias = c.AntiAlias;
            c.AntiAlias = true;
            if (level >= 4)
            {
                DrawLightBeams(c, hitPoint, scale);
            }
            // 一度にたくさんのノートが鳴っても重くならないよう、1 回に描く火花の総数を抑える
            int firstIndex = FirstNoteIndex(_position - TimeSpan.FromSeconds(seconds));
            int effectCount = Math.Max(1, FirstNoteIndex(_position + TimeSpan.FromTicks(1)) - firstIndex);
            int sparkCount = Math.Min(SparkCountByLevel[level], Math.Max(4, MaxSparksPerFrame / effectCount));
            for (int i = firstIndex; i < _notes.Count; i++)
            {
                NoteSpan note = _notes[i];
                if (note.Start > _position)
                {
                    break;
                }
                int pitch = PitchOf(note);
                if (!_audible[note.Channel] || pitch < 0 || !hitPoint(pitch, out PointF point, out float size, out PointF direction))
                {
                    continue;
                }
                float progress = (float)((_position - note.Start).TotalSeconds / seconds);
                float fade = 1 - progress;
                Color color = PianoRollPalette.Blend(NoteColorOf(note, pitch), Color.White, 0.45f);
                float glowSize = Math.Max(3, Math.Min(24, size)) * scale;

                // 広がりながら消える光
                float radius = glowSize * (0.3f + 0.5f * progress);
                c.FillEllipse(Color.FromArgb((int)(Math.Min(200, 100 + 20 * level) * fade), color), point.X - radius, point.Y - radius, radius * 2, radius * 2);

                // 衝撃の輪（速く広がって消える）
                if (level >= 3)
                {
                    float ring = glowSize * (0.5f + 2 * progress);
                    c.DrawEllipse(Color.FromArgb((int)(220 * fade), color), 1 + level * 0.5f * fade, point.X - ring, point.Y - ring, ring * 2, ring * 2);
                }

                // 向きのまわりに散る火花（向きと距離はノートごとに決まった乱数）
                float baseAngle = (float)Math.Atan2(direction.Y, direction.X);
                float spread = level >= 3 ? 2.8f : 2.2f;
                float reach = Math.Max(24, size * 3) * scale;
                float dot = (0.8f + 2.2f * fade) * (level >= 4 ? 1.2f : 1);
                for (int k = 0; k < sparkCount; k++)
                {
                    uint hash = Hash((uint)i, (uint)k);
                    float angle = baseAngle + ((hash & 0xFFFF) / 65535f - 0.5f) * spread;
                    float length = reach * (0.4f + ((hash >> 16) & 0xFF) / 255f * 0.6f);
                    float cos = (float)Math.Cos(angle);
                    float sin = (float)Math.Sin(angle);
                    float distance = length * (float)Math.Sqrt(progress);
                    var head = new PointF(point.X + cos * distance, point.Y + sin * distance);
                    Color sparkColor = level >= 4 && (hash >> 24) % 4 == 0 ? Color.White : color;
                    if (level >= 5)
                    {
                        // 少し前の位置から今の位置まで、尾を引く
                        float tail = length * (float)Math.Sqrt(Math.Max(0, progress - 0.15f));
                        c.DrawLine(Color.FromArgb((int)(160 * fade), sparkColor), dot, point.X + cos * tail, point.Y + sin * tail, head.X, head.Y);
                    }
                    c.FillEllipse(Color.FromArgb((int)(255 * fade), sparkColor), head.X - dot, head.Y - dot, dot * 2, dot * 2);
                }
            }
            c.AntiAlias = oldAntiAlias;
        }

        /// <summary>鳴っている間、鍵盤から流れと逆の向きへ伸びる光の柱を描く（先へ行くほど細く、透明になる。少し揺らめかせる）。</summary>
        private void DrawLightBeams(RollCanvas c, HitPointFunc hitPoint, float scale)
        {
            var polygon = new PointF[4];
            foreach (int i in VisibleNotes(_position, _position))
            {
                NoteSpan note = _notes[i];
                int pitch = PitchOf(note);
                if (!IsActive(note) || !_audible[note.Channel] || pitch < 0 || !hitPoint(pitch, out PointF point, out float size, out PointF direction))
                {
                    continue;
                }
                float length = Math.Max(40, size * 5) * scale;
                float halfWidth = Math.Max(3, Math.Min(24, size)) * 0.4f;
                var end = new PointF(point.X + direction.X * length, point.Y + direction.Y * length);
                var normal = new PointF(-direction.Y * halfWidth, direction.X * halfWidth);
                polygon[0] = new PointF(point.X + normal.X, point.Y + normal.Y);
                polygon[1] = new PointF(point.X - normal.X, point.Y - normal.Y);
                polygon[2] = new PointF(end.X - normal.X * 0.3f, end.Y - normal.Y * 0.3f);
                polygon[3] = new PointF(end.X + normal.X * 0.3f, end.Y + normal.Y * 0.3f);

                double flicker = 0.8 + 0.2 * Math.Sin(_position.TotalSeconds * 25 + i);
                // 明るい背景では、白に寄せると見えなくなるので、ノートの色に近いままにする
                Color color = PianoRollPalette.Blend(NoteColorOf(note, pitch), Color.White, IsDark ? 0.5f : 0.1f);
                // グラデーションの端より外がくり返し塗られないよう、両端を少し外へ広げる
                var start = new PointF(point.X - direction.X * 2, point.Y - direction.Y * 2);
                var stop = new PointF(end.X + direction.X * 2, end.Y + direction.Y * 2);
                c.FillPolygon(polygon, start, Color.FromArgb((int)(150 * flicker), color), stop, Color.FromArgb(0, color));
            }
        }

        private static uint Hash(uint a, uint b)
        {
            uint h = unchecked(a * 2654435761u ^ (b + 0x9E3779B9u) * 2246822519u);
            h ^= h >> 15;
            h = unchecked(h * 2246822519u);
            h ^= h >> 13;
            return h;
        }

        /// <summary>小節番号を何小節ごとに書くか（1 小節の平均の長さが、画面でこの px に満たないときは間引く）。</summary>
        private int BarLabelInterval(double pixelsPerSecond)
        {
            double barSeconds = _song.Duration.TotalSeconds / Math.Max(1, _song.BarTimes.Count);
            int interval = 1;
            while (barSeconds * interval * pixelsPerSecond < MinBarLabelSpacing && interval < 1024)
            {
                interval *= 2;
            }
            return interval;
        }

        /// <summary>ドラッグ中に、移動先の時間（分:秒.1/10 秒）を出す。</summary>
        private void DrawTimeLabel(RollCanvas c, Point center)
        {
            string text = $"{(int)_position.TotalMinutes:00}:{_position.Seconds:00}.{_position.Milliseconds / 100}";
            Size size = c.MeasureText(text, Font);
            var bounds = new Rectangle(center.X - size.Width / 2 - 4, center.Y - size.Height / 2 - 1, size.Width + 8, size.Height + 2);
            c.FillRectangle(PianoRollPalette.PlayheadColor, bounds);
            c.DrawText(text, Font, Color.White, bounds, ContentAlignment.MiddleCenter);
        }

        /// <summary>
        /// 今のマーカー（左上）と歌詞（上の中央。今の行は歌った所まで色を変え、次の行を小さく添える）を描く。
        /// </summary>
        private void DrawLyrics(RollCanvas c, Rectangle area)
        {
            int top = area.Top + 6;
            TimedText marker = LastAtOrBefore(_markers, _position);
            if (marker != null && marker.Text.Trim().Length > 0)
            {
                string text = "▶ " + marker.Text.Trim();
                Size size = c.MeasureText(text, Font);
                var bounds = new Rectangle(area.Left + 6, top, size.Width + 10, size.Height + 4);
                c.FillRectangle(Color.FromArgb(200, PianoRollPalette.MarkerColor), bounds);
                c.DrawText(text, Font, Color.White, bounds, ContentAlignment.MiddleCenter);
                top = bounds.Bottom + 4;
            }

            int index = CurrentLyricLine();
            if (index < 0)
            {
                return;
            }
            LyricLine line = _lyricLines[index];
            var widths = new int[line.Syllables.Count];
            int totalWidth = 0;
            for (int i = 0; i < widths.Length; i++)
            {
                widths[i] = c.MeasureText(line.Syllables[i].Text, LyricFont).Width;
                totalWidth += widths[i];
            }
            string next = index + 1 < _lyricLines.Count && _lyricLines[index + 1].Start - line.LastTime < TimeSpan.FromSeconds(8)
                ? string.Concat(_lyricLines[index + 1].Syllables.Select(s => s.Text)) : null;
            Size nextSize = next != null ? c.MeasureText(next, Font) : Size.Empty;

            int boxWidth = Math.Max(totalWidth, nextSize.Width) + 20;
            int boxHeight = LyricFont.Height + (next != null ? nextSize.Height + 2 : 0) + 8;
            var box = new Rectangle(area.Left + (area.Width - boxWidth) / 2, top, boxWidth, boxHeight);
            c.FillRectangle(Color.FromArgb(160, 0, 0, 0), box);

            int x = area.Left + (area.Width - totalWidth) / 2;
            for (int i = 0; i < widths.Length; i++)
            {
                TimedText syllable = line.Syllables[i];
                Color color = syllable.Time <= _position ? PianoRollPalette.LyricSungColor : Color.White;
                c.DrawText(syllable.Text, LyricFont, color, x, box.Top + 4);
                x += widths[i];
            }
            if (next != null)
            {
                c.DrawText(next, Font, Color.FromArgb(190, 190, 190), area.Left + (area.Width - nextSize.Width) / 2, box.Top + 4 + LyricFont.Height + 2);
            }
        }

        /// <summary>
        /// 表示する歌詞の行。始まった最後の行（歌い終わってしばらくしたら消す）。まだどの行も始まっていなければ、
        /// もうすぐ始まる最初の行。無ければ -1。
        /// </summary>
        private int CurrentLyricLine()
        {
            int index = -1;
            for (int i = 0; i < _lyricLines.Count && _lyricLines[i].Start <= _position; i++)
            {
                index = i;
            }
            if (index < 0)
            {
                return _lyricLines.Count > 0 && _lyricLines[0].Start - _position < TimeSpan.FromSeconds(3) ? 0 : -1;
            }
            // 歌い終わってから 4 秒たち、次の行がまだ先なら消す（次の行が近ければ、先に出す）
            if (_position - _lyricLines[index].LastTime > TimeSpan.FromSeconds(4))
            {
                bool nextIsNear = index + 1 < _lyricLines.Count && _lyricLines[index + 1].Start - _position < TimeSpan.FromSeconds(3);
                return nextIsNear ? index + 1 : -1;
            }
            return index;
        }

        private static TimedText LastAtOrBefore(IReadOnlyList<TimedText> texts, TimeSpan time)
        {
            TimedText last = null;
            foreach (TimedText text in texts)
            {
                if (text.Time > time)
                {
                    break;
                }
                last = text;
            }
            return last;
        }

        /// <summary>鳴り始めがその時間以降で最初のノートの位置（二分探索）。</summary>
        private int FirstNoteIndex(TimeSpan time)
        {
            int low = 0;
            int high = _notes.Count;
            while (low < high)
            {
                int middle = (low + high) / 2;
                if (_notes[middle].Start < time)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }
            return low;
        }

        private void SetAndInvalidate<T>(ref T field, T value)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                Invalidate();
            }
        }
    }
}
