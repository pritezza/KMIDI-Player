using System.Drawing;

namespace KMidiPlayer.Controls
{
    /// <summary>画面の配色の種類。</summary>
    public enum ThemeKind
    {
        /// <summary>明るい配色（Windows 11 の標準アプリに近い）。</summary>
        Light,

        /// <summary>暗い配色（ピアノロールの暗い背景に合わせる）。</summary>
        Dark,
    }

    /// <summary>
    /// 画面の配色（フラットデザイン）。各コントロールは <see cref="Current"/> の色で描く。
    /// </summary>
    /// <remarks>
    /// 配色を変えるときは <see cref="ThemeStyler.SetTheme"/> を使う（開いている画面にも反映する）。
    /// ミュート・ソロ・ペダルのランプなど、意味を表す色は配色によらず同じにする。
    /// </remarks>
    public sealed class Theme
    {
        public static readonly Theme Light = new Theme
        {
            Kind = ThemeKind.Light,
            Background = Rgb(0xF3F3F3),
            Surface = Rgb(0xFFFFFF),
            ControlFill = Rgb(0xFDFDFD),
            ControlHover = Rgb(0xF5F5F5),
            Text = Rgb(0x1B1B1B),
            MutedText = Rgb(0x5F5F5F),
            DisabledText = Rgb(0xA0A0A0),
            Border = Rgb(0xD1D1D1),
            Divider = Rgb(0xE5E5E5),
            Accent = Rgb(0x005FB8),
            AccentHover = Rgb(0x196EBF),
            AccentPressed = Rgb(0x31598F),
            AccentText = Rgb(0xFFFFFF),
            Hover = Rgb(0xE6E6E6),
            Pressed = Rgb(0xDADADA),
            Selection = Rgb(0xDCDCDC),
            SelectionInactive = Rgb(0xEBEBEB),
            CurrentItemBack = Rgb(0xE5F0FB),
            CurrentItemText = Rgb(0x003E7A),
            StarFilled = Rgb(0xE3A400),
            StarEmpty = Rgb(0xD4D4D4),
            TrackBack = Rgb(0xC4C4C4),
            ThumbBack = Rgb(0xFFFFFF),
            KeyWhite = Rgb(0xFFFFFF),
            KeyBlack = Rgb(0x2B2B2B),
            KeyBorder = Rgb(0xC8C8C8),
            MeterBack = Rgb(0xF3F3F3),
            MeterOff = Rgb(0xE3E3E3),
            MeterBorder = Rgb(0xDADADA),
        };

        public static readonly Theme Dark = new Theme
        {
            Kind = ThemeKind.Dark,
            Background = Rgb(0x15171C),
            Surface = Rgb(0x1B1E24),
            ControlFill = Rgb(0x1F232B),
            ControlHover = Rgb(0x262A33),
            Text = Rgb(0xE8EAED),
            MutedText = Rgb(0xA0A6B0),
            DisabledText = Rgb(0x5C626C),
            Border = Rgb(0x3A3F4A),
            Divider = Rgb(0x262A33),
            Accent = Rgb(0x3CCF91),
            AccentHover = Rgb(0x52D7A0),
            AccentPressed = Rgb(0x2FB57D),
            AccentText = Rgb(0x0D1A14),
            Hover = Rgb(0x262A33),
            Pressed = Rgb(0x2E333D),
            Selection = Rgb(0x343A45),
            SelectionInactive = Rgb(0x2A2E37),
            CurrentItemBack = Rgb(0x1D3A2D),
            CurrentItemText = Rgb(0x7EE2B4),
            StarFilled = Rgb(0xF2B84B),
            StarEmpty = Rgb(0x464C57),
            TrackBack = Rgb(0x3A3F4A),
            ThumbBack = Rgb(0xE8EAED),
            KeyWhite = Rgb(0x4A505C),
            KeyBlack = Rgb(0x111317),
            KeyBorder = Rgb(0x2A2E36),
            MeterBack = Rgb(0x101216),
            MeterOff = Rgb(0x262A33),
            MeterBorder = Rgb(0x2E333D),
        };

        private Theme()
        {
        }

        /// <summary>今の配色。</summary>
        public static Theme Current { get; internal set; } = Light;

        public static Theme FromKind(ThemeKind kind) => kind == ThemeKind.Dark ? Dark : Light;

        public ThemeKind Kind { get; private set; }

        public bool IsDark => Kind == ThemeKind.Dark;

        /// <summary>ウィンドウ・パネルの背景。</summary>
        public Color Background { get; private set; }

        /// <summary>リスト・チャンネルパネル・メニューの背景（背景より一段明るい面）。</summary>
        public Color Surface { get; private set; }

        /// <summary>ボタン・入力欄の地の色。</summary>
        public Color ControlFill { get; private set; }

        /// <summary>マウスを乗せたボタンの地の色。</summary>
        public Color ControlHover { get; private set; }

        public Color Text { get; private set; }

        /// <summary>見出し・補足など、控えめな文字。</summary>
        public Color MutedText { get; private set; }

        public Color DisabledText { get; private set; }

        /// <summary>ボタン・入力欄の枠。</summary>
        public Color Border { get; private set; }

        /// <summary>区切り線。</summary>
        public Color Divider { get; private set; }

        /// <summary>強調の色（再生ボタン、シークバー、鳴っている鍵、チェックなど）。</summary>
        public Color Accent { get; private set; }

        public Color AccentHover { get; private set; }

        public Color AccentPressed { get; private set; }

        /// <summary>強調の色の上に書く文字。</summary>
        public Color AccentText { get; private set; }

        /// <summary>枠の無いボタン・メニュー項目に、マウスを乗せたときの地の色。</summary>
        public Color Hover { get; private set; }

        public Color Pressed { get; private set; }

        /// <summary>リストで選んでいる行（フォーカスがあるとき）。</summary>
        public Color Selection { get; private set; }

        /// <summary>リストで選んでいる行（フォーカスが無いとき）。</summary>
        public Color SelectionInactive { get; private set; }

        /// <summary>再生中の曲の行。</summary>
        public Color CurrentItemBack { get; private set; }

        public Color CurrentItemText { get; private set; }

        /// <summary>お気に入りの評価の星（付いている星）。</summary>
        public Color StarFilled { get; private set; }

        /// <summary>お気に入りの評価の星（付いていない星）。</summary>
        public Color StarEmpty { get; private set; }

        /// <summary>シークバーの溝。</summary>
        public Color TrackBack { get; private set; }

        /// <summary>シークバーのつまみの外側。</summary>
        public Color ThumbBack { get; private set; }

        public Color KeyWhite { get; private set; }

        public Color KeyBlack { get; private set; }

        /// <summary>白鍵の間の線と、鍵盤の枠。</summary>
        public Color KeyBorder { get; private set; }

        /// <summary>レベルメーター・ペダルのランプ・ピッチベンドのバーの地の色。</summary>
        public Color MeterBack { get; private set; }

        /// <summary>レベルメーターの消えているセグメント。</summary>
        public Color MeterOff { get; private set; }

        /// <summary>レベルメーター・ペダルのランプ・ピッチベンドのバーの枠。</summary>
        public Color MeterBorder { get; private set; }

        private static Color Rgb(int rgb)
        {
            return Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
        }
    }
}
