using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace KMidiPlayer
{
    /// <summary>
    /// ファイルをごみ箱に移動する（エクスプローラーで削除したときと同じ。ごみ箱から元に戻せる）。
    /// </summary>
    internal static class RecycleBin
    {
        private const uint FO_DELETE = 3;
        private const ushort FOF_NOCONFIRMATION = 0x0010;
        private const ushort FOF_ALLOWUNDO = 0x0040;
        private const ushort FOF_NOERRORUI = 0x0400;
        private const ushort FOF_WANTNUKEWARNING = 0x4000;

        /// <remarks>x64 専用のレイアウト（32 ビットでは詰めて並ぶので合わない）。</remarks>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            public string pFrom;
            public string pTo;
            public ushort fFlags;
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

        /// <summary>
        /// ファイルをまとめてごみ箱に移動する。数が多いときは、Windows の進行状況のダイアログが出る。
        /// </summary>
        /// <remarks>
        /// 確認は呼び出し側で済ませておくこと（ここでは尋ねない）。
        /// ただし、ネットワーク上のファイルなどごみ箱に入れられない場合は、完全に削除してよいかを Windows が尋ねる。
        /// 削除できなかったファイルがあっても、ここではメッセージを出さない（呼び出し側で、残ったファイルを確かめること）。
        /// </remarks>
        /// <param name="owner">ダイアログの親ウィンドウ。</param>
        /// <returns>ユーザーが途中で取りやめたら true。</returns>
        public static bool MoveToRecycleBin(IntPtr owner, IEnumerable<string> paths)
        {
            var operation = new SHFILEOPSTRUCT
            {
                hwnd = owner,
                wFunc = FO_DELETE,
                // 複数のファイルは \0 で区切り、最後を \0\0 で終える（最後の 1 つはマーシャリングで付く）
                pFrom = string.Join("\0", paths) + "\0",
                fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_NOERRORUI | FOF_WANTNUKEWARNING,
            };
            SHFileOperation(ref operation);
            return operation.fAnyOperationsAborted;
        }
    }
}
