using System;

namespace KMidiPlayer.Midi
{
    /// <summary>
    /// 曲の中の 1 つのノート（ノートオンからノートオフまで）。ピアノロールの表示に使う。
    /// </summary>
    public sealed class NoteSpan
    {
        public NoteSpan(int channel, int note, int velocity, TimeSpan start, TimeSpan end)
        {
            Channel = channel;
            Note = note;
            Velocity = velocity;
            Start = start;
            End = end;
        }

        /// <summary>曲データのチャンネル（0～15）。</summary>
        public int Channel { get; }

        /// <summary>曲データの音高（移調する前）。</summary>
        public int Note { get; }

        public int Velocity { get; }

        /// <summary>鳴り始める時間（テンポ 100% のとき）。</summary>
        public TimeSpan Start { get; }

        /// <summary>鳴り終わる時間（テンポ 100% のとき）。ノートオフが無ければ曲の最後。</summary>
        public TimeSpan End { get; }
    }
}
