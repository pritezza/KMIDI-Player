using System;
using System.IO;
using KMidiPlayer.Midi;

namespace KMidiPlayer.Playlist
{
    /// <summary>
    /// プレイリストの 1 曲分の情報。
    /// </summary>
    public sealed class PlaylistItem
    {
        public const int MaxRating = 5;

        private int _rating;

        public PlaylistItem(string filePath, int rating = 0)
        {
            FilePath = filePath;
            Rating = rating;
        }

        public string FilePath { get; }

        /// <summary>お気に入りの評価（0 = 未評価、1～5）。範囲外の値は範囲内に丸める。</summary>
        public int Rating
        {
            get => _rating;
            set => _rating = Math.Max(0, Math.Min(MaxRating, value));
        }

        public string FileName => Path.GetFileName(FilePath);

        /// <summary>曲名。まだ読み込んでいない、または曲名が無い場合は null。</summary>
        public string Title { get; private set; }

        /// <summary>演奏時間。まだ読み込んでいない場合は null。</summary>
        public TimeSpan? Duration { get; private set; }

        /// <summary>読み込みに失敗した理由。成功している場合は null。</summary>
        public string ErrorMessage { get; private set; }

        public bool HasError => ErrorMessage != null;

        /// <summary>画面に表示する曲名。曲名が無い場合はファイル名（拡張子なし）。</summary>
        public string DisplayTitle => string.IsNullOrEmpty(Title) ? Path.GetFileNameWithoutExtension(FilePath) : Title;

        public void SetSongInfo(MidiFile song)
        {
            Title = song.Title;
            Duration = song.Duration;
            ErrorMessage = null;
        }

        public void SetError(string message)
        {
            Duration = null;
            ErrorMessage = message;
        }
    }
}
