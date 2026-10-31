using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using KMidiPlayer.Controls;
using KMidiPlayer.Midi;

namespace KMidiPlayer
{
    /// <summary>
    /// ピアノロールのウィンドウ。メイン画面に付属させて使う（閉じるボタンでは破棄せずに隠す）。
    /// </summary>
    /// <remarks>
    /// 上のツールバーと、ピアノロールの右クリックメニューに、同じ表示の設定を並べる（言語を変えたら作り直す）。
    /// F11 で全画面にし、Esc か F11 で戻る。
    /// </remarks>
    public partial class PianoRollForm : Form
    {
        /// <summary>ツールバーの、倍率のスライダーの幅。</summary>
        private const int ZoomSliderWidth = 120;

        /// <summary>ツールバーとメニューの項目のチェックや文字を、今の設定に合わせる処理。</summary>
        private readonly List<Action> _commandRefreshers = new List<Action>();

        private PianoRollOptions _options = new PianoRollOptions();
        private string _songTitle;

        /// <summary>表示している間、設定の fps で描き直しを起こす時計。</summary>
        private readonly FrameClock _frameClock;

        // 表示するシーケンサーと、今の曲名を返す処理（Attach で受け取る）
        private MidiSequencer _sequencer;
        private Func<string> _songTitleProvider;

        // 全画面にする前のウィンドウの位置・大きさと、最大化していたか
        private bool _fullScreen;
        private Rectangle _boundsBeforeFullScreen;
        private bool _maximizedBeforeFullScreen;

        // ツールバーの倍率のスライダーと、その右の「100%」の表示（ツールバーを作り直すたびに替わる）
        private ZoomSlider _zoomSlider;
        private ToolStripLabel _zoomLabel;

        public PianoRollForm()
        {
            InitializeComponent();
            pianoRollView.PixelsPerSecondChanged += pianoRollView_PixelsPerSecondChanged;
            _frameClock = new FrameClock(this, _options.FramesPerSecond);
            _frameClock.Tick += frameClock_Tick;
            Disposed += (sender, e) => _frameClock.Dispose();
            ApplyOptions();
            ApplyTexts();
            ThemeStyler.Apply(this); // ツールバーと右クリックメニュー（ピアノロールの絵は、表示の種類ごとの配色で描く）
        }

        /// <summary>ピアノロールか全体図で、再生位置の移動が求められたときに発生する。</summary>
        public event EventHandler<SeekRequestedEventArgs> SeekRequested;

        /// <summary>時間方向の拡大率（1 秒あたりの px）。</summary>
        public double PixelsPerSecond
        {
            get => pianoRollView.PixelsPerSecond;
            set => pianoRollView.PixelsPerSecond = value;
        }

        /// <summary>表示の設定（コピーを受け渡しする）。</summary>
        public PianoRollOptions Options
        {
            get => _options.Clone();
            set
            {
                _options = value?.Clone() ?? new PianoRollOptions();
                ApplyOptions();
            }
        }

        /// <summary>全画面・最大化していないときの、ウィンドウの位置と大きさ（設定に保存する）。</summary>
        public Rectangle NormalBounds => _fullScreen ? _boundsBeforeFullScreen
            : WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;

        /// <summary>表示言語に合わせて、文字列を設定し直す。</summary>
        public void ApplyTexts()
        {
            toolTip.SetToolTip(pianoRollView, Texts.PianoRollTip);
            toolTip.SetToolTip(overview, Texts.PianoRollOverviewBarTip);
            BuildCommands();
            _songTitle = null; // タイトルを付け直す
            UpdateTitle(null);
        }

        /// <summary>
        /// 表示するシーケンサーと、今の曲名を返す処理を渡す。表示している間は、設定の fps で状態を取り込んで描き直す。
        /// </summary>
        public void Attach(MidiSequencer sequencer, Func<string> songTitleProvider)
        {
            _sequencer = sequencer;
            _songTitleProvider = songTitleProvider;
            UpdateTimerState();
        }

        /// <summary>シーケンサーの状態を取り込んで描き直す。</summary>
        public void UpdateFrom(MidiSequencer sequencer, string songTitle)
        {
            UpdateTitle(songTitle);
            pianoRollView.UpdateFrom(sequencer);
            if (overview.Visible)
            {
                pianoRollView.GetVisibleRange(out TimeSpan viewStart, out TimeSpan viewEnd);
                overview.UpdateFrom(sequencer, viewStart, viewEnd, _options.ColorMode);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F11)
            {
                SetFullScreen(!_fullScreen);
                return true;
            }
            if (keyData == Keys.Escape && _fullScreen)
            {
                SetFullScreen(false);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // ユーザーが閉じたときは隠すだけにする（メニューの「ピアノロール」で、また表示できるように）
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                SetFullScreen(false);
                Hide();
            }
            base.OnFormClosing(e);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            UpdateTimerState();
        }

