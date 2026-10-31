using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KMidiPlayer.Playlist
{
    /// <summary>
    /// TMIDI Player のプレイリスト（.TO2）の読み込み。
    /// </summary>
    /// <remarks>
    /// Shift_JIS のテキストで、1 行目は「#! /usr/local/bin/tmidi」。
    /// 続いて曲ごとに「ファイルのパス、曲名、その他の設定値（数行）」が並び、「@」だけの行で区切られる。
    /// このアプリではファイルのパスだけを使う。
    /// </remarks>
    public static class TmidiPlaylist
    {
        private const string RecordSeparator = "@";

        public static bool IsPlaylistFile(string path)
        {
            return string.Equals(Path.GetExtension(path), ".to2", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// プレイリストに書かれたファイルのパスを、絶対パスにして返す。
        /// </summary>
        public static List<string> Load(string path)
        {
            string text = Encoding.GetEncoding(932).GetString(File.ReadAllBytes(path));
            string baseDirectory = Path.GetDirectoryName(Path.GetFullPath(path));

            var result = new List<string>();
            bool expectPath = true; // 曲の区切りの直後の行がパス
            foreach (string rawLine in text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
            {
                string line = rawLine.Trim();
                if (line == RecordSeparator)
                {
                    expectPath = true;
                }
                else if (expectPath && line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal))
                {
                    expectPath = false;
                    string filePath = PlaylistFile.ResolvePath(line, baseDirectory);
                    if (filePath != null)
                    {
                        result.Add(filePath);
                    }
                }
            }
            return result;
        }
    }
}
