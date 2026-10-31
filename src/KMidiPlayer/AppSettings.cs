using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using KMidiPlayer.Controls;
using KMidiPlayer.Midi;
using KMidiPlayer.Playlist;

namespace KMidiPlayer
{
    /// <summary>
    /// アプリの設定。XML ファイルに保存する。
    /// </summary>
    /// <remarks>
    /// 保存先は exe と同じフォルダ。そこに書き込めない場合は %APPDATA%\KMIDI Player。
    /// 項目が欠けていたり値が壊れていたりしても、その項目だけ初期値にして読み込む。
    /// </remarks>
    public sealed class AppSettings
    {
        public const string SettingsFileName = "KMidiPlayer.settings.xml";
        public const string PlaylistFileName = "KMidiPlayer.playlist.m3u8";

        private const string AppDataFolderName = "KMIDI Player";

        private static string _dataDirectory;

        /// <summary>出力デバイスの名前。未設定なら null。</summary>
        public string OutputDeviceName { get; set; }

        public MidiResetType ResetType { get; set; } = MidiResetType.GS;

        public PlayMode PlayMode { get; set; } = PlayMode.All;

        /// <summary>曲を切り替えたときに、テンポとキーを初期値に戻すかどうか。</summary>
        public bool ResetTempoAndKeyPerSong { get; set; } = true;

        /// <summary>通常表示（最大化していないとき）のウィンドウの位置と大きさ。未設定なら空。</summary>
        public Rectangle WindowBounds { get; set; }

        public bool WindowMaximized { get; set; }

        /// <summary>チャンネルパネルの高さ。未設定なら 0。</summary>
        public int ChannelPanelHeight { get; set; }

        public bool ChannelPanelVisible { get; set; } = true;

        /// <summary>チャンネルパネルに、曲の中でノートを鳴らすチャンネルだけを表示するかどうか。</summary>
        public bool ShowUsedChannelsOnly { get; set; } = true;

        /// <summary>終了時に再生していた曲のファイルパス。無ければ null。</summary>
        public string LastSongPath { get; set; }

        /// <summary>終了時に再生していた曲の、プレイリスト上の位置（0 から）。無ければ -1。</summary>
        public int LastSongIndex { get; set; } = -1;

        /// <summary>プレイリストの列幅。未設定なら null。</summary>
        public int[] ColumnWidths { get; set; }

        /// <summary>今のプレイリストを開いた（保存した）プレイリストファイルのパス。無ければ null。</summary>
        public string PlaylistPath { get; set; }

        /// <summary>お気に入りの評価のフィルター（0 = すべて表示、1～5 = その評価以上だけ表示）。</summary>
        public int RatingFilter { get; set; }

        /// <summary>ベロシティカーブ（-12～+12、0 で曲のまま）。</summary>
        public int VelocityCurve { get; set; }

        /// <summary>レベルメーターの表示設定。</summary>
        public LevelMeterSettings LevelMeter { get; set; } = new LevelMeterSettings();

        /// <summary>画面の表示言語。</summary>
        public UiLanguage Language { get; set; } = UiLanguage.Japanese;

        /// <summary>画面の配色（ライト / ダーク）。</summary>
        public ThemeKind ColorTheme { get; set; } = ThemeKind.Light;

        /// <summary>曲の先頭から再生するときに、最初のノートまでの無音をとばすかどうか。</summary>
        public bool SkipLeadingSilence { get; set; } = true;

        /// <summary>ウィンドウを常に手前に表示するかどうか。</summary>
        public bool TopMost { get; set; }

        /// <summary>ピアノロールのウィンドウを開いているかどうか。</summary>
        public bool PianoRollVisible { get; set; }

        /// <summary>ピアノロールのウィンドウの位置と大きさ。未設定なら空。</summary>
        public Rectangle PianoRollBounds { get; set; }

        /// <summary>ピアノロールの時間方向の拡大率（1 秒あたりの px）。</summary>
        public double PianoRollPixelsPerSecond { get; set; } = PianoRollView.DefaultPixelsPerSecond;

        /// <summary>ピアノロールの表示の設定（表示の種類、色分け、音名など）。</summary>
        public PianoRollOptions PianoRollOptions { get; set; } = new PianoRollOptions();

