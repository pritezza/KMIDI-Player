namespace KMidiPlayer.Midi
{
    /// <summary>
    /// 曲の上の位置を、小節・拍・ティックで表したもの。
    /// </summary>
    public struct MusicalPosition
    {
        public MusicalPosition(int bar, int beat, int beatsPerBar, int beatUnit, int tickInBeat, int ticksPerBeat)
        {
            Bar = bar;
            Beat = beat;
            BeatsPerBar = beatsPerBar;
            BeatUnit = beatUnit;
            TickInBeat = tickInBeat;
            TicksPerBeat = ticksPerBeat;
        }

        /// <summary>拍子の分母（4 なら 4 分音符が 1 拍）。</summary>
        public int BeatUnit { get; }

        /// <summary>小節番号（1 から）。</summary>
        public int Bar { get; }

        /// <summary>小節の中の拍（1 から）。</summary>
        public int Beat { get; }

        /// <summary>1 小節の拍数（拍子の分子）。</summary>
        public int BeatsPerBar { get; }

        /// <summary>拍の中のティック（0 から）。</summary>
        public int TickInBeat { get; }

        /// <summary>1 拍のティック数（拍子の分母の音符の長さ）。</summary>
        public int TicksPerBeat { get; }
    }
}
