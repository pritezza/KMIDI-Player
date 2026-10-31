using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace KMidiPlayer.Midi
{
    /// <summary>
    /// Windows の MIDI 出力デバイス。
    /// 再生スレッドと画面のスレッドの両方から呼ばれるため、送信処理はロックで保護している。
    /// </summary>
    public sealed class MidiOutDevice : IDisposable
    {
        /// <summary>SysEx の送信完了を待つ最大時間。</summary>
        private const int LongMessageTimeoutMilliseconds = 2000;

        private readonly object _sync = new object();
        private IntPtr _handle;

        private MidiOutDevice(int deviceId, string name, IntPtr handle)
        {
            DeviceId = deviceId;
            Name = name;
            _handle = handle;
        }

        public int DeviceId { get; }

        public string Name { get; }

        /// <summary>
        /// システムにある MIDI 出力デバイスの名前の一覧を返す。リストの添字がデバイス ID になる。
        /// </summary>
        public static IReadOnlyList<string> GetDeviceNames()
        {
            uint count = NativeMethods.midiOutGetNumDevs();
            var names = new List<string>((int)count);
            uint capsSize = (uint)Marshal.SizeOf(typeof(NativeMethods.MIDIOUTCAPS));
            for (uint i = 0; i < count; i++)
            {
                var caps = new NativeMethods.MIDIOUTCAPS();
                int result = NativeMethods.midiOutGetDevCaps(new UIntPtr(i), ref caps, capsSize);
                names.Add(result == NativeMethods.MMSYSERR_NOERROR ? caps.szPname : Texts.MidiOutputName((int)i + 1));
            }
            return names;
        }

        /// <summary>
        /// 指定した ID のデバイスを開く。
        /// </summary>
        /// <exception cref="MidiException">デバイスを開けなかった場合。</exception>
        public static MidiOutDevice Open(int deviceId)
        {
            IReadOnlyList<string> names = GetDeviceNames();
            if (deviceId < 0 || deviceId >= names.Count)
            {
                throw new MidiException(Texts.DeviceNotFound);
            }

            int result = NativeMethods.midiOutOpen(out IntPtr handle, (uint)deviceId, IntPtr.Zero, IntPtr.Zero, NativeMethods.CALLBACK_NULL);
            if (result != NativeMethods.MMSYSERR_NOERROR)
            {
                throw new MidiException(Texts.DeviceOpenFailed(names[deviceId], GetErrorText(result)));
            }
            return new MidiOutDevice(deviceId, names[deviceId], handle);
        }

        /// <summary>
        /// チャンネルメッセージ（ノートオン、コントロールチェンジなど）を送る。
        /// 送信に失敗しても例外は投げない（再生スレッドを止めないため）。
        /// </summary>
        public void SendShortMessage(int status, int data1, int data2)
        {
            uint message = (uint)(status & 0xFF) | (uint)((data1 & 0x7F) << 8) | (uint)((data2 & 0x7F) << 16);
            lock (_sync)
            {
                if (_handle != IntPtr.Zero)
                {
                    NativeMethods.midiOutShortMsg(_handle, message);
                }
            }
        }

        /// <summary>
        /// SysEx メッセージを送り、送信が終わるまで待つ。
        /// 送信に失敗しても例外は投げない（再生スレッドを止めないため）。
        /// </summary>
        public void SendLongMessage(byte[] message)
        {
            if (message == null || message.Length == 0)
            {
                return;
            }

            uint headerSize = (uint)Marshal.SizeOf(typeof(NativeMethods.MIDIHDR));
            IntPtr data = Marshal.AllocHGlobal(message.Length);
            IntPtr header = Marshal.AllocHGlobal((int)headerSize);
            bool canFree = true;
            try
            {
                Marshal.Copy(message, 0, data, message.Length);
                var midiHeader = new NativeMethods.MIDIHDR
                {
                    lpData = data,
                    dwBufferLength = (uint)message.Length,
                    dwBytesRecorded = (uint)message.Length,
                };
                Marshal.StructureToPtr(midiHeader, header, false);

                lock (_sync)
                {
                    if (_handle == IntPtr.Zero)
                    {
                        return;
                    }
                    if (NativeMethods.midiOutPrepareHeader(_handle, header, headerSize) != NativeMethods.MMSYSERR_NOERROR)
                    {
                        return;
                    }

                    NativeMethods.midiOutLongMsg(_handle, header, headerSize);

                    // ドライバーがバッファを使い終わるまで待ってから解放する
                    var stopwatch = Stopwatch.StartNew();
                    while (NativeMethods.midiOutUnprepareHeader(_handle, header, headerSize) == NativeMethods.MIDIERR_STILLPLAYING)
                    {
                        if (stopwatch.ElapsedMilliseconds > LongMessageTimeoutMilliseconds)
                        {
                            // まだ使われている可能性があるので、解放せずに諦める
                            canFree = false;
                            return;
                        }
                        Thread.Sleep(1);
                    }
                }
            }
            finally
            {
                if (canFree)
                {
                    Marshal.FreeHGlobal(header);
                    Marshal.FreeHGlobal(data);
                }
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_handle == IntPtr.Zero)
                {
                    return;
                }
                NativeMethods.midiOutReset(_handle);
                NativeMethods.midiOutClose(_handle);
                _handle = IntPtr.Zero;
            }
        }

        private static string GetErrorText(int errorCode)
        {
            var text = new StringBuilder(NativeMethods.MAXERRORLENGTH);
            if (NativeMethods.midiOutGetErrorText(errorCode, text, (uint)text.Capacity) == NativeMethods.MMSYSERR_NOERROR)
            {
                return text.ToString();
            }
            return Texts.ErrorCode(errorCode);
        }
    }
}