        /// <summary>曲データから音源に送らない CC の番号（0～127）。</summary>
        public int[] BlockedControllers { get; set; } = new int[0];

        /// <summary>設定ファイルとプレイリストの自動保存ファイルを置くフォルダ。</summary>
        public static string DataDirectory => _dataDirectory ?? (_dataDirectory = FindDataDirectory());

        public static string SettingsFilePath => Path.Combine(DataDirectory, SettingsFileName);

        public static string PlaylistFilePath => Path.Combine(DataDirectory, PlaylistFileName);

        /// <summary>
        /// 設定を読み込む。ファイルが無い、または読めない場合は初期値を返す。
        /// </summary>
        public static AppSettings Load()
        {
            var settings = new AppSettings();
            XElement root;
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    return settings;
                }
                root = XDocument.Load(SettingsFilePath).Root;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is XmlException)
            {
                return settings;
            }
            if (root == null)
            {
                return settings;
            }

            settings.OutputDeviceName = (string)root.Element("OutputDevice");
            settings.Language = ReadEnum(root, "Language", settings.Language);
            settings.ColorTheme = ReadEnum(root, "Theme", settings.ColorTheme);
            settings.SkipLeadingSilence = ReadBool(root, "SkipLeadingSilence", settings.SkipLeadingSilence);
            settings.TopMost = ReadBool(root, "TopMost", settings.TopMost);

            XElement pianoRoll = root.Element("PianoRoll");
            if (pianoRoll != null)
            {
                settings.PianoRollVisible = ReadBool(pianoRoll, "Visible", false);
                settings.PianoRollBounds = new Rectangle(
                    ReadInt(pianoRoll, "X", 0), ReadInt(pianoRoll, "Y", 0),
                    ReadInt(pianoRoll, "Width", 0), ReadInt(pianoRoll, "Height", 0));
                settings.PianoRollPixelsPerSecond = ReadDouble(pianoRoll, "PixelsPerSecond", settings.PianoRollPixelsPerSecond);
                PianoRollOptions defaults = settings.PianoRollOptions;
                settings.PianoRollOptions = new PianoRollOptions
                {
                    Style = ReadEnum(pianoRoll, "Style", defaults.Style),
                    ColorMode = ReadEnum(pianoRoll, "ColorMode", defaults.ColorMode),
                    NoteNames = ReadEnum(pianoRoll, "NoteNames", defaults.NoteNames),
                    // 以前はオン / オフ（HitEffects）だけだった。オンは強さ 1 にあたる
                    EffectLevel = Math.Max(0, Math.Min(PianoRollOptions.MaxEffectLevel, ReadInt(pianoRoll, "EffectLevel",
                        pianoRoll.Element("HitEffects") == null ? defaults.EffectLevel : ReadBool(pianoRoll, "HitEffects", true) ? 1 : 0))),
                    FullKeyboard = ReadBool(pianoRoll, "FullKeyboard", defaults.FullKeyboard),
                    ShowOverview = ReadBool(pianoRoll, "Overview", defaults.ShowOverview),
                    ShowLyrics = ReadBool(pianoRoll, "Lyrics", defaults.ShowLyrics),
                    UseGpu = ReadBool(pianoRoll, "Gpu", defaults.UseGpu),
                };
            }
            settings.BlockedControllers = ((string)root.Element("BlockedControllers") ?? string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(text => int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int controller) ? controller : -1)
                .Where(controller => controller >= 0 && controller < 128)
                .Distinct()
                .ToArray();
            settings.ResetType = ReadEnum(root, "ResetType", settings.ResetType);
            settings.PlayMode = ReadEnum(root, "PlayMode", settings.PlayMode);
            settings.ResetTempoAndKeyPerSong = ReadBool(root, "ResetTempoAndKeyPerSong", settings.ResetTempoAndKeyPerSong);

            XElement window = root.Element("Window");
            if (window != null)
            {
                settings.WindowBounds = new Rectangle(
                    ReadInt(window, "X", 0), ReadInt(window, "Y", 0),
                    ReadInt(window, "Width", 0), ReadInt(window, "Height", 0));
                settings.WindowMaximized = ReadBool(window, "Maximized", false);
            }

