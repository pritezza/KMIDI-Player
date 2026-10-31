using System;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// 鍵盤がクリックされたとき（または離されたとき）の情報。
    /// </summary>
    public class KeyboardNoteEventArgs : EventArgs
    {
        public KeyboardNoteEventArgs(int channel, int note, int velocity)
        {
            Channel = channel;
            Note = note;
            Velocity = velocity;
        }

        /// <summary>チャンネル番号（0～15）。</summary>
        public int Channel { get; }

        /// <summary>ノート番号（0～127）。</summary>
        public int Note { get; }

        /// <summary>ベロシティ（1～127）。離されたときは 0。</summary>
        public int Velocity { get; }
    }
}
