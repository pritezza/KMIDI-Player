using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using KMidiPlayer.Midi;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// 曲データのチャンネル（TR）ごとの、送信先のチャンネル（CH）、ミュート・ソロのボタン、
    /// 鍵盤（発音中のノート）、ペダルのランプ、レベルメーター兼ボリューム、移調・オクターブを表示する。
    /// </summary>
    /// <remarks>
    /// 変化の少ない部分（鍵盤や枠など）は画像に描いておき、毎フレームは発音中のノートとメーターだけを描き足す。
    /// ボリュームは、メーターの下辺の ▲ で表し、メーターの上をドラッグ（またはホイール）して変える。
    /// <see cref="ShowUsedChannelsOnly"/> が true の場合は、曲の中でノートを鳴らすチャンネル（とソロ中のチャンネル）
    /// だけを表示し、その分 1 行を高くする。
    /// 1 行の高さは CH のスピンボックスが収まる高さより低くせず、全行が収まらない場合はスクロールバーを出す。
    /// </remarks>
    public class ChannelView : Control, IThemeable
    {
        private const int ChannelCount = MidiSequencer.ChannelCount;
        private const int NoteCount = MidiSequencer.NoteCount;
        private const int WhiteKeyCount = 75; // ノート 0～127 の白鍵の数

        private const int HeaderHeight = 18;
        private const int NumberWidth = 30;
        private const int SpinWidth = 46;
        private const int OctaveColumnWidth = 62; // 見出しの「オクターブ」が収まる幅
        private const int ToggleWidth = 24;
        private const int MeterWidth = 100;
        private const int PedalColumnWidth = 36; // 見出しの「ペダル」が収まる幅
        private const int BendColumnWidth = 44; // 見出しの「PB/Mod」が収まる幅
        private const int PedalLampWidth = 10;
        private const int PedalLampGap = 3;
        private const int MaxPedalLampHeight = 10;
        private const int Gap = 6;

        /// <summary>1 行の高さの上限（チャンネルが少ない曲で、鍵盤が縦に伸びすぎないようにする）。</summary>
        private const int MaxRowHeight = 60;
        private const int MaxToggleHeight = 20;
        private const int MaxMeterHeight = 14;
        private const int VolumeKnobSize = 5; // ▲ の高さ（幅はこの 2 倍）

        // 1 オクターブ内の各音について、白鍵なら白鍵の番号（0～6）、黒鍵なら左隣の白鍵の番号
        private static readonly int[] KeyWhiteIndex = { 0, 0, 1, 1, 2, 3, 3, 4, 4, 5, 5, 6 };
        private static readonly bool[] KeyIsBlack = { false, true, false, true, false, false, true, false, true, false, true, false };

        // 白鍵の番号（0～6）から、1 オクターブ内の音
        private static readonly int[] WhiteKeyPitch = { 0, 2, 4, 5, 7, 9, 11 };

        // 鍵の表示: 鳴っていない・ダンパーペダルで伸びている（薄く点灯）・鳴っている
        private const byte KeyOff = 0;
        private const byte KeySustained = 1;
        private const byte KeyOn = 2;

        // ダンパーペダルで伸びている鍵に重ねる色の不透明度（0～255）。暗い配色では鍵が暗いので、少し濃くする
        private const int SustainAlpha = 90;
        private const int DarkSustainAlpha = 130;

        private static readonly Color MuteColor = Color.FromArgb(224, 112, 32);
        private static readonly Color SoloColor = Color.FromArgb(46, 139, 87);
        private static readonly Color PedalOnColor = Color.FromArgb(255, 160, 0);
        private static readonly Color BlockedPedalColor = Color.FromArgb(200, 30, 30);
        private static readonly Color ModulationColor = Color.FromArgb(140, 80, 200);

        // ペダルのランプ。本物のピアノと同じく、左からソフト・ソステヌート・ダンパーの順に並べる（名前は Texts.PedalTip）
        private static readonly int[] PedalControllers = { MidiSequencer.SoftPedal, MidiSequencer.SostenutoPedal, MidiSequencer.DamperPedal };

        // 鍵の表示（KeyOff・KeySustained・KeyOn）
        private readonly byte[,] _notes = new byte[ChannelCount, NoteCount];
        private readonly float[] _levels = new float[ChannelCount];

        // ピークホールドの印の位置（0～1）と、印をその位置に残す残り時間（秒）
        private readonly float[] _peaks = new float[ChannelCount];
        private readonly double[] _peakHoldRemaining = new double[ChannelCount];

        private LevelMeterSettings _meterSettings = new LevelMeterSettings();
        private readonly bool[] _muted = new bool[ChannelCount];
        private readonly bool[] _solo = new bool[ChannelCount];
        private readonly bool[] _audible = new bool[ChannelCount];
        private readonly int[] _volumes = new int[ChannelCount];
        private readonly bool[,] _pedals = new bool[ChannelCount, PedalControllers.Length];

        /// <summary>ペダルごとの、音源に送らない設定か（ランプに斜線を引く）。</summary>
        private readonly bool[] _pedalBlocked = new bool[PedalControllers.Length];

        // ピッチベンド（-8192～8191）とモジュレーション（0～127）
        private readonly int[] _pitchBends = new int[ChannelCount];
        private readonly int[] _modulations = new int[ChannelCount];
        private readonly Stopwatch _frameClock = Stopwatch.StartNew();
        private readonly ToolTip _toolTip = new ToolTip();

        /// <summary>行ごとの、表示するチャンネル番号。先頭から <see cref="_rowCount"/> 個が有効。</summary>
        private readonly int[] _rowChannels = new int[ChannelCount];
        private int _rowCount;

        // 行ごとのスピンボックス（送信先のチャンネル、移調、オクターブ）
        private readonly SpinColumn _outputSpins;
        private readonly SpinColumn _transposeSpins;
        private readonly SpinColumn _octaveSpins;
        private readonly VScrollBar _scrollBar;

        /// <summary>一番上に表示している行。</summary>
        private int _firstRow;
        private bool _scrollBarNeeded;

        private bool _showUsedChannelsOnly = true;
        private Bitmap _background;

        // マウスで押している鍵（押していなければ -1）
        private int _pressedChannel = -1;
        private int _pressedNote = -1;

        // ボリュームをドラッグしているチャンネル（していなければ -1）
        private int _volumeDragChannel = -1;

        // 出しているツールチップの文字列（出していなければ null）と位置
        private string _tipText;
        private Point _tipLocation;

        /// <summary>ボリュームの上で回したホイールの、1 目盛りに満たない分。</summary>
        private int _volumeWheelDelta;

        public ChannelView()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.UserPaint,
                true);
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                _audible[channel] = true;
                _volumes[channel] = MidiSequencer.DefaultChannelVolume;
                _rowChannels[channel] = channel;
            }
            _rowCount = ChannelCount;

            _scrollBar = new VScrollBar { Dock = DockStyle.Right, Visible = false };
            _scrollBar.ValueChanged += ScrollBar_ValueChanged;
            Controls.Add(_scrollBar);
            ThemeStyler.ApplyScrollBarTheme(_scrollBar);
            BackColor = Theme.Current.Surface;
            ForeColor = Theme.Current.Text;

            // CH は 1 から数えて表示する
            _outputSpins = new SpinColumn(this, 1, ChannelCount, channel => channel + 1,
                (channel, value) => OutputChannelChanged?.Invoke(this, new OutputChannelEventArgs(channel, value - 1)));
            _transposeSpins = new SpinColumn(this, MidiSequencer.MinChannelTranspose, MidiSequencer.MaxChannelTranspose, channel => 0,
                (channel, value) => TransposeChanged?.Invoke(this, new ChannelValueEventArgs(channel, value)));
            _octaveSpins = new SpinColumn(this, MidiSequencer.MinChannelOctave, MidiSequencer.MaxChannelOctave, channel => 0,
                (channel, value) => OctaveChanged?.Invoke(this, new ChannelValueEventArgs(channel, value)));
            UpdateLayout();
        }

        /// <summary>ミュート（M）ボタンがクリックされたときに発生する。</summary>
        [Category("Action")]
        public event EventHandler<ChannelEventArgs> MuteClicked;

        /// <summary>ソロ（S）ボタンがクリックされたときに発生する。</summary>
        [Category("Action")]
        public event EventHandler<ChannelEventArgs> SoloClicked;

        /// <summary>鍵盤がマウスで押されたときに発生する。ベロシティは、鍵の上の方ほど小さく、下の方ほど大きい。</summary>
        [Category("Action")]
        public event EventHandler<KeyboardNoteEventArgs> NotePressed;

        /// <summary>マウスで押していた鍵が離されたときに発生する。</summary>
        [Category("Action")]
        public event EventHandler<KeyboardNoteEventArgs> NoteReleased;

        /// <summary>送信先のチャンネル（CH）のスピンボックスの値が、操作によって変わったときに発生する。</summary>
        [Category("Action")]
        public event EventHandler<OutputChannelEventArgs> OutputChannelChanged;

        /// <summary>移調（半音）のスピンボックスの値が、操作によって変わったときに発生する。</summary>
        [Category("Action")]
        public event EventHandler<ChannelValueEventArgs> TransposeChanged;

        /// <summary>オクターブのスピンボックスの値が、操作によって変わったときに発生する。</summary>
        [Category("Action")]
        public event EventHandler<ChannelValueEventArgs> OctaveChanged;

        /// <summary>ボリューム（%）が、操作によって変わったときに発生する。</summary>
        [Category("Action")]
        public event EventHandler<ChannelValueEventArgs> VolumeChanged;

        /// <summary>ペダルのランプが左クリックされたときに発生する（音源に送る / 送らないの切り替え用）。</summary>
        [Category("Action")]
        public event EventHandler<PedalEventArgs> PedalClicked;

        /// <summary>
        /// 曲の中でノートを鳴らすチャンネルだけを表示するかどうか。曲を読み込んでいないときは常に全チャンネルを表示する。
        /// 次に <see cref="UpdateFrom"/> を呼んだときに反映される。
        /// </summary>
        [Category("Behavior")]
        [DefaultValue(true)]
        public bool ShowUsedChannelsOnly
        {
            get => _showUsedChannelsOnly;
            set => _showUsedChannelsOnly = value;
        }

        /// <summary>表示言語を変えたときに呼ぶ。見出しを描き直し、出ているツールチップを消す。</summary>
        public void UpdateTexts()
        {
            HideTip();
            InvalidateBackground();
        }

        /// <summary>
        /// レベルメーターの表示設定（表示タイプ・下降速度・ピークホールド）。更新頻度は、<see cref="UpdateFrom"/> を呼ぶ側で合わせる。
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public LevelMeterSettings MeterSettings
        {
            get => _meterSettings.Clone();
            set
            {
                _meterSettings = (value ?? new LevelMeterSettings()).Clone();
                _meterSettings.Normalize();
                if (!_meterSettings.PeakHold)
                {
                    Array.Clear(_peaks, 0, ChannelCount);
                    Array.Clear(_peakHoldRemaining, 0, ChannelCount);
                }
                Invalidate();
            }
        }

        protected override Size DefaultSize => new Size(600, HeaderHeight + ChannelCount * 16);

        /// <summary>
        /// シーケンサーの状態を取り込み、変化があれば再描画する。画面の更新タイマーから呼ぶ。
        /// </summary>
        public void UpdateFrom(MidiSequencer sequencer)
        {
            bool changed = UpdateRows(sequencer);

            for (int channel = 0; channel < ChannelCount; channel++)
            {
                _outputSpins.SetValue(channel, sequencer.GetOutputChannel(channel) + 1);
                _transposeSpins.SetValue(channel, sequencer.GetChannelTranspose(channel));
                _octaveSpins.SetValue(channel, sequencer.GetChannelOctave(channel));

                int volume = sequencer.GetChannelVolume(channel);
                if (_volumes[channel] != volume)
                {
                    _volumes[channel] = volume;
                    changed = true;
                }
            }

            for (int channel = 0; channel < ChannelCount; channel++)
            {
                bool muted = sequencer.IsMuted(channel);
                bool solo = sequencer.IsSolo(channel);
                bool audible = sequencer.IsAudible(channel);
                if (_muted[channel] != muted || _solo[channel] != solo || _audible[channel] != audible)
                {
                    _muted[channel] = muted;
                    _solo[channel] = solo;
                    _audible[channel] = audible;
                    InvalidateBackground();
                    changed = true;
                }

                for (int note = 0; note < NoteCount; note++)
                {
                    byte state = sequencer.IsNoteOn(channel, note) ? KeyOn : sequencer.IsNoteSustained(channel, note) ? KeySustained : KeyOff;
                    if (_notes[channel, note] != state)
                    {
                        _notes[channel, note] = state;
                        changed = true;
                    }
                }

                for (int pedal = 0; pedal < PedalControllers.Length; pedal++)
                {
                    bool on = sequencer.IsPedalOn(channel, PedalControllers[pedal]);
                    if (_pedals[channel, pedal] != on)
                    {
                        _pedals[channel, pedal] = on;
                        changed = true;
                    }
                }

                int bend = sequencer.GetPitchBend(channel);
                int modulation = sequencer.GetModulation(channel);
                if (_pitchBends[channel] != bend || _modulations[channel] != modulation)
                {
                    _pitchBends[channel] = bend;
                    _modulations[channel] = modulation;
                    changed = true;
                }
            }

            for (int pedal = 0; pedal < PedalControllers.Length; pedal++)
            {
                bool blocked = sequencer.IsControllerBlocked(PedalControllers[pedal]);
                if (_pedalBlocked[pedal] != blocked)
                {
                    _pedalBlocked[pedal] = blocked;
                    changed = true;
                }
            }

            double elapsedSeconds = _frameClock.Elapsed.TotalSeconds;
            _frameClock.Restart();
            float decay = (float)Math.Pow(0.1, elapsedSeconds / _meterSettings.FallSeconds);
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                float level = Math.Max(_levels[channel] * decay, sequencer.TakePeakLevel(channel));
                if (level < 0.005f)
                {
                    level = 0;
                }
                if (level != _levels[channel])
                {
                    _levels[channel] = level;
                    changed = true;
                }

                if (_meterSettings.PeakHold && UpdatePeak(channel, level, elapsedSeconds, decay))
                {
                    changed = true;
                }
            }

            if (changed)
            {
                Invalidate();
            }
        }

        /// <summary>
        /// ピークホールドの印を動かす。メーターが印を超えたら印を上げ、決めた時間そのまま残してから、メーターと同じ速さで下げる。
        /// </summary>
        /// <returns>印の位置が変わったかどうか。</returns>
        private bool UpdatePeak(int channel, float level, double elapsedSeconds, float decay)
        {
            float peak = _peaks[channel];
            if (level >= peak)
            {
                peak = level;
                _peakHoldRemaining[channel] = _meterSettings.PeakHoldSeconds;
            }
            else if (_peakHoldRemaining[channel] > 0)
            {
                _peakHoldRemaining[channel] -= elapsedSeconds;
            }
            else
            {
                peak = Math.Max(level, peak * decay);
                if (peak < 0.005f)
                {
                    peak = 0;
                }
            }

            if (peak == _peaks[channel])
            {
                return false;
            }
            _peaks[channel] = peak;
            return true;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            for (int row = _firstRow; row < EndRow; row++)
            {
                int channel = _rowChannels[row];
                if (GetMuteBounds(row).Contains(e.Location))
                {
                    MuteClicked?.Invoke(this, new ChannelEventArgs(channel));
                    return;
                }
                if (GetSoloBounds(row).Contains(e.Location))
                {
                    SoloClicked?.Invoke(this, new ChannelEventArgs(channel));
                    return;
                }
            }

            if (HitTestPedal(e.Location, out int pedalRow, out int pedal))
            {
                PedalClicked?.Invoke(this, new PedalEventArgs(PedalControllers[pedal]));
                ShowPedalTip(pedalRow, pedal); // 切り替えた後の状態を出す
                return;
            }

            int volumeChannel = HitTestVolume(e.Location);
            if (volumeChannel >= 0)
            {
                if (e.Clicks >= 2)
                {
                    SetVolume(volumeChannel, MidiSequencer.DefaultChannelVolume);
                }
                else
                {
                    _volumeDragChannel = volumeChannel;
                    SetVolume(volumeChannel, VolumeFromX(e.X));
                }
                ShowVolumeTip(volumeChannel);
                return;
            }

            if (HitTestKey(e.Location, out int keyChannel, out int note, out int velocity))
            {
                PressKey(keyChannel, note, velocity);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_volumeDragChannel >= 0)
            {
                SetVolume(_volumeDragChannel, VolumeFromX(e.X));
                ShowVolumeTip(_volumeDragChannel);
                return;
            }

            int volumeChannel = _pressedNote < 0 ? HitTestVolume(e.Location) : -1;
            int pedalRow = -1;
            int pedal = -1;
            bool onPedal = _pressedNote < 0 && volumeChannel < 0 && HitTestPedal(e.Location, out pedalRow, out pedal);
            Cursor = volumeChannel >= 0 ? Cursors.SizeWE : onPedal ? Cursors.Hand : Cursors.Default;
            if (volumeChannel >= 0)
            {
                ShowVolumeTip(volumeChannel);
            }
            else if (onPedal)
            {
                ShowPedalTip(pedalRow, pedal);
            }
            else if (_pressedNote < 0 && HitTestBend(e.Location, out int bendRow))
            {
                int bendChannel = _rowChannels[bendRow];
                ShowTip(Texts.BendTip(_pitchBends[bendChannel], _modulations[bendChannel]),
                    new Point(GetBendAreaBounds(bendRow).Left, GetRowBounds(bendRow).Bottom + 2));
            }
            else
            {
                HideTip();
            }

            // 押したままドラッグしたら、同じチャンネルの別の鍵に移る
            if (_pressedNote >= 0
                && HitTestKey(e.Location, out int channel, out int note, out int velocity)
                && channel == _pressedChannel && note != _pressedNote)
            {
                ReleaseKey();
                PressKey(channel, note, velocity);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                ReleaseKey();
                EndVolumeDrag(e.Location);
            }
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            // ウィンドウの切り替えなどでマウスを離したことが届かない場合に備える
            ReleaseKey();
            EndVolumeDrag(PointToClient(MousePosition));
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_volumeDragChannel < 0)
            {
                HideTip();
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            // ボリュームの上では、1 目盛りで 1% ずつ変える
            int volumeChannel = HitTestVolume(e.Location);
            if (volumeChannel >= 0)
            {
                _volumeWheelDelta += e.Delta;
                int steps = _volumeWheelDelta / 120;
                _volumeWheelDelta -= steps * 120;
                if (steps != 0)
                {
                    SetVolume(volumeChannel, _volumes[volumeChannel] + steps);
                    ShowVolumeTip(volumeChannel);
                }
                return;
            }

            if (!_scrollBarNeeded || e.Delta == 0)
            {
                return;
            }
            int rows = -e.Delta * SystemInformation.MouseWheelScrollLines / 120;
            if (rows == 0)
            {
                rows = -Math.Sign(e.Delta);
            }
            ScrollTo(_firstRow + rows);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateLayout();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e); // スピンボックスにもフォントが引き継がれ、高さが変わる
            UpdateLayout();
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            InvalidateBackground();
        }

        public void ApplyTheme()
        {
            BackColor = Theme.Current.Surface;
            ForeColor = Theme.Current.Text;
            ThemeStyler.ApplyScrollBarTheme(_scrollBar);
            foreach (IThemeable child in Controls.OfType<IThemeable>())
            {
                child.ApplyTheme();
            }
            InvalidateBackground();
        }

        protected override void OnSystemColorsChanged(EventArgs e)
        {
            base.OnSystemColorsChanged(e);
            InvalidateBackground();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _background?.Dispose();
                _background = null;
                _toolTip.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
            {
                return;
            }

            if (_background == null)
            {
                _background = CreateBackground();
            }
            Graphics g = e.Graphics;
            g.DrawImageUnscaled(_background, 0, 0);

            Theme theme = Theme.Current;
            using (var noteBrush = new SolidBrush(theme.Accent))
            using (var mutedNoteBrush = new SolidBrush(theme.DisabledText))
            using (var sustainBrush = new SolidBrush(Color.FromArgb(theme.IsDark ? DarkSustainAlpha : SustainAlpha, theme.Accent)))
            using (var mutedSustainBrush = new SolidBrush(Color.FromArgb(theme.IsDark ? DarkSustainAlpha : SustainAlpha, theme.DisabledText)))
            using (var blackKeyBrush = new SolidBrush(theme.KeyBlack))
            using (var dimBrush = CreateDimBrush())
            using (var knobBrush = new SolidBrush(theme.Text))
            using (var knobPen = new Pen(theme.Text))
            using (var mutedKnobPen = new Pen(theme.DisabledText))
            using (var pedalBrush = new SolidBrush(PedalOnColor))
            using (var blockedPen = new Pen(BlockedPedalColor, 1.5f))
            using (var modulationBrush = new SolidBrush(ModulationColor))
            {
                for (int row = _firstRow; row < EndRow; row++)
                {
                    int channel = _rowChannels[row];
                    Rectangle keyboard = GetKeyboardBounds(row);
                    if (keyboard.Width > 0 && keyboard.Height > 0)
                    {
                        if (_audible[channel])
                        {
                            DrawActiveNotes(g, channel, keyboard, noteBrush, sustainBrush, blackKeyBrush, null);
                        }
                        else
                        {
                            DrawActiveNotes(g, channel, keyboard, mutedNoteBrush, mutedSustainBrush, blackKeyBrush, dimBrush);
                        }
                    }

                    for (int pedal = 0; pedal < PedalControllers.Length; pedal++)
                    {
                        Rectangle lamp = GetPedalBounds(row, pedal);
                        if (_pedals[channel, pedal])
                        {
                            g.FillRectangle(_audible[channel] ? pedalBrush : mutedNoteBrush, lamp.Left + 1, lamp.Top + 1, lamp.Width - 2, lamp.Height - 2);
                        }
                        if (_pedalBlocked[pedal])
                        {
                            // 音源に送らないペダルは、斜線を引く（曲データで踏まれていれば、ランプは点灯させたまま）
                            g.DrawLine(blockedPen, lamp.Left, lamp.Bottom - 1, lamp.Right - 1, lamp.Top);
                        }
                    }

                    DrawBendAndModulation(g, row, channel, _audible[channel] ? noteBrush : mutedNoteBrush, _audible[channel] ? modulationBrush : mutedNoteBrush);

                    Rectangle meter = GetMeterBounds(row);
                    DrawLevel(g, meter, channel);

                    // ボリューム：メーターを縦に横切る線と、下辺の ▲
                    int x = VolumeToX(meter, _volumes[channel]);
                    g.DrawLine(_audible[channel] ? knobPen : mutedKnobPen, x, meter.Top + 1, x, meter.Bottom - 2);
                    var knob = new[]
                    {
                        new Point(x, meter.Bottom - 1),
                        new Point(x - VolumeKnobSize, meter.Bottom - 1 + VolumeKnobSize),
                        new Point(x + VolumeKnobSize, meter.Bottom - 1 + VolumeKnobSize),
                    };
                    g.FillPolygon(_audible[channel] ? knobBrush : mutedNoteBrush, knob);
                }
            }
        }

        /// <summary>
        /// ピッチベンド（中央から左右に伸びるバー）とモジュレーション（左から伸びるバー）を描く。枠は背景の画像に描いてある。
        /// </summary>
        private void DrawBendAndModulation(Graphics g, int row, int channel, Brush bendBrush, Brush modulationBrush)
        {
            Rectangle bendBar = Rectangle.Inflate(GetBendBarBounds(row), -1, -1);
            int center = bendBar.Left + bendBar.Width / 2;
            int bendWidth = (int)Math.Round(bendBar.Width / 2.0 * _pitchBends[channel] / (_pitchBends[channel] < 0 ? 8192 : 8191));
            if (bendWidth > 0)
            {
                g.FillRectangle(bendBrush, center, bendBar.Top, bendWidth, bendBar.Height);
            }
            else if (bendWidth < 0)
            {
                g.FillRectangle(bendBrush, center + bendWidth, bendBar.Top, -bendWidth, bendBar.Height);
            }

            Rectangle modulationBar = Rectangle.Inflate(GetModulationBarBounds(row), -1, -1);
            int modulationWidth = (int)Math.Round(modulationBar.Width * _modulations[channel] / 127.0);
            if (modulationWidth > 0)
            {
                g.FillRectangle(modulationBrush, modulationBar.Left, modulationBar.Top, modulationWidth, modulationBar.Height);
            }
        }

        /// <summary>
        /// メーターの中身（レベルとピークホールドの印）を、表示タイプに合わせて描く。音を出さないチャンネルは灰色で描く。
        /// </summary>
        private void DrawLevel(Graphics g, Rectangle meter, int channel)
        {
            var inner = new Rectangle(meter.Left + 1, meter.Top + 1, meter.Width - 2, meter.Height - 2);
            float peak = _meterSettings.PeakHold ? _peaks[channel] : 0;
            LevelMeterRenderer.Draw(g, inner, _meterSettings.Style, _levels[channel], peak, _audible[channel]);
        }

        /// <summary>
        /// 表示するチャンネルを決め直す。変わった場合は true を返す。
        /// </summary>
        private bool UpdateRows(MidiSequencer sequencer)
        {
            MidiFile song = sequencer.Song;
            int count = 0;
            var rows = new int[ChannelCount];
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                // ソロ中のチャンネルは、解除できるように必ず表示する
                bool visible = !_showUsedChannelsOnly || song == null || song.HasNotes(channel) || sequencer.IsSolo(channel);
                if (visible)
                {
                    rows[count++] = channel;
                }
            }
            if (count == 0)
            {
                // ノートが 1 つも無い曲は、全チャンネルを表示する
                for (int channel = 0; channel < ChannelCount; channel++)
                {
                    rows[channel] = channel;
                }
                count = ChannelCount;
            }

            bool changed = count != _rowCount;
            for (int row = 0; row < count && !changed; row++)
            {
                changed = rows[row] != _rowChannels[row];
            }
            if (changed)
            {
                Array.Copy(rows, _rowChannels, ChannelCount);
                _rowCount = count;
                UpdateLayout();
            }
            return changed;
        }

        private void ScrollBar_ValueChanged(object sender, EventArgs e)
        {
            ScrollTo(_scrollBar.Value);
        }

        private void ScrollTo(int firstRow)
        {
            firstRow = Math.Max(0, Math.Min(firstRow, _rowCount - VisibleRowCount));
            if (firstRow != _firstRow)
            {
                _firstRow = firstRow;
                UpdateLayout();
            }
        }

        /// <summary>
        /// スクロールバーと、各行のスピンボックスの位置を決め直し、背景を描き直す。
        /// </summary>
        private void UpdateLayout()
        {
            if (_octaveSpins == null)
            {
                return; // 基底クラスのコンストラクターから呼ばれた（まだ子コントロールを作っていない）
            }

            _scrollBarNeeded = _rowCount * RowHeight > ClientSize.Height - HeaderHeight;
            int visibleRows = VisibleRowCount;
            _firstRow = _scrollBarNeeded ? Math.Max(0, Math.Min(_firstRow, _rowCount - visibleRows)) : 0;

            if (_scrollBarNeeded)
            {
                _scrollBar.Maximum = _rowCount - 1;
                _scrollBar.LargeChange = visibleRows;
                _scrollBar.Value = _firstRow;
            }
            _scrollBar.Visible = _scrollBarNeeded;

            var visible = new bool[ChannelCount];
            for (int row = _firstRow; row < EndRow; row++)
            {
                int channel = _rowChannels[row];
                Rectangle rowBounds = GetRowBounds(row);
                _outputSpins.Place(channel, NumberWidth, rowBounds);
                _transposeSpins.Place(channel, TransposeLeft, rowBounds);
                _octaveSpins.Place(channel, OctaveLeft + (OctaveColumnWidth - SpinWidth) / 2, rowBounds);
                visible[channel] = true;
            }
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                _outputSpins.SetVisible(channel, visible[channel]);
                _transposeSpins.SetVisible(channel, visible[channel]);
                _octaveSpins.SetVisible(channel, visible[channel]);
            }

            InvalidateBackground();
        }

        /// <summary>ボリュームを変え、変わっていれば <see cref="VolumeChanged"/> を発生させる。</summary>
        private void SetVolume(int channel, int volume)
        {
            volume = Math.Max(MidiSequencer.MinChannelVolume, Math.Min(MidiSequencer.MaxChannelVolume, volume));
            if (volume == _volumes[channel])
            {
                return;
            }
            _volumes[channel] = volume;
            Invalidate();
            VolumeChanged?.Invoke(this, new ChannelValueEventArgs(channel, volume));
        }

        private void EndVolumeDrag(Point location)
        {
            if (_volumeDragChannel < 0)
            {
                return;
            }
            _volumeDragChannel = -1;
            if (!ClientRectangle.Contains(location))
            {
                HideTip();
            }
        }

        /// <summary>マウスの位置にある、ピッチベンド・モジュレーションの列の行を求める。</summary>
        private bool HitTestBend(Point point, out int row)
        {
            for (row = _firstRow; row < EndRow; row++)
            {
                Rectangle area = GetBendAreaBounds(row);
                if (point.X >= area.Left && point.X < area.Right && GetRowBounds(row).Contains(point))
                {
                    return true;
                }
            }
            row = -1;
            return false;
        }

        /// <summary>ペダルの名前と、音源に送るかどうかを、ランプの下にツールチップで表示する。</summary>
        private void ShowPedalTip(int row, int pedal)
        {
            Rectangle lamp = GetPedalBounds(row, pedal);
            ShowTip(Texts.PedalTip(PedalControllers[pedal], _pedalBlocked[pedal]), new Point(lamp.Left, GetRowBounds(row).Bottom + 2));
        }

        /// <summary>そのチャンネルのボリュームを、メーターの下にツールチップで表示する。</summary>
        private void ShowVolumeTip(int channel)
        {
            int row = Array.IndexOf(_rowChannels, channel, 0, _rowCount);
            if (row >= 0)
            {
                ShowTip(Texts.VolumeTip(_volumes[channel]), new Point(GetMeterBounds(row).Left, GetRowBounds(row).Bottom + 2));
            }
        }

        /// <summary>ツールチップを出す。同じ文字列を同じ位置に出し直すことはしない（ちらつくため）。</summary>
        private void ShowTip(string text, Point location)
        {
            if (text == _tipText && location == _tipLocation)
            {
                return;
            }
            _tipText = text;
            _tipLocation = location;
            _toolTip.Show(text, this, location);
        }

        private void HideTip()
        {
            if (_tipText == null)
            {
                return;
            }
            _tipText = null;
            _toolTip.Hide(this);
        }

        /// <summary>
        /// その位置（このコントロールの座標）にあるペダルのランプの CC 番号。ランプが無ければ -1。右クリックメニューで使う。
        /// </summary>
        public int GetPedalControllerAt(Point point)
        {
            return HitTestPedal(point, out _, out int pedal) ? PedalControllers[pedal] : -1;
        }

        /// <summary>マウスの位置にあるペダルのランプを求める。</summary>
        private bool HitTestPedal(Point point, out int row, out int pedal)
        {
            for (row = _firstRow; row < EndRow; row++)
            {
                for (pedal = 0; pedal < PedalControllers.Length; pedal++)
                {
                    Rectangle lamp = GetPedalBounds(row, pedal);
                    lamp.Inflate(1, 2);
                    if (lamp.Contains(point))
                    {
                        return true;
                    }
                }
            }
            row = -1;
            pedal = -1;
            return false;
        }

        /// <summary>マウスの位置にあるボリューム（メーターの列で、その行の高さ全体）のチャンネル。無ければ -1。</summary>
        private int HitTestVolume(Point point)
        {
            for (int row = _firstRow; row < EndRow; row++)
            {
                Rectangle meter = GetMeterBounds(row);
                Rectangle rowBounds = GetRowBounds(row);
                var bounds = new Rectangle(meter.Left - VolumeKnobSize, rowBounds.Top, meter.Width + VolumeKnobSize * 2, rowBounds.Height);
                if (bounds.Contains(point))
                {
                    return _rowChannels[row];
                }
            }
            return -1;
        }

        /// <summary>ボリューム（%）を、メーターの内側の左端（0%）から右端（最大）までの位置に置く。</summary>
        private static int VolumeToX(Rectangle meter, int volume)
        {
            return meter.Left + 1 + (int)Math.Round((meter.Width - 3) * volume / (double)MidiSequencer.MaxChannelVolume);
        }

        private int VolumeFromX(int x)
        {
            int left = MeterLeft + 1;
            double ratio = (x - left) / (double)(MeterWidth - 3);
            return (int)Math.Round(Math.Max(0, Math.Min(1, ratio)) * MidiSequencer.MaxChannelVolume);
        }

        private void PressKey(int channel, int note, int velocity)
        {
            _pressedChannel = channel;
            _pressedNote = note;
            Invalidate();
            NotePressed?.Invoke(this, new KeyboardNoteEventArgs(channel, note, velocity));
        }

        private void ReleaseKey()
        {
            if (_pressedNote < 0)
            {
                return;
            }
            int channel = _pressedChannel;
            int note = _pressedNote;
            _pressedChannel = -1;
            _pressedNote = -1;
            Invalidate();
            NoteReleased?.Invoke(this, new KeyboardNoteEventArgs(channel, note, 0));
        }

        /// <summary>
        /// マウスの位置にある鍵と、その位置に応じたベロシティ（鍵の上端で 1、下端で 127）を求める。
        /// </summary>
        private bool HitTestKey(Point point, out int channel, out int note, out int velocity)
        {
            for (int row = _firstRow; row < EndRow; row++)
            {
                Rectangle keyboard = GetKeyboardBounds(row);
                if (keyboard.Width <= 0 || keyboard.Height <= 0 || !keyboard.Contains(point))
                {
                    continue;
                }
                channel = _rowChannels[row];

                // 黒鍵は白鍵の上に重なっているので、先に調べる
                for (note = 0; note < NoteCount; note++)
                {
                    if (KeyIsBlack[note % 12])
                    {
                        RectangleF blackKey = GetKeyBounds(keyboard, note);
                        if (blackKey.Contains(point))
                        {
                            velocity = VelocityFromY(point.Y, blackKey);
                            return true;
                        }
                    }
                }

                float whiteWidth = keyboard.Width / (float)WhiteKeyCount;
                int whiteIndex = Math.Min(WhiteKeyCount - 1, (int)((point.X - keyboard.Left) / whiteWidth));
                note = whiteIndex / 7 * 12 + WhiteKeyPitch[whiteIndex % 7];
                velocity = VelocityFromY(point.Y, GetKeyBounds(keyboard, note));
                return true;
            }

            channel = -1;
            note = -1;
            velocity = 0;
            return false;
        }

        private static int VelocityFromY(int y, RectangleF key)
        {
            double ratio = (y - key.Top) / Math.Max(1.0, key.Height - 1);
            ratio = Math.Max(0, Math.Min(1, ratio));
            return 1 + (int)Math.Round(ratio * 126);
        }

        /// <summary>鍵の表示（曲で鳴っているか、マウスで押していれば点灯、ダンパーペダルで伸びていれば薄く点灯）。</summary>
        private byte KeyState(int channel, int note)
        {
            return channel == _pressedChannel && note == _pressedNote ? KeyOn : _notes[channel, note];
        }

        /// <summary>
        /// 鳴っている白鍵を塗り、その上に黒鍵を描き直す（白鍵の塗りが両隣の黒鍵に重なるため）。
        /// ダンパーペダルで伸びている鍵は、半透明の色を重ねて薄く塗る。
        /// </summary>
        /// <param name="sustainBrush">ペダルで伸びている鍵に重ねる、半透明のブラシ。</param>
        /// <param name="dimBrush">薄く表示するチャンネルなら、背景と同じく黒鍵に重ねるブラシ。そうでなければ null。</param>
        private void DrawActiveNotes(Graphics g, int channel, Rectangle keyboard, Brush noteBrush, Brush sustainBrush, Brush blackKeyBrush, Brush dimBrush)
        {
            for (int note = 0; note < NoteCount; note++)
            {
                byte state = KeyState(channel, note);
                if (state != KeyOff && !KeyIsBlack[note % 12])
                {
                    RectangleF key = GetKeyBounds(keyboard, note);
                    g.FillRectangle(state == KeyOn ? noteBrush : sustainBrush, key.X + 1, key.Y + 1, key.Width - 1, key.Height - 2);
                }
            }

            for (int note = 0; note < NoteCount; note++)
            {
                if (!KeyIsBlack[note % 12])
                {
                    continue;
                }
                byte state = KeyState(channel, note);
                if (state == KeyOn)
                {
                    g.FillRectangle(noteBrush, GetKeyBounds(keyboard, note));
                }
                else if (state == KeySustained || KeyState(channel, note - 1) != KeyOff || KeyState(channel, note + 1) != KeyOff)
                {
                    // 黒鍵の両隣は必ず白鍵で、範囲（0～127）にも収まる
                    RectangleF key = GetKeyBounds(keyboard, note);
                    g.FillRectangle(blackKeyBrush, key);
                    if (dimBrush != null)
                    {
                        g.FillRectangle(dimBrush, key);
                    }
                    if (state == KeySustained)
                    {
                        g.FillRectangle(sustainBrush, key);
                    }
                }
            }
        }

        /// <summary>音を出さないチャンネルを薄く見せるため、背景色を半透明で重ねるブラシを作る。</summary>
        private Brush CreateDimBrush()
        {
            return new SolidBrush(Color.FromArgb(150, BackColor));
        }

        private Bitmap CreateBackground()
        {
            var bitmap = new Bitmap(ClientSize.Width, ClientSize.Height);
            Theme theme = Theme.Current;
            using (Graphics g = Graphics.FromImage(bitmap))
            using (var whiteKeyBrush = new SolidBrush(theme.KeyWhite))
            using (var borderPen = new Pen(theme.KeyBorder))
            using (var blackKeyBrush = new SolidBrush(theme.KeyBlack))
            using (var meterBackBrush = new SolidBrush(theme.MeterBack))
            using (var meterBorderPen = new Pen(theme.MeterBorder))
            using (var dimBrush = CreateDimBrush())
            {
                g.Clear(BackColor);

                // 見出し
                Color headerColor = theme.MutedText;
                DrawCenteredText(g, "TR", new Rectangle(0, 0, NumberWidth, HeaderHeight), headerColor);
                DrawCenteredText(g, "CH", new Rectangle(NumberWidth, 0, SpinWidth, HeaderHeight), headerColor);
                DrawCenteredText(g, "M", new Rectangle(ToggleLeft, 0, ToggleWidth, HeaderHeight), headerColor);
                DrawCenteredText(g, "S", new Rectangle(ToggleLeft + ToggleWidth, 0, ToggleWidth, HeaderHeight), headerColor);
                Rectangle firstKeyboard = GetKeyboardBounds(_firstRow);
                TextRenderer.DrawText(g, Texts.KeyboardHeader, Font, new Rectangle(firstKeyboard.Left, 0, Math.Max(0, firstKeyboard.Width), HeaderHeight), headerColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                DrawCenteredText(g, Texts.PedalHeader, new Rectangle(PedalLeft, 0, PedalColumnWidth, HeaderHeight), headerColor);
                DrawCenteredText(g, "PB/Mod", new Rectangle(BendLeft, 0, BendColumnWidth, HeaderHeight), headerColor);
                Rectangle firstMeter = GetMeterBounds(_firstRow);
                TextRenderer.DrawText(g, Texts.LevelHeader, Font, new Rectangle(firstMeter.Left, 0, MeterWidth, HeaderHeight), headerColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                DrawCenteredText(g, Texts.TransposeHeader, new Rectangle(TransposeLeft, 0, SpinWidth, HeaderHeight), headerColor);
                DrawCenteredText(g, Texts.OctaveHeader, new Rectangle(OctaveLeft, 0, OctaveColumnWidth, HeaderHeight), headerColor);

                for (int row = _firstRow; row < EndRow; row++)
                {
                    int channel = _rowChannels[row];
                    Rectangle rowBounds = GetRowBounds(row);
                    Color textColor = _audible[channel] ? theme.Text : theme.DisabledText;
                    TextRenderer.DrawText(g, (channel + 1).ToString(), Font, new Rectangle(0, rowBounds.Top, NumberWidth - 4, rowBounds.Height), textColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);

                    DrawToggle(g, GetMuteBounds(row), "M", _muted[channel], MuteColor);
                    DrawToggle(g, GetSoloBounds(row), "S", _solo[channel], SoloColor);

                    Rectangle keyboard = GetKeyboardBounds(row);
                    if (keyboard.Width > 0 && keyboard.Height > 0)
                    {
                        g.FillRectangle(whiteKeyBrush, keyboard);
                        for (int note = 0; note < NoteCount; note++)
                        {
                            if (!KeyIsBlack[note % 12])
                            {
                                RectangleF key = GetKeyBounds(keyboard, note);
                                g.DrawLine(borderPen, key.Right, key.Top, key.Right, key.Bottom - 1);
                            }
                        }
                        for (int note = 0; note < NoteCount; note++)
                        {
                            if (KeyIsBlack[note % 12])
                            {
                                g.FillRectangle(blackKeyBrush, GetKeyBounds(keyboard, note));
                            }
                        }
                        g.DrawRectangle(borderPen, keyboard.Left, keyboard.Top, keyboard.Width - 1, keyboard.Height - 1);
                    }

                    for (int pedal = 0; pedal < PedalControllers.Length; pedal++)
                    {
                        Rectangle lamp = GetPedalBounds(row, pedal);
                        g.FillRectangle(meterBackBrush, lamp);
                        g.DrawRectangle(meterBorderPen, lamp.Left, lamp.Top, lamp.Width - 1, lamp.Height - 1);
                    }

                    // ピッチベンド（中央に目盛り）とモジュレーションの枠
                    Rectangle bendBar = GetBendBarBounds(row);
                    Rectangle modulationBar = GetModulationBarBounds(row);
                    foreach (Rectangle bar in new[] { bendBar, modulationBar })
                    {
                        g.FillRectangle(meterBackBrush, bar);
                        g.DrawRectangle(meterBorderPen, bar.Left, bar.Top, bar.Width - 1, bar.Height - 1);
                    }
                    int bendCenter = bendBar.Left + 1 + (bendBar.Width - 2) / 2;
                    g.DrawLine(meterBorderPen, bendCenter, bendBar.Top, bendCenter, bendBar.Bottom - 1);

                    Rectangle meter = GetMeterBounds(row);
                    g.FillRectangle(meterBackBrush, meter);
                    g.DrawRectangle(meterBorderPen, meter.Left, meter.Top, meter.Width - 1, meter.Height - 1);

                    // 音を出さないチャンネルは、鍵盤とメーターを薄く表示する
                    if (!_audible[channel])
                    {
                        g.FillRectangle(dimBrush, keyboard.Left, rowBounds.Top, meter.Right - keyboard.Left, rowBounds.Height);
                    }
                }
            }
            return bitmap;
        }

        /// <summary>ミュート・ソロのボタンを描く（オンは色で塗った角丸の四角、オフは枠だけ）。</summary>
        private void DrawToggle(Graphics g, Rectangle bounds, string text, bool isOn, Color onColor)
        {
            Theme theme = Theme.Current;
            SmoothingMode oldMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var shape = new RectangleF(bounds.Left + 0.5f, bounds.Top + 0.5f, bounds.Width - 1, bounds.Height - 1);
            using (GraphicsPath path = FlatPainter.RoundedRectangle(shape, 3))
            {
                if (isOn)
                {
                    using (var brush = new SolidBrush(onColor))
                    {
                        g.FillPath(brush, path);
                    }
                }
                else
                {
                    using (var brush = new SolidBrush(theme.ControlFill))
                    using (var pen = new Pen(theme.Border))
                    {
                        g.FillPath(brush, path);
                        g.DrawPath(pen, path);
                    }
                }
            }
            g.SmoothingMode = oldMode;
            DrawCenteredText(g, text, bounds, isOn ? Color.White : theme.MutedText);
        }

        private void DrawCenteredText(Graphics g, string text, Rectangle bounds, Color color)
        {
            TextRenderer.DrawText(g, text, Font, bounds, color, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        private void InvalidateBackground()
        {
            _background?.Dispose();
            _background = null;
            Invalidate();
        }

        // ---- レイアウト（引数はチャンネル番号ではなく、上から何行目か） ----

        /// <summary>1 行の高さ。CH のスピンボックスが収まる高さより低くはしない。</summary>
        private int RowHeight
        {
            get
            {
                int minimum = _outputSpins.Height;
                return Math.Min(Math.Max(MaxRowHeight, minimum), Math.Max(minimum, (ClientSize.Height - HeaderHeight) / _rowCount));
            }
        }

        /// <summary>全体が見えている行の数（1 以上）。</summary>
        private int VisibleRowCount => Math.Max(1, (ClientSize.Height - HeaderHeight) / RowHeight);

        /// <summary>表示している（一部だけ見えているものも含む）最後の行の、次の行。</summary>
        private int EndRow
        {
            get
            {
                int rowHeight = RowHeight;
                int rows = (Math.Max(0, ClientSize.Height - HeaderHeight) + rowHeight - 1) / rowHeight;
                return Math.Min(_rowCount, _firstRow + rows);
            }
        }

        /// <summary>スクロールバーを除いた幅。</summary>
        private int ContentWidth => ClientSize.Width - (_scrollBarNeeded ? _scrollBar.Width : 0);

        private static int ToggleLeft => NumberWidth + SpinWidth;

        /// <summary>レベルメーターの右の、移調の列の左端。</summary>
        private int TransposeLeft => ContentWidth - SpinWidth - OctaveColumnWidth;

        private int OctaveLeft => TransposeLeft + SpinWidth;

        /// <summary>レベルメーターの左端。</summary>
        private int MeterLeft => TransposeLeft - Gap - MeterWidth;

        /// <summary>ペダルとメーターの間の、ピッチベンド・モジュレーションの列の左端。</summary>
        private int BendLeft => MeterLeft - Gap - BendColumnWidth;

        /// <summary>鍵盤とピッチベンドの列の間の、ペダルの列の左端。</summary>
        private int PedalLeft => BendLeft - Gap - PedalColumnWidth;

        private Rectangle GetRowBounds(int row)
        {
            return new Rectangle(0, HeaderHeight + (row - _firstRow) * RowHeight, ContentWidth, RowHeight);
        }

        private Rectangle GetMuteBounds(int row)
        {
            return GetToggleBounds(row, ToggleLeft);
        }

        private Rectangle GetSoloBounds(int row)
        {
            return GetToggleBounds(row, ToggleLeft + ToggleWidth);
        }

        private Rectangle GetToggleBounds(int row, int left)
        {
            Rectangle rowBounds = GetRowBounds(row);
            int height = Math.Min(MaxToggleHeight, rowBounds.Height - 2);
            return new Rectangle(left + 2, rowBounds.Top + (rowBounds.Height - height) / 2, ToggleWidth - 4, height);
        }

        private Rectangle GetKeyboardBounds(int row)
        {
            Rectangle rowBounds = GetRowBounds(row);
            int left = ToggleLeft + ToggleWidth * 2 + Gap;
            int right = PedalLeft - Gap;
            return new Rectangle(left, rowBounds.Top + 1, right - left, rowBounds.Height - 2);
        }

        /// <summary>ペダルのランプ（<paramref name="pedal"/> は左から何番目か）。</summary>
        private Rectangle GetPedalBounds(int row, int pedal)
        {
            Rectangle rowBounds = GetRowBounds(row);
            int height = Math.Max(4, Math.Min(MaxPedalLampHeight, rowBounds.Height / 2));
            int lampsWidth = PedalControllers.Length * PedalLampWidth + (PedalControllers.Length - 1) * PedalLampGap;
            int left = PedalLeft + (PedalColumnWidth - lampsWidth) / 2 + pedal * (PedalLampWidth + PedalLampGap);
            return new Rectangle(left, rowBounds.Top + (rowBounds.Height - height) / 2, PedalLampWidth, height);
        }

        private Rectangle GetMeterBounds(int row)
        {
            Rectangle rowBounds = GetRowBounds(row);
            int height = Math.Max(4, Math.Min(MaxMeterHeight, rowBounds.Height / 2));
            return new Rectangle(MeterLeft, rowBounds.Top + (rowBounds.Height - height) / 2, MeterWidth, height);
        }

        /// <summary>ピッチベンドのバー（列の上半分）。</summary>
        private Rectangle GetBendBarBounds(int row)
        {
            Rectangle area = GetBendAreaBounds(row);
            return new Rectangle(area.Left, area.Top, area.Width, (area.Height - 1) / 2);
        }

        /// <summary>モジュレーションのバー（列の下半分）。</summary>
        private Rectangle GetModulationBarBounds(int row)
        {
            Rectangle area = GetBendAreaBounds(row);
            int height = (area.Height - 1) / 2;
            return new Rectangle(area.Left, area.Bottom - height, area.Width, height);
        }

        /// <summary>ピッチベンド・モジュレーションの 2 本のバーを合わせた範囲（高さはメーターと同じ。ただし低すぎないように）。</summary>
        private Rectangle GetBendAreaBounds(int row)
        {
            Rectangle rowBounds = GetRowBounds(row);
            int height = Math.Max(9, Math.Min(MaxMeterHeight, rowBounds.Height - 6));
            return new Rectangle(BendLeft, rowBounds.Top + (rowBounds.Height - height) / 2, BendColumnWidth, height);
        }

        private static RectangleF GetKeyBounds(Rectangle keyboard, int note)
        {
            float whiteWidth = keyboard.Width / (float)WhiteKeyCount;
            int octave = note / 12;
            int pitchClass = note % 12;
            float whiteLeft = keyboard.Left + (octave * 7 + KeyWhiteIndex[pitchClass]) * whiteWidth;

            if (!KeyIsBlack[pitchClass])
            {
                return new RectangleF(whiteLeft, keyboard.Top, whiteWidth, keyboard.Height);
            }

            // 黒鍵は左隣の白鍵との境目をまたぐように置く
            float blackWidth = whiteWidth * 0.6f;
            return new RectangleF(whiteLeft + whiteWidth - blackWidth / 2, keyboard.Top, blackWidth, keyboard.Height * 0.6f);
        }

        /// <summary>
        /// 行ごと（チャンネルごと）に 1 つずつ置くスピンボックスの列。表示している行に合わせて置き直す。
        /// </summary>
        private sealed class SpinColumn
        {
            private readonly NumericUpDown[] _spins = new NumericUpDown[ChannelCount];

            /// <summary>
            /// 表示している値。NumericUpDown.Value は、入力中に読むとその時点の文字列で確定してしまうので、
            /// 値はこちらで覚えておく。
            /// </summary>
            private readonly int[] _values = new int[ChannelCount];

            private readonly Action<int, int> _valueChanged;

            /// <summary>値を、シーケンサーの状態に合わせて書き換えている最中か。</summary>
            private bool _syncing;

            /// <param name="valueChanged">操作で値が変わったときに、チャンネルと新しい値を受け取る。</param>
            public SpinColumn(Control parent, int minimum, int maximum, Func<int, int> initialValue, Action<int, int> valueChanged)
            {
                _valueChanged = valueChanged;
                for (int channel = 0; channel < ChannelCount; channel++)
                {
                    int spinChannel = channel;
                    _values[channel] = initialValue(channel);
                    var spin = new StepNumericUpDown
                    {
                        Minimum = minimum,
                        Maximum = maximum,
                        Value = _values[channel],
                        Width = SpinWidth - 4,
                        Visible = false,
                    };
                    spin.ValueChanged += (sender, e) => OnValueChanged(spinChannel);
                    _spins[channel] = spin;
                    parent.Controls.Add(spin);
                }
            }

            /// <summary>スピンボックスの高さ（フォントで決まる）。</summary>
            public int Height => _spins[0].Height;

            /// <summary>値を書き換える。<c>valueChanged</c> は呼ばない。</summary>
            public void SetValue(int channel, int value)
            {
                if (_values[channel] == value)
                {
                    return;
                }
                _values[channel] = value;
                _syncing = true;
                try
                {
                    _spins[channel].Value = value;
                }
                finally
                {
                    _syncing = false;
                }
            }

            /// <summary>列の左端 <paramref name="left"/> と行の範囲に合わせて置く。</summary>
            public void Place(int channel, int left, Rectangle rowBounds)
            {
                NumericUpDown spin = _spins[channel];
                spin.Location = new Point(left + 2, rowBounds.Top + (rowBounds.Height - spin.Height) / 2);
            }

            public void SetVisible(int channel, bool visible)
            {
                _spins[channel].Visible = visible;
            }

            private void OnValueChanged(int channel)
            {
                if (_syncing)
                {
                    return;
                }
                int value = (int)_spins[channel].Value;
                if (value == _values[channel])
                {
                    return;
                }
                _values[channel] = value;
                _valueChanged(channel, value);
            }
        }
    }
}
