using System;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// チャンネルごとの数値（移調、オクターブなど）が変更されたときの情報。
    /// </summary>
    public class ChannelValueEventArgs : EventArgs
    {
        public ChannelValueEventArgs(int channel, int value)
        {
            Channel = channel;
            Value = value;
        }

        /// <summary>曲データのチャンネル番号（TR。0～15）。</summary>
        public int Channel { get; }

        /// <summary>新しい値。</summary>
        public int Value { get; }
    }
}
