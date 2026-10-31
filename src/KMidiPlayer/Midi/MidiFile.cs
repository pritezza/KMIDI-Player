using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KMidiPlayer.Midi
{
    /// <summary>
    /// 標準 MIDI ファイル（Format 0 / 1）を読み込み、全トラックのイベントを時間順に並べたもの。
    /// </summary>
    public sealed class MidiFile
    {
        private const byte MetaTrackName = 0x03;
        private const byte MetaLyric = 0x05;
        private const byte MetaMarker = 0x06;
        private const byte MetaEndOfTrack = 0x2F;
        private const byte MetaTempo = 0x51;
        private const byte MetaTimeSignature = 0x58;

        /// <summary>曲名などのテキストの文字コード（日本の MIDI データで一般的な Shift_JIS）。</summary>
        private static readonly Encoding TextEncoding = Encoding.GetEncoding(932);

        /// <summary>チャンネルごとの、ノートを鳴らすかどうか。</summary>
        private readonly bool[] _noteChannels;

        /// <summary>テンポが変わる位置（先頭は必ずティック 0）。時間からティックを求めるのに使う。</summary>
        private readonly List<TempoPoint> _tempoMap;

        /// <summary>拍子が変わる位置ごとの区間（先頭は必ずティック 0）。ティックから小節・拍を求めるのに使う。</summary>
        private readonly List<MeterSection> _meterMap;

        /// <summary>最後のイベントの位置（ティック）。</summary>
        private readonly long _lastTick;

        private MidiFile(string filePath, int format, int ticksPerQuarterNote, string title, IReadOnlyList<MidiEvent> events, TimeSpan duration)
        {
            FilePath = filePath;
            Format = format;
            TicksPerQuarterNote = ticksPerQuarterNote;
            Title = title;
            Events = events;
            Duration = duration;
            _lastTick = events.Count > 0 ? events[events.Count - 1].Tick : 0;
            MidiEvent firstNote = events.FirstOrDefault(e => e.Kind == MidiEventKind.Channel && e.Command == 0x90 && e.Data2 > 0);
            FirstNoteTime = firstNote?.Time ?? TimeSpan.Zero;
            _tempoMap = BuildTempoMap(events);
            _meterMap = ticksPerQuarterNote > 0 ? BuildMeterMap(events, ticksPerQuarterNote) : null;
            BarCount = _meterMap != null ? GetPositionAtTick(Math.Max(0, _lastTick - 1)).Bar : 0;

            var noteChannels = new bool[16];
            foreach (MidiEvent e in events)
            {
                if (e.Kind == MidiEventKind.Channel && e.Command == 0x90 && e.Data2 > 0)
                {
                    noteChannels[e.Channel] = true;
                }
            }
            _noteChannels = noteChannels;
        }

        public string FilePath { get; }

        public int Format { get; }

        /// <summary>4 分音符あたりのティック数。SMPTE 形式の場合は 0。</summary>
        public int TicksPerQuarterNote { get; }

        public bool IsSmpteTiming => TicksPerQuarterNote == 0;

        /// <summary>曲名（最初のトラックのトラック名）。無い場合は null。</summary>
        public string Title { get; }

        /// <summary>全トラックのイベントを時間順に並べたもの。</summary>
        public IReadOnlyList<MidiEvent> Events { get; }

        /// <summary>演奏時間（テンポ 100% のとき）。</summary>
        public TimeSpan Duration { get; }

        /// <summary>そのチャンネル（0～15）で、ノートを 1 つ以上鳴らすかどうか。</summary>
        public bool HasNotes(int channel) => _noteChannels[channel];

        /// <summary>全小節数。SMPTE 形式（小節を数えられない）の場合は 0。</summary>
        public int BarCount { get; }

        /// <summary>最初のノートオンの時間。ノートが無ければ 0。</summary>
        public TimeSpan FirstNoteTime { get; }

        private IReadOnlyList<NoteSpan> _notes;
        private IReadOnlyList<TimeSpan> _barTimes;

        /// <summary>
        /// 曲のすべてのノート（鳴り始めの時間順）。初めて使うときに作る。
        /// 同じチャンネル・音高のノートが重なっている場合は、先に鳴ったものから順に止まるものとする。
        /// </summary>
        public IReadOnlyList<NoteSpan> Notes => _notes ?? (_notes = BuildNotes());

        /// <summary>各小節の始まりの時間（1 小節目から順に）。SMPTE 形式の場合は空。初めて使うときに作る。</summary>
        public IReadOnlyList<TimeSpan> BarTimes => _barTimes ?? (_barTimes = BuildBarTimes());

        private IReadOnlyList<TimedText> _lyrics;
        private IReadOnlyList<TimedText> _markers;

        /// <summary>歌詞のイベント（時間順。多くは 1 文字～1 音節ずつ）。初めて使うときに作る。</summary>
        public IReadOnlyList<TimedText> Lyrics => _lyrics ?? (_lyrics = BuildTexts(MetaLyric));

        /// <summary>マーカー（「A メロ」「サビ」など。時間順）。初めて使うときに作る。</summary>
        public IReadOnlyList<TimedText> Markers => _markers ?? (_markers = BuildTexts(MetaMarker));

        /// <summary>その種類のメタイベントの文字列を、時間順に集める。</summary>
        private IReadOnlyList<TimedText> BuildTexts(byte metaType)
        {
            var texts = new List<TimedText>();
            foreach (MidiEvent e in Events)
            {
                if (e.Kind == MidiEventKind.Meta && e.MetaType == metaType && e.Data != null && e.Data.Length > 0)
                {
                    texts.Add(new TimedText(e.Time, TextEncoding.GetString(e.Data).Replace("\0", string.Empty)));
                }
            }
            return texts;
        }

        private IReadOnlyList<NoteSpan> BuildNotes()
        {
            var notes = new List<NoteSpan>();
            var playing = new Queue<MidiEvent>[16, 128];
            foreach (MidiEvent e in Events)
            {
                if (e.Kind != MidiEventKind.Channel)
                {
                    continue;
                }
                bool isNoteOn = e.Command == 0x90 && e.Data2 > 0;
                bool isNoteOff = e.Command == 0x80 || (e.Command == 0x90 && e.Data2 == 0);
                if (isNoteOn)
                {
                    Queue<MidiEvent> queue = playing[e.Channel, e.Data1] ?? (playing[e.Channel, e.Data1] = new Queue<MidiEvent>());
                    queue.Enqueue(e);
                }
                else if (isNoteOff)
                {
                    Queue<MidiEvent> queue = playing[e.Channel, e.Data1];
                    if (queue != null && queue.Count > 0)
                    {
                        MidiEvent on = queue.Dequeue();
                        notes.Add(new NoteSpan(on.Channel, on.Data1, on.Data2, on.Time, e.Time));
                    }
                }
            }

            // ノートオフが無いノートは、曲の最後まで鳴っているものとする
            foreach (Queue<MidiEvent> queue in playing)
            {
                while (queue != null && queue.Count > 0)
                {
                    MidiEvent on = queue.Dequeue();
                    notes.Add(new NoteSpan(on.Channel, on.Data1, on.Data2, on.Time, Duration));
                }
            }
            return notes.OrderBy(note => note.Start).ToList();
        }

        private IReadOnlyList<TimeSpan> BuildBarTimes()
        {
            var times = new List<TimeSpan>();
            if (_meterMap == null)
            {
                return times;
            }
            for (int i = 0; i < _meterMap.Count && times.Count < BarCount; i++)
            {
                MeterSection section = _meterMap[i];
                long end = i + 1 < _meterMap.Count ? _meterMap[i + 1].StartTick : long.MaxValue;
                for (long tick = section.StartTick; tick < end && times.Count < BarCount; tick += section.BarLength)
                {
                    times.Add(TimeAtTick(tick));
                }
            }
            return times;
        }

        /// <summary>ティックにあたる時間（テンポ 100% のとき）を、テンポの変化をたどって求める。</summary>
        private TimeSpan TimeAtTick(long tick)
        {
            TempoPoint point = _tempoMap[0];
            foreach (TempoPoint candidate in _tempoMap)
            {
                if (candidate.Tick > tick)
                {
                    break;
                }
                point = candidate;
            }
            double microseconds = (tick - point.Tick) * (double)point.MicrosecondsPerQuarterNote / TicksPerQuarterNote;
            return point.Time + TimeSpan.FromTicks((long)Math.Round(microseconds * 10));
        }

        /// <summary>
        /// その時間（テンポ 100% のとき）のテンポ（4 分音符 / 分）。SMPTE 形式の場合は 0。
        /// </summary>
        public double GetTempoAt(TimeSpan time)
        {
            if (IsSmpteTiming)
            {
                return 0;
            }
            TempoPoint point = _tempoMap[0];
            foreach (TempoPoint candidate in _tempoMap)
            {
                if (candidate.Time > time)
                {
                    break;
                }
                point = candidate;
            }
            return 60000000.0 / point.MicrosecondsPerQuarterNote;
        }

        /// <summary>
        /// 曲の上の時間（テンポ 100% のとき）を、小節・拍・ティックで返す。SMPTE 形式の場合は null。
        /// 曲の最後（最後のイベントの位置）は、最後の小節の中として扱う。
        /// </summary>
        public MusicalPosition? GetMusicalPosition(TimeSpan time)
        {
            if (_meterMap == null)
            {
                return null;
            }
            long tick = Math.Max(0, Math.Min(TickAt(time), _lastTick - 1));
            return GetPositionAtTick(tick);
        }

        /// <summary>時間（テンポ 100% のとき）にあたるティックを、テンポの変化をたどって求める。</summary>
        private long TickAt(TimeSpan time)
        {
            // 時間がその位置以下で最後のテンポを探す（二分探索）
            int low = 0;
            int high = _tempoMap.Count - 1;
            while (low < high)
            {
                int middle = (low + high + 1) / 2;
                if (_tempoMap[middle].Time <= time)
                {
                    low = middle;
                }
                else
                {
                    high = middle - 1;
                }
            }
            TempoPoint point = _tempoMap[low];
            double microseconds = (time - point.Time).Ticks / 10.0;
            return point.Tick + (long)Math.Floor(microseconds * TicksPerQuarterNote / point.MicrosecondsPerQuarterNote);
        }

        private MusicalPosition GetPositionAtTick(long tick)
        {
            MeterSection section = _meterMap[0];
            foreach (MeterSection candidate in _meterMap)
            {
                if (candidate.StartTick > tick)
                {
                    break;
                }
                section = candidate;
            }
            long offset = tick - section.StartTick;
            long inBar = offset % section.BarLength;
            return new MusicalPosition(
                section.StartBar + (int)(offset / section.BarLength) + 1,
                (int)(inBar / section.TicksPerBeat) + 1,
                section.BeatsPerBar,
                section.BeatUnit,
                (int)(inBar % section.TicksPerBeat),
                section.TicksPerBeat);
        }

        private static List<TempoPoint> BuildTempoMap(IReadOnlyList<MidiEvent> events)
        {
            var map = new List<TempoPoint> { new TempoPoint(0, TimeSpan.Zero, 500000) }; // 指定が無ければ 120 BPM
            foreach (MidiEvent e in events)
            {
                if (e.Kind != MidiEventKind.Meta || e.MetaType != MetaTempo || e.Data.Length < 3)
                {
                    continue;
                }
                int tempo = (e.Data[0] << 16) | (e.Data[1] << 8) | e.Data[2];
                if (tempo <= 0)
                {
                    continue;
                }
                if (map[map.Count - 1].Tick == e.Tick)
                {
                    map.RemoveAt(map.Count - 1); // 同じ位置のテンポは、後のものが有効
                }
                map.Add(new TempoPoint(e.Tick, e.Time, tempo));
            }
            return map;
        }

        /// <summary>
        /// 拍子の区間の一覧を作る。拍子の指定が無ければ 4/4 拍子。
        /// 小節の途中で拍子が変わった場合は、そこから新しい小節を数える。
        /// </summary>
        private static List<MeterSection> BuildMeterMap(IReadOnlyList<MidiEvent> events, int ticksPerQuarterNote)
        {
            var map = new List<MeterSection> { MeterSection.Create(0, 0, 4, 2, ticksPerQuarterNote) };
            foreach (MidiEvent e in events)
            {
                if (e.Kind != MidiEventKind.Meta || e.MetaType != MetaTimeSignature || e.Data.Length < 2 || e.Data[0] == 0)
                {
                    continue;
                }
                MeterSection last = map[map.Count - 1];
                int startBar;
                if (e.Tick == last.StartTick)
                {
                    map.RemoveAt(map.Count - 1); // 同じ位置の拍子は、後のものが有効
                    startBar = last.StartBar;
                }
                else
                {
                    long length = e.Tick - last.StartTick;
                    startBar = last.StartBar + (int)((length + last.BarLength - 1) / last.BarLength);
                }
                map.Add(MeterSection.Create(e.Tick, startBar, e.Data[0], Math.Min((int)e.Data[1], 6), ticksPerQuarterNote));
            }
            return map;
        }

        /// <summary>テンポが変わる位置。</summary>
        private sealed class TempoPoint
        {
            public TempoPoint(long tick, TimeSpan time, int microsecondsPerQuarterNote)
            {
                Tick = tick;
                Time = time;
                MicrosecondsPerQuarterNote = microsecondsPerQuarterNote;
            }

            public long Tick { get; }

            public TimeSpan Time { get; }

            public int MicrosecondsPerQuarterNote { get; }
        }

        /// <summary>同じ拍子が続く区間。</summary>
        private sealed class MeterSection
        {
            private MeterSection(long startTick, int startBar, int beatsPerBar, int beatUnit, int ticksPerBeat)
            {
                StartTick = startTick;
                StartBar = startBar;
                BeatsPerBar = beatsPerBar;
                BeatUnit = beatUnit;
                TicksPerBeat = ticksPerBeat;
            }

            /// <summary>拍子の分母（4 なら 4 分音符）。</summary>
            public int BeatUnit { get; }

            public long StartTick { get; }

            /// <summary>この区間より前の小節数。</summary>
            public int StartBar { get; }

            public int BeatsPerBar { get; }

            public int TicksPerBeat { get; }

            public long BarLength => (long)TicksPerBeat * BeatsPerBar;

            /// <param name="denominatorPower">拍子の分母の 2 の累乗（2 なら 4 分音符、3 なら 8 分音符）。</param>
            public static MeterSection Create(long startTick, int startBar, int beatsPerBar, int denominatorPower, int ticksPerQuarterNote)
            {
                int ticksPerBeat = Math.Max(1, ticksPerQuarterNote * 4 >> denominatorPower);
                return new MeterSection(startTick, startBar, beatsPerBar, 1 << denominatorPower, ticksPerBeat);
            }
        }

        /// <exception cref="IOException">ファイルを読めなかった場合。</exception>
        /// <exception cref="InvalidDataException">MIDI ファイルとして解釈できなかった場合。</exception>
        public static MidiFile Load(string filePath)
        {
            return Parse(File.ReadAllBytes(filePath), filePath);
        }

        /// <exception cref="InvalidDataException">MIDI ファイルとして解釈できなかった場合。</exception>
        public static MidiFile Parse(byte[] data, string filePath)
        {
            if (data.Length < 14 || ReadChunkId(data, 0) != "MThd")
            {
                throw new InvalidDataException(Texts.NotMidiFile);
            }

            long headerLength = ReadUInt32(data, 4);
            int format = ReadUInt16(data, 8);
            int division = ReadUInt16(data, 12);
            if (headerLength < 6 || division == 0)
            {
                throw new InvalidDataException(Texts.BrokenHeader);
            }
            if (format == 2)
            {
                throw new InvalidDataException(Texts.Format2NotSupported);
            }
            if (format > 2)
            {
                throw new InvalidDataException(Texts.UnknownFormat(format));
            }

            var events = new List<MidiEvent>();
            int trackCount = 0;
            long position = 8 + headerLength;
            while (position + 8 <= data.Length)
            {
                string chunkId = ReadChunkId(data, (int)position);
                long chunkLength = ReadUInt32(data, (int)position + 4);
                int start = (int)(position + 8);
                int end = (int)Math.Min(data.Length, position + 8 + chunkLength);
                if (chunkId == "MTrk")
                {
                    ReadTrack(data, start, end, trackCount, events);
                    trackCount++;
                }
                // MTrk 以外のチャンクは読み飛ばす
                position += 8 + chunkLength;
            }
            if (trackCount == 0)
            {
                throw new InvalidDataException(Texts.NoTracks);
            }

            // OrderBy は安定ソートなので、同じティックのイベントはトラック順・記録順のまま並ぶ
            List<MidiEvent> sorted = events.OrderBy(e => e.Tick).ToList();
            int ticksPerQuarterNote = AssignTimes(sorted, division);

            TimeSpan duration = sorted.Count > 0 ? sorted[sorted.Count - 1].Time : TimeSpan.Zero;
            return new MidiFile(filePath, format, ticksPerQuarterNote, ReadTitle(sorted), sorted, duration);
        }

        private static void ReadTrack(byte[] data, int position, int end, int track, List<MidiEvent> events)
        {
            long tick = 0;
            byte runningStatus = 0;
            while (position < end)
            {
                if (!TryReadVariableLength(data, ref position, end, out int delta))
                {
                    break;
                }
                tick += delta;
                if (position >= end)
                {
                    break;
                }

                byte first = data[position];
                if (first == 0xFF)
                {
                    position++;
                    if (position >= end)
                    {
                        break;
                    }
                    byte metaType = data[position++];
                    if (!TryReadVariableLength(data, ref position, end, out int length))
                    {
                        break;
                    }
                    byte[] body = Copy(data, position, Math.Min(length, end - position));
                    position += length;
                    events.Add(MidiEvent.CreateMeta(tick, track, metaType, body));
                    if (metaType == MetaEndOfTrack)
                    {
                        break;
                    }
                }
                else if (first == 0xF0 || first == 0xF7)
                {
                    position++;
                    if (!TryReadVariableLength(data, ref position, end, out int length))
                    {
                        break;
                    }
                    byte[] body = Copy(data, position, Math.Min(length, end - position));
                    position += length;

                    // F0 で始まる SysEx はファイル上で先頭の F0 が省かれているので付け直す。
                    // F7 はそれ以降のバイト列をそのまま送る（分割 SysEx やエスケープ用）。
                    byte[] message = body;
                    if (first == 0xF0)
                    {
                        message = new byte[body.Length + 1];
                        message[0] = 0xF0;
                        Buffer.BlockCopy(body, 0, message, 1, body.Length);
                    }
                    if (message.Length > 0)
                    {
                        events.Add(MidiEvent.CreateSysEx(tick, track, message));
                    }
                }
                else
                {
                    byte status;
                    if (first >= 0x80)
                    {
                        if (first >= 0xF0)
                        {
                            // SMF では使えないステータス。壊れているので、このトラックの残りは読まない
                            break;
                        }
                        status = first;
                        runningStatus = first;
                        position++;
                    }
                    else
                    {
                        // ランニングステータス（直前と同じステータスの省略）
                        if (runningStatus == 0)
                        {
                            break;
                        }
                        status = runningStatus;
                    }

                    int command = status & 0xF0;
                    int dataLength = command == 0xC0 || command == 0xD0 ? 1 : 2;
                    if (position + dataLength > end)
                    {
                        break;
                    }
                    byte data1 = (byte)(data[position++] & 0x7F);
                    byte data2 = dataLength == 2 ? (byte)(data[position++] & 0x7F) : (byte)0;
                    events.Add(MidiEvent.CreateChannel(tick, track, status, data1, data2));
                }
            }
        }

        /// <summary>
        /// テンポ情報をもとに、各イベントの時間を計算する。
        /// </summary>
        /// <returns>4 分音符あたりのティック数。SMPTE 形式の場合は 0。</returns>
        private static int AssignTimes(List<MidiEvent> events, int division)
        {
            if ((division & 0x8000) != 0)
            {
                // SMPTE 形式：上位バイトがフレームレート（負の値）、下位バイトが 1 フレームあたりのティック数
                int framesPerSecond = -(sbyte)(division >> 8);
                int ticksPerFrame = division & 0xFF;
                if (framesPerSecond <= 0 || ticksPerFrame == 0)
                {
                    throw new InvalidDataException(Texts.BrokenHeader);
                }
                double actualFramesPerSecond = framesPerSecond == 29 ? 29.97 : framesPerSecond;
                double microsecondsPerTick = 1000000.0 / (actualFramesPerSecond * ticksPerFrame);
                foreach (MidiEvent e in events)
                {
                    e.Time = FromMicroseconds(e.Tick * microsecondsPerTick);
                }
                return 0;
            }

            int ticksPerQuarterNote = division;
            double microsecondsPerQuarterNote = 500000; // テンポ指定が無い場合は 120 BPM
            long lastTick = 0;
            double microseconds = 0;
            foreach (MidiEvent e in events)
            {
                microseconds += (e.Tick - lastTick) * microsecondsPerQuarterNote / ticksPerQuarterNote;
                lastTick = e.Tick;
                e.Time = FromMicroseconds(microseconds);

                if (e.Kind == MidiEventKind.Meta && e.MetaType == MetaTempo && e.Data.Length >= 3)
                {
                    int tempo = (e.Data[0] << 16) | (e.Data[1] << 8) | e.Data[2];
                    if (tempo > 0)
                    {
                        microsecondsPerQuarterNote = tempo;
                    }
                }
            }
            return ticksPerQuarterNote;
        }

        private static string ReadTitle(List<MidiEvent> events)
        {
            MidiEvent nameEvent = events.FirstOrDefault(e => e.Track == 0 && e.Kind == MidiEventKind.Meta && e.MetaType == MetaTrackName);
            if (nameEvent == null)
            {
                return null;
            }
            string title = TextEncoding.GetString(nameEvent.Data).Replace("\0", string.Empty).Trim();
            return title.Length > 0 ? title : null;
        }

        private static bool TryReadVariableLength(byte[] data, ref int position, int end, out int value)
        {
            value = 0;
            for (int i = 0; i < 4; i++)
            {
                if (position >= end)
                {
                    return false;
                }
                byte b = data[position++];
                value = (value << 7) | (b & 0x7F);
                if ((b & 0x80) == 0)
                {
                    return true;
                }
            }
            return false; // 5 バイト以上の可変長数値は不正
        }

        private static TimeSpan FromMicroseconds(double microseconds)
        {
            return TimeSpan.FromTicks((long)Math.Round(microseconds * 10));
        }

        private static byte[] Copy(byte[] data, int offset, int length)
        {
            var result = new byte[Math.Max(0, length)];
            Buffer.BlockCopy(data, offset, result, 0, result.Length);
            return result;
        }

        private static string ReadChunkId(byte[] data, int offset)
        {
            return Encoding.ASCII.GetString(data, offset, 4);
        }

        private static int ReadUInt16(byte[] data, int offset)
        {
            return (data[offset] << 8) | data[offset + 1];
        }

        private static long ReadUInt32(byte[] data, int offset)
        {
            return ((long)data[offset] << 24) | ((long)data[offset + 1] << 16) | ((long)data[offset + 2] << 8) | data[offset + 3];
        }
    }
}
