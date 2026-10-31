using System;

namespace KMidiPlayer.Midi
{
    /// <summary>
    /// MIDI デバイスの操作に失敗したときの例外。
    /// </summary>
    public class MidiException : Exception
    {
        public MidiException(string message)
            : base(message)
        {
        }
    }
}
