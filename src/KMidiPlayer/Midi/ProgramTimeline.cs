using System;
using System.Collections.Generic;

namespace KMidiPlayer.Midi
{
    /// <summary>
    /// 曲のある時点での、1 つのチャンネルの音色（プログラム・バンク）と、ドラムのパートかどうか。
    /// </summary>
    public struct InstrumentState : IEquatable<InstrumentState>
    {
        public InstrumentState(int program, int bankMsb, int bankLsb, bool isDrum)
        {
            Program = program;
            BankMsb = bankMsb;
            BankLsb = bankLsb;
            IsDrum = isDrum;
        }

        /// <summary>プログラム番号（0～127。曲でまだ送っていなければ 0）。</summary>
        public int Program { get; }

        /// <summary>プログラムチェンジの時点のバンクセレクト MSB（CC0）。送っていなければ -1。</summary>
        public int BankMsb { get; }

        /// <summary>プログラムチェンジの時点のバンクセレクト LSB（CC32）。送っていなければ -1。</summary>
        public int BankLsb { get; }

        /// <summary>ドラム（リズム）のパートか。</summary>
        public bool IsDrum { get; }

        public bool Equals(InstrumentState other)
        {
            return Program == other.Program && BankMsb == other.BankMsb && BankLsb == other.BankLsb && IsDrum == other.IsDrum;
        }

        public override bool Equals(object obj) => obj is InstrumentState other && Equals(other);

        public override int GetHashCode() => (((Program * 397) ^ BankMsb) * 397 ^ BankLsb) * 2 + (IsDrum ? 1 : 0);
    }

    /// <summary>
    /// 曲データから、各時点でチャンネルごとに選ばれている音色（プログラム・バンク）と、
    /// ドラムのパートかどうかを求める（チャンネルパネルの音色名の表示用）。
    /// </summary>
    /// <remarks>
    /// ドラムかどうかは、音源の動作モード（曲中のリセットの SysEx。無ければ自動で送るリセットの種類）で判定の仕方を変える。
    /// <list type="bullet">
    /// <item>GS・GM: リズムパートかどうか（10ch、または GS の SysEx「Use for Rhythm Part」で指定したパート）</item>
    /// <item>XG: バンク MSB が 126・127 ならドラム。バンクを送っていなければ、リズムパートかどうか（10ch、または XG の SysEx「Part Mode」）</item>
    /// <item>GM2: バンク MSB が 120 ならドラム、121 なら楽器。それ以外はリズムパートかどうか（10ch）</item>
    /// </list>
    /// リズムパートの指定は音源のパートに対するものなので、送信先のチャンネル（CH）で判定する。
    /// </remarks>
    public sealed class ProgramTimeline
    {
        private const int ChannelCount = MidiSequencer.ChannelCount;
        private const int DrumChannel = 9; // 10ch

        private readonly List<ProgramPoint>[] _programs = new List<ProgramPoint>[ChannelCount];
        private readonly List<RhythmPoint>[] _rhythmParts = new List<RhythmPoint>[ChannelCount];
        private readonly List<ModePoint> _modes = new List<ModePoint>();

        public ProgramTimeline(IReadOnlyList<MidiEvent> events)
        {
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                _programs[channel] = new List<ProgramPoint>();
                _rhythmParts[channel] = new List<RhythmPoint>();
            }

            var bankMsb = new int[ChannelCount];
            var bankLsb = new int[ChannelCount];
            ClearBanks(bankMsb, bankLsb);
            foreach (MidiEvent e in events)
            {
                if (e.Kind == MidiEventKind.Channel)
                {
                    int channel = e.Channel;
                    if (e.Command == 0xB0 && e.Data1 == 0)
                    {
                        bankMsb[channel] = e.Data2;
                    }
                    else if (e.Command == 0xB0 && e.Data1 == 32)
                    {
                        bankLsb[channel] = e.Data2;
                    }
                    else if (e.Command == 0xC0)
                    {
                        // 音色は、プログラムチェンジの時点のバンクセレクトで決まる
                        _programs[channel].Add(new ProgramPoint(e.Time, e.Data1, bankMsb[channel], bankLsb[channel]));
                    }
                }
                else if (e.Kind == MidiEventKind.SysEx)
                {
                    ReadSysEx(e, bankMsb, bankLsb);
                }
            }
        }

        /// <summary>
        /// その時点での、曲データのチャンネル（0～15）の音色とドラムかどうか。
        /// </summary>
        /// <param name="outputChannel">そのチャンネルの送信先（音源のパート。0～15）。</param>
        /// <param name="time">曲の上での時間。その時間ちょうどのイベントまで反映する。</param>
        /// <param name="autoReset">曲を再生する前に自動で送るリセット（曲中にリセットが無いときの動作モード）。</param>
        public InstrumentState GetState(int channel, int outputChannel, TimeSpan time, MidiResetType autoReset)
        {
            ProgramPoint program = FindLast(_programs[channel], time, point => point.Time) ?? ProgramPoint.Default;
            RhythmPoint rhythm = FindLast(_rhythmParts[outputChannel], time, point => point.Time);
            bool rhythmPart = rhythm?.IsRhythm ?? outputChannel == DrumChannel;
            ModePoint modePoint = FindLast(_modes, time, point => point.Time);
            ModuleMode mode = modePoint?.Mode ?? ToMode(autoReset);

            bool isDrum;
            switch (mode)
            {
                case ModuleMode.XG:
                    isDrum = program.BankMsb >= 0 ? program.BankMsb >= 126 : rhythmPart;
                    break;
                case ModuleMode.GM2:
                    isDrum = program.BankMsb == 120 || (program.BankMsb != 121 && rhythmPart);
                    break;
                default:
                    isDrum = rhythmPart;
                    break;
            }
            return new InstrumentState(program.Program, program.BankMsb, program.BankLsb, isDrum);
        }

