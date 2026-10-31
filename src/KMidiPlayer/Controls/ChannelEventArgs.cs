using System;

namespace KMidiPlayer.Controls
{
    public class ChannelEventArgs : EventArgs
    {
        public ChannelEventArgs(int channel)
        {
            Channel = channel;
        }

        /// <summary>チャンネル番号（0～15）。</summary>
        public int Channel { get; }
    }
}
