using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace KMidiPlayer.Midi
{
    /// <summary>
    /// MIDI ファイルのイベントを、専用スレッドで正しいタイミングに音源へ送る。
    /// </summary>
    /// <remarks>
    /// 公開メソッドは画面（UI）のスレッドから呼ぶこと。
    /// <see cref="SongEnded"/> は再生スレッドから発生するので、受け取る側で UI スレッドに処理を移す必要がある。
    /// </remarks>
    public sealed class MidiSequencer : IDisposable
    {
        public const int ChannelCount = 16;
        public const int NoteCount = 128;

        public const int MinTempoPercent = 25;
        public const int MaxTempoPercent = 400;
        public const int MinTranspose = -12;
        public const int MaxTranspose = 12;
        public const int MinChannelTranspose = -12;
        public const int MaxChannelTranspose = 12;
        public const int MinChannelOctave = -4;
        public const int MaxChannelOctave = 4;
        public const int MinChannelVolume = 0;
        public const int MaxChannelVolume = 150;
        public const int DefaultChannelVolume = 100;
        public const int MinVelocityCurve = -12;
        public const int MaxVelocityCurve = 12;

        // ペダルのコントロールチェンジ番号（値が 64 以上でオン）
        public const int DamperPedal = 64;
        public const int SostenutoPedal = 66;
        public const int SoftPedal = 67;

        /// <summary>これより短い先頭の無音は、とばさない。</summary>
        public static readonly TimeSpan MinLeadingSilence = TimeSpan.FromSeconds(0.3);

        private const int AllChannelsMask = 0xFFFF;
        private const int ModulationController = 1;
        private const int PitchBendCenter = 8192;
        private const int VolumeController = 7;

        // オン / オフで使う CC（ダンパー・ポルタメント・ソステヌート・ソフト・レガート・ホールド 2）
        private const int FirstSwitchController = 64;
        private const int LastSwitchController = 69;
        private const int PedalCount = 3;
        private const int DefaultVolume = 100;
        private const int DefaultExpression = 127;
        private const int DrumChannel = 9; // 10ch

        // _noteMap の値: 移調後の音高に加える「音源に送った」印と、音域外で鳴らさなかったことを表す値
        private const int NoteSentFlag = 0x100;
        private const int NoteOutOfRange = -1;

        private readonly Stopwatch _clock = new Stopwatch();

        /// <summary>再生位置の計算に使う値（_startPosition、_anchorElapsed、_tempoRate）を守るロック。</summary>
        private readonly object _timeLock = new object();

        // 音源に送って、まだノートオフを送っていないノートオンの数（停止時に確実に消すため）。添字は移調後の音高。
        // 同じ音高を重ねて鳴らす曲もあるので、数で持つ
        private readonly int[,] _activeNotes = new int[ChannelCount, NoteCount];

        // 曲データ上のノートごとの、鳴っているノートオンの記録（先に鳴った順）。
        // ノートオフを、対応するノートオンと同じ移調量で送るために使う。
        // 同じ音高のノートオンが重なっても、ノートオフで先に鳴った方から 1 つずつ取り出す（ピアノロールの対応づけと同じ）。
        // 値は移調後の音高に、音源に送ったかどうか（NoteSentFlag）を加えたもの。音域外で送らなかったものは NoteOutOfRange
        private readonly List<int>[,] _noteMap = new List<int>[ChannelCount, NoteCount];

        // 画面表示用の状態（ミュート中のチャンネルも含め、曲データどおりに更新する）。鳴っているノートオンの数
        private readonly int[,] _displayNotes = new int[ChannelCount, NoteCount];

        // 画面表示用の、ダンパーペダルを踏んでいる間にノートオフになった音（ペダルを離すまで伸びている音）。添字は移調後の音高
        private readonly bool[,] _sustainedNotes = new bool[ChannelCount, NoteCount];
        private readonly float[] _peakLevels = new float[ChannelCount];
        private readonly int[] _volumes = new int[ChannelCount];
        private readonly int[] _expressions = new int[ChannelCount];

        // ペダル（ダンパー・ソステヌート・ソフト）がオンかどうか。添字は PedalIndex の値
        private readonly bool[,] _pedals = new bool[ChannelCount, PedalCount];

        // 画面表示用の、ピッチベンド（-8192～8191、0 が中央）とモジュレーション（CC1、0～127）
        private readonly int[] _pitchBends = new int[ChannelCount];
        private readonly int[] _modulations = new int[ChannelCount];

        // 鍵盤のクリックで鳴らしている音の送信先のチャンネル（鳴らしていなければ -1。UI スレッドだけで使う）
        private readonly int[,] _manualNoteChannels = new int[ChannelCount, NoteCount];

        // 曲データのチャンネルごとの、送信先のチャンネル（画面で指定した値）
        private readonly int[] _outputChannels = new int[ChannelCount];

        // 再生スレッドが反映済みの送信先。曲のイベントはこちらの値で送る
        private readonly int[] _appliedOutputChannels = new int[ChannelCount];

        /// <summary>曲データから音源に送らない CC（添字が CC 番号）。再生スレッドも読む（bool の読み書きは分割されない）。</summary>
        private readonly bool[] _blockedControllers = new bool[128];

        private readonly bool[] _muted = new bool[ChannelCount];
        private readonly bool[] _solo = new bool[ChannelCount];

        /// <summary>音を出すチャンネルのビットマスク（ビット n がチャンネル n）。</summary>
        private volatile int _audibleMask = AllChannelsMask;

        /// <summary>再生スレッドが反映済みの <see cref="_audibleMask"/>。</summary>
        private int _appliedAudibleMask = AllChannelsMask;

        private volatile int _transpose;

        private int _velocityCurve;

        /// <summary>ベロシティカーブの変換表（添字が曲のベロシティ）。変更時は表ごと差し替える（再生スレッドが読むため）。</summary>
        private volatile int[] _velocityTable = CreateVelocityTable(0);

        // 曲データのチャンネルごとの移調（半音）とオクターブ。ノートオンを送るときに読む
        private readonly int[] _channelTransposes = new int[ChannelCount];
        private readonly int[] _channelOctaves = new int[ChannelCount];

        // 曲データのチャンネルごとのボリューム（%。画面で指定した値）。曲のボリューム（CC7）にこの倍率を掛けて送る
        private readonly int[] _channelVolumes = new int[ChannelCount];

        // 再生スレッドが反映済みの _channelVolumes
        private readonly int[] _appliedChannelVolumes = new int[ChannelCount];

        private volatile LoopRange _loop;
        private int _tempoPercent = 100;

        private MidiOutDevice _output;
        private MidiFile _song;
        private Thread _thread;
        private volatile bool _stopRequested;
        private volatile bool _reachedEnd;
        private bool _isRunning;

        /// <summary>
        /// 基準の時点での曲上の位置。再生中は、_clock が _anchorElapsed を指した時点の位置を表す。
        /// </summary>
        private TimeSpan _startPosition;

        /// <summary>基準の時点での _clock の経過時間。</summary>
        private TimeSpan _anchorElapsed;

        /// <summary>テンポの倍率（1.0 = 100%）。</summary>
        private double _tempoRate = 1.0;

        /// <summary>次に送るイベントのインデックス。</summary>
        private int _nextIndex;

        /// <summary>曲の先頭からこの数までの SysEx は、現在のデバイスに送信済み。</summary>
        private int _sysExSentCount;

        public MidiSequencer()
        {
            ResetControllerState();
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                for (int note = 0; note < NoteCount; note++)
                {
                    _noteMap[channel, note] = new List<int>(2);
                }
                _outputChannels[channel] = channel;
                _appliedOutputChannels[channel] = channel;
                _channelVolumes[channel] = DefaultChannelVolume;
                _appliedChannelVolumes[channel] = DefaultChannelVolume;
                for (int note = 0; note < NoteCount; note++)
                {
                    _manualNoteChannels[channel, note] = -1;
                }
            }
        }

        /// <summary>曲の最後まで再生したときに発生する（再生スレッドから呼ばれる）。</summary>
        public event EventHandler SongEnded;

        public MidiFile Song => _song;

        public bool IsPlaying => _isRunning && !_reachedEnd;

        /// <summary>曲を先頭から再生する前に、自動で送るリセットの種類。</summary>
        public MidiResetType AutoReset { get; set; } = MidiResetType.GS;

        /// <summary>現在の再生位置（曲の上での時間。テンポを変えても曲の長さは変わらない）。</summary>
        public TimeSpan Position
        {
            get
            {
                if (_song == null)
                {
                    return TimeSpan.Zero;
                }
                TimeSpan position = _isRunning ? CurrentSongTime() : _startPosition;
                return position < _song.Duration ? position : _song.Duration;
            }
        }

        /// <summary>テンポ（%）。100 で曲どおりの速さ。</summary>
        public int TempoPercent
        {
            get => _tempoPercent;
            set
            {
                value = Math.Max(MinTempoPercent, Math.Min(MaxTempoPercent, value));
                lock (_timeLock)
                {
                    if (_isRunning)
                    {
                        // ここまでは古いテンポで進んだものとして、基準の時点を今に移す
                        _startPosition = CurrentSongTime();
                        _anchorElapsed = _clock.Elapsed;
                    }
                    _tempoRate = value / 100.0;
                }
                _tempoPercent = value;
            }
        }

        /// <summary>
        /// 曲全体の移調量（半音単位）。10ch（ドラム）のデータと、10ch へ送るデータには適用しない。
        /// チャンネルごとの移調・オクターブは別に設定する（<see cref="SetChannelTranspose"/>）。
        /// </summary>
        public int Transpose
        {
            get => _transpose;
            set => _transpose = Math.Max(MinTranspose, Math.Min(MaxTranspose, value));
        }

        /// <summary>
        /// 曲のすべてのノート（ドラムを含む）にかけるベロシティカーブ（-12～+12）。0 で曲のまま。
        /// + にするほど弱い音を強めに（強弱の差を小さく）、- にするほど弱い音をより弱く（強弱の差を大きく）する。
        /// 鍵盤のクリックで鳴らす音にはかけない。変更は次のノートオンから反映される。
        /// </summary>
        public int VelocityCurve
        {
            get => _velocityCurve;
            set
            {
                value = Math.Max(MinVelocityCurve, Math.Min(MaxVelocityCurve, value));
                if (value != _velocityCurve)
                {
                    _velocityCurve = value;
                    _velocityTable = CreateVelocityTable(value);
                }
            }
        }

        /// <summary>
        /// ベロシティ（1～127）にカーブをかける。出力 = 127 ×（入力 ÷ 127）^γ、γ = 2^(-curve ÷ 8)。
        /// 1～127 の範囲に収め、127 は 127 のまま。例: curve = +12 で 64 → 100、-12 で 64 → 18。
        /// </summary>
        public static int ApplyVelocityCurve(int velocity, int curve)
        {
            if (curve == 0 || velocity <= 0)
            {
                return velocity;
            }
            double gamma = Math.Pow(2, -curve / 8.0);
            int output = (int)Math.Round(127 * Math.Pow(Math.Min(127, velocity) / 127.0, gamma));
            return Math.Max(1, Math.Min(127, output));
        }

        private static int[] CreateVelocityTable(int curve)
        {
            var table = new int[128];
            for (int velocity = 0; velocity < table.Length; velocity++)
            {
                table[velocity] = ApplyVelocityCurve(velocity, curve);
            }
            return table;
        }

        /// <summary>A-B 区間ループの開始位置。ループしていなければ null。</summary>
        public TimeSpan? LoopStart => _loop?.Start;

        /// <summary>A-B 区間ループの終了位置。ループしていなければ null。</summary>
        public TimeSpan? LoopEnd => _loop?.End;

        /// <summary>
        /// 出力先のデバイスを変える。古いデバイスの破棄は呼び出し側で行う。
        /// </summary>
        public void SetOutput(MidiOutDevice output)
        {
            bool wasPlaying = IsPlaying;
            StopThread();
            ReleaseManualNotes(); // 古いデバイスで鳴らしたままにしない
            _output = output;
            _sysExSentCount = 0; // 新しいデバイスには、まだ曲の SysEx を送っていない
            if (wasPlaying && _output != null)
            {
                StartThread();
            }
        }

        /// <summary>
        /// 曲を読み込み、先頭で停止した状態にする。A-B 区間ループは解除する。
        /// </summary>
        public void Load(MidiFile song)
        {
            StopThread();
            _song = song;
            ResetControllerState(); // 前の曲のペダルなどを表示に残さない
            _loop = null;
            _startPosition = TimeSpan.Zero;
            _nextIndex = 0;
            _sysExSentCount = 0;
        }

        /// <summary>
        /// 再生を止め、曲を読み込んでいない状態にする。
        /// </summary>
        public void Unload()
        {
            Load(null);
        }

        /// <summary>
        /// 現在の位置から再生する。曲の最後にいる場合は先頭から再生する。
        /// </summary>
        public void Play()
        {
            if (_song == null || _output == null || IsPlaying)
            {
                return;
            }
            StopThread(); // 曲の終わりに達して止まったスレッドを片付ける
            if (_startPosition >= _song.Duration)
            {
                _startPosition = TimeSpan.Zero;
                _nextIndex = 0;
            }

            if (_startPosition == TimeSpan.Zero)
            {
                // 曲の先頭から再生するので、自動リセットを送り、曲の SysEx も初めから送り直す
                // （無音をとばして途中から始めても、送るようにする）
                _sysExSentCount = 0;

                // 最初のノートの位置から始める（それまでの音色や SysEx は、途中から再生するときと同じく送る）
                if (SkipLeadingSilence && _song.FirstNoteTime >= MinLeadingSilence)
                {
                    _startPosition = _song.FirstNoteTime;
                    _nextIndex = FindEventIndex(_startPosition);
                }
            }
            StartThread();
        }

        /// <summary>
        /// 曲の先頭から再生するとき（<see cref="Play"/> で位置が 0 のとき）に、最初のノートまでの無音をとばすかどうか。
        /// 無音が <see cref="MinLeadingSilence"/> より短ければとばさない。
        /// </summary>
        public bool SkipLeadingSilence { get; set; } = true;

        /// <summary>
        /// 現在の位置で一時停止する。
        /// </summary>
        public void Pause()
        {
            StopThread();
        }

        /// <summary>
        /// 停止して、先頭に戻る。
        /// </summary>
        public void Stop()
        {
            StopThread();
            _startPosition = TimeSpan.Zero;
            _nextIndex = 0;
        }

        /// <summary>
        /// 再生位置を移動する。再生中なら、移動先から再生を続ける。
        /// </summary>
        public void Seek(TimeSpan position)
        {
            if (_song == null)
            {
                return;
            }
            if (position < TimeSpan.Zero)
            {
                position = TimeSpan.Zero;
            }
            if (position > _song.Duration)
            {
                position = _song.Duration;
            }

            bool wasPlaying = IsPlaying;
            StopThread();
            _startPosition = position;
            _nextIndex = FindEventIndex(position);
            if (wasPlaying)
            {
                StartThread();
            }
        }

        /// <summary>
        /// A-B 区間ループを設定する。再生位置が終了位置に達すると、開始位置に戻る。
        /// </summary>
        public void SetLoop(TimeSpan start, TimeSpan end)
        {
            if (end <= start)
            {
                throw new ArgumentException("終了位置は開始位置より後にしてください。", nameof(end));
            }
            _loop = new LoopRange(start, end);
        }

        public void ClearLoop()
        {
            _loop = null;
        }

        /// <summary>
        /// リセットメッセージを今すぐ音源に送る。
        /// </summary>
        public void SendReset(MidiResetType type)
        {
            byte[] message = MidiReset.GetMessage(type);
            if (_output == null || message == null)
            {
                return;
            }
            _output.SendLongMessage(message);
            ResetControllerState();
            SendAdjustedVolumes(); // リセットで曲どおりの音量に戻るので、倍率を掛け直す
        }

        // ---- 鍵盤のクリック ----

        /// <summary>
        /// 曲とは別に、ノートオンを送る（鍵盤のクリック用）。移調はかけず、ミュート中のチャンネルでも鳴らす。
        /// 送信先は <see cref="GetOutputChannel"/> のチャンネル。
        /// </summary>
        public void SendManualNoteOn(int channel, int note, int velocity)
        {
            if (_output == null)
            {
                return;
            }
            int outputChannel = _outputChannels[channel];
            _output.SendShortMessage(0x90 | outputChannel, note, velocity);
            _manualNoteChannels[channel, note] = outputChannel;

            RecordLevel(channel, velocity);
        }

        /// <summary>
        /// <see cref="SendManualNoteOn"/> で鳴らした音を止める。
        /// </summary>
        public void SendManualNoteOff(int channel, int note)
        {
            // 鳴らした後で送信先が変わっても、鳴らしたチャンネルに送る
            int outputChannel = _manualNoteChannels[channel, note];
            if (_output == null || outputChannel < 0)
            {
                return;
            }
            _output.SendShortMessage(0x80 | outputChannel, note, 0);
            _manualNoteChannels[channel, note] = -1;
        }

        private void ReleaseManualNotes()
        {
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                for (int note = 0; note < NoteCount; note++)
                {
                    SendManualNoteOff(channel, note);
                }
            }
        }

        // ---- ミュート・ソロ ----

        public bool IsMuted(int channel) => _muted[channel];

        public bool IsSolo(int channel) => _solo[channel];

        /// <summary>ミュート・ソロの設定を踏まえて、そのチャンネルの音を出すかどうか。</summary>
        public bool IsAudible(int channel) => (_audibleMask & (1 << channel)) != 0;

        public void SetMuted(int channel, bool muted)
        {
            _muted[channel] = muted;
            UpdateAudibleMask();
        }

        public void SetSolo(int channel, bool solo)
        {
            _solo[channel] = solo;
            UpdateAudibleMask();
        }

        public void ClearMuteAndSolo()
        {
            Array.Clear(_muted, 0, ChannelCount);
            Array.Clear(_solo, 0, ChannelCount);
            UpdateAudibleMask();
        }

        // ---- 送信先のチャンネル ----

        /// <summary>曲データのチャンネル（0～15）の音を送る、音源のチャンネル（0～15）。</summary>
        public int GetOutputChannel(int channel) => _outputChannels[channel];

        /// <summary>
        /// 曲データのチャンネルの音を送る、音源のチャンネルを変える。
        /// 再生中なら、鳴っている音を止め、そこまでの音色やコントロールチェンジを新しいチャンネルに送り直す。
        /// </summary>
        public void SetOutputChannel(int channel, int outputChannel)
        {
            if (outputChannel < 0 || outputChannel >= ChannelCount)
            {
                throw new ArgumentOutOfRangeException(nameof(outputChannel));
            }

            // 再生中は、切り替えの処理を再生スレッドが行う（ノートの送信と競合させないため）
            Volatile.Write(ref _outputChannels[channel], outputChannel);
            if (!_isRunning)
            {
                _appliedOutputChannels[channel] = outputChannel;
            }
        }

        /// <summary>すべてのチャンネルの送信先を、曲データと同じチャンネルに戻す。</summary>
        public void ResetOutputChannels()
        {
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                SetOutputChannel(channel, channel);
            }
        }

        // ---- チャンネルごとの移調・オクターブ ----

        /// <summary>
        /// 曲データのチャンネルごとの移調（半音）。全体のキー（<see cref="Transpose"/>）に加えてかかる。
        /// 10ch（ドラム）にもかける。変更は、次に送るノートオンから反映される。
        /// </summary>
        public int GetChannelTranspose(int channel) => _channelTransposes[channel];

        public void SetChannelTranspose(int channel, int semitones)
        {
            semitones = Math.Max(MinChannelTranspose, Math.Min(MaxChannelTranspose, semitones));
            Volatile.Write(ref _channelTransposes[channel], semitones);
        }

        /// <summary>
        /// 曲データのチャンネルごとのオクターブ（12 半音単位）。全体のキーとチャンネルごとの移調に加えてかかる。
        /// </summary>
        public int GetChannelOctave(int channel) => _channelOctaves[channel];

        public void SetChannelOctave(int channel, int octaves)
        {
            octaves = Math.Max(MinChannelOctave, Math.Min(MaxChannelOctave, octaves));
            Volatile.Write(ref _channelOctaves[channel], octaves);
        }

        /// <summary>すべてのチャンネルの移調とオクターブを 0 に戻す。</summary>
        public void ResetChannelTransposes()
        {
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                SetChannelTranspose(channel, 0);
                SetChannelOctave(channel, 0);
            }
        }

        // ---- チャンネルごとのボリューム ----

        /// <summary>
        /// 曲データのチャンネルごとのボリューム（%）。曲のボリューム（CC7）にこの倍率を掛け、127 を上限として送る。
        /// 変更すると、すぐに送信先へボリュームを送り直す。
        /// </summary>
        public int GetChannelVolume(int channel) => _channelVolumes[channel];

        public void SetChannelVolume(int channel, int percent)
        {
            percent = Math.Max(MinChannelVolume, Math.Min(MaxChannelVolume, percent));

            // 再生中は、送り直す処理を再生スレッドが行う（曲のボリュームの送信と競合させないため）
            Volatile.Write(ref _channelVolumes[channel], percent);
            if (!_isRunning)
            {
                ApplyChannelVolumes();
            }
        }

        /// <summary>すべてのチャンネルのボリュームを 100% に戻す。</summary>
        public void ResetChannelVolumes()
        {
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                SetChannelVolume(channel, DefaultChannelVolume);
            }
        }

        // ---- 画面表示用 ----

        /// <summary>曲データ上で、そのノートが鳴っているかどうか（移調後の音高。ミュート中のチャンネルも含む）。</summary>
        public bool IsNoteOn(int channel, int note) => Volatile.Read(ref _displayNotes[channel, note]) > 0;

        /// <summary>
        /// 曲データ上で、ダンパーペダルを踏んでいる間にノートオフになり、ペダルで伸びている音かどうか（移調後の音高）。
        /// ペダルを離すと false に戻る。鳴っている（<see cref="IsNoteOn"/>）かどうかとは別に返す。
        /// </summary>
        public bool IsNoteSustained(int channel, int note) => _sustainedNotes[channel, note];

        // ---- 送らない CC ----

        /// <summary>その CC（0～127）を、曲データから音源に送らない設定にしているかどうか。</summary>
        public bool IsControllerBlocked(int controller) => _blockedControllers[controller];

        /// <summary>
        /// そのチャンネル（曲データのチャンネル）のノートを、今の設定で何半音ずらして送るか
        /// （全体のキー・チャンネルごとの移調・オクターブの合計。10ch のドラムには全体のキーをかけない）。
        /// </summary>
        public int GetNoteOffset(int channel)
        {
            bool isDrum = channel == DrumChannel || _outputChannels[channel] == DrumChannel;
            return (isDrum ? 0 : _transpose) + _channelTransposes[channel] + _channelOctaves[channel] * 12;
        }

        /// <summary>曲データ上の、そのチャンネルのピッチベンド（-8192～8191、0 が中央）。</summary>
        public int GetPitchBend(int channel) => _pitchBends[channel];

        /// <summary>曲データ上の、そのチャンネルのモジュレーション（CC1、0～127）。</summary>
        public int GetModulation(int channel) => _modulations[channel];

        /// <summary>
        /// その CC（0～127）を、曲データから音源に送らないようにする（または元に戻す）。全チャンネル共通。
        /// 途中から再生するときに送り直す状態や、チャンネルごとの音量で送り直すボリューム（CC7）にも効く。
        /// 停止時などにアプリ自身が送る音止め（CC120・CC123・ダンパーペダル オフ）は、設定にかかわらず送る。
        /// </summary>
        /// <remarks>
        /// ペダルのようなオン / オフの CC（64～69）を送らないようにしたときは、音源側で踏んだままにならないよう、
        /// 全チャンネルにオフ（値 0）を 1 度だけ送る。
        /// </remarks>
        public void SetControllerBlocked(int controller, bool blocked)
        {
            if (_blockedControllers[controller] == blocked)
            {
                return;
            }
            _blockedControllers[controller] = blocked;
            if (blocked && controller >= FirstSwitchController && controller <= LastSwitchController && _output != null)
            {
                for (int channel = 0; channel < ChannelCount; channel++)
                {
                    _output.SendShortMessage(0xB0 | channel, controller, 0);
                }
            }
        }

        /// <summary>
        /// 曲データ上で、そのペダル（<see cref="DamperPedal"/>、<see cref="SostenutoPedal"/>、<see cref="SoftPedal"/>）が
        /// 踏まれているかどうか（ミュート中のチャンネルも含む）。
        /// </summary>
        public bool IsPedalOn(int channel, int controller)
        {
            int index = PedalIndex(controller);
            return index >= 0 && _pedals[channel, index];
        }

        /// <summary>
        /// 前回呼び出してから今までに鳴ったノートの最大レベル（0～1）を返し、記録を消す。
        /// </summary>
        public float TakePeakLevel(int channel)
        {
            float level = _peakLevels[channel];
            _peakLevels[channel] = 0;
            return level;
        }

        public void Dispose()
        {
            StopThread();
        }

        private void UpdateAudibleMask()
        {
            bool anySolo = Array.IndexOf(_solo, true) >= 0;
            int mask = 0;
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                bool audible = anySolo ? _solo[channel] : !_muted[channel];
                if (audible)
                {
                    mask |= 1 << channel;
                }
            }

            // 再生中は、音を止める処理を再生スレッドが行う（ノートの送信と競合させないため）
            _audibleMask = mask;
            if (!_isRunning)
            {
                _appliedAudibleMask = mask;
            }
        }

        /// <summary>
        /// 再生中の、曲上の現在位置（曲の長さで切り詰めない）。
        /// </summary>
        private TimeSpan CurrentSongTime()
        {
            lock (_timeLock)
            {
                long elapsedTicks = (_clock.Elapsed - _anchorElapsed).Ticks;
                return _startPosition + TimeSpan.FromTicks((long)(elapsedTicks * _tempoRate));
            }
        }

        private void StartThread()
        {
            PrepareOutputState(_nextIndex);
            RebuildControllerState(_nextIndex);
            Array.Copy(_channelVolumes, _appliedChannelVolumes, ChannelCount);
            SendAdjustedVolumes();

            _appliedAudibleMask = _audibleMask;
            _stopRequested = false;
            _reachedEnd = false;
            _isRunning = true;
            lock (_timeLock)
            {
                _anchorElapsed = TimeSpan.Zero;
                _clock.Restart();
            }
            _thread = new Thread(PlaybackLoop)
            {
                IsBackground = true,
                Priority = ThreadPriority.Highest,
                Name = "MIDI Playback",
            };
            _thread.Start();
        }

        private void StopThread()
        {
            if (!_isRunning)
            {
                return;
            }

            _stopRequested = true;
            _thread.Join();
            _thread = null;

            lock (_timeLock)
            {
                TimeSpan position = CurrentSongTime();
                _clock.Stop();
                _startPosition = position < _song.Duration ? position : _song.Duration;
            }
            _isRunning = false;
            _appliedAudibleMask = _audibleMask;
            SilenceAll();

            // 再生スレッドが反映する前に止めた場合に備える（音は反映済みの送信先で止めてから）
            Array.Copy(_outputChannels, _appliedOutputChannels, ChannelCount);
            ApplyChannelVolumes();
        }

        private void PlaybackLoop()
        {
            IReadOnlyList<MidiEvent> events = _song.Events;
            TimeSpan previous = CurrentSongTime();

            // Thread.Sleep の精度を 1ms にする
            NativeMethods.timeBeginPeriod(1);
            try
            {
                while (!_stopRequested)
                {
                    ApplyAudibleMask();
                    ApplyOutputChannels();
                    ApplyChannelVolumes();

                    TimeSpan now = CurrentSongTime();

                    // A-B 区間ループの終了位置を、今回の間に通り過ぎたか
                    // （終了位置より後へシークした場合は、ループせずにそのまま再生する）
                    LoopRange loop = _loop;
                    bool jump = loop != null && previous < loop.End && now >= loop.End;
                    previous = now;

                    while (_nextIndex < events.Count)
                    {
                        TimeSpan time = events[_nextIndex].Time;
                        if (jump ? time >= loop.End : time > now)
                        {
                            break;
                        }
                        Dispatch(events[_nextIndex]);
                        _nextIndex++;
                        if (_sysExSentCount < _nextIndex)
                        {
                            _sysExSentCount = _nextIndex;
                        }
                    }

                    if (jump)
                    {
                        JumpTo(loop.Start);
                        previous = loop.Start;
                        continue;
                    }
                    if (_nextIndex >= events.Count)
                    {
                        _reachedEnd = true;
                        break;
                    }
                    Thread.Sleep(1);
                }
            }
            finally
            {
                NativeMethods.timeEndPeriod(1);
            }

            if (_reachedEnd)
            {
                SongEnded?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 再生を続けたまま、指定した位置へ移る（A-B 区間ループ用。再生スレッドで呼ぶ）。
        /// </summary>
        private void JumpTo(TimeSpan position)
        {
            SilenceAll();

            int index = FindEventIndex(position);
            PrepareOutputState(index);
            RebuildControllerState(index);
            SendAdjustedVolumes();
            _nextIndex = index;

            // 状態の再現にかかった時間は数えずに、ここから再生を再開する
            lock (_timeLock)
            {
                _startPosition = position;
                _anchorElapsed = _clock.Elapsed;
            }
        }

        /// <summary>
        /// 曲のイベント <paramref name="index"/> から鳴らす前に、音源の状態（音色・コントロールチェンジ・SysEx など）を、
        /// 曲の先頭からそこまで再生したときと同じにする。
        /// </summary>
        /// <remarks>
        /// 次の場合は、自動リセットを送ってから、曲の SysEx を初めから送り直す。
        /// <list type="bullet">
        /// <item>曲の先頭から鳴らすとき、またはこのデバイスにまだ曲の SysEx を送っていないとき</item>
        /// <item>
        /// 後ろへ戻るとき（シーク・A-B ループ）に、戻り先から今までに送ったデータに、状態を送り直すだけでは戻せないものがあるとき
        /// （SysEx や、戻り先までに一度も設定していない音量・音色・RPN など。<see cref="MidiChaser.NeedsReset"/>）
        /// </item>
        /// </list>
        /// それ以外は、リセットせずに状態だけを送り直す（A-B ループのつなぎ目に、リセットの待ち時間を入れないため）。
        /// </remarks>
        private void PrepareOutputState(int index)
        {
            bool reset = index == 0
                || _sysExSentCount == 0
                || (index < _sysExSentCount && MidiChaser.NeedsReset(_song.Events, index, _sysExSentCount));
            if (reset)
            {
                SendAutoReset();
                _sysExSentCount = 0;
            }

            if (index > 0)
            {
                // 途中から鳴らすので、そこまでの音色や音量を再現する
                MidiChaser.Chase(_output, _song.Events, index, _sysExSentCount, _appliedOutputChannels, _blockedControllers);
                _sysExSentCount = Math.Max(_sysExSentCount, index);
            }
        }

        private void SendAutoReset()
        {
            if (AutoReset != MidiResetType.None)
            {
                _output.SendLongMessage(MidiReset.GetMessage(AutoReset));
                Thread.Sleep(MidiReset.WaitMilliseconds);
            }
        }

        /// <summary>
        /// ミュート・ソロの変更で音を出さなくなったチャンネルの、鳴っている音を止める（再生スレッドで呼ぶ）。
        /// </summary>
        /// <remarks>
        /// ここでは All Sound Off / All Notes Off を送らず、鳴らした音のノートオフだけを送る。
        /// 音源によっては（HALion 7 など）、これらを受け取ると他のチャンネルの音まで一瞬途切れるため。
        /// </remarks>
        private void ApplyAudibleMask()
        {
            int mask = _audibleMask;
            int silenced = _appliedAudibleMask & ~mask;
            _appliedAudibleMask = mask;
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                if ((silenced & (1 << channel)) != 0)
                {
                    ReleaseNotes(channel);
                }
            }
        }

        /// <summary>
        /// 送信先のチャンネルの変更を反映する（再生スレッドで呼ぶ）。
        /// 古い送信先で鳴っている音を止め、ここまでの音色やコントロールチェンジを新しい送信先に送る。
        /// </summary>
        private void ApplyOutputChannels()
        {
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                int outputChannel = Volatile.Read(ref _outputChannels[channel]);
                int oldOutputChannel = _appliedOutputChannels[channel];
                if (outputChannel == oldOutputChannel)
                {
                    continue;
                }

                ReleaseNotes(channel);
                _appliedOutputChannels[channel] = outputChannel;

                // 古い送信先を他のチャンネルが使っていなければ、ペダルで伸びている音も止める
                if (Array.IndexOf(_appliedOutputChannels, oldOutputChannel) < 0)
                {
                    _output.SendShortMessage(0xB0 | oldOutputChannel, 64, 0);
                }
                MidiChaser.ChaseChannel(_output, _song.Events, _nextIndex, channel, outputChannel, _blockedControllers);
                if (_appliedChannelVolumes[channel] != DefaultChannelVolume)
                {
                    SendChannelVolume(channel);
                }
            }
        }

        /// <summary>
        /// ボリューム（%）の変更を反映し、変わったチャンネルのボリュームを送り直す
        /// （再生中は再生スレッドで、停止中は UI スレッドで呼ぶ）。
        /// </summary>
        private void ApplyChannelVolumes()
        {
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                int percent = Volatile.Read(ref _channelVolumes[channel]);
                if (percent == _appliedChannelVolumes[channel])
                {
                    continue;
                }
                _appliedChannelVolumes[channel] = percent;
                if (_output != null)
                {
                    SendChannelVolume(channel);
                }
            }
        }

        /// <summary>
        /// ボリュームを 100% 以外にしているチャンネルに、倍率を掛けたボリュームを送る
        /// （リセットや途中からの再生で、音源のボリュームが曲どおりの値になった後に呼ぶ）。
        /// </summary>
        private void SendAdjustedVolumes()
        {
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                if (_appliedChannelVolumes[channel] != DefaultChannelVolume)
                {
                    SendChannelVolume(channel);
                }
            }
        }

        private void SendChannelVolume(int channel)
        {
            if (_blockedControllers[VolumeController])
            {
                return;
            }
            _output.SendShortMessage(0xB0 | _appliedOutputChannels[channel], VolumeController, AdjustedVolume(channel));
        }

        /// <summary>曲のボリューム（CC7）に、チャンネルごとのボリューム（%）を掛けた値（0～127）。</summary>
        private int AdjustedVolume(int channel)
        {
            return Math.Min(127, (_volumes[channel] * _appliedChannelVolumes[channel] + 50) / 100);
        }

        private void Dispatch(MidiEvent e)
        {
            switch (e.Kind)
            {
                case MidiEventKind.Channel:
                    DispatchChannelMessage(e);
                    break;

                case MidiEventKind.SysEx:
                    _output.SendLongMessage(e.Data);
                    if (MidiReset.IsResetMessage(e.Data))
                    {
                        ResetControllerState();
                        SendAdjustedVolumes();
                    }
                    break;
            }
        }

        private void DispatchChannelMessage(MidiEvent e)
        {
            int channel = e.Channel;
            int status = e.Command | _appliedOutputChannels[channel];
            bool isNoteOn = e.Command == 0x90 && e.Data2 > 0;
            bool isNoteOff = e.Command == 0x80 || (e.Command == 0x90 && e.Data2 == 0);

            if (isNoteOn)
            {
                DispatchNoteOn(e);
                return;
            }

            if (isNoteOff)
            {
                // 先に鳴ったノートオンから順に、そのときに決めた音高で止める
                List<int> sounding = _noteMap[channel, e.Data1];
                if (sounding.Count == 0)
                {
                    return; // ノートオンの無いノートオフ
                }
                int entry = sounding[0];
                sounding.RemoveAt(0);
                if (entry == NoteOutOfRange)
                {
                    return; // 音域外に移調されて、鳴らしていない
                }

                int note = entry & ~NoteSentFlag;
                _displayNotes[channel, note]--;
                if (_pedals[channel, PedalIndex(DamperPedal)])
                {
                    _sustainedNotes[channel, note] = true; // ダンパーペダルで、ペダルを離すまで伸びる
                }
                if ((entry & NoteSentFlag) == 0 || _activeNotes[channel, note] == 0)
                {
                    return; // ミュート中でノートオンを送っていない、またはミュートなどで止め済み
                }
                _activeNotes[channel, note]--;
                _output.SendShortMessage(status, note, e.Data2);
                return;
            }

            if (e.Command == 0xA0)
            {
                // ポリフォニックキープレッシャーは、最後に鳴ったノートオンの音高で送る
                List<int> sounding = _noteMap[channel, e.Data1];
                int entry = sounding.Count > 0 ? sounding[sounding.Count - 1] : NoteOutOfRange;
                if (entry == NoteOutOfRange || (entry & NoteSentFlag) == 0)
                {
                    return; // 鳴らしていないノート
                }
                _output.SendShortMessage(status, entry & ~NoteSentFlag, e.Data2);
                return;
            }

            if (e.Command == 0xE0)
            {
                TrackPitchBend(channel, e.Data1, e.Data2);
            }
            if (e.Command == 0xB0)
            {
                // 送らない CC も、レベルメーターやペダルのランプのために状態は追いかける
                TrackController(channel, e.Data1, e.Data2);
                if (_blockedControllers[e.Data1])
                {
                    return;
                }
                if (e.Data1 == VolumeController)
                {
                    _output.SendShortMessage(status, VolumeController, AdjustedVolume(channel));
                    return;
                }
            }
            _output.SendShortMessage(status, e.Data1, e.Data2);
        }

        private void DispatchNoteOn(MidiEvent e)
        {
            int channel = e.Channel;
            int outputChannel = _appliedOutputChannels[channel];
            bool isDrum = channel == DrumChannel || outputChannel == DrumChannel;
            int note = e.Data1
                + (isDrum ? 0 : _transpose)
                + Volatile.Read(ref _channelTransposes[channel])
                + Volatile.Read(ref _channelOctaves[channel]) * 12;
            List<int> sounding = _noteMap[channel, e.Data1];
            if (note < 0 || note >= NoteCount)
            {
                sounding.Add(NoteOutOfRange); // 対応するノートオフを読み捨てるため
                return;
            }

            _displayNotes[channel, note]++;
            int velocity = _velocityTable[e.Data2];
            RecordLevel(channel, velocity);

            // ミュート中のチャンネルはノートオンを送らない
            if ((_appliedAudibleMask & (1 << channel)) == 0)
            {
                sounding.Add(note);
                return;
            }
            sounding.Add(note | NoteSentFlag);
            _activeNotes[channel, note]++;
            _output.SendShortMessage(e.Command | outputChannel, note, velocity);
        }

        /// <summary>
        /// 鳴っている音をすべて止める。
        /// </summary>
        private void SilenceAll()
        {
            if (_output == null)
            {
                return;
            }

            for (int channel = 0; channel < ChannelCount; channel++)
            {
                ReleaseNotes(channel);
            }

            // 送信先を変えていても漏れがないよう、音源の全チャンネルに送る
            for (int outputChannel = 0; outputChannel < ChannelCount; outputChannel++)
            {
                _output.SendShortMessage(0xB0 | outputChannel, 120, 0); // All Sound Off
                _output.SendShortMessage(0xB0 | outputChannel, 123, 0); // All Notes Off
                _output.SendShortMessage(0xB0 | outputChannel, 64, 0); // ダンパーペダル オフ
            }
            Array.Clear(_displayNotes, 0, _displayNotes.Length);
            Array.Clear(_sustainedNotes, 0, _sustainedNotes.Length);
            ClearNoteMap();

            // ダンパーペダルは上でオフにしたので、表示も消す
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                _pedals[channel, PedalIndex(DamperPedal)] = false;
            }
        }

        /// <summary>
        /// そのチャンネル（曲データのチャンネル）で鳴らした音に、反映済みの送信先でノートオフを送る。
        /// 同じ音高を重ねて鳴らしていれば、その数だけ送る（音を重ねて鳴らす音源で残らないように）。
        /// </summary>
        private void ReleaseNotes(int channel)
        {
            int outputChannel = _appliedOutputChannels[channel];
            for (int note = 0; note < NoteCount; note++)
            {
                for (; _activeNotes[channel, note] > 0; _activeNotes[channel, note]--)
                {
                    _output.SendShortMessage(0x80 | outputChannel, note, 0);
                }
            }
        }

        private void ClearNoteMap()
        {
            foreach (List<int> sounding in _noteMap)
            {
                sounding.Clear();
            }
        }

        /// <summary>
        /// ノートのレベル（ベロシティ × ボリューム × エクスプレッション）を、メーター用の最大値に記録する。
        /// ボリュームは、チャンネルごとのボリューム（%）を掛けた後の値を使う。
        /// </summary>
        private void RecordLevel(int channel, int velocity)
        {
            float level = velocity / 127f * AdjustedVolume(channel) / 127f * _expressions[channel] / 127f;
            if (level > _peakLevels[channel])
            {
                _peakLevels[channel] = level;
            }
        }

        /// <summary>
        /// 曲データから追いかけているコントロールチェンジ（音量・エクスプレッション・ペダル）を初期値に戻す。
        /// </summary>
        private void ResetControllerState()
        {
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                _volumes[channel] = DefaultVolume;
                _expressions[channel] = DefaultExpression;
            }
            Array.Clear(_pedals, 0, _pedals.Length);
            Array.Clear(_sustainedNotes, 0, _sustainedNotes.Length);
            Array.Clear(_pitchBends, 0, ChannelCount);
            Array.Clear(_modulations, 0, ChannelCount);
        }

        /// <summary>
        /// 途中から再生するときに、レベルメーターの計算に使う音量・エクスプレッションと、ペダルの状態を求め直す。
        /// </summary>
        private void RebuildControllerState(int endIndex)
        {
            ResetControllerState();
            IReadOnlyList<MidiEvent> events = _song.Events;
            for (int i = 0; i < endIndex; i++)
            {
                MidiEvent e = events[i];
                if (e.Kind == MidiEventKind.Channel && e.Command == 0xB0)
                {
                    TrackController(e.Channel, e.Data1, e.Data2);
                }
                else if (e.Kind == MidiEventKind.Channel && e.Command == 0xE0)
                {
                    TrackPitchBend(e.Channel, e.Data1, e.Data2);
                }
                else if (e.Kind == MidiEventKind.SysEx && MidiReset.IsResetMessage(e.Data))
                {
                    ResetControllerState();
                }
            }
        }

        private void TrackController(int channel, int controller, int value)
        {
            switch (controller)
            {
                case 7:
                    _volumes[channel] = value;
                    break;
                case 11:
                    _expressions[channel] = value;
                    break;
                case ModulationController:
                    _modulations[channel] = value;
                    break;
                case 121: // Reset All Controllers
                    _expressions[channel] = DefaultExpression;
                    _modulations[channel] = 0;
                    _pitchBends[channel] = 0;
                    for (int pedal = 0; pedal < PedalCount; pedal++)
                    {
                        _pedals[channel, pedal] = false;
                    }
                    ClearSustainedNotes(channel);
                    break;
                default:
                    int index = PedalIndex(controller);
                    if (index >= 0)
                    {
                        _pedals[channel, index] = value >= 64;
                        if (controller == DamperPedal && value < 64)
                        {
                            ClearSustainedNotes(channel); // ペダルを離したので、伸びていた音は止まる
                        }
                    }
                    break;
            }
        }

        private void ClearSustainedNotes(int channel)
        {
            for (int note = 0; note < NoteCount; note++)
            {
                _sustainedNotes[channel, note] = false;
            }
        }

        private void TrackPitchBend(int channel, int lsb, int msb)
        {
            _pitchBends[channel] = (lsb | (msb << 7)) - PitchBendCenter;
        }

        private static int PedalIndex(int controller)
        {
            switch (controller)
            {
                case DamperPedal:
                    return 0;
                case SostenutoPedal:
                    return 1;
                case SoftPedal:
                    return 2;
                default:
                    return -1;
            }
        }

        /// <summary>
        /// 指定した時間以降で最初のイベントのインデックスを返す（二分探索）。
        /// </summary>
        private int FindEventIndex(TimeSpan position)
        {
            IReadOnlyList<MidiEvent> events = _song.Events;
            int low = 0;
            int high = events.Count;
            while (low < high)
            {
                int middle = (low + high) / 2;
                if (events[middle].Time < position)
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

        /// <summary>A-B 区間ループの範囲（再生スレッドと共有するため、変更しないクラスにしている）。</summary>
        private sealed class LoopRange
        {
            public LoopRange(TimeSpan start, TimeSpan end)
            {
                Start = start;
                End = end;
            }

            public TimeSpan Start { get; }

            public TimeSpan End { get; }
        }
    }
}
