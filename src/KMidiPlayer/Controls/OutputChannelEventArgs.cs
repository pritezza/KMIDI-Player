using System;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// 送信先のチャンネル（CH）が変更されたときの情報。
    /// </summary>
    public class OutputChannelEventArgs : EventArgs
    {
        public OutputChannelEventArgs(int channel, int outputChannel)
        {
            Channel = channel;
            OutputChannel = outputChannel;
        }

        /// <summary>曲データのチャンネル番号（TR。0～15）。</summary>
        public int Channel { get; }

        /// <summary>新しい送信先のチャンネル番号（0～15）。</summary>
        public int OutputChannel { get; }
    }
}