        private void ReadSysEx(MidiEvent e, int[] bankMsb, int[] bankLsb)
        {
            byte[] d = e.Data;
            if (MidiReset.IsResetMessage(d))
            {
                // リセットで、音色・バンク・リズムパートの指定は初期状態に戻る
                ClearBanks(bankMsb, bankLsb);
                for (int channel = 0; channel < ChannelCount; channel++)
                {
                    _programs[channel].Add(new ProgramPoint(e.Time, 0, -1, -1));
                    _rhythmParts[channel].Add(new RhythmPoint(e.Time, channel == DrumChannel));
                }
                _modes.Add(new ModePoint(e.Time, ResetMode(d)));
                return;
            }

            // GS「Use for Rhythm Part」: F0 41 dev 42 12 40 1x 15 vv sum F7（x はパート。0 が 10、1～9 が 1～9、A～F が 11～16）
            if (d.Length >= 11 && d[1] == 0x41 && d[3] == 0x42 && d[4] == 0x12 && d[5] == 0x40 && (d[6] & 0xF0) == 0x10 && d[7] == 0x15)
            {
                int block = d[6] & 0x0F;
                int part = block == 0 ? DrumChannel : block <= 9 ? block - 1 : block;
                _rhythmParts[part].Add(new RhythmPoint(e.Time, d[8] != 0));
                return;
            }

            // XG「Part Mode」: F0 43 1n 4C 08 pp 07 vv F7（vv が 0 なら通常のパート、1 以上ならドラム）
            if (d.Length >= 9 && d[1] == 0x43 && (d[2] & 0xF0) == 0x10 && d[3] == 0x4C && d[4] == 0x08 && d[5] < ChannelCount && d[6] == 0x07)
            {
                _rhythmParts[d[5]].Add(new RhythmPoint(e.Time, d[7] != 0));
            }
        }

        private static void ClearBanks(int[] bankMsb, int[] bankLsb)
        {
            for (int channel = 0; channel < ChannelCount; channel++)
            {
                bankMsb[channel] = -1;
                bankLsb[channel] = -1;
            }
        }

        /// <summary>リセットの SysEx から、その後の音源の動作モードを決める。</summary>
        private static ModuleMode ResetMode(byte[] d)
        {
            switch (d[1])
            {
                case 0x7E:
                    return d[4] == 0x03 ? ModuleMode.GM2 : ModuleMode.GM;
                case 0x43:
                    return ModuleMode.XG;
                default:
                    return ModuleMode.GS;
            }
        }

        private static ModuleMode ToMode(MidiResetType autoReset)
        {
            switch (autoReset)
            {
                case MidiResetType.XG:
                    return ModuleMode.XG;
                case MidiResetType.GS:
                    return ModuleMode.GS;
                default:
                    return ModuleMode.GM;
            }
        }

        /// <summary>時間順に並んだ一覧から、<paramref name="time"/> 以前で最後のものを探す（二分探索）。無ければ null。</summary>
        private static T FindLast<T>(List<T> points, TimeSpan time, Func<T, TimeSpan> timeOf) where T : class
        {
            int low = 0;
            int high = points.Count;
            while (low < high)
            {
                int middle = (low + high) / 2;
                if (timeOf(points[middle]) <= time)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }
            return low > 0 ? points[low - 1] : null;
        }

        private enum ModuleMode
        {
            GM,
            GM2,
            GS,
            XG,
        }

        private sealed class ProgramPoint
        {
            public static readonly ProgramPoint Default = new ProgramPoint(TimeSpan.Zero, 0, -1, -1);

            public ProgramPoint(TimeSpan time, int program, int bankMsb, int bankLsb)
            {
                Time = time;
                Program = program;
                BankMsb = bankMsb;
                BankLsb = bankLsb;
            }

            public TimeSpan Time { get; }

            public int Program { get; }

            public int BankMsb { get; }

            public int BankLsb { get; }
        }

        private sealed class RhythmPoint
        {
            public RhythmPoint(TimeSpan time, bool isRhythm)
            {
                Time = time;
                IsRhythm = isRhythm;
            }

            public TimeSpan Time { get; }

            public bool IsRhythm { get; }
        }

        private sealed class ModePoint
        {
            public ModePoint(TimeSpan time, ModuleMode mode)
            {
                Time = time;
                Mode = mode;
            }

            public TimeSpan Time { get; }

            public ModuleMode Mode { get; }
        }
    }
}