        /// <summary>表示している間だけ、描き直しのタイマーを動かす。</summary>
        private void UpdateTimerState()
        {
            _frameClock.Enabled = Visible && _sequencer != null;
        }

        private void frameClock_Tick(object sender, EventArgs e)
        {
            UpdateFrom(_sequencer, _songTitleProvider?.Invoke());
        }

        private void ApplyOptions()
        {
            _options.FramesPerSecond = Math.Max(PianoRollOptions.MinFramesPerSecond, Math.Min(PianoRollOptions.MaxFramesPerSecond, _options.FramesPerSecond));
            _frameClock.FramesPerSecond = _options.FramesPerSecond;
            pianoRollView.ApplyOptions(_options);
            overview.Visible = _options.ShowOverview;
            RefreshCommands();
        }

        private void ChangeOptions(Action<PianoRollOptions> change)
        {
            change(_options);
            ApplyOptions();
        }

        private void SetFullScreen(bool fullScreen)
        {
            if (fullScreen == _fullScreen)
            {
                return;
            }
            if (fullScreen)
            {
                _maximizedBeforeFullScreen = WindowState == FormWindowState.Maximized;
                _boundsBeforeFullScreen = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
                Rectangle screen = Screen.FromControl(this).Bounds;
                _fullScreen = true;
                toolStrip.Visible = false;
                WindowState = FormWindowState.Normal;
                FormBorderStyle = FormBorderStyle.None;
                Bounds = screen;
            }
            else
            {
                _fullScreen = false;
                FormBorderStyle = FormBorderStyle.Sizable;
                Bounds = _boundsBeforeFullScreen;
                if (_maximizedBeforeFullScreen)
                {
                    WindowState = FormWindowState.Maximized;
                }
                toolStrip.Visible = true;
            }
            RefreshCommands();
        }

