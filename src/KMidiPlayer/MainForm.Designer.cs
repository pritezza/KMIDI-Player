namespace KMidiPlayer
{
    partial class MainForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addFilesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addFolderMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileMenuSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.openPlaylistMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.savePlaylistMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.savePlaylistAsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileMenuSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.removeSelectedMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearPlaylistMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileMenuSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.playMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.playPauseMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stopMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.seekBackMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.seekForwardMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.playMenuSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.previousMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.nextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.playMenuSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.resetTempoKeyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.playMenuSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.sendResetMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.channelPanelMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.usedChannelsOnlyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.outputDeviceMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.noDeviceMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsMenuSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.resetTempoKeyPerSongMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.skipSilenceMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.topMostMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pianoRollMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tempoStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.settingsMenuSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.levelMeterSettingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.preferencesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.controlPanel = new System.Windows.Forms.Panel();
            this.previousButton = new KMidiPlayer.Controls.FlatButton();
            this.playPauseButton = new KMidiPlayer.Controls.FlatButton();
            this.stopButton = new KMidiPlayer.Controls.FlatButton();
            this.nextButton = new KMidiPlayer.Controls.FlatButton();
            this.pianoRollButton = new KMidiPlayer.Controls.FlatToggleButton();
            this.seekBar = new KMidiPlayer.Controls.SeekBar();
            this.timeLabel = new System.Windows.Forms.Label();
            this.tempoLabel = new System.Windows.Forms.Label();
            this.tempoUpDown = new KMidiPlayer.Controls.StepNumericUpDown();
            this.tempoUnitLabel = new System.Windows.Forms.Label();
            this.keyLabel = new System.Windows.Forms.Label();
            this.keyUpDown = new KMidiPlayer.Controls.StepNumericUpDown();
            this.modeLabel = new System.Windows.Forms.Label();
            this.modeComboBox = new KMidiPlayer.Controls.FlatComboBox();
            this.loopAButton = new KMidiPlayer.Controls.FlatToggleButton();
            this.loopBButton = new KMidiPlayer.Controls.FlatToggleButton();
            this.ratingFilterLabel = new System.Windows.Forms.Label();
            this.ratingFilterBox = new KMidiPlayer.Controls.RatingFilterBox();
            this.velocityCurveLabel = new System.Windows.Forms.Label();
            this.velocityCurveBox = new KMidiPlayer.Controls.VelocityCurveBox();
            this.resetLabel = new System.Windows.Forms.Label();
            this.resetComboBox = new KMidiPlayer.Controls.FlatComboBox();
            this.sendResetButton = new KMidiPlayer.Controls.FlatButton();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.playlistView = new KMidiPlayer.Controls.PlaylistView();
            this.numberColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.titleColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.durationColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ratingColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.fileNameColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.playlistContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pasteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.playlistMenuSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.removeFromListMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteFilesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.channelView = new KMidiPlayer.Controls.ChannelView();
            this.channelContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.blockPedalMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.blockPedalMenuSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.clearMuteSoloMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.resetOutputChannelsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.resetChannelTransposeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.resetChannelVolumesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.deviceStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.songInfoStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.positionStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.positionTimer = new System.Windows.Forms.Timer(this.components);
            this.channelTimer = new System.Windows.Forms.Timer(this.components);
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.menuStrip.SuspendLayout();
            this.controlPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tempoUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.keyUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.playlistContextMenu.SuspendLayout();
            this.channelContextMenu.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileMenuItem,
            this.playMenuItem,
            this.viewMenuItem,
            this.settingsMenuItem,
            this.helpMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(944, 24);
            this.menuStrip.TabIndex = 0;
            // 
            // fileMenuItem
            // 
            this.fileMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addFilesMenuItem,
            this.addFolderMenuItem,
            this.fileMenuSeparator1,
            this.openPlaylistMenuItem,
            this.savePlaylistMenuItem,
            this.savePlaylistAsMenuItem,
            this.fileMenuSeparator3,
            this.removeSelectedMenuItem,
            this.clearPlaylistMenuItem,
            this.fileMenuSeparator2,
            this.exitMenuItem});
            this.fileMenuItem.Name = "fileMenuItem";
            this.fileMenuItem.Size = new System.Drawing.Size(67, 20);
            this.fileMenuItem.Text = "ファイル(&F)";
            // 
            // addFilesMenuItem
            // 
            this.addFilesMenuItem.Name = "addFilesMenuItem";
            this.addFilesMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.addFilesMenuItem.Size = new System.Drawing.Size(221, 22);
            this.addFilesMenuItem.Text = "ファイルを追加(&A)...";
            this.addFilesMenuItem.Click += new System.EventHandler(this.addFilesMenuItem_Click);
            // 
            // addFolderMenuItem
            // 
            this.addFolderMenuItem.Name = "addFolderMenuItem";
            this.addFolderMenuItem.Size = new System.Drawing.Size(221, 22);
            this.addFolderMenuItem.Text = "フォルダを追加(&D)...";
            this.addFolderMenuItem.Click += new System.EventHandler(this.addFolderMenuItem_Click);
            // 
            // fileMenuSeparator1
            // 
            this.fileMenuSeparator1.Name = "fileMenuSeparator1";
            this.fileMenuSeparator1.Size = new System.Drawing.Size(218, 6);
            // 
            // openPlaylistMenuItem
            // 
            this.openPlaylistMenuItem.Name = "openPlaylistMenuItem";
            this.openPlaylistMenuItem.Size = new System.Drawing.Size(221, 22);
            this.openPlaylistMenuItem.Text = "プレイリストを開く(&L)...";
            this.openPlaylistMenuItem.Click += new System.EventHandler(this.openPlaylistMenuItem_Click);
            // 
            // savePlaylistMenuItem
            // 
            this.savePlaylistMenuItem.Name = "savePlaylistMenuItem";
            this.savePlaylistMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.savePlaylistMenuItem.Size = new System.Drawing.Size(221, 22);
            this.savePlaylistMenuItem.Text = "プレイリストを保存(&S)";
            this.savePlaylistMenuItem.Click += new System.EventHandler(this.savePlaylistMenuItem_Click);
            //
            // savePlaylistAsMenuItem
            //
            this.savePlaylistAsMenuItem.Name = "savePlaylistAsMenuItem";
            this.savePlaylistAsMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
            | System.Windows.Forms.Keys.S)));
            this.savePlaylistAsMenuItem.Size = new System.Drawing.Size(221, 22);
            this.savePlaylistAsMenuItem.Text = "プレイリストに名前を付けて保存(&E)...";
            this.savePlaylistAsMenuItem.Click += new System.EventHandler(this.savePlaylistAsMenuItem_Click);
            // 
            // fileMenuSeparator3
            // 
            this.fileMenuSeparator3.Name = "fileMenuSeparator3";
            this.fileMenuSeparator3.Size = new System.Drawing.Size(218, 6);
            // 
            // removeSelectedMenuItem
            // 
            this.removeSelectedMenuItem.Name = "removeSelectedMenuItem";
            this.removeSelectedMenuItem.ShortcutKeyDisplayString = "Del";
            this.removeSelectedMenuItem.Size = new System.Drawing.Size(221, 22);
            this.removeSelectedMenuItem.Text = "選択した曲をリストから削除(&R)";
            this.removeSelectedMenuItem.Click += new System.EventHandler(this.removeSelectedMenuItem_Click);
            // 
            // clearPlaylistMenuItem
            // 
            this.clearPlaylistMenuItem.Name = "clearPlaylistMenuItem";
            this.clearPlaylistMenuItem.Size = new System.Drawing.Size(221, 22);
            this.clearPlaylistMenuItem.Text = "プレイリストを消去(&C)";
            this.clearPlaylistMenuItem.Click += new System.EventHandler(this.clearPlaylistMenuItem_Click);
            // 
            // fileMenuSeparator2
            // 
            this.fileMenuSeparator2.Name = "fileMenuSeparator2";
            this.fileMenuSeparator2.Size = new System.Drawing.Size(218, 6);
            // 
            // exitMenuItem
            // 
            this.exitMenuItem.Name = "exitMenuItem";
            this.exitMenuItem.Size = new System.Drawing.Size(221, 22);
            this.exitMenuItem.Text = "終了(&X)";
            this.exitMenuItem.Click += new System.EventHandler(this.exitMenuItem_Click);
            // 
            // playMenuItem
            // 
            this.playMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.playPauseMenuItem,
            this.stopMenuItem,
            this.seekBackMenuItem,
            this.seekForwardMenuItem,
            this.playMenuSeparator1,
            this.previousMenuItem,
            this.nextMenuItem,
            this.playMenuSeparator2,
            this.resetTempoKeyMenuItem,
            this.playMenuSeparator3,
            this.sendResetMenuItem});
            this.playMenuItem.Name = "playMenuItem";
            this.playMenuItem.Size = new System.Drawing.Size(58, 20);
            this.playMenuItem.Text = "再生(&P)";
            // 
            // playPauseMenuItem
            // 
            this.playPauseMenuItem.Name = "playPauseMenuItem";
            this.playPauseMenuItem.ShortcutKeyDisplayString = "Space";
            this.playPauseMenuItem.Size = new System.Drawing.Size(194, 22);
            this.playPauseMenuItem.Text = "再生 / 一時停止(&P)";
            this.playPauseMenuItem.Click += new System.EventHandler(this.playPause_Click);
            // 
            // stopMenuItem
            // 
            this.stopMenuItem.Name = "stopMenuItem";
            this.stopMenuItem.Size = new System.Drawing.Size(194, 22);
            this.stopMenuItem.Text = "停止(&S)";
            this.stopMenuItem.Click += new System.EventHandler(this.stop_Click);
            //
            // seekBackMenuItem
            //
            this.seekBackMenuItem.Name = "seekBackMenuItem";
            this.seekBackMenuItem.ShortcutKeyDisplayString = "←";
            this.seekBackMenuItem.Size = new System.Drawing.Size(194, 22);
            this.seekBackMenuItem.Text = "5 秒戻る(&J)";
            this.seekBackMenuItem.Click += new System.EventHandler(this.seekBack_Click);
            //
            // seekForwardMenuItem
            //
            this.seekForwardMenuItem.Name = "seekForwardMenuItem";
            this.seekForwardMenuItem.ShortcutKeyDisplayString = "→";
            this.seekForwardMenuItem.Size = new System.Drawing.Size(194, 22);
            this.seekForwardMenuItem.Text = "5 秒進む(&K)";
            this.seekForwardMenuItem.Click += new System.EventHandler(this.seekForward_Click);
            //
            // playMenuSeparator1
            // 
            this.playMenuSeparator1.Name = "playMenuSeparator1";
            this.playMenuSeparator1.Size = new System.Drawing.Size(191, 6);
            // 
            // previousMenuItem
            // 
            this.previousMenuItem.Name = "previousMenuItem";
            this.previousMenuItem.ShortcutKeyDisplayString = "Ctrl+←";
            this.previousMenuItem.Size = new System.Drawing.Size(194, 22);
            this.previousMenuItem.Text = "前の曲(&B)";
            this.previousMenuItem.Click += new System.EventHandler(this.previous_Click);
            // 
            // nextMenuItem
            // 
            this.nextMenuItem.Name = "nextMenuItem";
            this.nextMenuItem.ShortcutKeyDisplayString = "Ctrl+→";
            this.nextMenuItem.Size = new System.Drawing.Size(194, 22);
            this.nextMenuItem.Text = "次の曲(&N)";
            this.nextMenuItem.Click += new System.EventHandler(this.next_Click);
            // 
            // playMenuSeparator2
            // 
            this.playMenuSeparator2.Name = "playMenuSeparator2";
            this.playMenuSeparator2.Size = new System.Drawing.Size(191, 6);
            // 
            // resetTempoKeyMenuItem
            // 
            this.resetTempoKeyMenuItem.Name = "resetTempoKeyMenuItem";
            this.resetTempoKeyMenuItem.Size = new System.Drawing.Size(194, 22);
            this.resetTempoKeyMenuItem.Text = "テンポとキーを元に戻す(&T)";
            this.resetTempoKeyMenuItem.Click += new System.EventHandler(this.resetTempoKeyMenuItem_Click);
            // 
            // playMenuSeparator3
            // 
            this.playMenuSeparator3.Name = "playMenuSeparator3";
            this.playMenuSeparator3.Size = new System.Drawing.Size(191, 6);
            // 
            // sendResetMenuItem
            // 
            this.sendResetMenuItem.Name = "sendResetMenuItem";
            this.sendResetMenuItem.Size = new System.Drawing.Size(194, 22);
            this.sendResetMenuItem.Text = "リセットを送信(&R)";
            this.sendResetMenuItem.Click += new System.EventHandler(this.sendReset_Click);
            // 
            // viewMenuItem
            // 
            this.viewMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.channelPanelMenuItem,
            this.usedChannelsOnlyMenuItem,
            this.pianoRollMenuItem,
            this.topMostMenuItem});
            this.viewMenuItem.Name = "viewMenuItem";
            this.viewMenuItem.Size = new System.Drawing.Size(58, 20);
            this.viewMenuItem.Text = "表示(&V)";
            // 
            // channelPanelMenuItem
            // 
            this.channelPanelMenuItem.Checked = true;
            this.channelPanelMenuItem.CheckOnClick = true;
            this.channelPanelMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.channelPanelMenuItem.Name = "channelPanelMenuItem";
            this.channelPanelMenuItem.Size = new System.Drawing.Size(165, 22);
            this.channelPanelMenuItem.Text = "チャンネルパネル(&C)";
            this.channelPanelMenuItem.CheckedChanged += new System.EventHandler(this.channelPanelMenuItem_CheckedChanged);
            //
            // usedChannelsOnlyMenuItem
            //
            this.usedChannelsOnlyMenuItem.Checked = true;
            this.usedChannelsOnlyMenuItem.CheckOnClick = true;
            this.usedChannelsOnlyMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.usedChannelsOnlyMenuItem.Name = "usedChannelsOnlyMenuItem";
            this.usedChannelsOnlyMenuItem.Size = new System.Drawing.Size(165, 22);
            this.usedChannelsOnlyMenuItem.Text = "データのあるチャンネルだけ表示(&U)";
            this.usedChannelsOnlyMenuItem.CheckedChanged += new System.EventHandler(this.usedChannelsOnlyMenuItem_CheckedChanged);
            //
            // pianoRollMenuItem
            //
            this.pianoRollMenuItem.CheckOnClick = true;
            this.pianoRollMenuItem.Name = "pianoRollMenuItem";
            this.pianoRollMenuItem.Size = new System.Drawing.Size(165, 22);
            this.pianoRollMenuItem.Text = "ピアノロール(&R)";
            this.pianoRollMenuItem.CheckedChanged += new System.EventHandler(this.pianoRollMenuItem_CheckedChanged);
            //
            // topMostMenuItem
            //
            this.topMostMenuItem.CheckOnClick = true;
            this.topMostMenuItem.Name = "topMostMenuItem";
            this.topMostMenuItem.Size = new System.Drawing.Size(165, 22);
            this.topMostMenuItem.Text = "常に手前に表示(&T)";
            this.topMostMenuItem.CheckedChanged += new System.EventHandler(this.topMostMenuItem_CheckedChanged);
            //
            // settingsMenuItem
            // 
            this.settingsMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.outputDeviceMenuItem,
            this.settingsMenuSeparator1,
            this.resetTempoKeyPerSongMenuItem,
            this.skipSilenceMenuItem,
            this.settingsMenuSeparator2,
            this.levelMeterSettingsMenuItem,
            this.preferencesMenuItem});
            this.settingsMenuItem.Name = "settingsMenuItem";
            this.settingsMenuItem.Size = new System.Drawing.Size(57, 20);
            this.settingsMenuItem.Text = "設定(&S)";
            // 
            // outputDeviceMenuItem
            // 
            this.outputDeviceMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.noDeviceMenuItem});
            this.outputDeviceMenuItem.Name = "outputDeviceMenuItem";
            this.outputDeviceMenuItem.Size = new System.Drawing.Size(275, 22);
            this.outputDeviceMenuItem.Text = "出力デバイス(&O)";
            this.outputDeviceMenuItem.DropDownOpening += new System.EventHandler(this.outputDeviceMenuItem_DropDownOpening);
            // 
            // noDeviceMenuItem
            // 
            this.noDeviceMenuItem.Enabled = false;
            this.noDeviceMenuItem.Name = "noDeviceMenuItem";
            this.noDeviceMenuItem.Size = new System.Drawing.Size(192, 22);
            this.noDeviceMenuItem.Text = "（デバイスがありません）";
            // 
            // settingsMenuSeparator1
            // 
            this.settingsMenuSeparator1.Name = "settingsMenuSeparator1";
            this.settingsMenuSeparator1.Size = new System.Drawing.Size(272, 6);
            // 
            // resetTempoKeyPerSongMenuItem
            // 
            this.resetTempoKeyPerSongMenuItem.Checked = true;
            this.resetTempoKeyPerSongMenuItem.CheckOnClick = true;
            this.resetTempoKeyPerSongMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.resetTempoKeyPerSongMenuItem.Name = "resetTempoKeyPerSongMenuItem";
            this.resetTempoKeyPerSongMenuItem.Size = new System.Drawing.Size(275, 22);
            this.resetTempoKeyPerSongMenuItem.Text = "曲を切り替えたらテンポとキーを元に戻す(&K)";
            //
            // skipSilenceMenuItem
            //
            this.skipSilenceMenuItem.Checked = true;
            this.skipSilenceMenuItem.CheckOnClick = true;
            this.skipSilenceMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.skipSilenceMenuItem.Name = "skipSilenceMenuItem";
            this.skipSilenceMenuItem.Size = new System.Drawing.Size(275, 22);
            this.skipSilenceMenuItem.Text = "曲の頭の無音をとばす(&L)";
            this.skipSilenceMenuItem.CheckedChanged += new System.EventHandler(this.skipSilenceMenuItem_CheckedChanged);
            //
            // settingsMenuSeparator2
            //
            this.settingsMenuSeparator2.Name = "settingsMenuSeparator2";
            this.settingsMenuSeparator2.Size = new System.Drawing.Size(272, 6);
            //
            // levelMeterSettingsMenuItem
            //
            this.levelMeterSettingsMenuItem.Name = "levelMeterSettingsMenuItem";
            this.levelMeterSettingsMenuItem.Size = new System.Drawing.Size(275, 22);
            this.levelMeterSettingsMenuItem.Text = "レベルメーターの設定(&M)...";
            this.levelMeterSettingsMenuItem.Click += new System.EventHandler(this.levelMeterSettingsMenuItem_Click);
            //
            // preferencesMenuItem
            //
            this.preferencesMenuItem.Name = "preferencesMenuItem";
            this.preferencesMenuItem.Size = new System.Drawing.Size(275, 22);
            this.preferencesMenuItem.Text = "環境設定(&P)...";
            this.preferencesMenuItem.Click += new System.EventHandler(this.preferencesMenuItem_Click);
            // 
            // helpMenuItem
            // 
            this.helpMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutMenuItem});
            this.helpMenuItem.Name = "helpMenuItem";
            this.helpMenuItem.Size = new System.Drawing.Size(65, 20);
            this.helpMenuItem.Text = "ヘルプ(&H)";
            // 
            // aboutMenuItem
            // 
            this.aboutMenuItem.Name = "aboutMenuItem";
            this.aboutMenuItem.Size = new System.Drawing.Size(167, 22);
            this.aboutMenuItem.Text = "バージョン情報(&A)...";
            this.aboutMenuItem.Click += new System.EventHandler(this.aboutMenuItem_Click);
            // 
            // controlPanel
            // 
            this.controlPanel.Controls.Add(this.previousButton);
            this.controlPanel.Controls.Add(this.playPauseButton);
            this.controlPanel.Controls.Add(this.stopButton);
            this.controlPanel.Controls.Add(this.nextButton);
            this.controlPanel.Controls.Add(this.pianoRollButton);
            this.controlPanel.Controls.Add(this.seekBar);
            this.controlPanel.Controls.Add(this.timeLabel);
            this.controlPanel.Controls.Add(this.tempoLabel);
            this.controlPanel.Controls.Add(this.tempoUpDown);
            this.controlPanel.Controls.Add(this.tempoUnitLabel);
            this.controlPanel.Controls.Add(this.keyLabel);
            this.controlPanel.Controls.Add(this.keyUpDown);
            this.controlPanel.Controls.Add(this.modeLabel);
            this.controlPanel.Controls.Add(this.modeComboBox);
            this.controlPanel.Controls.Add(this.loopAButton);
            this.controlPanel.Controls.Add(this.loopBButton);
            this.controlPanel.Controls.Add(this.ratingFilterLabel);
            this.controlPanel.Controls.Add(this.ratingFilterBox);
            this.controlPanel.Controls.Add(this.velocityCurveLabel);
            this.controlPanel.Controls.Add(this.velocityCurveBox);
            this.controlPanel.Controls.Add(this.resetLabel);
            this.controlPanel.Controls.Add(this.resetComboBox);
            this.controlPanel.Controls.Add(this.sendResetButton);
            this.controlPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.controlPanel.Location = new System.Drawing.Point(0, 24);
            this.controlPanel.Name = "controlPanel";
            this.controlPanel.Size = new System.Drawing.Size(944, 76);
            this.controlPanel.TabIndex = 1;
            // 
            // previousButton
            // 
            this.previousButton.Font = new System.Drawing.Font("Segoe MDL2 Assets", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.previousButton.Location = new System.Drawing.Point(8, 8);
            this.previousButton.Name = "previousButton";
            this.previousButton.Size = new System.Drawing.Size(36, 30);
            this.previousButton.TabIndex = 0;
            this.previousButton.Text = "";
            this.previousButton.Kind = KMidiPlayer.Controls.FlatButtonKind.Subtle;
            this.previousButton.Click += new System.EventHandler(this.previous_Click);
            // 
            // playPauseButton
            // 
            this.playPauseButton.Font = new System.Drawing.Font("Segoe MDL2 Assets", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.playPauseButton.Location = new System.Drawing.Point(48, 8);
            this.playPauseButton.Name = "playPauseButton";
            this.playPauseButton.Size = new System.Drawing.Size(36, 30);
            this.playPauseButton.TabIndex = 1;
            this.playPauseButton.Text = "";
            this.playPauseButton.Kind = KMidiPlayer.Controls.FlatButtonKind.Accent;
            this.playPauseButton.Round = true;
            this.playPauseButton.Click += new System.EventHandler(this.playPause_Click);
            // 
            // stopButton
            // 
            this.stopButton.Font = new System.Drawing.Font("Segoe MDL2 Assets", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.stopButton.Location = new System.Drawing.Point(88, 8);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(36, 30);
            this.stopButton.TabIndex = 2;
            this.stopButton.Text = "";
            this.stopButton.Kind = KMidiPlayer.Controls.FlatButtonKind.Subtle;
            this.stopButton.Click += new System.EventHandler(this.stop_Click);
            // 
            // nextButton
            // 
            this.nextButton.Font = new System.Drawing.Font("Segoe MDL2 Assets", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nextButton.Location = new System.Drawing.Point(128, 8);
            this.nextButton.Name = "nextButton";
            this.nextButton.Size = new System.Drawing.Size(36, 30);
            this.nextButton.TabIndex = 3;
            this.nextButton.Text = "";
            this.nextButton.Kind = KMidiPlayer.Controls.FlatButtonKind.Subtle;
            this.nextButton.Click += new System.EventHandler(this.next_Click);
            //
            // pianoRollButton
            //
            this.pianoRollButton.Appearance = System.Windows.Forms.Appearance.Button;
            this.pianoRollButton.AutoCheck = false;
            this.pianoRollButton.Font = new System.Drawing.Font("Segoe MDL2 Assets", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.pianoRollButton.Location = new System.Drawing.Point(172, 8);
            this.pianoRollButton.Name = "pianoRollButton";
            this.pianoRollButton.Size = new System.Drawing.Size(36, 30);
            this.pianoRollButton.TabIndex = 22;
            this.pianoRollButton.Text = "";
            this.pianoRollButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.pianoRollButton.Kind = KMidiPlayer.Controls.FlatButtonKind.Subtle;
            this.pianoRollButton.Click += new System.EventHandler(this.pianoRollButton_Click);
            //
            // seekBar
            // 
            this.seekBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.seekBar.Location = new System.Drawing.Point(216, 13);
            this.seekBar.Name = "seekBar";
            this.seekBar.Size = new System.Drawing.Size(604, 20);
            this.seekBar.TabIndex = 4;
            this.seekBar.SeekRequested += new System.EventHandler(this.seekBar_SeekRequested);
            // 
            // timeLabel
            // 
            this.timeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.timeLabel.Location = new System.Drawing.Point(828, 8);
            this.timeLabel.Name = "timeLabel";
            this.timeLabel.Size = new System.Drawing.Size(108, 30);
            this.timeLabel.TabIndex = 5;
            this.timeLabel.Text = "00:00 / 00:00";
            this.timeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // tempoLabel
            // 
            this.tempoLabel.Location = new System.Drawing.Point(8, 45);
            this.tempoLabel.Name = "tempoLabel";
            this.tempoLabel.Size = new System.Drawing.Size(44, 20);
            this.tempoLabel.TabIndex = 6;
            this.tempoLabel.Text = "テンポ:";
            this.tempoLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tempoUpDown
            // 
            this.tempoUpDown.Increment = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.tempoUpDown.Location = new System.Drawing.Point(54, 45);
            this.tempoUpDown.Maximum = new decimal(new int[] {
            400,
            0,
            0,
            0});
            this.tempoUpDown.Minimum = new decimal(new int[] {
            25,
            0,
            0,
            0});
            this.tempoUpDown.Name = "tempoUpDown";
            this.tempoUpDown.Size = new System.Drawing.Size(52, 19);
            this.tempoUpDown.TabIndex = 7;
            this.tempoUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tempoUpDown.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.tempoUpDown.ValueChanged += new System.EventHandler(this.tempoUpDown_ValueChanged);
            // 
            // tempoUnitLabel
            // 
            this.tempoUnitLabel.Location = new System.Drawing.Point(106, 45);
            this.tempoUnitLabel.Name = "tempoUnitLabel";
            this.tempoUnitLabel.Size = new System.Drawing.Size(18, 20);
            this.tempoUnitLabel.TabIndex = 8;
            this.tempoUnitLabel.Text = "%";
            this.tempoUnitLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // keyLabel
            // 
            this.keyLabel.Location = new System.Drawing.Point(132, 45);
            this.keyLabel.Name = "keyLabel";
            this.keyLabel.Size = new System.Drawing.Size(32, 20);
            this.keyLabel.TabIndex = 9;
            this.keyLabel.Text = "キー:";
            this.keyLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // keyUpDown
            // 
            this.keyUpDown.Location = new System.Drawing.Point(166, 45);
            this.keyUpDown.Maximum = new decimal(new int[] {
            12,
            0,
            0,
            0});
            this.keyUpDown.Minimum = new decimal(new int[] {
            12,
            0,
            0,
            -2147483648});
            this.keyUpDown.Name = "keyUpDown";
            this.keyUpDown.Size = new System.Drawing.Size(44, 19);
            this.keyUpDown.TabIndex = 10;
            this.keyUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.keyUpDown.ValueChanged += new System.EventHandler(this.keyUpDown_ValueChanged);
            // 
            // modeLabel
            // 
            this.modeLabel.Location = new System.Drawing.Point(220, 45);
            this.modeLabel.Name = "modeLabel";
            this.modeLabel.Size = new System.Drawing.Size(44, 20);
            this.modeLabel.TabIndex = 11;
            this.modeLabel.Text = "モード:";
            this.modeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // modeComboBox
            // 
            this.modeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.modeComboBox.FormattingEnabled = true;
            this.modeComboBox.Items.AddRange(new object[] {
            "1曲再生",
            "1曲リピート",
            "全曲再生",
            "全曲リピート",
            "シャッフル"});
            this.modeComboBox.Location = new System.Drawing.Point(266, 45);
            this.modeComboBox.Name = "modeComboBox";
            this.modeComboBox.Size = new System.Drawing.Size(100, 20);
            this.modeComboBox.TabIndex = 12;
            this.modeComboBox.SelectedIndexChanged += new System.EventHandler(this.modeComboBox_SelectedIndexChanged);
            // 
            // loopAButton
            // 
            this.loopAButton.Appearance = System.Windows.Forms.Appearance.Button;
            this.loopAButton.AutoCheck = false;
            this.loopAButton.Location = new System.Drawing.Point(376, 43);
            this.loopAButton.Name = "loopAButton";
            this.loopAButton.Size = new System.Drawing.Size(32, 24);
            this.loopAButton.TabIndex = 13;
            this.loopAButton.Text = "A";
            this.loopAButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.loopAButton.Click += new System.EventHandler(this.loopAButton_Click);
            // 
            // loopBButton
            // 
            this.loopBButton.Appearance = System.Windows.Forms.Appearance.Button;
            this.loopBButton.AutoCheck = false;
            this.loopBButton.Location = new System.Drawing.Point(410, 43);
            this.loopBButton.Name = "loopBButton";
            this.loopBButton.Size = new System.Drawing.Size(32, 24);
            this.loopBButton.TabIndex = 14;
            this.loopBButton.Text = "B";
            this.loopBButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.loopBButton.Click += new System.EventHandler(this.loopBButton_Click);
            //
            // ratingFilterLabel
            //
            this.ratingFilterLabel.Location = new System.Drawing.Point(450, 45);
            this.ratingFilterLabel.Name = "ratingFilterLabel";
            this.ratingFilterLabel.Size = new System.Drawing.Size(40, 20);
            this.ratingFilterLabel.TabIndex = 15;
            this.ratingFilterLabel.Text = "評価:";
            this.ratingFilterLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // ratingFilterBox
            //
            this.ratingFilterBox.Location = new System.Drawing.Point(490, 45);
            this.ratingFilterBox.Name = "ratingFilterBox";
            this.ratingFilterBox.Size = new System.Drawing.Size(112, 20);
            this.ratingFilterBox.TabIndex = 16;
            this.ratingFilterBox.ValueChanged += new System.EventHandler(this.ratingFilterBox_ValueChanged);
            //
            // velocityCurveLabel
            //
            this.velocityCurveLabel.Location = new System.Drawing.Point(610, 45);
            this.velocityCurveLabel.Name = "velocityCurveLabel";
            this.velocityCurveLabel.Size = new System.Drawing.Size(60, 20);
            this.velocityCurveLabel.TabIndex = 17;
            this.velocityCurveLabel.Text = "ベロシティ:";
            this.velocityCurveLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // velocityCurveBox
            //
            this.velocityCurveBox.Location = new System.Drawing.Point(670, 43);
            this.velocityCurveBox.Name = "velocityCurveBox";
            this.velocityCurveBox.Size = new System.Drawing.Size(72, 24);
            this.velocityCurveBox.TabIndex = 18;
            this.velocityCurveBox.ValueChanged += new System.EventHandler(this.velocityCurveBox_ValueChanged);
            // 
            // resetLabel
            // 
            this.resetLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.resetLabel.Location = new System.Drawing.Point(744, 45);
            this.resetLabel.Name = "resetLabel";
            this.resetLabel.Size = new System.Drawing.Size(44, 20);
            this.resetLabel.TabIndex = 19;
            this.resetLabel.Text = "リセット:";
            this.resetLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // resetComboBox
            // 
            this.resetComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.resetComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.resetComboBox.FormattingEnabled = true;
            this.resetComboBox.Items.AddRange(new object[] {
            "送らない",
            "GM",
            "GS",
            "XG"});
            this.resetComboBox.Location = new System.Drawing.Point(792, 45);
            this.resetComboBox.Name = "resetComboBox";
            this.resetComboBox.Size = new System.Drawing.Size(84, 20);
            this.resetComboBox.TabIndex = 20;
            this.resetComboBox.SelectedIndexChanged += new System.EventHandler(this.resetComboBox_SelectedIndexChanged);
            // 
            // sendResetButton
            // 
            this.sendResetButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.sendResetButton.Location = new System.Drawing.Point(880, 43);
            this.sendResetButton.Name = "sendResetButton";
            this.sendResetButton.Size = new System.Drawing.Size(56, 24);
            this.sendResetButton.TabIndex = 21;
            this.sendResetButton.Text = "送信";
            this.sendResetButton.Click += new System.EventHandler(this.sendReset_Click);
            // 
            // splitContainer
            // 
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer.Location = new System.Drawing.Point(0, 100);
            this.splitContainer.Name = "splitContainer";
            this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.playlistView);
            this.splitContainer.Panel1MinSize = 60;
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.channelView);
            this.splitContainer.Panel2MinSize = 100;
            this.splitContainer.Size = new System.Drawing.Size(944, 578);
            this.splitContainer.SplitterDistance = 284;
            this.splitContainer.TabIndex = 2;
            // 
            // playlistView
            // 
            this.playlistView.AllowDrop = true;
            this.playlistView.ContextMenuStrip = this.playlistContextMenu;
            this.playlistView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.numberColumn,
            this.titleColumn,
            this.durationColumn,
            this.ratingColumn,
            this.fileNameColumn});
            this.playlistView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.playlistView.FullRowSelect = true;
            this.playlistView.HideSelection = false;
            this.playlistView.Location = new System.Drawing.Point(0, 0);
            this.playlistView.Name = "playlistView";
            this.playlistView.RatingColumnIndex = 3;
            this.playlistView.ShowItemToolTips = true;
            this.playlistView.Size = new System.Drawing.Size(944, 284);
            this.playlistView.TabIndex = 0;
            this.playlistView.UseCompatibleStateImageBehavior = false;
            this.playlistView.View = System.Windows.Forms.View.Details;
            this.playlistView.ItemActivate += new System.EventHandler(this.playlistView_ItemActivate);
            this.playlistView.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.playlistView_ItemDrag);
            this.playlistView.DragDrop += new System.Windows.Forms.DragEventHandler(this.playlistView_DragDrop);
            this.playlistView.DragEnter += new System.Windows.Forms.DragEventHandler(this.playlistView_DragOver);
            this.playlistView.DragOver += new System.Windows.Forms.DragEventHandler(this.playlistView_DragOver);
            this.playlistView.DragLeave += new System.EventHandler(this.playlistView_DragLeave);
            this.playlistView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.playlistView_KeyDown);
            this.playlistView.RatingChanged += new System.EventHandler<KMidiPlayer.Controls.RatingEventArgs>(this.playlistView_RatingChanged);
            // 
            // numberColumn
            // 
            this.numberColumn.Text = "#";
            this.numberColumn.Width = 56;
            // 
            // titleColumn
            // 
            this.titleColumn.Text = "タイトル";
            this.titleColumn.Width = 320;
            // 
            // durationColumn
            // 
            this.durationColumn.Text = "時間";
            this.durationColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.durationColumn.Width = 64;
            //
            // ratingColumn
            //
            this.ratingColumn.Text = "評価";
            this.ratingColumn.Width = 80;
            // 
            // fileNameColumn
            // 
            this.fileNameColumn.Text = "ファイル名";
            this.fileNameColumn.Width = 320;
            // 
            // channelView
            // 
            this.channelView.ContextMenuStrip = this.channelContextMenu;
            this.channelView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.channelView.Location = new System.Drawing.Point(0, 0);
            this.channelView.Name = "channelView";
            this.channelView.Size = new System.Drawing.Size(944, 290);
            this.channelView.TabIndex = 0;
            this.channelView.MuteClicked += new System.EventHandler<KMidiPlayer.Controls.ChannelEventArgs>(this.channelView_MuteClicked);
            this.channelView.SoloClicked += new System.EventHandler<KMidiPlayer.Controls.ChannelEventArgs>(this.channelView_SoloClicked);
            this.channelView.NotePressed += new System.EventHandler<KMidiPlayer.Controls.KeyboardNoteEventArgs>(this.channelView_NotePressed);
            this.channelView.NoteReleased += new System.EventHandler<KMidiPlayer.Controls.KeyboardNoteEventArgs>(this.channelView_NoteReleased);
            this.channelView.OutputChannelChanged += new System.EventHandler<KMidiPlayer.Controls.OutputChannelEventArgs>(this.channelView_OutputChannelChanged);
            this.channelView.TransposeChanged += new System.EventHandler<KMidiPlayer.Controls.ChannelValueEventArgs>(this.channelView_TransposeChanged);
            this.channelView.OctaveChanged += new System.EventHandler<KMidiPlayer.Controls.ChannelValueEventArgs>(this.channelView_OctaveChanged);
            this.channelView.VolumeChanged += new System.EventHandler<KMidiPlayer.Controls.ChannelValueEventArgs>(this.channelView_VolumeChanged);
            this.channelView.PedalClicked += new System.EventHandler<KMidiPlayer.Controls.PedalEventArgs>(this.channelView_PedalClicked);
            //
            // playlistContextMenu
            //
            this.playlistContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyMenuItem,
            this.cutMenuItem,
            this.pasteMenuItem,
            this.playlistMenuSeparator,
            this.removeFromListMenuItem,
            this.deleteFilesMenuItem});
            this.playlistContextMenu.Name = "playlistContextMenu";
            this.playlistContextMenu.Size = new System.Drawing.Size(250, 120);
            this.playlistContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.playlistContextMenu_Opening);
            //
            // copyMenuItem
            //
            this.copyMenuItem.Name = "copyMenuItem";
            this.copyMenuItem.ShortcutKeyDisplayString = "Ctrl+C";
            this.copyMenuItem.Size = new System.Drawing.Size(249, 22);
            this.copyMenuItem.Text = "コピー(&C)";
            this.copyMenuItem.Click += new System.EventHandler(this.copyMenuItem_Click);
            //
            // cutMenuItem
            //
            this.cutMenuItem.Name = "cutMenuItem";
            this.cutMenuItem.ShortcutKeyDisplayString = "Ctrl+X";
            this.cutMenuItem.Size = new System.Drawing.Size(249, 22);
            this.cutMenuItem.Text = "カット(&T)";
            this.cutMenuItem.Click += new System.EventHandler(this.cutMenuItem_Click);
            //
            // pasteMenuItem
            //
            this.pasteMenuItem.Name = "pasteMenuItem";
            this.pasteMenuItem.ShortcutKeyDisplayString = "Ctrl+V";
            this.pasteMenuItem.Size = new System.Drawing.Size(249, 22);
            this.pasteMenuItem.Text = "貼り付け(&P)";
            this.pasteMenuItem.Click += new System.EventHandler(this.pasteMenuItem_Click);
            //
            // playlistMenuSeparator
            //
            this.playlistMenuSeparator.Name = "playlistMenuSeparator";
            this.playlistMenuSeparator.Size = new System.Drawing.Size(246, 6);
            //
            // removeFromListMenuItem
            //
            this.removeFromListMenuItem.Name = "removeFromListMenuItem";
            this.removeFromListMenuItem.ShortcutKeyDisplayString = "Del";
            this.removeFromListMenuItem.Size = new System.Drawing.Size(249, 22);
            this.removeFromListMenuItem.Text = "リストから削除(&R)";
            this.removeFromListMenuItem.Click += new System.EventHandler(this.removeSelectedMenuItem_Click);
            //
            // deleteFilesMenuItem
            //
            this.deleteFilesMenuItem.Name = "deleteFilesMenuItem";
            this.deleteFilesMenuItem.ShortcutKeyDisplayString = "Shift+Del";
            this.deleteFilesMenuItem.Size = new System.Drawing.Size(249, 22);
            this.deleteFilesMenuItem.Text = "リストとファイルを削除(&D)...";
            this.deleteFilesMenuItem.Click += new System.EventHandler(this.deleteFilesMenuItem_Click);
            //
            // channelContextMenu
            //
            this.channelContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.blockPedalMenuItem,
            this.blockPedalMenuSeparator,
            this.clearMuteSoloMenuItem,
            this.resetOutputChannelsMenuItem,
            this.resetChannelTransposeMenuItem,
            this.resetChannelVolumesMenuItem});
            this.channelContextMenu.Name = "channelContextMenu";
            this.channelContextMenu.Size = new System.Drawing.Size(250, 120);
            this.channelContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.channelContextMenu_Opening);
            //
            // blockPedalMenuItem
            //
            this.blockPedalMenuItem.Name = "blockPedalMenuItem";
            this.blockPedalMenuItem.Size = new System.Drawing.Size(249, 22);
            this.blockPedalMenuItem.Text = "ソフトペダル（CC67）を音源に送らない(&B)";
            this.blockPedalMenuItem.Click += new System.EventHandler(this.blockPedalMenuItem_Click);
            //
            // blockPedalMenuSeparator
            //
            this.blockPedalMenuSeparator.Name = "blockPedalMenuSeparator";
            this.blockPedalMenuSeparator.Size = new System.Drawing.Size(246, 6);
            //
            // clearMuteSoloMenuItem
            //
            this.clearMuteSoloMenuItem.Name = "clearMuteSoloMenuItem";
            this.clearMuteSoloMenuItem.Size = new System.Drawing.Size(249, 22);
            this.clearMuteSoloMenuItem.Text = "ミュートとソロをすべて解除(&C)";
            this.clearMuteSoloMenuItem.Click += new System.EventHandler(this.clearMuteSoloMenuItem_Click);
            //
            // resetOutputChannelsMenuItem
            //
            this.resetOutputChannelsMenuItem.Name = "resetOutputChannelsMenuItem";
            this.resetOutputChannelsMenuItem.Size = new System.Drawing.Size(249, 22);
            this.resetOutputChannelsMenuItem.Text = "CH（送信先）をすべて TR と同じに戻す(&R)";
            this.resetOutputChannelsMenuItem.Click += new System.EventHandler(this.resetOutputChannelsMenuItem_Click);
            //
            // resetChannelTransposeMenuItem
            //
            this.resetChannelTransposeMenuItem.Name = "resetChannelTransposeMenuItem";
            this.resetChannelTransposeMenuItem.Size = new System.Drawing.Size(249, 22);
            this.resetChannelTransposeMenuItem.Text = "移調とオクターブをすべて 0 に戻す(&T)";
            this.resetChannelTransposeMenuItem.Click += new System.EventHandler(this.resetChannelTransposeMenuItem_Click);
            //
            // resetChannelVolumesMenuItem
            //
            this.resetChannelVolumesMenuItem.Name = "resetChannelVolumesMenuItem";
            this.resetChannelVolumesMenuItem.Size = new System.Drawing.Size(249, 22);
            this.resetChannelVolumesMenuItem.Text = "音量をすべて 100% に戻す(&V)";
            this.resetChannelVolumesMenuItem.Click += new System.EventHandler(this.resetChannelVolumesMenuItem_Click);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deviceStatusLabel,
            this.positionStatusLabel,
            this.tempoStatusLabel,
            this.songInfoStatusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 678);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.ShowItemToolTips = true;
            this.statusStrip.Size = new System.Drawing.Size(944, 22);
            this.statusStrip.TabIndex = 3;
            // 
            // deviceStatusLabel
            // 
            this.deviceStatusLabel.Name = "deviceStatusLabel";
            this.deviceStatusLabel.Size = new System.Drawing.Size(929, 17);
            this.deviceStatusLabel.Spring = true;
            this.deviceStatusLabel.Text = "出力: （なし）";
            this.deviceStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // positionStatusLabel
            //
            this.positionStatusLabel.Margin = new System.Windows.Forms.Padding(12, 3, 12, 2);
            this.positionStatusLabel.Name = "positionStatusLabel";
            this.positionStatusLabel.Size = new System.Drawing.Size(0, 17);
            //
            // tempoStatusLabel
            //
            this.tempoStatusLabel.Margin = new System.Windows.Forms.Padding(0, 3, 12, 2);
            this.tempoStatusLabel.Name = "tempoStatusLabel";
            this.tempoStatusLabel.Size = new System.Drawing.Size(0, 17);
            //
            // songInfoStatusLabel
            //
            this.songInfoStatusLabel.Name = "songInfoStatusLabel";
            this.songInfoStatusLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // positionTimer
            // 
            this.positionTimer.Enabled = true;
            this.positionTimer.Interval = 33;
            this.positionTimer.Tick += new System.EventHandler(this.positionTimer_Tick);
            //
            // channelTimer
            //
            this.channelTimer.Enabled = true;
            this.channelTimer.Interval = 33;
            this.channelTimer.Tick += new System.EventHandler(this.channelTimer_Tick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(944, 700);
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.controlPanel);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.menuStrip);
            this.MainMenuStrip = this.menuStrip;
            this.MinimumSize = new System.Drawing.Size(960, 420);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "KMIDI Player";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.controlPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.tempoUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.keyUpDown)).EndInit();
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.playlistContextMenu.ResumeLayout(false);
            this.channelContextMenu.ResumeLayout(false);
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addFilesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addFolderMenuItem;
        private System.Windows.Forms.ToolStripSeparator fileMenuSeparator1;
        private System.Windows.Forms.ToolStripMenuItem openPlaylistMenuItem;
        private System.Windows.Forms.ToolStripMenuItem savePlaylistMenuItem;
        private System.Windows.Forms.ToolStripMenuItem savePlaylistAsMenuItem;
        private System.Windows.Forms.ToolStripSeparator fileMenuSeparator3;
        private System.Windows.Forms.ToolStripMenuItem removeSelectedMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearPlaylistMenuItem;
        private System.Windows.Forms.ToolStripSeparator fileMenuSeparator2;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;
        private System.Windows.Forms.ToolStripMenuItem playMenuItem;
        private System.Windows.Forms.ToolStripMenuItem playPauseMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stopMenuItem;
        private System.Windows.Forms.ToolStripMenuItem seekBackMenuItem;
        private System.Windows.Forms.ToolStripMenuItem seekForwardMenuItem;
        private System.Windows.Forms.ToolStripSeparator playMenuSeparator1;
        private System.Windows.Forms.ToolStripMenuItem previousMenuItem;
        private System.Windows.Forms.ToolStripMenuItem nextMenuItem;
        private System.Windows.Forms.ToolStripSeparator playMenuSeparator2;
        private System.Windows.Forms.ToolStripMenuItem resetTempoKeyMenuItem;
        private System.Windows.Forms.ToolStripSeparator playMenuSeparator3;
        private System.Windows.Forms.ToolStripMenuItem sendResetMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewMenuItem;
        private System.Windows.Forms.ToolStripMenuItem channelPanelMenuItem;
        private System.Windows.Forms.ToolStripMenuItem usedChannelsOnlyMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem outputDeviceMenuItem;
        private System.Windows.Forms.ToolStripMenuItem noDeviceMenuItem;
        private System.Windows.Forms.ToolStripSeparator settingsMenuSeparator1;
        private System.Windows.Forms.ToolStripMenuItem resetTempoKeyPerSongMenuItem;
        private System.Windows.Forms.ToolStripMenuItem skipSilenceMenuItem;
        private System.Windows.Forms.ToolStripMenuItem topMostMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pianoRollMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel tempoStatusLabel;
        private System.Windows.Forms.ToolStripSeparator settingsMenuSeparator2;
        private System.Windows.Forms.ToolStripMenuItem levelMeterSettingsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem preferencesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutMenuItem;
        private System.Windows.Forms.Panel controlPanel;
        private KMidiPlayer.Controls.FlatButton previousButton;
        private KMidiPlayer.Controls.FlatButton playPauseButton;
        private KMidiPlayer.Controls.FlatButton stopButton;
        private KMidiPlayer.Controls.FlatButton nextButton;
        private KMidiPlayer.Controls.FlatToggleButton pianoRollButton;
        private KMidiPlayer.Controls.SeekBar seekBar;
        private System.Windows.Forms.Label timeLabel;
        private System.Windows.Forms.Label tempoLabel;
        private KMidiPlayer.Controls.StepNumericUpDown tempoUpDown;
        private System.Windows.Forms.Label tempoUnitLabel;
        private System.Windows.Forms.Label keyLabel;
        private KMidiPlayer.Controls.StepNumericUpDown keyUpDown;
        private System.Windows.Forms.Label modeLabel;
        private KMidiPlayer.Controls.FlatComboBox modeComboBox;
        private KMidiPlayer.Controls.FlatToggleButton loopAButton;
        private KMidiPlayer.Controls.FlatToggleButton loopBButton;
        private System.Windows.Forms.Label ratingFilterLabel;
        private KMidiPlayer.Controls.RatingFilterBox ratingFilterBox;
        private System.Windows.Forms.Label velocityCurveLabel;
        private KMidiPlayer.Controls.VelocityCurveBox velocityCurveBox;
        private System.Windows.Forms.Label resetLabel;
        private KMidiPlayer.Controls.FlatComboBox resetComboBox;
        private KMidiPlayer.Controls.FlatButton sendResetButton;
        private System.Windows.Forms.SplitContainer splitContainer;
        private KMidiPlayer.Controls.PlaylistView playlistView;
        private System.Windows.Forms.ColumnHeader numberColumn;
        private System.Windows.Forms.ColumnHeader titleColumn;
        private System.Windows.Forms.ColumnHeader durationColumn;
        private System.Windows.Forms.ColumnHeader ratingColumn;
        private System.Windows.Forms.ColumnHeader fileNameColumn;
        private System.Windows.Forms.ContextMenuStrip playlistContextMenu;
        private System.Windows.Forms.ToolStripMenuItem copyMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cutMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pasteMenuItem;
        private System.Windows.Forms.ToolStripSeparator playlistMenuSeparator;
        private System.Windows.Forms.ToolStripMenuItem removeFromListMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteFilesMenuItem;
        private KMidiPlayer.Controls.ChannelView channelView;
        private System.Windows.Forms.ContextMenuStrip channelContextMenu;
        private System.Windows.Forms.ToolStripMenuItem blockPedalMenuItem;
        private System.Windows.Forms.ToolStripSeparator blockPedalMenuSeparator;
        private System.Windows.Forms.ToolStripMenuItem clearMuteSoloMenuItem;
        private System.Windows.Forms.ToolStripMenuItem resetOutputChannelsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem resetChannelTransposeMenuItem;
        private System.Windows.Forms.ToolStripMenuItem resetChannelVolumesMenuItem;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel deviceStatusLabel;
        private System.Windows.Forms.ToolStripStatusLabel songInfoStatusLabel;
        private System.Windows.Forms.ToolStripStatusLabel positionStatusLabel;
        private System.Windows.Forms.Timer positionTimer;
        private System.Windows.Forms.Timer channelTimer;
        private System.Windows.Forms.ToolTip toolTip;
    }
}
