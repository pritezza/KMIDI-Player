using System;

namespace KMidiPlayer.Controls
{
    /// <summary>レベルメーターの表示タイプ（設定画面の一覧はこの順に並ぶ）。</summary>
    public enum LevelMeterStyle
    {
        /// <summary>単色のバー。</summary>
        Solid,

        /// <summary>緑 → 黄 → 赤のグラデーションのバー。</summary>
        Gradient,

        /// <summary>LED のように区切ったセグメント（位置によって緑・黄・赤）。</summary>
        Segments,

        /// <summary>LED のように区切ったセグメント（単色）。</summary>
        SegmentsMono,

        /// <summary>大きく 8 つに区切ったブロック（位置によって緑・黄・赤）。</summary>
        Blocks,

        /// <summary>丸い LED を並べたもの（位置によって緑・黄・赤）。</summary>
        Dots,

        /// <summary>黒地に光る LED（消灯したセグメントも暗く見える、機材風）。</summary>
        Hardware,

        /// <summary>虹色のグラデーションのバー（紫 → 赤）。</summary>
        Rainbow,

        /// <summary>中央から左右に伸びるバー。</summary>
        Center,

        /// <summary>レベルの位置に細い針だけを表示する。</summary>
        Needle,
    }

    /// <summary>
    /// レベルメーターの表示設定。
    /// </summary>
    public sealed class LevelMeterSettings
    {
        public const int MinFramesPerSecond = 10;
        public const int MaxFramesPerSecond = 60;
        public const double MinFallSeconds = 0.1;
        public const double MaxFallSeconds = 3.0;
        public const double MinPeakHoldSeconds = 0.2;
        public const double MaxPeakHoldSeconds = 5.0;

        /// <summary>
        /// 1 秒あたりの更新回数。チャンネルパネル全体（鍵盤・ペダルのランプを含む）の表示の更新に使う。
        /// </summary>
        public int FramesPerSecond { get; set; } = 30;

        public LevelMeterStyle Style { get; set; } = LevelMeterStyle.Solid;

        /// <summary>メーターが 10% まで下がるのにかかる時間（秒）。小さいほど速く下がる。</summary>
        public double FallSeconds { get; set; } = 0.8;

        /// <summary>直近の最大レベルの位置に、印をしばらく残すかどうか。</summary>
        public bool PeakHold { get; set; } = true;

        /// <summary>ピークの印を残す時間（秒）。過ぎたら、メーターと同じ速さで下がる。</summary>
        public double PeakHoldSeconds { get; set; } = 1.5;

        public LevelMeterSettings Clone()
        {
            return (LevelMeterSettings)MemberwiseClone();
        }

        /// <summary>範囲外の値を範囲内に丸める（設定ファイルが壊れていた場合に備える）。</summary>
        public void Normalize()
        {
            FramesPerSecond = Math.Max(MinFramesPerSecond, Math.Min(MaxFramesPerSecond, FramesPerSecond));
            if (!Enum.IsDefined(typeof(LevelMeterStyle), Style))
            {
                Style = LevelMeterStyle.Solid;
            }
            FallSeconds = Clamp(FallSeconds, MinFallSeconds, MaxFallSeconds, 0.8);
            PeakHoldSeconds = Clamp(PeakHoldSeconds, MinPeakHoldSeconds, MaxPeakHoldSeconds, 1.5);
        }

        private static double Clamp(double value, double min, double max, double defaultValue)
        {
            return double.IsNaN(value) ? defaultValue : Math.Max(min, Math.Min(max, value));
        }
    }
}
