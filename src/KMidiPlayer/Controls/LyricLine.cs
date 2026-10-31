using System;
using System.Collections.Generic;
using KMidiPlayer.Midi;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// 歌詞の 1 行（画面に一度に出す分）。歌詞のイベントを、改行の印や間の空き具合で行に分けたもの。
    /// </summary>
    internal sealed class LyricLine
    {
        /// <summary>前の歌詞からこれだけ空いたら、次の行にする。</summary>
        private static readonly TimeSpan LineGap = TimeSpan.FromSeconds(3);

        /// <summary>1 行がこれより長くなるなら、次の行にする（文字数）。</summary>
        private const int MaxLineLength = 32;

        private LyricLine(List<TimedText> syllables)
        {
            Syllables = syllables;
        }

        /// <summary>行の中の歌詞（歌う時間順）。</summary>
        public IReadOnlyList<TimedText> Syllables { get; }

        public TimeSpan Start => Syllables[0].Time;

        public TimeSpan LastTime => Syllables[Syllables.Count - 1].Time;

        /// <summary>
        /// 歌詞のイベントを行に分ける。改行（\r・\n）、カラオケの MIDI の改行の印（行頭の / と \）、
        /// 前の歌詞からの空き、1 行の長さで区切る。
        /// </summary>
        public static List<LyricLine> Build(IReadOnlyList<TimedText> lyrics)
        {
            var lines = new List<LyricLine>();
            var current = new List<TimedText>();
            int length = 0;
            void CloseLine()
            {
                if (current.Count > 0)
                {
                    lines.Add(new LyricLine(current));
                    current = new List<TimedText>();
                    length = 0;
                }
            }

            foreach (TimedText lyric in lyrics)
            {
                string text = lyric.Text;
                bool breakBefore = text.StartsWith("/", StringComparison.Ordinal) || text.StartsWith("\\", StringComparison.Ordinal);
                bool breakAfter = text.EndsWith("\r", StringComparison.Ordinal) || text.EndsWith("\n", StringComparison.Ordinal);
                text = text.Trim('/', '\\', '\r', '\n');

                if (breakBefore || (current.Count > 0 && lyric.Time - current[current.Count - 1].Time > LineGap)
                    || length + text.Length > MaxLineLength)
                {
                    CloseLine();
                }
                if (text.Length > 0)
                {
                    current.Add(new TimedText(lyric.Time, text));
                    length += text.Length;
                }
                if (breakAfter)
                {
                    CloseLine();
                }
            }
            CloseLine();
            return lines;
        }
    }
}
