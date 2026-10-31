using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using KMidiPlayer.Controls;
using KMidiPlayer.Midi;
using KMidiPlayer.Playlist;

namespace KMidiPlayer
{
    public partial class MainForm : Form
    {
        private const string AppName = "KMIDI Player";

        /// <summary>プレイリスト内での並べ替えドラッグを表すデータ形式名。</summary>
        private const string PlaylistDragFormat = "KMidiPlayer.PlaylistDrag";

        /// <summary>
        /// コピーした曲を、評価と一緒にクリップボードに入れるデータ形式名（このアプリと、ほかに起動した KMIDI Player の貼り付け用）。
        /// 中身は「評価 TAB ファイルのパス」の行を並べた UTF-8 の文字列。
        /// </summary>
        private const string PlaylistClipboardFormat = "KMidiPlayer.PlaylistItems";

        /// <summary>エクスプローラーに、貼り付けをコピーと移動のどちらで行うかを伝えるデータ形式名。</summary>
        private const string PreferredDropEffectFormat = "Preferred DropEffect";

        /// <summary>ファイルを削除する確認のダイアログに、名前を並べるファイルの数の上限。</summary>
        private const int MaxListedFiles = 10;

        /// <summary>再生 / 一時停止ボタンの記号（Segoe MDL2 Assets フォント）。</summary>
        private const string PlayGlyph = "\uE768";
        private const string PauseGlyph = "\uE769";

        /// <summary>ドラッグ中、リストの上端・下端からこの距離に入ると自動でスクロールする。</summary>
        private const int DragScrollMargin = 16;

        /// <summary>「前の曲」で戻れる曲の数。</summary>
        private const int MaxHistoryCount = 1000;

        /// <summary>「前の曲」を押したとき、再生位置がこれより後なら曲の先頭に戻る。</summary>
        private static readonly TimeSpan RestartThreshold = TimeSpan.FromSeconds(3);

        /// <summary>← → キー（「5 秒戻る / 進む」）で移動する時間。</summary>
        private static readonly TimeSpan SeekStep = TimeSpan.FromSeconds(5);

        private static readonly string[] MidiExtensions = { ".mid", ".midi" };

        private readonly MidiSequencer _sequencer = new MidiSequencer();
        private readonly AppSettings _settings = AppSettings.Load();
        private readonly Font _currentItemFont;
        private readonly Random _random = new Random();

        /// <summary>再生した曲の履歴（末尾が現在の曲）。シャッフル中の「前の曲」で使う。</summary>
        private readonly List<PlaylistItem> _history = new List<PlaylistItem>();

        /// <summary>シャッフルの今の一巡で、すでに再生した曲。</summary>
        private readonly HashSet<PlaylistItem> _shufflePlayed = new HashSet<PlaylistItem>();

        /// <summary>ドラッグ＆ドロップの挿入位置を示す線。</summary>
        private readonly Panel _insertionLine = new Panel
        {
            Height = 2,
            Visible = false,
        };

        /// <summary>
        /// プレイリストの全曲（並び順どおり）。画面のリスト（playlistView）には、このうち評価のフィルターを通った曲だけを表示する。
        /// </summary>
        private readonly List<ListViewItem> _allItems = new List<ListViewItem>();

        /// <summary>プレイリストファイルを開いた（保存した）ときの、曲ごとの評価。評価が変わったかどうかの判定に使う。</summary>
        private readonly Dictionary<PlaylistItem, int> _savedRatings = new Dictionary<PlaylistItem, int>();

        private MidiOutDevice _output;
        private PlaylistItem _currentItem;

        /// <summary>今のプレイリストを開いた（保存した）プレイリストファイル。無ければ null。</summary>
        private string _playlistPath;

        /// <summary>表示言語を変えて、コンボボックスの項目を置き換えている最中か（その間の選択の変化は無視する）。</summary>
        private bool _replacingComboItems;

        // A-B 区間ループの A 地点・B 地点（未設定なら null）
        private TimeSpan? _loopA;
        private TimeSpan? _loopB;

        /// <summary>起動時に渡されたファイル（MIDI ファイル・フォルダ・プレイリストファイル）。</summary>
        private readonly string[] _startupPaths;

        /// <summary>ピアノロールのウィンドウ（初めて開くまでは null）。</summary>
        private PianoRollForm _pianoRoll;

        /// <summary>アプリを終了している最中か。</summary>
        private bool _closing;

        /// <summary>デザイナー用。</summary>
        public MainForm()
            : this(null)
        {
        }

        /// <param name="startupPaths">起動時に開くファイル。無ければ null。</param>
        public MainForm(string[] startupPaths)
        {
            // 配色は、コントロールを作る前に決める（自分で描くコントロールは、作るときに今の配色を使う）
            ThemeStyler.SetTheme(_settings.ColorTheme);
            InitializeComponent();
            using (var iconStream = typeof(MainForm).Assembly.GetManifestResourceStream("KMidiPlayer.ico"))
                Icon = new Icon(iconStream);
            _startupPaths = startupPaths ?? new string[0];
            _currentItemFont = new Font(playlistView.Font, FontStyle.Bold);
            playlistView.Controls.Add(_insertionLine);
            _sequencer.SongEnded += Sequencer_SongEnded;

            // 画面の文字列は、デザイナーの日本語を、設定の言語のものに置き換える
            Texts.Current = _settings.Language;
            ApplyTexts();

            foreach (Label label in new[] { tempoLabel, tempoUnitLabel, keyLabel, modeLabel, ratingFilterLabel, velocityCurveLabel, resetLabel })
            {
                ThemeStyler.SetMuted(label);
            }
            ApplyTheme();

            resetComboBox.SelectedIndex = (int)_settings.ResetType;
            modeComboBox.SelectedIndex = (int)_settings.PlayMode;
            resetTempoKeyPerSongMenuItem.Checked = _settings.ResetTempoAndKeyPerSong;
            skipSilenceMenuItem.Checked = _settings.SkipLeadingSilence;
            topMostMenuItem.Checked = _settings.TopMost;
            ratingFilterBox.Value = _settings.RatingFilter;
            velocityCurveBox.Value = _settings.VelocityCurve;
            foreach (int controller in _settings.BlockedControllers)
            {
                _sequencer.SetControllerBlocked(controller, true);
            }
            UpdateVelocityCurveDisplay();
            ApplyMeterSettings(_settings.LevelMeter);
            ratingColumn.Width = playlistView.RatingColumnWidth;
            RestoreWindowBounds();
            RestoreColumnWidths();
            UpdateLoopDisplay();
        }

        /// <summary>
        /// 画面の文字列を、表示言語（<see cref="Texts.Current"/>）に合わせて設定し直す。起動時と、環境設定で言語を変えたときに呼ぶ。
        /// </summary>
        private void ApplyTexts()
        {
            fileMenuItem.Text = Texts.FileMenu;
            addFilesMenuItem.Text = Texts.AddFiles;
            addFolderMenuItem.Text = Texts.AddFolder;
            openPlaylistMenuItem.Text = Texts.OpenPlaylist;
            savePlaylistMenuItem.Text = Texts.SavePlaylist;
            savePlaylistAsMenuItem.Text = Texts.SavePlaylistAs;
            removeSelectedMenuItem.Text = Texts.RemoveSelected;
            clearPlaylistMenuItem.Text = Texts.ClearPlaylist;
            exitMenuItem.Text = Texts.Exit;

            playMenuItem.Text = Texts.PlayMenu;
            playPauseMenuItem.Text = Texts.PlayPause;
            stopMenuItem.Text = Texts.Stop;
            seekBackMenuItem.Text = Texts.SeekBack;
            seekForwardMenuItem.Text = Texts.SeekForward;
            previousMenuItem.Text = Texts.Previous;
            nextMenuItem.Text = Texts.Next;
            resetTempoKeyMenuItem.Text = Texts.ResetTempoKey;
            sendResetMenuItem.Text = Texts.SendReset;

            viewMenuItem.Text = Texts.ViewMenu;
            channelPanelMenuItem.Text = Texts.ChannelPanel;
            usedChannelsOnlyMenuItem.Text = Texts.UsedChannelsOnly;

            settingsMenuItem.Text = Texts.SettingsMenu;
            outputDeviceMenuItem.Text = Texts.OutputDevice;
            noDeviceMenuItem.Text = Texts.NoDevices;
            resetTempoKeyPerSongMenuItem.Text = Texts.ResetTempoKeyPerSong;
            skipSilenceMenuItem.Text = Texts.SkipLeadingSilence;
            topMostMenuItem.Text = Texts.AlwaysOnTop;
            pianoRollMenuItem.Text = Texts.PianoRollMenu;
            toolTip.SetToolTip(pianoRollButton, Texts.PianoRollButtonTip);
            _pianoRoll?.ApplyTexts();
            levelMeterSettingsMenuItem.Text = Texts.LevelMeterSettings;
            preferencesMenuItem.Text = Texts.Preferences;

            helpMenuItem.Text = Texts.HelpMenu;
            aboutMenuItem.Text = Texts.About;

            tempoLabel.Text = Texts.TempoLabel;
            keyLabel.Text = Texts.KeyLabel;
            modeLabel.Text = Texts.ModeLabel;
            resetLabel.Text = Texts.ResetLabel;
            ratingFilterLabel.Text = Texts.RatingFilterLabel;
            velocityCurveLabel.Text = Texts.VelocityCurveLabel;
            sendResetButton.Text = Texts.SendResetButton;
            _replacingComboItems = true;
            try
            {
                ReplaceItems(modeComboBox, Texts.PlayModes);
                ReplaceItems(resetComboBox, Texts.ResetTypes);
            }
            finally
            {
                _replacingComboItems = false;
            }
            toolTip.SetToolTip(ratingFilterBox, Texts.RatingFilterTip);
            ratingFilterBox.Invalidate(); // 星の右の「すべて」「以上」を描き直す
            positionStatusLabel.ToolTipText = Texts.PositionDisplayTip;
            tempoStatusLabel.ToolTipText = Texts.TempoStatusTip;

            titleColumn.Text = Texts.TitleColumn;
            durationColumn.Text = Texts.DurationColumn;
            ratingColumn.Text = Texts.RatingColumn;
            fileNameColumn.Text = Texts.FileNameColumn;

            clearMuteSoloMenuItem.Text = Texts.ClearMuteSolo;
            resetOutputChannelsMenuItem.Text = Texts.ResetOutputChannels;
            resetChannelTransposeMenuItem.Text = Texts.ResetChannelTransposes;
            resetChannelVolumesMenuItem.Text = Texts.ResetChannelVolumes;
            channelView.UpdateTexts();

            copyMenuItem.Text = Texts.CopyItems;
            cutMenuItem.Text = Texts.CutItems;
            pasteMenuItem.Text = Texts.PasteItems;
            removeFromListMenuItem.Text = Texts.RemoveFromList;
            deleteFilesMenuItem.Text = Texts.DeleteFiles;

            // 状態によって変わる文字列
            UpdateLoopDisplay();
            toolTip.SetToolTip(velocityCurveBox, Texts.VelocityCurveTip(velocityCurveBox.Value));
            UpdateDeviceStatus();
            UpdateTitle();
            RefreshListItems();
        }

