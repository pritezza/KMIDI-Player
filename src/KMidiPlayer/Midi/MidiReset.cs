namespace KMidiPlayer.Midi
{
    /// <summary>
    /// 曲の再生前に音源へ送るリセットの種類。
    /// </summary>
    public enum MidiResetType
    {
        None,
        GM,
        GS,
        XG,
    }

    /// <summary>
    /// 音源をリセットする SysEx メッセージ。
    /// </summary>
    public static class MidiReset
    {
        /// <summary>リセットを送った後、音源の初期化が終わるのを待つ時間。</summary>
        public const int WaitMilliseconds = 50;

        private static readonly byte[] GmSystemOn = { 0xF0, 0x7E, 0x7F, 0x09, 0x01, 0xF7 };
        private static readonly byte[] GsReset = { 0xF0, 0x41, 0x10, 0x42, 0x12, 0x40, 0x00, 0x7F, 0x00, 0x41, 0xF7 };
        private static readonly byte[] XgSystemOn = { 0xF0, 0x43, 0x10, 0x4C, 0x00, 0x00, 0x7E, 0x00, 0xF7 };

        /// <summary>
        /// 指定した種類のリセットメッセージを返す。<see cref="MidiResetType.None"/> の場合は null。
        /// </summary>
        public static byte[] GetMessage(MidiResetType type)
        {
            switch (type)
            {
                case MidiResetType.GM:
                    return (byte[])GmSystemOn.Clone();
                case MidiResetType.GS:
                    return (byte[])GsReset.Clone();
                case MidiResetType.XG:
                    return (byte[])XgSystemOn.Clone();
                default:
                    return null;
            }
        }

        /// <summary>
        /// GM System On / GS Reset / XG System On などの、音源全体をリセットする SysEx かどうか。
        /// </summary>
        public static bool IsResetMessage(byte[] d)
        {
            if (d == null || d.Length < 6 || d[0] != 0xF0)
            {
                return false;
            }
            // GM System On（F0 7E 7F 09 01 F7）/ GM2 System On（F0 7E 7F 09 03 F7）
            if (d[1] == 0x7E && d[3] == 0x09 && (d[4] == 0x01 || d[4] == 0x03))
            {
                return true;
            }
            // GS Reset（F0 41 10 42 12 40 00 7F 00 41 F7）/ System Mode Set（F0 41 10 42 12 00 00 7F ...）
            if (d.Length >= 10 && d[1] == 0x41 && d[3] == 0x42 && d[4] == 0x12
                && (d[5] == 0x40 || d[5] == 0x00) && d[6] == 0x00 && d[7] == 0x7F)
            {
                return true;
            }
            // XG System On（F0 43 10 4C 00 00 7E 00 F7）
            if (d.Length >= 8 && d[1] == 0x43 && d[3] == 0x4C && d[4] == 0x00 && d[5] == 0x00 && d[6] == 0x7E)
            {
                return true;
            }
            return false;
        }
    }
}
