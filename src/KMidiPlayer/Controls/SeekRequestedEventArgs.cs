using System;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// 再生位置の移動が求められたことを表す。
    /// </summary>
    public sealed class SeekRequestedEventArgs : EventArgs
    {
        public SeekRequestedEventArgs(TimeSpan position)
        {
            Position = position;
        }

        /// <summary>移動先（曲の上の時間）。</summary>
        public TimeSpan Position { get; }
    }
}
