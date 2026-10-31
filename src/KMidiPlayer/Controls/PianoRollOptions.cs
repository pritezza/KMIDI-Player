namespace KMidiPlayer.Controls
{
    /// <summary>ピアノロールの表示の種類。</summary>
    public enum PianoRollStyle
    {
        /// <summary>右から左へ流れる（縦軸が音の高さ）。</summary>
        Horizontal,

        /// <summary>上から下の鍵盤へ落ちてくる。</summary>
        Vertical,

        /// <summary>奥から手前の鍵盤へ流れてくる（遠近法の 3D 表示）。</summary>
        Perspective,

        /// <summary>中心から外側の円い鍵盤へ広がる（角度が音の高さ）。</summary>
        Circular,
    }

    /// <summary>ノートの色分けの方法。</summary>
    public enum PianoRollColorMode
    {
        /// <summary>トラック（チャンネル）ごとの色。</summary>
        Track,

        /// <summary>ベロシティ（強さ）で、弱い青から強い赤まで。</summary>
        Velocity,

        /// <summary>音名（ド・レ・ミ…）ごとの色。</summary>
        PitchClass,
    }

    /// <summary>ノートと鍵盤に書く音名。</summary>
    public enum PianoRollNoteNames
    {
        /// <summary>書かない（鍵盤の C にだけ C4 などを書く）。</summary>
        None,

        /// <summary>C、C#、D…</summary>
        Letters,

        /// <summary>ド、ド#、レ…</summary>
        Solfege,
    }

    /// <summary>
    /// ピアノロールの表示の設定。
    /// </summary>
    public sealed class PianoRollOptions
    {
        public PianoRollStyle Style { get; set; } = PianoRollStyle.Perspective;

        public PianoRollColorMode ColorMode { get; set; } = PianoRollColorMode.PitchClass;

        public PianoRollNoteNames NoteNames { get; set; } = PianoRollNoteNames.Letters;

        public const int MinFramesPerSecond = 10;
        public const int MaxFramesPerSecond = 60;
        public const int DefaultFramesPerSecond = 30;

        /// <summary>メニューに並べる fps。</summary>
        public static readonly int[] FramesPerSecondChoices = { 15, 24, 30, 45, 60 };

        /// <summary>1 秒あたりの描き直しの回数（<see cref="MinFramesPerSecond"/>～<see cref="MaxFramesPerSecond"/>）。</summary>
        public int FramesPerSecond { get; set; } = DefaultFramesPerSecond;

        /// <summary>エフェクトの強さの最大。</summary>
        public const int MaxEffectLevel = 5;

        /// <summary>
        /// ノートが鍵盤（再生位置の線）に届いたときのエフェクトの強さ。0 = なし、1 = 光と火花（控えめ）～
        /// <see cref="MaxEffectLevel"/> = 衝撃の輪・光の柱・尾を引く火花まで出す（派手）。
        /// </summary>
        public int EffectLevel { get; set; } = 3;

        /// <summary>音域を曲に合わせず、ピアノの 88 鍵（A0～C8）の幅で表示するかどうか。</summary>
        public bool FullKeyboard { get; set; } = true;

        /// <summary>曲全体の縮小図を、ウィンドウの下に表示するかどうか。</summary>
        public bool ShowOverview { get; set; } = true;

        /// <summary>曲に入っている歌詞とマーカーを表示するかどうか。</summary>
        public bool ShowLyrics { get; set; } = true;

        /// <summary>GPU（WPF の描画）で描くかどうか。GPU を使えない環境では、オンでも GDI+（CPU）で描く。</summary>
        public bool UseGpu { get; set; } = true;

        public PianoRollOptions Clone()
        {
            return (PianoRollOptions)MemberwiseClone();
        }
    }
}