            settings.ChannelPanelHeight = ReadInt(root, "ChannelPanelHeight", 0);
            settings.ChannelPanelVisible = ReadBool(root, "ChannelPanelVisible", settings.ChannelPanelVisible);
            settings.ShowUsedChannelsOnly = ReadBool(root, "ShowUsedChannelsOnly", settings.ShowUsedChannelsOnly);

            XElement lastSong = root.Element("LastSong");
            if (lastSong != null)
            {
                string lastSongPath = (string)lastSong.Element("Path");
                settings.LastSongPath = string.IsNullOrEmpty(lastSongPath) ? null : lastSongPath;
                settings.LastSongIndex = ReadInt(lastSong, "Index", -1);
            }

            string playlistPath = (string)root.Element("PlaylistPath");
            settings.PlaylistPath = string.IsNullOrEmpty(playlistPath) ? null : playlistPath;
            settings.RatingFilter = Math.Max(0, Math.Min(PlaylistItem.MaxRating, ReadInt(root, "RatingFilter", 0)));
            int velocityCurve = ReadInt(root, "VelocityCurve", 0);
            if (root.Element("VelocityCurve")?.Attribute("Steps") == null)
            {
                velocityCurve *= 2; // 以前の -6～+6 の形式。今の 2 つ分が 1 だったので、同じカーブになるよう 2 倍にする
            }
            settings.VelocityCurve = Math.Max(MidiSequencer.MinVelocityCurve, Math.Min(MidiSequencer.MaxVelocityCurve, velocityCurve));

            XElement meter = root.Element("LevelMeter");
            if (meter != null)
            {
                LevelMeterSettings defaults = settings.LevelMeter;
                settings.LevelMeter = new LevelMeterSettings
                {
                    FramesPerSecond = ReadInt(meter, "Fps", defaults.FramesPerSecond),
                    Style = ReadEnum(meter, "Style", defaults.Style),
                    FallSeconds = ReadDouble(meter, "FallSeconds", defaults.FallSeconds),
                    PeakHold = ReadBool(meter, "PeakHold", defaults.PeakHold),
                    PeakHoldSeconds = ReadDouble(meter, "PeakHoldSeconds", defaults.PeakHoldSeconds),
                };
                settings.LevelMeter.Normalize();
            }

            // ピアノロールの fps は、以前はレベルメーターの fps で描き直していたので、保存されていなければそれを引き継ぐ
            PianoRollOptions pianoRollOptions = settings.PianoRollOptions;
            pianoRollOptions.FramesPerSecond = pianoRoll?.Element("FramesPerSecond") != null
                ? ReadInt(pianoRoll, "FramesPerSecond", pianoRollOptions.FramesPerSecond)
                : settings.LevelMeter.FramesPerSecond;
            pianoRollOptions.FramesPerSecond = Math.Max(PianoRollOptions.MinFramesPerSecond, Math.Min(PianoRollOptions.MaxFramesPerSecond, pianoRollOptions.FramesPerSecond));

