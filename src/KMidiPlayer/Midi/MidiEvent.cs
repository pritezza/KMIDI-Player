using System;

namespace KMidiPlayer.Midi
{
    public enum MidiEventKind
    {
        /// <summary>チャンネルメッセージ（ノートオン、コントロールチェンジなど）。</summary>
        Channel,

        /// <summary>システムエクスクルーシブ。</summary>
        SysEx,

        /// <summary>メタイベント（テンポ、曲名など。音源には送らない）。</summary>
        Meta,
    }

    /// <summary>
    /// MIDI ファイル内の 1 つのイベント。
    /// </summary>
    public sealed class MidiEvent
    {
        private MidiEvent(MidiEventKind kind, long tick, int track, byte status, byte data1, byte data2, byte metaType, byte[] data)
        {
            Kind = kind;
            Tick = tick;
            Track = track;
            Status = status;
            Data1 = data1;
            Data2 = data2;
            MetaType = metaType;
            Data = data;
        }

        public MidiEventKind Kind { get; }

        /// <summary>曲の先頭からの位置（ティック）。</summary>
        public long Tick { get; }

        /// <summary>曲の先頭からの時間（テンポ 100% のとき）。</summary>
        public TimeSpan Time { get; internal set; }

        /// <summary>イベントが含まれていたトラックの番号（0 始まり）。</summary>
        public int Track { get; }

        /// <summary>ステータスバイト。チャンネルメッセージは 0x80～0xEF、SysEx は 0xF0 / 0xF7、メタイベントは 0xFF。</summary>
        public byte Status { get; }

        /// <summary>チャンネルメッセージの種類（0x80、0x90、…、0xE0）。</summary>
        public int Command => Status & 0xF0;

        /// <summary>チャンネル番号（0～15）。</summary>
        public int Channel => Status & 0x0F;

        public byte Data1 { get; }

        public byte Data2 { get; }

        /// <summary>メタイベントの種類（0x03 = トラック名、0x51 = テンポなど）。</summary>
        public byte MetaType { get; }

        /// <summary>SysEx の場合は音源に送るバイト列、メタイベントの場合はデータ部。</summary>
        public byte[] Data { get; }

        public static MidiEvent CreateChannel(long tick, int track, byte status, byte data1, byte data2)
        {
            return new MidiEvent(MidiEventKind.Channel, tick, track, status, data1, data2, 0, null);
        }

        public static MidiEvent CreateSysEx(long tick, int track, byte[] message)
        {
            byte status = message.Length > 0 && message[0] == 0xF0 ? (byte)0xF0 : (byte)0xF7;
            return new MidiEvent(MidiEventKind.SysEx, tick, track, status, 0, 0, 0, message);
        }

        public static MidiEvent CreateMeta(long tick, int track, byte metaType, byte[] data)
        {
            return new MidiEvent(MidiEventKind.Meta, tick, track, 0xFF, 0, 0, metaType, data);
        }
    }
}
