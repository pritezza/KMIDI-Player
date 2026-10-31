using System;
using System.Runtime.InteropServices;
using System.Text;

namespace KMidiPlayer.Midi
{
    /// <summary>
    /// winmm.dll（Windows マルチメディア API）の関数宣言。
    /// </summary>
    internal static class NativeMethods
    {
        public const int MMSYSERR_NOERROR = 0;
        public const int MIDIERR_STILLPLAYING = 65;
        public const uint CALLBACK_NULL = 0;
        public const int MAXERRORLENGTH = 256;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct MIDIOUTCAPS
        {
            public ushort wMid;
            public ushort wPid;
            public uint vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            public ushort wTechnology;
            public ushort wVoices;
            public ushort wNotes;
            public ushort wChannelMask;
            public uint dwSupport;
        }

        /// <summary>
        /// SysEx 送信用のバッファ情報。ドライバーが非同期に参照するため、アンマネージドメモリに置いて使う。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MIDIHDR
        {
            public IntPtr lpData;
            public uint dwBufferLength;
            public uint dwBytesRecorded;
            public IntPtr dwUser;
            public uint dwFlags;
            public IntPtr lpNext;
            public IntPtr reserved;
            public uint dwOffset;
            public IntPtr dwReserved0;
            public IntPtr dwReserved1;
            public IntPtr dwReserved2;
            public IntPtr dwReserved3;
            public IntPtr dwReserved4;
            public IntPtr dwReserved5;
            public IntPtr dwReserved6;
            public IntPtr dwReserved7;
        }

        [DllImport("winmm.dll")]
        public static extern uint midiOutGetNumDevs();

        [DllImport("winmm.dll", EntryPoint = "midiOutGetDevCapsW", CharSet = CharSet.Unicode)]
        public static extern int midiOutGetDevCaps(UIntPtr uDeviceID, ref MIDIOUTCAPS pmoc, uint cbmoc);

        [DllImport("winmm.dll")]
        public static extern int midiOutOpen(out IntPtr phmo, uint uDeviceID, IntPtr dwCallback, IntPtr dwInstance, uint fdwOpen);

        [DllImport("winmm.dll")]
        public static extern int midiOutClose(IntPtr hmo);

        [DllImport("winmm.dll")]
        public static extern int midiOutReset(IntPtr hmo);

        [DllImport("winmm.dll")]
        public static extern int midiOutShortMsg(IntPtr hmo, uint dwMsg);

        [DllImport("winmm.dll")]
        public static extern int midiOutPrepareHeader(IntPtr hmo, IntPtr pmh, uint cbmh);

        [DllImport("winmm.dll")]
        public static extern int midiOutUnprepareHeader(IntPtr hmo, IntPtr pmh, uint cbmh);

        [DllImport("winmm.dll")]
        public static extern int midiOutLongMsg(IntPtr hmo, IntPtr pmh, uint cbmh);

        [DllImport("winmm.dll", EntryPoint = "midiOutGetErrorTextW", CharSet = CharSet.Unicode)]
        public static extern int midiOutGetErrorText(int mmrError, StringBuilder pszText, uint cchText);

        [DllImport("winmm.dll")]
        public static extern uint timeBeginPeriod(uint uPeriod);

        [DllImport("winmm.dll")]
        public static extern uint timeEndPeriod(uint uPeriod);
    }
}