        /// <summary>コンボボックスの項目を、選択位置を変えずに置き換える。</summary>
        private static void ReplaceItems(ComboBox comboBox, string[] items)
        {
            int index = comboBox.SelectedIndex;
            comboBox.BeginUpdate();
            try
            {
                comboBox.Items.Clear();
                comboBox.Items.AddRange(items);
                comboBox.SelectedIndex = index < items.Length ? index : -1;
            }
            finally
            {
                comboBox.EndUpdate();
            }
        }

        private void preferencesMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new PreferencesForm(Texts.Current, Theme.Current.Kind, GetBlockedControllers()))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                if (dialog.Theme != Theme.Current.Kind)
                {
                    ThemeStyler.SetTheme(dialog.Theme); // 開いている画面（ピアノロールも）に反映する
                    ApplyTheme();
                }

                bool[] blocked = dialog.BlockedControllers;
                for (int controller = 0; controller < blocked.Length; controller++)
                {
                    _sequencer.SetControllerBlocked(controller, blocked[controller]);
                }
                if (dialog.Language != Texts.Current)
                {
                    Texts.Current = dialog.Language;
                    ApplyTexts();
                }
                channelView.UpdateFrom(_sequencer);
            }
        }

        /// <summary>
        /// 今の配色（<see cref="Theme.Current"/>）を、この画面に当てる。起動時と、環境設定で配色を変えたときに呼ぶ。
        /// </summary>
        private void ApplyTheme()
        {
            ThemeStyler.Apply(this);
            _insertionLine.BackColor = Theme.Current.Accent;
            RefreshListItems(); // 再生中の曲の行の色
        }

        /// <summary>送らない CC（添字が CC 番号）。</summary>
        private bool[] GetBlockedControllers()
        {
            var blocked = new bool[128];
            for (int controller = 0; controller < blocked.Length; controller++)
            {
                blocked[controller] = _sequencer.IsControllerBlocked(controller);
            }
            return blocked;
        }

        // ---- ペダルのランプの右クリック ----

        /// <summary>右クリックメニューを開いたときの、マウスの下のペダルの CC 番号（無ければ -1）。</summary>
        private int _menuPedalController = -1;

        private void channelContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // ペダルのランプの上で開いたときだけ、そのペダルを送らない設定の項目を出す
            _menuPedalController = channelView.GetPedalControllerAt(channelView.PointToClient(Cursor.Position));
            bool onPedal = _menuPedalController >= 0;
            blockPedalMenuItem.Visible = onPedal;
            blockPedalMenuSeparator.Visible = onPedal;
            if (onPedal)
            {
                blockPedalMenuItem.Text = Texts.BlockPedalMenu(_menuPedalController);
                blockPedalMenuItem.Checked = _sequencer.IsControllerBlocked(_menuPedalController);
            }
        }

        private void blockPedalMenuItem_Click(object sender, EventArgs e)
        {
            if (_menuPedalController >= 0)
            {
                TogglePedalBlocked(_menuPedalController);
            }
        }

        private void channelView_PedalClicked(object sender, PedalEventArgs e)
        {
            TogglePedalBlocked(e.Controller);
        }

        /// <summary>ペダルの CC を、音源に送る / 送らないで切り替える。</summary>
        private void TogglePedalBlocked(int controller)
        {
            _sequencer.SetControllerBlocked(controller, !_sequencer.IsControllerBlocked(controller));
            channelView.UpdateFrom(_sequencer);
        }

        private PlayMode CurrentPlayMode => (PlayMode)Math.Max(0, modeComboBox.SelectedIndex);

        /// <summary>表示する曲の評価の下限（0 ならすべて表示）。</summary>
        private int RatingFilter => ratingFilterBox.Value;

        // ---- フォーム ----

        private void MainForm_Load(object sender, EventArgs e)
        {
            RestoreChannelPanel();
            RestoreOutputDevice();
            RestorePlaylist();
            UpdateDeviceStatus();
            OpenStartupPaths();
            UpdatePlaybackDisplay();
            pianoRollMenuItem.Checked = _settings.PianoRollVisible;

            // プレイリストを操作の中心にする（選択中の曲が強調表示され、Enter キーで再生できる）
            ActiveControl = playlistView;
        }

        /// <summary>
        /// 起動時に渡されたファイルを開いて、最初の曲から再生する。
        /// プレイリストファイルが 1 つだけなら、「プレイリストを開く」と同じく今のリストを置き換える。
        /// それ以外（MIDI ファイル・フォルダ・複数のファイル）は、プレイリストの末尾に追加する。
        /// </summary>
        private void OpenStartupPaths()
        {
            // 「KMidiPlayer.exe song.mid」のような相対パスは、起動時のフォルダを元に絶対パスにする
            // （後でファイルダイアログなどでカレントフォルダが変わっても、同じファイルを指すように）
            string[] paths = _startupPaths
                .Select(ToFullPath)
                .Where(path => path != null && (File.Exists(path) || Directory.Exists(path)))
                .ToArray();
            if (paths.Length == 0)
            {
                return;
            }

            // 渡した曲が評価のフィルターで隠れないようにする
            ratingFilterBox.Value = 0;

            if (paths.Length == 1 && File.Exists(paths[0]) && PlaylistFile.IsPlaylistFile(paths[0]))
            {
                if (OpenPlaylistFile(paths[0]))
                {
                    PlayFrom(0, 1, false);
                }
                return;
            }

            List<PlaylistItem> items = ExpandPaths(paths);
            if (items.Count == 0)
            {
                return;
            }
            int firstAdded = _allItems.Count;
            AddFiles(items, playlistView.Items.Count);
            PlayFrom(_allItems[firstAdded].Index, 1, false);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!ConfirmSaveRatings())
            {
                e.Cancel = true;
                return;
            }
            _closing = true;

            positionTimer.Stop();
            channelTimer.Stop();
            SaveSettings();
            SavePlaylistAutomatically();

            _sequencer.Dispose();
            _output?.Dispose();
            _output = null;
        }

        /// <summary>
        /// キーボードショートカット（Space、← →、Ctrl+← →）を、どのコントロールに入力中でも先に受け取る。
        /// </summary>
        /// <remarks>
        /// テンポ・キーの数値を入力中は、矢印キーやスペースをその欄に任せる。
        /// コンボボックスを選んでいるときは、← → を項目の切り替えに任せる。
        /// ボタンにフォーカスがあっても、Space はそのボタンではなく再生 / 一時停止にする。
        /// </remarks>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Control focused = GetFocusedControl();
            bool isTextInput = focused is TextBoxBase;
            bool isComboBox = focused is ComboBox;

            // キーを押し続けたことによる繰り返し（lParam のビット 30 が「直前も押されていた」）
            bool isRepeat = (msg.LParam.ToInt64() & 0x40000000) != 0;

            switch (keyData)
            {
                case Keys.Space when !isTextInput:
                    if (!isRepeat)
                    {
                        TogglePlayPause();
                    }
                    return true;

                case Keys.Left when !isTextInput && !isComboBox:
                    SeekBy(-SeekStep);
                    return true;

                case Keys.Right when !isTextInput && !isComboBox:
                    SeekBy(SeekStep);
                    return true;

                case Keys.Control | Keys.Left when !isTextInput:
                    if (!isRepeat)
                    {
                        PlayPrevious();
                    }
                    return true;

                case Keys.Control | Keys.Right when !isTextInput:
                    if (!isRepeat)
                    {
                        PlayNext();
                    }
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// 実際にキー入力を受け取るコントロールを返す（SplitContainer や NumericUpDown の中まで辿る）。
        /// </summary>
        private Control GetFocusedControl()
        {
            Control control = ActiveControl;
            while (control is ContainerControl container && container.ActiveControl != null)
            {
                control = container.ActiveControl;
            }
            return control;
        }

        private void positionTimer_Tick(object sender, EventArgs e)
        {
            UpdatePlaybackDisplay();
        }

        /// <summary>チャンネルパネル（鍵盤・ペダル・レベルメーター）の更新。間隔はレベルメーターの設定の fps で決まる。</summary>
        private void channelTimer_Tick(object sender, EventArgs e)
        {
            if (!splitContainer.Panel2Collapsed)
            {
                channelView.UpdateFrom(_sequencer);
            }
        }

        // ---- レベルメーターの設定 ----

        private void levelMeterSettingsMenuItem_Click(object sender, EventArgs e)
        {
            LevelMeterSettings original = channelView.MeterSettings;
            using (var dialog = new LevelMeterSettingsForm(original, ApplyMeterSettings))
            {
                // 画面で変えている間はすぐに反映し、キャンセルされたら元に戻す
                ApplyMeterSettings(dialog.ShowDialog(this) == DialogResult.OK ? dialog.Settings : original);
            }
        }

        private void ApplyMeterSettings(LevelMeterSettings settings)
        {
            channelView.MeterSettings = settings;
            channelTimer.Interval = Math.Max(1, (int)Math.Round(1000.0 / channelView.MeterSettings.FramesPerSecond));
        }

        // ---- メニュー・ボタン ----

        private void addFilesMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = Texts.AddFilesTitle;
                dialog.Filter = Texts.MidiFileFilter;
                dialog.Multiselect = true;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    AddPaths(dialog.FileNames, playlistView.Items.Count);
                }
            }
        }

        private void addFolderMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = Texts.AddFolderDescription;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    AddPaths(new[] { dialog.SelectedPath }, playlistView.Items.Count);
                }
            }
        }

        private void openPlaylistMenuItem_Click(object sender, EventArgs e)
        {
            if (!ConfirmSaveRatings())
            {
                return;
            }

            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = Texts.OpenPlaylistTitle;
                dialog.Filter = Texts.OpenPlaylistFilter;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    OpenPlaylistFile(dialog.FileName);
                }
            }
        }

        /// <summary>
        /// プレイリストファイルを読み込み、今のリストを置き換える（評価を保存するかどうかは、呼び出す前に確かめること）。
        /// </summary>
        /// <returns>読み込めたかどうか。</returns>
        private bool OpenPlaylistFile(string path)
        {
            List<PlaylistItem> items;
            try
            {
                items = PlaylistFile.Load(path).Where(item => IsMidiFile(item.FilePath)).ToList();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show(this, Texts.OpenPlaylistFailed(ex.Message), AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            ClearPlaylist();
            AddFiles(items, 0);
            _playlistPath = path;
            MarkRatingsSaved();
            return true;
        }

        private void savePlaylistMenuItem_Click(object sender, EventArgs e)
        {
            SavePlaylist();
        }

        private void savePlaylistAsMenuItem_Click(object sender, EventArgs e)
        {
            SavePlaylistAs();
        }

        private void removeSelectedMenuItem_Click(object sender, EventArgs e)
        {
            ConfirmRemoveSelectedItems();
        }

        private void clearPlaylistMenuItem_Click(object sender, EventArgs e)
        {
            if (ConfirmSaveRatings())
            {
                ClearPlaylist();
                MarkRatingsSaved();
            }
        }

        // ---- プレイリストファイルの保存 ----

        /// <summary>
        /// 開いた M3U ファイルに上書き保存する。M3U ファイルから開いていなければ、名前を付けて保存する。
        /// </summary>
        /// <returns>保存したかどうか。</returns>
        private bool SavePlaylist()
        {
            return CanOverwritePlaylist ? SavePlaylistTo(_playlistPath) : SavePlaylistAs();
        }

        private bool SavePlaylistAs()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = Texts.SavePlaylistAsTitle;
                dialog.Filter = Texts.SavePlaylistFilter;
                dialog.DefaultExt = "m3u8";
                if (_playlistPath != null)
                {
                    // TO2 から開いた場合は、同じ名前の .m3u8 を勧める
                    dialog.InitialDirectory = Path.GetDirectoryName(_playlistPath);
                    dialog.FileName = Path.GetFileNameWithoutExtension(_playlistPath) + ".m3u8";
                }
                return dialog.ShowDialog(this) == DialogResult.OK && SavePlaylistTo(dialog.FileName);
            }
        }

        private bool SavePlaylistTo(string path)
        {
            try
            {
                M3uPlaylist.Save(path, GetPlaylistItems());
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show(this, Texts.SavePlaylistFailed(ex.Message), AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            _playlistPath = path;
            MarkRatingsSaved();
            return true;
        }

        /// <summary>開いたプレイリストファイルに上書き保存できるか（M3U ファイルから開いたか）。</summary>
        private bool CanOverwritePlaylist => _playlistPath != null && M3uPlaylist.IsPlaylistFile(_playlistPath);

        /// <summary>プレイリストファイルを開いた（保存した）ときから、評価が変わったかどうか。</summary>
        private bool RatingsModified => GetPlaylistItems().Any(item => item.Rating != (_savedRatings.TryGetValue(item, out int rating) ? rating : 0));

        /// <summary>今の評価を、プレイリストファイルと同じ状態として記録する。</summary>
        private void MarkRatingsSaved()
        {
            _savedRatings.Clear();
            foreach (PlaylistItem item in GetPlaylistItems())
            {
                _savedRatings[item] = item.Rating;
            }
            UpdateTitle();
        }

        /// <summary>
        /// 評価を変えたままプレイリストファイルに保存していなければ、保存するかどうかを尋ねる。
        /// </summary>
        /// <returns>続けてよければ true（保存した、または保存しないことを選んだ）。キャンセルされたら false。</returns>
        private bool ConfirmSaveRatings()
        {
            if (!RatingsModified)
            {
                return true;
            }

            string message = CanOverwritePlaylist
                ? Texts.RatingsChangedSaveTo(Path.GetFileName(_playlistPath))
                : Texts.RatingsChangedSaveNew;
            switch (MessageBox.Show(this, message, AppName, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
            {
                case DialogResult.Yes:
                    return SavePlaylist();
                case DialogResult.No:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// タイトルバーに、再生中の曲名、開いているプレイリストファイルの名前と、評価を保存していないことを示す「*」を出す。
        /// 例: 「♪ Field Map - 作曲家研究.m3u8 * - KMIDI Player」
        /// </summary>
        private void UpdateTitle()
        {
            var parts = new List<string>();
            if (_currentItem != null)
            {
                parts.Add("♪ " + _currentItem.DisplayTitle);
            }

            string name = _playlistPath != null ? Path.GetFileName(_playlistPath) : null;
            bool modified = RatingsModified;
            if (name != null || modified)
            {
                parts.Add((name ?? Texts.Untitled) + (modified ? " *" : string.Empty));
            }
            parts.Add(AppName);

            string title = string.Join(" - ", parts);
            if (Text != title)
            {
                Text = title; // 変わらないときは、タイトルバーを描き直させない
            }
        }

        private void exitMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void playPause_Click(object sender, EventArgs e)
        {
            TogglePlayPause();
        }

        private void stop_Click(object sender, EventArgs e)
        {
            _sequencer.Stop();
            UpdatePlaybackDisplay();
        }

        private void previous_Click(object sender, EventArgs e)
        {
            PlayPrevious();
        }

        private void next_Click(object sender, EventArgs e)
        {
            PlayNext();
        }

        private void seekBack_Click(object sender, EventArgs e)
        {
            SeekBy(-SeekStep);
        }

        private void seekForward_Click(object sender, EventArgs e)
        {
            SeekBy(SeekStep);
        }

        /// <summary>
        /// 再生位置を、今の位置から指定した時間だけ動かす。再生中なら、移動先から再生を続ける。
        /// </summary>
        private void SeekBy(TimeSpan offset)
        {
            if (_sequencer.Song == null)
            {
                return;
            }
            _sequencer.Seek(_sequencer.Position + offset);
            UpdatePlaybackDisplay();
        }

        private void outputDeviceMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            // メニューを開くたびにデバイスの一覧を取り直す（USB 機器の抜き差しに対応するため）
            outputDeviceMenuItem.DropDownItems.Clear();
            IReadOnlyList<string> names = MidiOutDevice.GetDeviceNames();
            if (names.Count == 0)
            {
                outputDeviceMenuItem.DropDownItems.Add(new ToolStripMenuItem(Texts.NoDevices) { Enabled = false });
                return;
            }

            for (int i = 0; i < names.Count; i++)
            {
                int deviceId = i;
                var item = new ToolStripMenuItem(names[i])
                {
                    Checked = _output != null && _output.DeviceId == deviceId,
                };
                item.Click += (s, args) => SelectOutputDevice(deviceId);
                outputDeviceMenuItem.DropDownItems.Add(item);
            }
        }

        private void aboutMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this, Texts.AboutText(Application.ProductVersion), Texts.AboutTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void seekBar_SeekRequested(object sender, EventArgs e)
        {
            _sequencer.Seek(TimeSpan.FromMilliseconds(seekBar.Value));
            UpdatePlaybackDisplay();
        }

        private void channelPanelMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            splitContainer.Panel2Collapsed = !channelPanelMenuItem.Checked;
        }

        private void usedChannelsOnlyMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            channelView.ShowUsedChannelsOnly = usedChannelsOnlyMenuItem.Checked;
            channelView.UpdateFrom(_sequencer);
        }

        private void topMostMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            TopMost = topMostMenuItem.Checked;
        }

        // ---- ピアノロール ----

        /// <summary>操作部のボタンでも、メニューと同じくピアノロールを開閉する（状態はメニューのチェックに一本化する）。</summary>
        private void pianoRollButton_Click(object sender, EventArgs e)
        {
            pianoRollMenuItem.Checked = !pianoRollMenuItem.Checked;
        }

        private void pianoRollMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            pianoRollButton.Checked = pianoRollMenuItem.Checked;
            if (pianoRollMenuItem.Checked)
            {
                ShowPianoRoll();
            }
            else
            {
                _pianoRoll?.Hide();
            }
        }

        /// <summary>ピアノロールのウィンドウを開く。初めて開くときは作り、前回の位置と大きさに置く。</summary>
        private void ShowPianoRoll()
        {
            if (_pianoRoll == null)
            {
                _pianoRoll = new PianoRollForm();
                _pianoRoll.PixelsPerSecond = _settings.PianoRollPixelsPerSecond;
                _pianoRoll.Options = _settings.PianoRollOptions;
                Rectangle bounds = _settings.PianoRollBounds;
                bool isUsable = bounds.Width >= _pianoRoll.MinimumSize.Width && bounds.Height >= _pianoRoll.MinimumSize.Height
                    && Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(new Rectangle(bounds.X, bounds.Y, bounds.Width, SystemInformation.CaptionHeight)));
                _pianoRoll.Bounds = isUsable ? bounds : new Rectangle(Left + 40, Top + 120, Math.Max(600, Width - 80), 360);
                _pianoRoll.SeekRequested += PianoRoll_SeekRequested;
                _pianoRoll.Attach(_sequencer, () => _currentItem?.DisplayTitle); // 描き直しは、ピアノロールの設定の fps で行う

                // 閉じるボタンで隠したときに、メニューのチェックを外す（アプリの終了で閉じるときは、開いていたことを残す）
                _pianoRoll.VisibleChanged += (sender, e) =>
                {
                    if (!_closing)
                    {
                        pianoRollMenuItem.Checked = _pianoRoll.Visible;
                    }
                };
            }
            if (!_pianoRoll.Visible)
            {
                _pianoRoll.Show(this); // メイン画面に付属させる（手前に出て、最小化も一緒になる）
            }
            _pianoRoll.UpdateFrom(_sequencer, _currentItem?.DisplayTitle);
        }

        private void PianoRoll_SeekRequested(object sender, SeekRequestedEventArgs e)
        {
            _sequencer.Seek(e.Position);
            UpdatePlaybackDisplay();
        }

        private void skipSilenceMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            _sequencer.SkipLeadingSilence = skipSilenceMenuItem.Checked;
        }

        // ---- テンポ・キー・再生モード ----

        private void tempoUpDown_ValueChanged(object sender, EventArgs e)
        {
            _sequencer.TempoPercent = (int)tempoUpDown.Value;
        }

        private void keyUpDown_ValueChanged(object sender, EventArgs e)
        {
            _sequencer.Transpose = (int)keyUpDown.Value;
        }

        private void resetTempoKeyMenuItem_Click(object sender, EventArgs e)
        {
            ResetTempoAndKey();
        }

        private void modeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_replacingComboItems)
            {
                return;
            }
            if (CurrentPlayMode == PlayMode.Shuffle)
            {
                // シャッフルを新しく始める（今の曲は再生済みとして扱う）
                _shufflePlayed.Clear();
                if (_currentItem != null)
                {
                    _shufflePlayed.Add(_currentItem);
                }
            }
        }

        private void velocityCurveBox_ValueChanged(object sender, EventArgs e)
        {
            UpdateVelocityCurveDisplay();
        }

        /// <summary>ベロシティカーブをシーケンサーに伝え、ツールチップの説明を今の値に合わせる。</summary>
        private void UpdateVelocityCurveDisplay()
        {
            int curve = velocityCurveBox.Value;
            _sequencer.VelocityCurve = curve;
            toolTip.SetToolTip(velocityCurveBox, Texts.VelocityCurveTip(curve));
        }

        private void ResetTempoAndKey()
        {
            tempoUpDown.Value = 100;
            keyUpDown.Value = 0;
        }

        // ---- A-B 区間ループ ----

        private void loopAButton_Click(object sender, EventArgs e)
        {
            if (_sequencer.Song == null)
            {
                return;
            }

            if (_loopA.HasValue)
            {
                // A を解除すると、区間ループ全体を解除する
                _loopA = null;
                _loopB = null;
                _sequencer.ClearLoop();
            }
            else
            {
                _loopA = _sequencer.Position;
            }
            UpdateLoopDisplay();
        }

        private void loopBButton_Click(object sender, EventArgs e)
        {
            if (_sequencer.Song == null)
            {
                return;
            }

            if (_loopB.HasValue)
            {
                _loopB = null;
                _sequencer.ClearLoop();
            }
            else
            {
                // A を設定していなければ、曲の先頭を A とする
                TimeSpan start = _loopA ?? TimeSpan.Zero;
                TimeSpan end = _sequencer.Position;
                if (end <= start)
                {
                    SystemSounds.Beep.Play();
                    return;
                }
                _loopA = start;
                _loopB = end;
                _sequencer.SetLoop(start, end);
            }
            UpdateLoopDisplay();
        }

        private void ClearLoop()
        {
            _loopA = null;
            _loopB = null;
            _sequencer.ClearLoop();
            UpdateLoopDisplay();
        }

        private void UpdateLoopDisplay()
        {
            loopAButton.Checked = _loopA.HasValue;
            loopBButton.Checked = _loopB.HasValue;
            seekBar.LoopStart = _loopA.HasValue ? (int)_loopA.Value.TotalMilliseconds : -1;
            seekBar.LoopEnd = _loopB.HasValue ? (int)_loopB.Value.TotalMilliseconds : -1;

            toolTip.SetToolTip(loopAButton, _loopA.HasValue ? Texts.LoopATip(FormatTime(_loopA.Value)) : Texts.LoopAEmptyTip);
            toolTip.SetToolTip(loopBButton, _loopB.HasValue ? Texts.LoopBTip(FormatTime(_loopB.Value)) : Texts.LoopBEmptyTip);
        }

        // ---- リセット ----

        private void resetComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_replacingComboItems)
            {
                return;
            }
            var resetType = (MidiResetType)resetComboBox.SelectedIndex;
            _sequencer.AutoReset = resetType;
            sendResetButton.Enabled = resetType != MidiResetType.None;
            sendResetMenuItem.Enabled = sendResetButton.Enabled;
        }

        private void sendReset_Click(object sender, EventArgs e)
        {
            if (EnsureOutputDevice())
            {
                _sequencer.SendReset((MidiResetType)resetComboBox.SelectedIndex);
            }
        }

        // ---- ミュート・ソロ ----

        private void channelView_MuteClicked(object sender, ChannelEventArgs e)
        {
            _sequencer.SetMuted(e.Channel, !_sequencer.IsMuted(e.Channel));
            channelView.UpdateFrom(_sequencer);
        }

        private void channelView_SoloClicked(object sender, ChannelEventArgs e)
        {
            _sequencer.SetSolo(e.Channel, !_sequencer.IsSolo(e.Channel));
            channelView.UpdateFrom(_sequencer);
        }

        private void clearMuteSoloMenuItem_Click(object sender, EventArgs e)
        {
            _sequencer.ClearMuteAndSolo();
            channelView.UpdateFrom(_sequencer);
        }

        // ---- 送信先のチャンネル ----

        private void channelView_OutputChannelChanged(object sender, OutputChannelEventArgs e)
        {
            _sequencer.SetOutputChannel(e.Channel, e.OutputChannel);
        }

        private void resetOutputChannelsMenuItem_Click(object sender, EventArgs e)
        {
            _sequencer.ResetOutputChannels();
            channelView.UpdateFrom(_sequencer);
        }

        // ---- チャンネルごとの移調・オクターブ ----

        private void channelView_TransposeChanged(object sender, ChannelValueEventArgs e)
        {
            _sequencer.SetChannelTranspose(e.Channel, e.Value);
        }

        private void channelView_OctaveChanged(object sender, ChannelValueEventArgs e)
        {
            _sequencer.SetChannelOctave(e.Channel, e.Value);
        }

        private void resetChannelTransposeMenuItem_Click(object sender, EventArgs e)
        {
            _sequencer.ResetChannelTransposes();
            channelView.UpdateFrom(_sequencer);
        }

        // ---- チャンネルごとのボリューム ----

        private void channelView_VolumeChanged(object sender, ChannelValueEventArgs e)
        {
            _sequencer.SetChannelVolume(e.Channel, e.Value);
        }

        private void resetChannelVolumesMenuItem_Click(object sender, EventArgs e)
        {
            _sequencer.ResetChannelVolumes();
            channelView.UpdateFrom(_sequencer);
        }

        // ---- 鍵盤のクリック ----

        private void channelView_NotePressed(object sender, KeyboardNoteEventArgs e)
        {
            _sequencer.SendManualNoteOn(e.Channel, e.Note, e.Velocity);
        }

        private void channelView_NoteReleased(object sender, KeyboardNoteEventArgs e)
        {
            _sequencer.SendManualNoteOff(e.Channel, e.Note);
        }

        // ---- プレイリスト ----

        private void playlistView_ItemActivate(object sender, EventArgs e)
        {
            // ダブルクリック、または Enter キー
            if (playlistView.SelectedIndices.Count > 0)
            {
                PlayFrom(playlistView.SelectedIndices[0], 1, false);
            }
        }

        private void playlistView_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyData)
            {
                case Keys.Control | Keys.A:
                    playlistView.BeginUpdate();
                    foreach (ListViewItem item in playlistView.Items)
                    {
                        item.Selected = true;
                    }
                    playlistView.EndUpdate();
                    e.Handled = true;
                    return;

                // 右クリックメニューの項目と同じ。メニューの ShortcutKeys にすると、
                // テンポ・キーの欄で Delete を押しても曲が削除されてしまうので、プレイリストを選んでいるときだけ受け付ける
                case Keys.Control | Keys.C:
                    CopySelectedItems();
                    e.Handled = e.SuppressKeyPress = true;
                    return;

                case Keys.Control | Keys.X:
                    CutSelectedItems();
                    e.Handled = e.SuppressKeyPress = true;
                    return;

                case Keys.Control | Keys.V:
                    PasteItems();
                    e.Handled = e.SuppressKeyPress = true;
                    return;

                case Keys.Delete:
                    ConfirmRemoveSelectedItems();
                    e.Handled = true;
                    return;

                case Keys.Shift | Keys.Delete:
                    DeleteSelectedFiles();
                    e.Handled = true;
                    return;
            }

            // 0～5 キーで、選択中の曲の評価を変える
            int rating = RatingFromKey(e.KeyCode);
            if (rating >= 0 && e.Modifiers == Keys.None && playlistView.SelectedItems.Count > 0)
            {
                SetRating(playlistView.SelectedItems.Cast<ListViewItem>().ToList(), rating);
                e.Handled = true;
                e.SuppressKeyPress = true; // 頭文字で曲を探す機能に渡さない
            }
        }

        private static int RatingFromKey(Keys key)
        {
            if (key >= Keys.D0 && key <= Keys.D0 + PlaylistItem.MaxRating)
            {
                return key - Keys.D0;
            }
            if (key >= Keys.NumPad0 && key <= Keys.NumPad0 + PlaylistItem.MaxRating)
            {
                return key - Keys.NumPad0;
            }
            return -1;
        }

        // ---- プレイリストの右クリックメニュー（コピー・カット・貼り付け・削除） ----

        private void playlistContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool selected = playlistView.SelectedItems.Count > 0;
            copyMenuItem.Enabled = selected;
            cutMenuItem.Enabled = selected;
            removeFromListMenuItem.Enabled = selected;
            deleteFilesMenuItem.Enabled = selected;
            pasteMenuItem.Enabled = CanPaste();
        }

        private void copyMenuItem_Click(object sender, EventArgs e)
        {
            CopySelectedItems();
        }

        private void cutMenuItem_Click(object sender, EventArgs e)
        {
            CutSelectedItems();
        }

        private void pasteMenuItem_Click(object sender, EventArgs e)
        {
            PasteItems();
        }

        private void deleteFilesMenuItem_Click(object sender, EventArgs e)
        {
            DeleteSelectedFiles();
        }

        /// <summary>選択中の曲（リストの並び順）。</summary>
        private List<PlaylistItem> GetSelectedPlaylistItems()
        {
            return playlistView.SelectedItems.Cast<ListViewItem>().Select(listItem => (PlaylistItem)listItem.Tag).ToList();
        }

        /// <summary>
        /// 選択中の曲をクリップボードに入れる。次の 3 通りで貼り付けられる。
        /// <list type="bullet">
        /// <item>このアプリ・ほかに起動した KMIDI Player のプレイリスト（評価も引き継ぐ）</item>
        /// <item>エクスプローラー（ファイルのコピー。見つからないファイルは入れない）</item>
        /// <item>メモ帳など（ファイルのパスの文字列）</item>
        /// </list>
        /// </summary>
        /// <returns>クリップボードに入れたかどうか。</returns>
        private bool CopySelectedItems()
        {
            List<PlaylistItem> items = GetSelectedPlaylistItems();
            if (items.Count == 0)
            {
                return false;
            }

            var data = new DataObject();
            string[] files = items.Select(item => item.FilePath)
                .Where(File.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (files.Length > 0)
            {
                var fileList = new StringCollection();
                fileList.AddRange(files);
                data.SetFileDropList(fileList);

                // カットした曲でも、エクスプローラーではファイルを移動せずにコピーさせる（プレイリストの曲のカットのため）
                data.SetData(PreferredDropEffectFormat, new MemoryStream(BitConverter.GetBytes((int)DragDropEffects.Copy)));
            }
            data.SetText(string.Join(Environment.NewLine, items.Select(item => item.FilePath)));

            string lines = string.Join("\n", items.Select(item => item.Rating.ToString(CultureInfo.InvariantCulture) + "\t" + item.FilePath));
            data.SetData(PlaylistClipboardFormat, new MemoryStream(Encoding.UTF8.GetBytes(lines)));

            try
            {
                // 別のアプリがクリップボードを開いていると失敗するので、少し待ってやり直す。アプリを終了しても残す
                Clipboard.SetDataObject(data, true, 5, 100);
                return true;
            }
            catch (ExternalException)
            {
                MessageBox.Show(this, Texts.ClipboardFailed, AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>選択中の曲をクリップボードに入れ、リストから削除する（ファイルは削除しない）。</summary>
        private void CutSelectedItems()
        {
            if (CopySelectedItems())
            {
                RemoveSelectedItems();
            }
        }

        /// <summary>クリップボードに、プレイリストに貼り付けられるもの（曲、ファイル・フォルダ）があるか。</summary>
        private static bool CanPaste()
        {
            try
            {
                return Clipboard.ContainsData(PlaylistClipboardFormat) || Clipboard.ContainsFileDropList();
            }
            catch (ExternalException)
            {
                return false;
            }
        }

        /// <summary>
        /// クリップボードの曲（エクスプローラーでコピーしたファイル・フォルダ・プレイリストファイルも）を、
        /// 選択中の最後の曲の次に貼り付ける。選択していなければ末尾に貼り付ける。貼り付けた曲を選択する。
        /// </summary>
        private void PasteItems()
        {
            List<PlaylistItem> items;
            try
            {
                items = ReadClipboardItems();
            }
            catch (ExternalException)
            {
                MessageBox.Show(this, Texts.ClipboardFailed, AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (items.Count == 0)
            {
                return;
            }

            int insertIndex = playlistView.SelectedIndices.Count > 0
                ? playlistView.SelectedIndices.Cast<int>().Max() + 1
                : playlistView.Items.Count;
            List<ListViewItem> added = AddFiles(items, insertIndex);

            playlistView.BeginUpdate();
            try
            {
                playlistView.SelectedItems.Clear();
                foreach (ListViewItem listItem in added.Where(listItem => listItem.ListView != null))
                {
                    listItem.Selected = true;
                }
            }
            finally
            {
                playlistView.EndUpdate();
            }

            ListViewItem first = added.FirstOrDefault(listItem => listItem.ListView != null);
            if (first != null)
            {
                playlistView.FocusedItem = first;
                first.EnsureVisible();
            }
        }

        /// <summary>クリップボードから、貼り付ける曲を読み出す。無ければ空のリスト。</summary>
        private static List<PlaylistItem> ReadClipboardItems()
        {
            var items = new List<PlaylistItem>();
            if (Clipboard.GetData(PlaylistClipboardFormat) is Stream stream)
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] fields = line.Split(new[] { '\t' }, 2);
                        if (fields.Length == 2 && int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int rating)
                            && IsMidiFile(fields[1]))
                        {
                            items.Add(new PlaylistItem(fields[1], rating));
                        }
                    }
                }
            }
            else if (Clipboard.ContainsFileDropList())
            {
                items.AddRange(ExpandPaths(Clipboard.GetFileDropList().Cast<string>()));
            }
            return items;
        }

        /// <summary>
        /// 選択した曲のファイルを、2 回確認してからごみ箱に移動し、リストからも削除する。
        /// </summary>
        /// <remarks>
        /// 同じファイルの曲がリストにほかにもあれば、それも削除する（評価のフィルターで表示していない曲も）。
        /// 削除できなかったファイルの曲はリストに残す。見つからないファイルの曲は、リストから削除するだけ。
        /// </remarks>
        private void DeleteSelectedFiles()
        {
            List<string> paths = GetSelectedPlaylistItems()
                .Select(item => item.FilePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (paths.Count == 0)
            {
                return;
            }

            // 1 回目: ファイルそのものを削除することと、どのファイルかを見せる
            if (MessageBox.Show(this, Texts.ConfirmDeleteFiles(paths, MaxListedFiles), AppName,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }

            // 2 回目: 最終確認
            if (MessageBox.Show(this, Texts.ConfirmDeleteFilesFinal(paths.Count), AppName,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }

            List<string> existing = paths.Where(File.Exists).ToList();
            bool aborted = existing.Count > 0 && RecycleBin.MoveToRecycleBin(Handle, existing);

            var remaining = new HashSet<string>(existing.Where(File.Exists), StringComparer.OrdinalIgnoreCase);
            var deleted = new HashSet<string>(paths.Where(path => !remaining.Contains(path)), StringComparer.OrdinalIgnoreCase);
            RemoveListItems(_allItems.Where(listItem => deleted.Contains(((PlaylistItem)listItem.Tag).FilePath)).ToList());

            if (remaining.Count > 0 && !aborted)
            {
                MessageBox.Show(this, Texts.DeleteFilesFailed(remaining.ToList(), MaxListedFiles), AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ---- お気に入りの評価 ----

        private void playlistView_RatingChanged(object sender, RatingEventArgs e)
        {
            SetRating(new[] { e.Item }, e.Rating);
        }

        private void ratingFilterBox_ValueChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        /// <summary>
        /// 曲の評価を変える。フィルターで表示しない評価になった曲は、リストから隠す。
        /// </summary>
        /// <remarks>ちらつかないよう、描き直すのは評価の欄だけにする。</remarks>
        private void SetRating(IReadOnlyCollection<ListViewItem> listItems, int rating)
        {
            List<ListViewItem> changed = listItems.Where(listItem => ((PlaylistItem)listItem.Tag).Rating != rating).ToList();
            if (changed.Count == 0)
            {
                return;
            }

            bool hide = rating < RatingFilter;
            if (hide)
            {
                playlistView.BeginUpdate();
            }
            try
            {
                foreach (ListViewItem listItem in changed)
                {
                    ((PlaylistItem)listItem.Tag).Rating = rating;
                    if (hide)
                    {
                        playlistView.Items.Remove(listItem);
                    }
                    else
                    {
                        playlistView.InvalidateRating(listItem);
                    }
                }
            }
            finally
            {
                if (hide)
                {
                    playlistView.EndUpdate();
                }
            }
            UpdateTitle();
        }

        private bool PassesFilter(ListViewItem listItem)
        {
            return ((PlaylistItem)listItem.Tag).Rating >= RatingFilter;
        }

        /// <summary>
        /// 評価のフィルターに合わせて、リストに表示する曲を選び直す。選択していた曲は選択したままにする。
        /// </summary>
        private void ApplyFilter()
        {
            var selected = new HashSet<ListViewItem>(playlistView.SelectedItems.Cast<ListViewItem>());
            playlistView.BeginUpdate();
            try
            {
                playlistView.Items.Clear();
                playlistView.Items.AddRange(_allItems.Where(PassesFilter).ToArray());
                foreach (ListViewItem listItem in playlistView.Items)
                {
                    listItem.Selected = selected.Contains(listItem);
                }
            }
            finally
            {
                playlistView.EndUpdate();
            }
            RefreshListItems();

            // 選択中の曲（無ければ再生中の曲）が見えるようにする
            ListViewItem visible = playlistView.SelectedItems.Cast<ListViewItem>().FirstOrDefault()
                ?? (_currentItem != null ? FindListItem(_currentItem) : null);
            visible?.EnsureVisible();
        }

        private void playlistView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (playlistView.SelectedItems.Count == 0)
            {
                return;
            }
            playlistView.DoDragDrop(new DataObject(PlaylistDragFormat, true), DragDropEffects.Move);
            _insertionLine.Visible = false;
        }

        private void playlistView_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else if (e.Data.GetDataPresent(PlaylistDragFormat))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
                _insertionLine.Visible = false;
                return;
            }

            Point point = playlistView.PointToClient(new Point(e.X, e.Y));
            ScrollDuringDrag(point);
            ShowInsertionLine(GetDropIndex(point));
        }

        private void playlistView_DragLeave(object sender, EventArgs e)
        {
            _insertionLine.Visible = false;
        }

        private void playlistView_DragDrop(object sender, DragEventArgs e)
        {
            _insertionLine.Visible = false;
            int dropIndex = GetDropIndex(playlistView.PointToClient(new Point(e.X, e.Y)));

            if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
            {
                AddPaths(paths, dropIndex);
            }
            else if (e.Data.GetDataPresent(PlaylistDragFormat))
            {
                MoveSelectedItems(dropIndex);
            }
        }

        /// <summary>
        /// ファイル・フォルダ・プレイリストファイルを、プレイリストの指定位置に追加する。
        /// </summary>
        private void AddPaths(IEnumerable<string> paths, int insertIndex)
        {
            AddFiles(ExpandPaths(paths), insertIndex);
        }

        /// <summary>
        /// 曲をプレイリストの指定位置（表示中のリスト上の位置）に追加する。曲名と演奏時間は後から読み込む。
        /// </summary>
        /// <remarks>
        /// 評価のフィルターで表示しない曲も、プレイリストには加える（リストには表示しない）。
        /// </remarks>
        /// <returns>追加した曲の行（表示していない行も含む）。</returns>
        private List<ListViewItem> AddFiles(IReadOnlyCollection<PlaylistItem> items, int insertIndex)
        {
            var added = new List<ListViewItem>();
            if (items.Count == 0)
            {
                return added;
            }

            // 表示中のリスト上の位置の曲の前に入れる。リストの末尾なら、プレイリストの末尾に入れる
            int allIndex = insertIndex < playlistView.Items.Count ? _allItems.IndexOf(playlistView.Items[insertIndex]) : _allItems.Count;

            playlistView.BeginUpdate();
            try
            {
                foreach (PlaylistItem item in items)
                {
                    // 評価の列（3 番目）は playlistView_DrawSubItem で描くので、文字は入れない
                    var listItem = new ListViewItem(new[] { string.Empty, string.Empty, string.Empty, string.Empty, item.FileName })
                    {
                        Tag = item,
                    };
                    _allItems.Insert(allIndex++, listItem);
                    if (PassesFilter(listItem))
                    {
                        playlistView.Items.Insert(insertIndex++, listItem);
                    }
                    added.Add(listItem);
                }
            }
            finally
            {
                playlistView.EndUpdate();
            }

            RefreshListItems();
            UpdateTitle();
            LoadSongInfoAsync(added);
            return added;
        }

        /// <summary>
        /// MIDI ファイルを読み込む。読み込めなければ、曲の代わりに理由を返す。
        /// </summary>
        /// <remarks>
        /// 例外をこの中で受け止めるのは、Task.Run の中から例外を外へ出すと、
        /// 呼び出し側で受け止めていても VS のデバッガーが「ユーザーが処理していない例外」で止まるため。
        /// 見つからないファイルはプレイリストによくあるので、例外を使わずに先に確かめる。
        /// </remarks>
        private static (MidiFile Song, string Error) TryLoadSong(string path)
        {
            if (!File.Exists(path))
            {
                return (null, Texts.FileNotFound);
            }
            try
            {
                return (MidiFile.Load(path), null);
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        /// <summary>
        /// 追加した曲の曲名と演奏時間を、画面を止めないようにバックグラウンドで読み込む。
        /// </summary>
        private async void LoadSongInfoAsync(List<ListViewItem> listItems)
        {
            foreach (ListViewItem listItem in listItems)
            {
                var item = (PlaylistItem)listItem.Tag;
                (MidiFile song, string error) = await Task.Run(() => TryLoadSong(item.FilePath));
                if (song != null)
                {
                    item.SetSongInfo(song);
                }
                else
                {
                    item.SetError(error);
                }

                if (IsDisposed)
                {
                    return;
                }
                if (listItem.ListView != null)
                {
                    UpdateListItem(listItem);
                }
            }
        }

        /// <summary>選択した曲を、確認してからリストから削除する（ファイルは削除しない）。</summary>
        private void ConfirmRemoveSelectedItems()
        {
            List<PlaylistItem> selected = GetSelectedPlaylistItems();
            if (selected.Count == 0)
            {
                return;
            }

            string message = Texts.ConfirmRemoveFromList(selected.Count, selected[0].DisplayTitle);
            if (MessageBox.Show(this, message, AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                RemoveSelectedItems();
            }
        }

        private void RemoveSelectedItems()
        {
            RemoveListItems(playlistView.SelectedItems.Cast<ListViewItem>().ToList());
        }

        /// <summary>曲をプレイリストから取り除く（評価のフィルターで表示していない曲も取り除ける）。</summary>
        private void RemoveListItems(IReadOnlyCollection<ListViewItem> listItems)
        {
            if (listItems.Count == 0)
            {
                return;
            }

            playlistView.BeginUpdate();
            try
            {
                foreach (ListViewItem listItem in listItems)
                {
                    if (listItem.Tag == _currentItem)
                    {
                        UnloadCurrentSong();
                    }
                    if (listItem.ListView != null)
                    {
                        playlistView.Items.Remove(listItem);
                    }
                    _allItems.Remove(listItem);
                }
            }
            finally
            {
                playlistView.EndUpdate();
            }
            RefreshListItems();
            UpdateTitle();
        }

        /// <summary>プレイリストを空にする。開いていたプレイリストファイルとの関係も解く。</summary>
        private void ClearPlaylist()
        {
            UnloadCurrentSong();
            playlistView.Items.Clear();
            _allItems.Clear();
            _history.Clear();
            _shufflePlayed.Clear();
            _playlistPath = null;
        }

        private void MoveSelectedItems(int dropIndex)
        {
            List<ListViewItem> moving = playlistView.SelectedItems.Cast<ListViewItem>().OrderBy(item => item.Index).ToList();
            if (moving.Count == 0)
            {
                return;
            }

            // 取り除いた分だけ、挿入位置が前にずれる
            int insertIndex = dropIndex - moving.Count(item => item.Index < dropIndex);

            // プレイリスト全体では、ドロップ位置より後で最初の（動かさない）曲の前に入れる。無ければ末尾に入れる
            ListViewItem anchor = playlistView.Items.Cast<ListViewItem>().Skip(dropIndex).FirstOrDefault(item => !item.Selected);
            foreach (ListViewItem item in moving)
            {
                _allItems.Remove(item);
            }
            _allItems.InsertRange(anchor != null ? _allItems.IndexOf(anchor) : _allItems.Count, moving);

            playlistView.BeginUpdate();
            try
            {
                foreach (ListViewItem item in moving)
                {
                    playlistView.Items.Remove(item);
                }
                for (int i = 0; i < moving.Count; i++)
                {
                    playlistView.Items.Insert(insertIndex + i, moving[i]);
                    moving[i].Selected = true;
                }
            }
            finally
            {
                playlistView.EndUpdate();
            }
            RefreshListItems();
        }

        /// <summary>
        /// マウス位置から、ドロップしたときの挿入位置を求める。
        /// </summary>
        private int GetDropIndex(Point clientPoint)
        {
            int count = playlistView.Items.Count;
            if (count == 0)
            {
                return 0;
            }

            ListViewItem top = playlistView.TopItem;
            if (top != null && clientPoint.Y < top.Bounds.Top)
            {
                return top.Index;
            }

            ListViewItem target = playlistView.GetItemAt(1, clientPoint.Y);
            if (target == null)
            {
                return count;
            }
            Rectangle bounds = target.Bounds;
            return clientPoint.Y < bounds.Top + bounds.Height / 2 ? target.Index : target.Index + 1;
        }

        private void ShowInsertionLine(int index)
        {
            int count = playlistView.Items.Count;
            if (count == 0)
            {
                _insertionLine.Visible = false;
                return;
            }

            int y = index < count ? playlistView.Items[index].Bounds.Top : playlistView.Items[count - 1].Bounds.Bottom;
            _insertionLine.SetBounds(0, y - 1, playlistView.ClientSize.Width, _insertionLine.Height);
            _insertionLine.Visible = true;
            _insertionLine.BringToFront();
        }

        private void ScrollDuringDrag(Point clientPoint)
        {
            ListViewItem top = playlistView.TopItem;
            if (top == null)
            {
                return;
            }

            if (clientPoint.Y < top.Bounds.Top + DragScrollMargin && top.Index > 0)
            {
                playlistView.EnsureVisible(top.Index - 1);
            }
            else if (clientPoint.Y > playlistView.ClientSize.Height - DragScrollMargin)
            {
                ListViewItem bottom = playlistView.GetItemAt(1, playlistView.ClientSize.Height - DragScrollMargin);
                if (bottom != null && bottom.Index + 1 < playlistView.Items.Count)
                {
                    playlistView.EnsureVisible(bottom.Index + 1);
                }
            }
        }

        /// <summary>
        /// 番号・再生中の印・曲名などの表示を、表示中のすべての曲について更新する。
        /// </summary>
        private void RefreshListItems()
        {
            playlistView.BeginUpdate();
            try
            {
                for (int i = 0; i < _allItems.Count; i++)
                {
                    if (_allItems[i].ListView != null)
                    {
                        UpdateListItem(_allItems[i], i + 1);
                    }
                }
            }
            finally
            {
                playlistView.EndUpdate();
            }
        }

        private void UpdateListItem(ListViewItem listItem)
        {
            UpdateListItem(listItem, _allItems.IndexOf(listItem) + 1);
        }

        /// <param name="number">プレイリスト全体での番号（評価のフィルターで隠した曲も数える）。</param>
        private void UpdateListItem(ListViewItem listItem, int number)
        {
            var item = (PlaylistItem)listItem.Tag;
            bool isCurrent = item == _currentItem;

            listItem.SubItems[0].Text = number.ToString(); // 再生中の曲の印は playlistView が描く
            listItem.SubItems[1].Text = item.HasError ? item.DisplayTitle + Texts.LoadError : item.DisplayTitle;
            listItem.SubItems[2].Text = item.Duration.HasValue ? FormatTime(item.Duration.Value) : (item.HasError ? string.Empty : "--:--");
            Theme theme = Theme.Current;
            listItem.ForeColor = item.HasError ? theme.DisabledText : isCurrent ? theme.CurrentItemText : theme.Text;
            listItem.BackColor = isCurrent ? theme.CurrentItemBack : theme.Surface;
            listItem.Font = isCurrent ? _currentItemFont : playlistView.Font;
            listItem.ToolTipText = item.HasError ? item.ErrorMessage : item.FilePath;
        }

        /// <summary>プレイリストの全曲（評価のフィルターで隠した曲も含む）。</summary>
        private IEnumerable<PlaylistItem> GetPlaylistItems()
        {
            return _allItems.Select(listItem => (PlaylistItem)listItem.Tag);
        }

        private ListViewItem FindListItem(PlaylistItem item)
        {
            return playlistView.Items.Cast<ListViewItem>().FirstOrDefault(listItem => listItem.Tag == item);
        }

        // ---- 再生 ----

        private void TogglePlayPause()
        {
            if (_sequencer.IsPlaying)
            {
                _sequencer.Pause();
            }
            else if (_currentItem != null && _sequencer.Song != null)
            {
                if (EnsureOutputDevice())
                {
                    _sequencer.Play();
                }
            }
            else if (playlistView.SelectedIndices.Count > 0)
            {
                PlayFrom(playlistView.SelectedIndices[0], 1, false);
            }
            else if (CurrentPlayMode == PlayMode.Shuffle)
            {
                PlayRandom();
            }
            else
            {
                PlayFrom(0, 1, false);
            }
            UpdatePlaybackDisplay();
        }

        private void PlayPrevious()
        {
            if (_currentItem == null)
            {
                return;
            }

            bool played = false;
            if (_sequencer.Position <= RestartThreshold)
            {
                PlayMode mode = CurrentPlayMode;
                if (mode == PlayMode.Shuffle)
                {
                    played = PlayFromHistory();
                }
                else
                {
                    int previous = PreviousIndex();
                    bool wrap = mode == PlayMode.RepeatAll;
                    played = playlistView.Items.Count > 0 && (previous >= 0 || wrap) && PlayFrom(previous, -1, wrap);
                }
            }

            if (!played)
            {
                _sequencer.Seek(TimeSpan.Zero);
            }
            UpdatePlaybackDisplay();
        }

        /// <summary>
        /// 再生モードに従って、次の曲を再生する。次の曲が無ければ false を返す。
        /// </summary>
        private bool PlayNext()
        {
            PlayMode mode = CurrentPlayMode;
            if (mode == PlayMode.Shuffle)
            {
                return PlayRandom();
            }
            return PlayFrom(NextIndex(), 1, mode == PlayMode.RepeatAll);
        }

        /// <summary>
        /// 指定位置の曲を再生する。読み込めない曲は飛ばして、step の方向に次の曲を探す。
        /// </summary>
        /// <param name="wrap">リストの端に達したら反対側の端から続けて探すかどうか。</param>
        /// <returns>再生を始められたかどうか。</returns>
        private bool PlayFrom(int index, int step, bool wrap)
        {
            if (!EnsureOutputDevice())
            {
                return false;
            }

            int count = playlistView.Items.Count;
            for (int n = 0; n < count; n++)
            {
                int i = index + n * step;
                if (wrap)
                {
                    i = ((i % count) + count) % count;
                }
                else if (i < 0 || i >= count)
                {
                    break;
                }

                if (PlayItem(playlistView.Items[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// シャッフル: この一巡でまだ再生していない曲から、ランダムに選んで再生する。
        /// </summary>
        private bool PlayRandom()
        {
            if (!EnsureOutputDevice())
            {
                return false;
            }

            List<ListViewItem> playable = playlistView.Items.Cast<ListViewItem>()
                .Where(listItem => !((PlaylistItem)listItem.Tag).HasError)
                .ToList();
            List<ListViewItem> candidates = playable.Where(listItem => !_shufflePlayed.Contains((PlaylistItem)listItem.Tag)).ToList();
            if (candidates.Count == 0)
            {
                // 一巡したので、新しい順番で繰り返す（同じ曲が続かないよう、今の曲は除く）
                _shufflePlayed.Clear();
                candidates = playable.Where(listItem => listItem.Tag != _currentItem).ToList();
                if (candidates.Count == 0)
                {
                    candidates = playable;
                }
            }

            while (candidates.Count > 0)
            {
                int pick = _random.Next(candidates.Count);
                if (PlayItem(candidates[pick]))
                {
                    return true;
                }
                candidates.RemoveAt(pick);
            }
            return false;
        }

        /// <summary>
        /// シャッフル中の「前の曲」: 履歴をさかのぼって、直前に再生した曲を再生する。
        /// </summary>
        private bool PlayFromHistory()
        {
            if (_history.Count > 0 && _history[_history.Count - 1] == _currentItem)
            {
                _history.RemoveAt(_history.Count - 1);
            }

            while (_history.Count > 0)
            {
                PlaylistItem previous = _history[_history.Count - 1];
                _history.RemoveAt(_history.Count - 1);
                ListViewItem listItem = FindListItem(previous);
                if (listItem != null && previous != _currentItem && PlayItem(listItem))
                {
                    return true; // PlayItem が履歴に戻す
                }
            }

            if (_currentItem != null)
            {
                _history.Add(_currentItem);
            }
            return false;
        }

        private bool PlayItem(ListViewItem listItem)
        {
            var item = (PlaylistItem)listItem.Tag;
            (MidiFile song, string error) = TryLoadSong(item.FilePath);
            if (song == null)
            {
                item.SetError(error);
                UpdateListItem(listItem);
                return false;
            }
            item.SetSongInfo(song);

            if (resetTempoKeyPerSongMenuItem.Checked)
            {
                ResetTempoAndKey();
            }
            _sequencer.Load(song);
            ClearLoop();
            _sequencer.Play();
            _currentItem = item;
            playlistView.CurrentItem = listItem;
            UpdateTitle();
            RecordPlayed(item);

            RefreshListItems();
            listItem.EnsureVisible();
            UpdateSongInfoStatus(song);
            UpdatePlaybackDisplay();
            return true;
        }

        private void RecordPlayed(PlaylistItem item)
        {
            _history.Add(item);
            if (_history.Count > MaxHistoryCount)
            {
                _history.RemoveAt(0);
            }
            _shufflePlayed.Add(item);
        }

        private void UnloadCurrentSong()
        {
            _sequencer.Unload();
            ClearLoop();
            _currentItem = null;
            playlistView.CurrentItem = null;
            UpdateTitle();
            UpdateSongInfoStatus(null);
            UpdatePlaybackDisplay();
        }

        private void Sequencer_SongEnded(object sender, EventArgs e)
        {
            // 再生スレッドから呼ばれるので、画面のスレッドに処理を移す
            try
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    BeginInvoke(new Action(OnSongEnded));
                }
            }
            catch (InvalidOperationException)
            {
                // 終了処理中にウィンドウが閉じられた
            }
        }

        private void OnSongEnded()
        {
            // 通知が届くまでの間に、停止や別の曲の再生が行われていたら何もしない
            MidiFile song = _sequencer.Song;
            if (song == null || _sequencer.IsPlaying || _sequencer.Position < song.Duration)
            {
                return;
            }

            switch (CurrentPlayMode)
            {
                case PlayMode.Single:
                    _sequencer.Stop();
                    break;

                case PlayMode.RepeatOne:
                    _sequencer.Play(); // 曲の最後にいるので、先頭から再生する
                    break;

                default:
                    if (!PlayNext())
                    {
                        _sequencer.Stop();
                    }
                    break;
            }
            UpdatePlaybackDisplay();
        }

        /// <summary>
        /// 再生中の曲の前の曲の、表示中のリスト上の位置。再生中の曲がフィルターで隠れていても、プレイリストの並びで前の曲を指す。
        /// </summary>
        private int PreviousIndex()
        {
            ListViewItem listItem = _currentItem != null ? FindListItem(_currentItem) : null;
            return (listItem != null ? listItem.Index : NextIndex()) - 1;
        }

        /// <summary>
        /// 再生中の曲の次の曲の、表示中のリスト上の位置。再生中の曲がフィルターで隠れていても、プレイリストの並びで次の曲を指す。
        /// </summary>
        private int NextIndex()
        {
            if (_currentItem == null)
            {
                return 0;
            }
            ListViewItem listItem = FindListItem(_currentItem);
            if (listItem != null)
            {
                return listItem.Index + 1;
            }

            // 隠れている曲より前にある、表示中の曲の数
            int count = 0;
            foreach (ListViewItem item in _allItems)
            {
                if (item.Tag == _currentItem)
                {
                    return count;
                }
                if (item.ListView != null)
                {
                    count++;
                }
            }
            return 0; // プレイリストから消された曲
        }

        /// <summary>再生中の曲の、プレイリスト全体での位置（評価のフィルターで隠した曲も数える）。無ければ -1。</summary>
        private int CurrentIndexInPlaylist()
        {
            return _allItems.FindIndex(listItem => listItem.Tag == _currentItem);
        }

        // ---- 出力デバイス ----

        private void SelectOutputDevice(int deviceId)
        {
            if (_output != null && _output.DeviceId == deviceId)
            {
                return;
            }

            MidiOutDevice device;
            try
            {
                device = MidiOutDevice.Open(deviceId);
            }
            catch (MidiException ex)
            {
                MessageBox.Show(this, ex.Message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MidiOutDevice oldDevice = _output;
            _output = device;
            _sequencer.SetOutput(device);
            oldDevice?.Dispose();
            UpdateDeviceStatus();
        }

        private bool EnsureOutputDevice()
        {
            if (_output != null)
            {
                return true;
            }
            MessageBox.Show(this, Texts.NoOutputDevice, AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        // ---- 設定の復元・保存 ----

        private void RestoreWindowBounds()
        {
            Rectangle bounds = _settings.WindowBounds;
            bool isUsable = bounds.Width >= MinimumSize.Width && bounds.Height >= MinimumSize.Height
                // タイトルバーがどこかの画面に見えていること（外したモニターの位置に出ないように）
                && Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(new Rectangle(bounds.X, bounds.Y, bounds.Width, SystemInformation.CaptionHeight)));
            if (isUsable)
            {
                StartPosition = FormStartPosition.Manual;
                Bounds = bounds;
            }
            if (_settings.WindowMaximized)
            {
                WindowState = FormWindowState.Maximized;
            }
        }

        private void RestoreColumnWidths()
        {
            int[] widths = _settings.ColumnWidths;
            if (widths == null || widths.Length != playlistView.Columns.Count)
            {
                return;
            }
            for (int i = 0; i < widths.Length; i++)
            {
                playlistView.Columns[i].Width = widths[i];
            }
        }

        private void RestoreChannelPanel()
        {
            int height = _settings.ChannelPanelHeight;
            if (height > 0)
            {
                int maxHeight = splitContainer.Height - splitContainer.SplitterWidth - splitContainer.Panel1MinSize;
                height = Math.Min(Math.Max(height, splitContainer.Panel2MinSize), maxHeight);
                if (height >= splitContainer.Panel2MinSize)
                {
                    splitContainer.SplitterDistance = splitContainer.Height - splitContainer.SplitterWidth - height;
                }
            }
            channelPanelMenuItem.Checked = _settings.ChannelPanelVisible;
            usedChannelsOnlyMenuItem.Checked = _settings.ShowUsedChannelsOnly;
        }

        private void RestoreOutputDevice()
        {
            IReadOnlyList<string> names = MidiOutDevice.GetDeviceNames();
            if (names.Count == 0)
            {
                return;
            }

            // 前回のデバイスが見つからなければ、最初のデバイスを使う
            int deviceId = 0;
            for (int i = 0; i < names.Count; i++)
            {
                if (names[i] == _settings.OutputDeviceName)
                {
                    deviceId = i;
                    break;
                }
            }
            SelectOutputDevice(deviceId);
        }

        private void RestorePlaylist()
        {
            if (!File.Exists(AppSettings.PlaylistFilePath))
            {
                return;
            }
            try
            {
                AddFiles(M3uPlaylist.Load(AppSettings.PlaylistFilePath).Where(item => IsMidiFile(item.FilePath)).ToList(), 0);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // 読めなければ、空のプレイリストで始める
                return;
            }

            // 前回終了時の評価を、開いたプレイリストファイルと同じ状態として扱う
            _playlistPath = _settings.PlaylistPath;
            MarkRatingsSaved();
            SelectLastSong();
        }

        /// <summary>
        /// 前回終了時に再生していた曲を選択する。再生ボタンを押すと、選択中の曲から再生される。
        /// </summary>
        private void SelectLastSong()
        {
            string path = _settings.LastSongPath;
            if (path == null)
            {
                return;
            }

            // 同じファイルが複数あるときのために、まず前回の位置を見て、違っていればパスで探す
            // （位置は、評価のフィルターで隠した曲も数えたプレイリスト全体での位置）
            int index = _settings.LastSongIndex;
            bool matchesIndex = index >= 0 && index < _allItems.Count
                && string.Equals(((PlaylistItem)_allItems[index].Tag).FilePath, path, StringComparison.OrdinalIgnoreCase);
            ListViewItem listItem = matchesIndex
                ? _allItems[index]
                : _allItems.FirstOrDefault(item => string.Equals(((PlaylistItem)item.Tag).FilePath, path, StringComparison.OrdinalIgnoreCase));
            if (listItem == null || listItem.ListView == null)
            {
                return; // 見つからないか、フィルターで隠れている
            }

            listItem.Selected = true;
            listItem.Focused = true;
            listItem.EnsureVisible();
        }

        private void SaveSettings()
        {
            _settings.ResetType = (MidiResetType)resetComboBox.SelectedIndex;
            _settings.PlayMode = CurrentPlayMode;
            _settings.ResetTempoAndKeyPerSong = resetTempoKeyPerSongMenuItem.Checked;
            if (_output != null)
            {
                _settings.OutputDeviceName = _output.Name;
            }

            _settings.WindowMaximized = WindowState == FormWindowState.Maximized;
            _settings.WindowBounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            if (!splitContainer.Panel2Collapsed)
            {
                _settings.ChannelPanelHeight = splitContainer.Panel2.Height;
            }
            _settings.ChannelPanelVisible = channelPanelMenuItem.Checked;
            _settings.ShowUsedChannelsOnly = usedChannelsOnlyMenuItem.Checked;
            _settings.ColumnWidths = playlistView.Columns.Cast<ColumnHeader>().Select(column => column.Width).ToArray();
            if (_currentItem != null)
            {
                // 今回何も再生しなかった場合は、前回の記録を残す
                _settings.LastSongPath = _currentItem.FilePath;
                _settings.LastSongIndex = CurrentIndexInPlaylist();
            }
            _settings.PlaylistPath = _playlistPath;
            _settings.RatingFilter = RatingFilter;
            _settings.VelocityCurve = velocityCurveBox.Value;
            _settings.Language = Texts.Current;
            _settings.ColorTheme = Theme.Current.Kind;
            _settings.SkipLeadingSilence = skipSilenceMenuItem.Checked;
            _settings.TopMost = topMostMenuItem.Checked;
            _settings.PianoRollVisible = pianoRollMenuItem.Checked;
            if (_pianoRoll != null)
            {
                _settings.PianoRollBounds = _pianoRoll.NormalBounds;
                _settings.PianoRollPixelsPerSecond = _pianoRoll.PixelsPerSecond;
                _settings.PianoRollOptions = _pianoRoll.Options;
            }
            _settings.BlockedControllers = Enumerable.Range(0, 128).Where(_sequencer.IsControllerBlocked).ToArray();
            _settings.LevelMeter = channelView.MeterSettings;

            _settings.Save();
        }

        private void SavePlaylistAutomatically()
        {
            try
            {
                M3uPlaylist.Save(AppSettings.PlaylistFilePath, GetPlaylistItems());
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // 保存できなくても終了は止めない
            }
        }

        // ---- 表示 ----

        private void UpdatePlaybackDisplay()
        {
            MidiFile song = _sequencer.Song;
            TimeSpan duration = song?.Duration ?? TimeSpan.Zero;
            seekBar.Maximum = (int)duration.TotalMilliseconds;
            seekBar.Enabled = song != null;
            loopAButton.Enabled = song != null;
            loopBButton.Enabled = song != null;

            TimeSpan position;
            if (seekBar.IsDragging)
            {
                position = TimeSpan.FromMilliseconds(seekBar.Value);
            }
            else
            {
                position = _sequencer.Position;
                seekBar.Value = (int)position.TotalMilliseconds;
            }
            timeLabel.Text = FormatTime(position) + " / " + FormatTime(duration);
            MusicalPosition? musicalPosition = song?.GetMusicalPosition(position);
            string positionText = FormatPosition(position, musicalPosition, song?.BarCount ?? 0);
            if (positionStatusLabel.Text != positionText)
            {
                positionStatusLabel.Text = positionText;
            }
            string tempoText = FormatTempo(song, position, musicalPosition);
            if (tempoStatusLabel.Text != tempoText)
            {
                tempoStatusLabel.Text = tempoText;
            }

            string glyph = _sequencer.IsPlaying ? PauseGlyph : PlayGlyph;
            if (playPauseButton.Text != glyph)
            {
                playPauseButton.Text = glyph;
            }

            // 再生中の曲の印（▶ / ❚❚ / ■）。変わったときだけ描き直される
            playlistView.CurrentState = CurrentPlaybackState;
        }

        private PlaybackState CurrentPlaybackState
        {
            get
            {
                if (_sequencer.IsPlaying)
                {
                    return PlaybackState.Playing;
                }
                return _sequencer.Song != null && _sequencer.Position > TimeSpan.Zero ? PlaybackState.Paused : PlaybackState.Stopped;
            }
        }

        private void UpdateDeviceStatus()
        {
            deviceStatusLabel.Text = Texts.OutputStatus(_output?.Name);
        }

        private void UpdateSongInfoStatus(MidiFile song)
        {
            if (song == null)
            {
                songInfoStatusLabel.Text = string.Empty;
                return;
            }
            string timing = song.IsSmpteTiming ? "SMPTE" : $"{song.TicksPerQuarterNote} TPQN";
            songInfoStatusLabel.Text = $"Format {song.Format} / {timing}";
        }

        private static string FormatTime(TimeSpan time)
        {
            int totalSeconds = (int)time.TotalSeconds;
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        /// <summary>
        /// ステータスバーの今のテンポと拍子（例: 「♩=120  4/4」）。テンポは、テンポ欄の % を掛けた実際の速さ。
        /// 曲が無い、または SMPTE 形式の曲では空。
        /// </summary>
        private string FormatTempo(MidiFile song, TimeSpan position, MusicalPosition? musicalPosition)
        {
            if (song == null || song.IsSmpteTiming || !musicalPosition.HasValue)
            {
                return string.Empty;
            }
            double bpm = song.GetTempoAt(position) * _sequencer.TempoPercent / 100.0;
            string tempo = Math.Abs(bpm - Math.Round(bpm)) < 0.05
                ? Math.Round(bpm).ToString("0", CultureInfo.InvariantCulture)
                : bpm.ToString("0.0", CultureInfo.InvariantCulture);
            return $"♩={tempo}  {musicalPosition.Value.BeatsPerBar}/{musicalPosition.Value.BeatUnit}";
        }

        /// <summary>
        /// ステータスバーの再生位置（例: 「TIME 00:01:23/45  BAR 0012/0120  BEAT 03/04 0240/0480」）。
        /// 小節・拍が無い（曲が無い、SMPTE 形式）場合は 0 を並べる。
        /// </summary>
        private static string FormatPosition(TimeSpan time, MusicalPosition? position, int barCount)
        {
            if (time < TimeSpan.Zero)
            {
                time = TimeSpan.Zero;
            }
            MusicalPosition p = position ?? default(MusicalPosition);
            return $"TIME {(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}/{time.Milliseconds / 10:00}"
                + $"  BAR {p.Bar:0000}/{barCount:0000}"
                + $"  BEAT {p.Beat:00}/{p.BeatsPerBar:00} {p.TickInBeat:0000}/{p.TicksPerBeat:0000}";
        }

        /// <summary>
        /// フォルダは配下の MIDI ファイルに、プレイリストファイルは中に書かれた MIDI ファイルに展開する。
        /// </summary>
        /// <remarks>
        /// プレイリストに書かれたファイルは、存在しなくてもそのまま加える（読み込みエラーとして表示するため）。
        /// プレイリストに書かれた評価も引き継ぐ。
        /// 相対パスは、今のフォルダを元に絶対パスにする（プレイリストには絶対パスで持つ）。
        /// </remarks>
        private static List<PlaylistItem> ExpandPaths(IEnumerable<string> paths)
        {
            var result = new List<PlaylistItem>();
            foreach (string path in paths.Select(ToFullPath).Where(path => path != null))
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        result.AddRange(EnumerateMidiFiles(path)
                            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
                            .Select(file => new PlaylistItem(file)));
                    }
                    else if (File.Exists(path) && PlaylistFile.IsPlaylistFile(path))
                    {
                        result.AddRange(PlaylistFile.Load(path).Where(item => IsMidiFile(item.FilePath)));
                    }
                    else if (File.Exists(path) && IsMidiFile(path))
                    {
                        result.Add(new PlaylistItem(path));
                    }
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    // アクセスできないフォルダやファイルは飛ばす
                }
            }
            return result;
        }

        /// <summary>
        /// フォルダと、その中のすべてのサブフォルダにある MIDI ファイルを集める。
        /// 開けないサブフォルダ（ドライブ直下の System Volume Information など）は飛ばし、ほかのフォルダの曲は集める。
        /// </summary>
        /// <remarks>
        /// Directory.EnumerateFiles の SearchOption.AllDirectories は、開けないフォルダが 1 つでもあると
        /// 途中で例外になって何も返さないので、フォルダを 1 つずつたどる。
        /// </remarks>
        private static List<string> EnumerateMidiFiles(string folder)
        {
            var files = new List<string>();
            var pending = new Stack<string>();
            pending.Push(folder);
            while (pending.Count > 0)
            {
                string current = pending.Pop();
                try
                {
                    files.AddRange(Directory.GetFiles(current).Where(IsMidiFile));
                    foreach (string subfolder in Directory.GetDirectories(current))
                    {
                        pending.Push(subfolder);
                    }
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    // 開けないフォルダは飛ばす
                }
            }
            return files;
        }

        /// <summary>絶対パスにする。パスとして正しくなければ null。</summary>
        private static string ToFullPath(string path)
        {
            try
            {
                return Path.GetFullPath(path);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException
                || ex is System.Security.SecurityException)
            {
                return null;
            }
        }

        private static bool IsMidiFile(string path)
        {
            string extension = Path.GetExtension(path);
            return MidiExtensions.Any(midiExtension => string.Equals(extension, midiExtension, StringComparison.OrdinalIgnoreCase));
        }
    }
}