        /// <summary>ツールバーと右クリックメニューの項目を作る。</summary>
        private void BuildCommands()
        {
            _commandRefreshers.Clear();
            DisposeItems(toolStrip.Items);
            DisposeItems(viewMenu.Items);

            var styleMenu = new ToolStripMenuItem(Texts.PianoRollStyleMenu);
            foreach (PianoRollStyle style in Enum.GetValues(typeof(PianoRollStyle)))
            {
                PianoRollStyle value = style;
                toolStrip.Items.Add(CreateButton(Texts.PianoRollStyleName(value), Texts.PianoRollStyleTip(value),
                    () => _options.Style == value, () => ChangeOptions(o => o.Style = value)));
                styleMenu.DropDownItems.Add(CreateMenuItem(Texts.PianoRollStyleName(value),
                    () => _options.Style == value, () => ChangeOptions(o => o.Style = value)));
            }
            toolStrip.Items.Add(new ToolStripSeparator());

            var colorButton = new ToolStripDropDownButton { ToolTipText = Texts.PianoRollColorTip };
            var colorMenu = new ToolStripMenuItem(Texts.PianoRollColorMenu);
            foreach (PianoRollColorMode mode in Enum.GetValues(typeof(PianoRollColorMode)))
            {
                PianoRollColorMode value = mode;
                colorButton.DropDownItems.Add(CreateMenuItem(Texts.PianoRollColorName(value),
                    () => _options.ColorMode == value, () => ChangeOptions(o => o.ColorMode = value)));
                colorMenu.DropDownItems.Add(CreateMenuItem(Texts.PianoRollColorName(value),
                    () => _options.ColorMode == value, () => ChangeOptions(o => o.ColorMode = value)));
            }
            _commandRefreshers.Add(() => colorButton.Text = $"{Texts.PianoRollColorLabel}: {Texts.PianoRollColorName(_options.ColorMode)}");
            toolStrip.Items.Add(colorButton);

            var namesButton = new ToolStripDropDownButton { ToolTipText = Texts.PianoRollNamesTip };
            var namesMenu = new ToolStripMenuItem(Texts.PianoRollNamesMenu);
            foreach (PianoRollNoteNames names in Enum.GetValues(typeof(PianoRollNoteNames)))
            {
                PianoRollNoteNames value = names;
                namesButton.DropDownItems.Add(CreateMenuItem(Texts.PianoRollNamesName(value),
                    () => _options.NoteNames == value, () => ChangeOptions(o => o.NoteNames = value)));
                namesMenu.DropDownItems.Add(CreateMenuItem(Texts.PianoRollNamesName(value),
                    () => _options.NoteNames == value, () => ChangeOptions(o => o.NoteNames = value)));
            }
            _commandRefreshers.Add(() => namesButton.Text = $"{Texts.PianoRollNamesLabel}: {Texts.PianoRollNamesName(_options.NoteNames)}");
            toolStrip.Items.Add(namesButton);

            var effectButton = new ToolStripDropDownButton { ToolTipText = Texts.PianoRollEffectsTip };
            var effectMenu = new ToolStripMenuItem(Texts.PianoRollEffectsMenu);
            for (int level = 0; level <= PianoRollOptions.MaxEffectLevel; level++)
            {
                int value = level;
                effectButton.DropDownItems.Add(CreateMenuItem(Texts.PianoRollEffectLevelName(value),
                    () => _options.EffectLevel == value, () => ChangeOptions(o => o.EffectLevel = value)));
                effectMenu.DropDownItems.Add(CreateMenuItem(Texts.PianoRollEffectLevelName(value),
                    () => _options.EffectLevel == value, () => ChangeOptions(o => o.EffectLevel = value)));
            }
            _commandRefreshers.Add(() => effectButton.Text = $"{Texts.PianoRollEffectsLabel}: {Texts.PianoRollEffectLevelShortName(_options.EffectLevel)}");
            toolStrip.Items.Add(effectButton);

            var fpsButton = new ToolStripDropDownButton { ToolTipText = Texts.PianoRollFpsTip };
            var fpsMenu = new ToolStripMenuItem(Texts.PianoRollFpsMenu);
            foreach (int fps in PianoRollOptions.FramesPerSecondChoices)
            {
                int value = fps;
                fpsButton.DropDownItems.Add(CreateMenuItem(value.ToString(),
                    () => _options.FramesPerSecond == value, () => ChangeOptions(o => o.FramesPerSecond = value)));
                fpsMenu.DropDownItems.Add(CreateMenuItem(value.ToString(),
                    () => _options.FramesPerSecond == value, () => ChangeOptions(o => o.FramesPerSecond = value)));
            }
            _commandRefreshers.Add(() => fpsButton.Text = $"FPS: {_options.FramesPerSecond}");
            toolStrip.Items.Add(fpsButton);
            toolStrip.Items.Add(new ToolStripSeparator());

            viewMenu.Items.Add(styleMenu);
            viewMenu.Items.Add(colorMenu);
            viewMenu.Items.Add(namesMenu);
            viewMenu.Items.Add(effectMenu);
            viewMenu.Items.Add(fpsMenu);
            viewMenu.Items.Add(new ToolStripSeparator());
            AddToggle(Texts.PianoRollFullKeyboard, Texts.PianoRollFullKeyboardTip, () => _options.FullKeyboard, o => o.FullKeyboard = !o.FullKeyboard);
            AddToggle(Texts.PianoRollOverview, Texts.PianoRollOverviewTip, () => _options.ShowOverview, o => o.ShowOverview = !o.ShowOverview);
            AddToggle(Texts.PianoRollLyrics, Texts.PianoRollLyricsTip, () => _options.ShowLyrics, o => o.ShowLyrics = !o.ShowLyrics);

            // GPU で描くかどうか（右クリックメニューだけ）。使えない環境では選べなくする
            bool gpuAvailable = PianoRollView.IsGpuAvailable;
            viewMenu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem gpuItem = CreateMenuItem(Texts.PianoRollGpu, () => _options.UseGpu && gpuAvailable, () => ChangeOptions(o => o.UseGpu = !o.UseGpu));
            gpuItem.Enabled = gpuAvailable;
            gpuItem.ToolTipText = gpuAvailable ? Texts.PianoRollGpuTip : Texts.PianoRollGpuUnavailable;
            viewMenu.Items.Add(gpuItem);

            ToolStripButton fullScreenButton = CreateButton(Texts.PianoRollFullScreen, Texts.PianoRollFullScreenTip,
                () => _fullScreen, () => SetFullScreen(!_fullScreen));
            fullScreenButton.Alignment = ToolStripItemAlignment.Right;
            toolStrip.Items.Add(fullScreenButton);
            AddZoomSlider();
            viewMenu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem fullScreenItem = CreateMenuItem(Texts.PianoRollFullScreen, () => _fullScreen, () => SetFullScreen(!_fullScreen));
            fullScreenItem.ShortcutKeyDisplayString = "F11";
            viewMenu.Items.Add(fullScreenItem);

            RefreshCommands();
        }

