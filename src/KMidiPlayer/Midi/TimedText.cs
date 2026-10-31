using System;

namespace KMidiPlayer.Midi
{
    /// <summary>
    /// 曲の中の、時間の付いた文字列（歌詞やマーカー）。
    /// </summary>
    public sealed class TimedText
    {
        public TimedText(TimeSpan time, string text)
        {
            Time = time;
            Text = text;
        }

        /// <summary>時間（テンポ 100% のとき）。</summary>
        public TimeSpan Time { get; }

        public string Text { get; }
    }
}