            string columns = (string)root.Element("ColumnWidths");
            if (!string.IsNullOrEmpty(columns))
            {
                int[] widths = columns.Split(',')
                    .Select(text => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int width) ? width : -1)
                    .ToArray();
                if (widths.All(width => width > 0))
                {
                    settings.ColumnWidths = widths;
                }
            }
            return settings;
        }

        /// <summary>
        /// 設定を保存する。保存できなかった場合は何もしない（設定の保存失敗でアプリを止めないため）。
        /// </summary>
        public void Save()
        {
            var root = new XElement("KMidiPlayerSettings",
                new XElement("Language", Language),
                new XElement("Theme", ColorTheme),
                new XElement("SkipLeadingSilence", SkipLeadingSilence),
                new XElement("TopMost", TopMost),
                new XElement("PianoRoll",
                    new XElement("Visible", PianoRollVisible),
                    new XElement("X", PianoRollBounds.X),
                    new XElement("Y", PianoRollBounds.Y),
                    new XElement("Width", PianoRollBounds.Width),
                    new XElement("Height", PianoRollBounds.Height),
                    new XElement("PixelsPerSecond", PianoRollPixelsPerSecond),
                    new XElement("Style", PianoRollOptions.Style),
                    new XElement("ColorMode", PianoRollOptions.ColorMode),
                    new XElement("NoteNames", PianoRollOptions.NoteNames),
                    new XElement("EffectLevel", PianoRollOptions.EffectLevel),
                    new XElement("FramesPerSecond", PianoRollOptions.FramesPerSecond),
                    new XElement("FullKeyboard", PianoRollOptions.FullKeyboard),
                    new XElement("Overview", PianoRollOptions.ShowOverview),
                    new XElement("Lyrics", PianoRollOptions.ShowLyrics),
                    new XElement("Gpu", PianoRollOptions.UseGpu)),
                new XElement("BlockedControllers", string.Join(",", BlockedControllers ?? new int[0])),
                new XElement("OutputDevice", OutputDeviceName ?? string.Empty),
                new XElement("ResetType", ResetType),
                new XElement("PlayMode", PlayMode),
                new XElement("ResetTempoAndKeyPerSong", ResetTempoAndKeyPerSong),
                new XElement("Window",
                    new XElement("X", WindowBounds.X),
                    new XElement("Y", WindowBounds.Y),
                    new XElement("Width", WindowBounds.Width),
                    new XElement("Height", WindowBounds.Height),
                    new XElement("Maximized", WindowMaximized)),
                new XElement("ChannelPanelHeight", ChannelPanelHeight),
                new XElement("ChannelPanelVisible", ChannelPanelVisible),
                new XElement("ShowUsedChannelsOnly", ShowUsedChannelsOnly),
                new XElement("ColumnWidths", ColumnWidths == null ? string.Empty : string.Join(",", ColumnWidths)),
                new XElement("LastSong",
                    new XElement("Path", LastSongPath ?? string.Empty),
                    new XElement("Index", LastSongIndex)),
                new XElement("PlaylistPath", PlaylistPath ?? string.Empty),
                new XElement("RatingFilter", RatingFilter),
                new XElement("VelocityCurve", new XAttribute("Steps", MidiSequencer.MaxVelocityCurve), VelocityCurve),
                new XElement("LevelMeter",
                    new XElement("Fps", LevelMeter.FramesPerSecond),
                    new XElement("Style", LevelMeter.Style),
                    new XElement("FallSeconds", LevelMeter.FallSeconds),
                    new XElement("PeakHold", LevelMeter.PeakHold),
                    new XElement("PeakHoldSeconds", LevelMeter.PeakHoldSeconds)));

            try
            {
                new XDocument(root).Save(SettingsFilePath);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // 保存できなくても、次回は初期値で起動するだけなので無視する
            }
        }

        private static string FindDataDirectory()
        {
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (File.Exists(Path.Combine(exeDirectory, SettingsFileName)) || CanWrite(exeDirectory))
            {
                return exeDirectory;
            }

            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppDataFolderName);
            try
            {
                Directory.CreateDirectory(appData);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // 作れなければ、保存時に失敗するだけ
            }
            return appData;
        }

        /// <summary>フォルダにファイルを作れるかどうかを、実際に一時ファイルを作って確かめる。</summary>
        private static bool CanWrite(string directory)
        {
            string testFile = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (new FileStream(testFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose))
                {
                }
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static T ReadEnum<T>(XElement parent, string name, T defaultValue) where T : struct
        {
            string text = (string)parent.Element(name);
            return Enum.TryParse(text, out T value) && Enum.IsDefined(typeof(T), value) ? value : defaultValue;
        }

        private static bool ReadBool(XElement parent, string name, bool defaultValue)
        {
            return bool.TryParse((string)parent.Element(name), out bool value) ? value : defaultValue;
        }

        private static int ReadInt(XElement parent, string name, int defaultValue)
        {
            return int.TryParse((string)parent.Element(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : defaultValue;
        }

        private static double ReadDouble(XElement parent, string name, double defaultValue)
        {
            return double.TryParse((string)parent.Element(name), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : defaultValue;
        }
    }
}
