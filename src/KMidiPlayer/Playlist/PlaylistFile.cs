using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KMidiPlayer.Playlist
{
    /// <summary>
    /// プレイリストファイル（M3U、TMIDI Player の TO2）の読み込みの窓口と、共通の処理。
    /// </summary>
    public static class PlaylistFile
    {
        public static bool IsPlaylistFile(string path)
        {
            return M3uPlaylist.IsPlaylistFile(path) || TmidiPlaylist.IsPlaylistFile(path);
        }

        /// <summary>
        /// プレイリストに書かれた曲を、パスを絶対パスにして返す。お気に入りの評価は M3U だけが持つ（TO2 の曲は未評価）。
        /// </summary>
        public static List<PlaylistItem> Load(string path)
        {
            return TmidiPlaylist.IsPlaylistFile(path)
                ? TmidiPlaylist.Load(path).Select(filePath => new PlaylistItem(filePath)).ToList()
                : M3uPlaylist.Load(path);
        }

        /// <summary>
        /// プレイリストの 1 行に書かれたパスを、絶対パスにする。パスとして使えない行は null を返す。
        /// </summary>
        /// <remarks>
        /// 書かれた場所にファイルが無い場合は、ドライブやフォルダを移した可能性を考えて、
        /// パスの後ろの部分をプレイリストのフォルダにつないだ場所を、長いものから順に探す。
        /// 例: プレイリストが E:\MIDI にあり、G:\古い\作曲家\曲.mid が見つからなければ、
        /// E:\MIDI\古い\作曲家\曲.mid、E:\MIDI\作曲家\曲.mid、E:\MIDI\曲.mid の順に探す。
        /// どこにも無ければ、書かれたとおりのパスを返す（読み込みエラーとして表示するため）。
        /// </remarks>
        internal static string ResolvePath(string entry, string baseDirectory)
        {
            string fullPath;
            try
            {
                if (Uri.TryCreate(entry, UriKind.Absolute, out Uri uri) && !IsLocalPathLike(entry))
                {
                    // file:/// 形式だけを受け付け、http などの URL は読み飛ばす
                    if (!uri.IsFile)
                    {
                        return null;
                    }
                    fullPath = uri.LocalPath;
                }
                else
                {
                    fullPath = Path.GetFullPath(Path.Combine(baseDirectory, entry));
                }
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
            {
                return null; // パスとして使えない文字を含む行
            }

            return File.Exists(fullPath) ? fullPath : FindMovedFile(fullPath, baseDirectory) ?? fullPath;
        }

        private static string FindMovedFile(string fullPath, string baseDirectory)
        {
            string root = Path.GetPathRoot(fullPath) ?? string.Empty;
            string[] parts = fullPath.Substring(root.Length).Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            for (int start = 0; start < parts.Length; start++)
            {
                string candidate = Path.Combine(baseDirectory, string.Join(Path.DirectorySeparatorChar.ToString(), parts, start, parts.Length - start));
                if (!string.Equals(candidate, fullPath, StringComparison.OrdinalIgnoreCase) && File.Exists(candidate))
                {
                    return candidate;
                }
            }
            return null;
        }

        /// <summary>「C:\...」や「\\server\...」のような、URI としても解釈できてしまう Windows のパスかどうか。</summary>
        private static bool IsLocalPathLike(string entry)
        {
            return (entry.Length >= 2 && entry[1] == ':') || entry.StartsWith(@"\\", StringComparison.Ordinal);
        }
    }
}
