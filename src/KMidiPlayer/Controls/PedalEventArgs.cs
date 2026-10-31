using System;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// ペダルのランプが操作されたことを表す。
    /// </summary>
    public sealed class PedalEventArgs : EventArgs
    {
        public PedalEventArgs(int controller)
        {
            Controller = controller;
        }

        /// <summary>ペダルの CC 番号（64 = ダンパー、66 = ソステヌート、67 = ソフト）。</summary>
        public int Controller { get; }
    }
}
