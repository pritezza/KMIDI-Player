using System;
using System.Drawing;
using System.Windows.Forms;
using KMidiPlayer.Playlist;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// お気に入りの評価の星（★☆）の描き方と、横位置から評価を求める計算。プレイリストと評価のフィルターで共通に使う。
    /// </summary>
    public static class RatingStars
    {
        /// <summary>星の並びの左端の余白。</summary>
        public const int Padding = 4;

        /// <summary>
        /// 星 1 つ分の幅。「★」は代わりのフォントで描かれて幅が測りにくいので、全角 1 文字分（フォントの高さ）で並べる。
        /// </summary>
        public static int StarWidth(Font font) => font.Height + 2;

        /// <summary>星 5 つ（左右の余白を含む）の幅。</summary>
        public static int TotalWidth(Font font) => Padding * 2 + StarWidth(font) * PlaylistItem.MaxRating;

        /// <summary>
        /// area の左端から、星を 5 つ描く（rating 個を配色の星の色で、残りを emptyColor で。どちらも塗りつぶしの ★）。
        /// </summary>
        public static void Draw(Graphics g, Rectangle area, Font font, int rating, Color emptyColor)
        {
            int starWidth = StarWidth(font);
            for (int star = 0; star < PlaylistItem.MaxRating; star++)
            {
                bool filled = star < rating;
                var bounds = new Rectangle(area.Left + Padding + star * starWidth, area.Top, starWidth, area.Height);
                TextRenderer.DrawText(g, "★", font, bounds, filled ? Theme.Current.StarFilled : emptyColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }

        /// <summary>
        /// 星の並びの左端（left）からの横位置で、評価を求める。最初の星より左なら 0、5 つ目の星より右なら 5。
        /// </summary>
        public static int RatingFromX(Font font, int left, int x)
        {
            int offset = x - (left + Padding);
            if (offset < 0)
            {
                return 0;
            }
            return Math.Min(PlaylistItem.MaxRating, offset / StarWidth(font) + 1);
        }
    }
}