        /// <summary>
        /// ツールバーの右端（全画面の左）に、時間方向の倍率のスライダーを置く。「倍率 [スライダー] 100%」の順に並ぶ。
        /// </summary>
        /// <remarks>右寄せの項目は、先に足したものほど右に並ぶので、右から順に足す。</remarks>
        private void AddZoomSlider()
        {
            var slider = new ZoomSlider
            {
                Minimum = PianoRollView.MinPixelsPerSecond,
                Maximum = PianoRollView.MaxPixelsPerSecond,
                ResetValue = PianoRollView.DefaultPixelsPerSecond,
                Step = PianoRollView.ZoomStep,
                Value = pianoRollView.PixelsPerSecond,
                Size = new Size(ZoomSliderWidth, 22),
            };
            slider.ValueChanged += (sender, e) => pianoRollView.PixelsPerSecond = slider.Value;
            _zoomSlider = slider;

            var valueLabel = new ToolStripLabel { Alignment = ToolStripItemAlignment.Right, AutoSize = false, Width = 44, TextAlign = ContentAlignment.MiddleRight };
            var host = new ToolStripControlHost(slider) { Alignment = ToolStripItemAlignment.Right, AutoSize = false, Width = ZoomSliderWidth, ToolTipText = Texts.PianoRollZoomTip };
            var caption = new ToolStripLabel(Texts.PianoRollZoomLabel) { Alignment = ToolStripItemAlignment.Right };
            _zoomLabel = valueLabel;
            toolStrip.Items.Add(new ToolStripSeparator { Alignment = ToolStripItemAlignment.Right });
            toolStrip.Items.Add(valueLabel);
            toolStrip.Items.Add(host);
            toolStrip.Items.Add(caption);
            UpdateZoomDisplay();
        }

        /// <summary>倍率のスライダーと、その右の「100%」の表示を、今の倍率に合わせる。</summary>
        private void UpdateZoomDisplay()
        {
            if (_zoomSlider == null)
            {
                return;
            }
            double pixelsPerSecond = pianoRollView.PixelsPerSecond;
            _zoomSlider.Value = pixelsPerSecond;
            _zoomLabel.Text = Math.Round(pixelsPerSecond * 100 / PianoRollView.DefaultPixelsPerSecond) + "%";
        }

        private void pianoRollView_PixelsPerSecondChanged(object sender, EventArgs e)
        {
            UpdateZoomDisplay();
        }

        /// <summary>オン / オフを切り替える項目を、ツールバーと右クリックメニューの両方に足す。</summary>
        private void AddToggle(string text, string tip, Func<bool> isChecked, Action<PianoRollOptions> toggle)
        {
            toolStrip.Items.Add(CreateButton(text, tip, isChecked, () => ChangeOptions(toggle)));
            viewMenu.Items.Add(CreateMenuItem(text, isChecked, () => ChangeOptions(toggle)));
        }

        private ToolStripButton CreateButton(string text, string tip, Func<bool> isChecked, Action onClick)
        {
            var button = new ToolStripButton(WithoutMnemonic(text))
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                ToolTipText = tip,
            };
            button.Click += (sender, e) => onClick();
            _commandRefreshers.Add(() => button.Checked = isChecked());
            return button;
        }

        private ToolStripMenuItem CreateMenuItem(string text, Func<bool> isChecked, Action onClick)
        {
            var item = new ToolStripMenuItem(text);
            item.Click += (sender, e) => onClick();
            _commandRefreshers.Add(() => item.Checked = isChecked());
            return item;
        }

        private void RefreshCommands()
        {
            foreach (Action refresh in _commandRefreshers)
            {
                refresh();
            }
        }

        /// <summary>
        /// メニュー用の文字列から、アクセスキーの印（「(&amp;E)」や 1 つだけの「&amp;」）を取り除く。
        /// 「&amp;&amp;」（&amp; そのものを表す）は残す。
        /// </summary>
        private static string WithoutMnemonic(string text)
        {
            text = Regex.Replace(text, @"\(&[^&]\)", string.Empty);
            return Regex.Replace(text, "(?<!&)&(?!&)", string.Empty);
        }

        private static void DisposeItems(ToolStripItemCollection items)
        {
            var list = new List<ToolStripItem>();
            foreach (ToolStripItem item in items)
            {
                list.Add(item);
            }
            items.Clear();
            foreach (ToolStripItem item in list)
            {
                item.Dispose();
            }
        }

        private void UpdateTitle(string songTitle)
        {
            if (songTitle == _songTitle && Text.Length > 0)
            {
                return;
            }
            _songTitle = songTitle;
            Text = string.IsNullOrEmpty(songTitle) ? Texts.PianoRollTitle : $"{Texts.PianoRollTitle} - {songTitle}";
        }

        private void pianoRollView_SeekRequested(object sender, SeekRequestedEventArgs e)
        {
            SeekRequested?.Invoke(this, e);
        }
    }
}
