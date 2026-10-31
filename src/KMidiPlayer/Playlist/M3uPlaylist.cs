using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace KMidiPlayer.Playlist
{
    /// <summary>
    /// M3U 形式のプレイリストの読み書き。
    /// </summary>
    /// <remarks>
    /// お気に入りの評価は、曲のパスの前の行に「#KMIDI-RATING:3」のように書く（未評価の曲には書かない）。
    /// 「#」で始まる行は、他のプレイヤーではコメントとして読み飛ばされる。
    /// </remarks>
    public static class M3uPlaylist
    {
        private const string RatingDirective = "#KMIDI-RATING:";

        private static readonly string[] Extensions = { ".m3u", ".m3u8" };

        public static bool IsPlaylistFile(string path)
        {
            string extension = Path.GetExtension(path);
            return Extensions.Any(playlistExtension => string.Equals(extension, playlistExtension, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// プレイリストに書かれた曲を、パスを絶対パスにし、お気に入りの評価を付けて返す。ファイルが存在するかどうかは確かめない。
        /// </summary>
        /// <remarks>
        /// .m3u8 は UTF-8 として読む。.m3u は Shift_JIS として読むが、BOM がある場合や、
        /// UTF-8 として正しく解釈できる場合は UTF-8 として読む。
        /// </remarks>
        public static List<PlaylistItem> Load(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            string text = Decode(bytes, string.Equals(Path.GetExtension(path), ".m3u8", StringComparison.OrdinalIgnoreCase));
            string baseDirectory = Path.GetDirectoryName(Path.GetFullPath(path));

            var result = new List<PlaylistItem>();
            int rating = 0; // 次の曲の評価
            foreach (string rawLine in text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
            {
                string line = rawLine.Trim();
                if (line.StartsWith(RatingDirective, StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(line.Substring(RatingDirective.Length).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out rating);
                    continue;
                }
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                string filePath = PlaylistFile.ResolvePath(line, baseDirectory);
                if (filePath != null)
                {
                    result.Add(new PlaylistItem(filePath, rating));
                }
                rating = 0;
            }
            return result;
        }

        /// <summary>
        /// プレイリストを UTF-8 で保存する（.m3u8 は BOM なし。.m3u は Shift_JIS と区別できるよう BOM 付き）。
        /// パスは常に絶対パスで書く（プレイリストファイルだけを別の場所へ移しても読めるようにするため）。
        /// </summary>
        public static void Save(string path, IEnumerable<PlaylistItem> items)
        {
            var builder = new StringBuilder();
            builder.Append("#EXTM3U\r\n");
            foreach (PlaylistItem item in items)
            {
                int seconds = item.Duration.HasValue ? (int)Math.Round(item.Duration.Value.TotalSeconds) : -1;
                builder.Append("#EXTINF:").Append(seconds).Append(',').Append(item.DisplayTitle).Append("\r\n");
                if (item.Rating > 0)
                {
                    builder.Append(RatingDirective).Append(item.Rating.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
                }
                builder.Append(item.FilePath).Append("\r\n");
            }

            bool isUtf8Extension = string.Equals(Path.GetExtension(path), ".m3u8", StringComparison.OrdinalIgnoreCase);
            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(!isUtf8Extension));
        }

        private static string Decode(byte[] bytes, bool isUtf8)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            }
            if (isUtf8)
            {
                return Encoding.UTF8.GetString(bytes);
            }

            try
            {
                // 正しくない UTF-8 のバイト列なら例外を投げる設定で試す
                return new UTF8Encoding(false, true).GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                return Encoding.GetEncoding(932).GetString(bytes);
            }
        }
    }
}
