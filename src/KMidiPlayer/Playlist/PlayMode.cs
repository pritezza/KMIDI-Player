namespace KMidiPlayer.Playlist
{
    /// <summary>
    /// 曲の最後まで再生したときの動作。値の並びは画面のコンボボックスの項目順と合わせている。
    /// </summary>
    public enum PlayMode
    {
        /// <summary>現在の曲を再生して停止する。</summary>
        Single,

        /// <summary>現在の曲を繰り返す。</summary>
        RepeatOne,

        /// <summary>プレイリストを最後まで再生して停止する。</summary>
        All,

        /// <summary>プレイリストを繰り返す。</summary>
        RepeatAll,

        /// <summary>プレイリストをランダムな順で再生する（一巡したら順番を変えて繰り返す）。</summary>
        Shuffle,
    }
}
