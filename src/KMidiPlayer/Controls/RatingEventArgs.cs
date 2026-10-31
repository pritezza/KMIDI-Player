using System;
using System.Windows.Forms;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// プレイリストの曲の、お気に入りの評価が操作で変えられたことを表す。
    /// </summary>
    public sealed class RatingEventArgs : EventArgs
    {
        public RatingEventArgs(ListViewItem item, int rating)
        {
            Item = item;
            Rating = rating;
        }

        public ListViewItem Item { get; }

        /// <summary>新しい評価（0 = 未評価、1～5）。</summary>
        public int Rating { get; }
    }
}
