using System.Collections.Generic;

namespace KMidiPlayer
{
    /// <summary>画面に表示する言語。</summary>
    public enum UiLanguage
    {
        Japanese,
        English,
    }

    /// <summary>
    /// 画面に表示する文字列。日本語と英語を並べて書き、<see cref="Current"/> の言語のものを返す。
    /// </summary>
    /// <remarks>
    /// 言語を変えたら、表示中の画面の文字列を設定し直すこと（MainForm.ApplyTexts など）。
    /// </remarks>
    public static class Texts
    {
        public static UiLanguage Current { get; set; } = UiLanguage.Japanese;

        private static string T(string japanese, string english)
        {
            return Current == UiLanguage.English ? english : japanese;
        }

        // ---- メニュー: ファイル ----
        public static string FileMenu => T("ファイル(&F)", "&File");
        public static string AddFiles => T("ファイルを追加(&A)...", "&Add Files...");
        public static string AddFolder => T("フォルダを追加(&D)...", "Add Fol&der...");
        public static string OpenPlaylist => T("プレイリストを開く(&L)...", "Open P&laylist...");
        public static string SavePlaylist => T("プレイリストを保存(&S)", "&Save Playlist");
        public static string SavePlaylistAs => T("プレイリストに名前を付けて保存(&E)...", "Save Playlist &As...");
        public static string RemoveSelected => T("選択した曲をリストから削除(&R)", "&Remove Selected from List");
        public static string ClearPlaylist => T("プレイリストを消去(&C)", "&Clear Playlist");
        public static string Exit => T("終了(&X)", "E&xit");

        // ---- メニュー: 再生 ----
        public static string PlayMenu => T("再生(&P)", "&Play");
        public static string PlayPause => T("再生 / 一時停止(&P)", "&Play / Pause");
        public static string Stop => T("停止(&S)", "&Stop");
        public static string SeekBack => T("5 秒戻る(&J)", "Back 5 Seconds (&J)");
        public static string SeekForward => T("5 秒進む(&K)", "Forward 5 Seconds (&K)");
        public static string Previous => T("前の曲(&B)", "Previous (&B)");
        public static string Next => T("次の曲(&N)", "&Next");
        public static string ResetTempoKey => T("テンポとキーを元に戻す(&T)", "Reset &Tempo and Key");
        public static string SendReset => T("リセットを送信(&R)", "Send &Reset");

        // ---- メニュー: 表示 ----
        public static string ViewMenu => T("表示(&V)", "&View");
        public static string ChannelPanel => T("チャンネルパネル(&C)", "&Channel Panel");
        public static string UsedChannelsOnly => T("データのあるチャンネルだけ表示(&U)", "Show &Used Channels Only");

        // ---- メニュー: 設定 ----
        public static string SettingsMenu => T("設定(&S)", "&Settings");
        public static string OutputDevice => T("出力デバイス(&O)", "&Output Device");
        public static string NoDevices => T("（デバイスがありません）", "(No devices)");
        public static string PianoRollMenu => T("ピアノロール(&R)", "Piano &Roll");
        public static string PianoRollButtonTip => T("ピアノロールを表示 / 非表示", "Show / hide the piano roll");
        public static string PianoRollTitle => T("ピアノロール", "Piano Roll");
        public static string PianoRollNoSong => T("曲を読み込んでいません", "No song loaded");
        public static string PianoRollTip => T(
            "クリックでその位置へ移動、ドラッグで位置を変える、ホイールで時間方向に拡大・縮小\n右クリックで表示の設定、F11 で全画面",
            "Click to move there, drag to change the position, use the wheel to zoom in time\nRight-click for display options, F11 for full screen");

        public static string PianoRollStyleMenu => T("表示の種類(&V)", "&View");

        public static string PianoRollStyleName(Controls.PianoRollStyle style)
        {
            switch (style)
            {
                case Controls.PianoRollStyle.Vertical: return T("縦", "Falling");
                case Controls.PianoRollStyle.Perspective: return "3D";
                case Controls.PianoRollStyle.Circular: return T("円", "Circle");
                default: return T("横", "Side");
            }
        }

        public static string PianoRollStyleTip(Controls.PianoRollStyle style)
        {
            switch (style)
            {
                case Controls.PianoRollStyle.Vertical: return T("上から下の鍵盤へ落ちてくる表示", "Notes fall onto a keyboard at the bottom");
                case Controls.PianoRollStyle.Perspective: return T("奥から手前の鍵盤へ流れてくる 3D の表示", "Notes come toward a keyboard from the distance (3D)");
                case Controls.PianoRollStyle.Circular: return T("中心から外側の円い鍵盤へ広がる表示（角度が音の高さ）", "Notes spread out from the center to a round keyboard (angle = pitch)");
                default: return T("右から左へ流れる表示", "Notes scroll from right to left");
            }
        }

        public static string PianoRollColorMenu => T("色分け(&C)", "&Color");
        public static string PianoRollColorLabel => T("色", "Color");

        public static string PianoRollColorName(Controls.PianoRollColorMode mode)
        {
            switch (mode)
            {
                case Controls.PianoRollColorMode.Velocity: return T("強さ別", "Velocity");
                case Controls.PianoRollColorMode.PitchClass: return T("音名別", "Pitch");
                default: return T("トラック別", "Track");
            }
        }

        public static string PianoRollColorTip => T(
            "ノートの色分け（トラック別 / 強さ別: 弱い青～強い赤 / 音名別: ドが赤）",
            "How to color notes (Track / Velocity: soft blue to loud red / Pitch: C is red)");

        public static string PianoRollNamesMenu => T("音名(&N)", "Note &Names");
        public static string PianoRollNamesLabel => T("音名", "Names");

        public static string PianoRollNamesName(Controls.PianoRollNoteNames names)
        {
            switch (names)
            {
                case Controls.PianoRollNoteNames.Letters: return "C D E";
                case Controls.PianoRollNoteNames.Solfege: return T("ドレミ", "Do Re Mi");
                default: return T("なし", "None");
            }
        }

        public static string PianoRollNamesTip => T("ノートと鍵盤に書く音名", "Note names on notes and keys");

        /// <summary>ドレミの音名（C から半音ずつ 12 個）。</summary>
        public static string[] SolfegeNames => Current == UiLanguage.English
            ? new[] { "Do", "Do#", "Re", "Re#", "Mi", "Fa", "Fa#", "Sol", "Sol#", "La", "La#", "Ti" }
            : new[] { "ド", "ド#", "レ", "レ#", "ミ", "ファ", "ファ#", "ソ", "ソ#", "ラ", "ラ#", "シ" };

        public static string PianoRollEffectsMenu => T("エフェクト(&E)", "&Effects");
        public static string PianoRollEffectsLabel => T("エフェクト", "Effects");
        public static string PianoRollEffectsTip => T(
            "ノートが鍵盤に届いたときのエフェクトの強さ（1 は光と火花だけ、5 に近いほど派手）",
            "Effects when a note reaches the keyboard (1 = flash and sparks, up to 5 = flashiest)");

        /// <summary>メニューに出すエフェクトの強さ（0 は「なし」、両端には説明を添える）。</summary>
        public static string PianoRollEffectLevelName(int level)
        {
            switch (level)
            {
                case 0: return T("なし", "Off");
                case 1: return T("1（控えめ）", "1 (Subtle)");
                case Controls.PianoRollOptions.MaxEffectLevel: return T($"{level}（派手）", $"{level} (Flashy)");
                default: return level.ToString();
            }
        }

        public static string PianoRollFpsMenu => T("更新頻度 (FPS)(&U)", "&Update Rate (FPS)");
        public static string PianoRollFpsTip => T(
            "ピアノロールを 1 秒に何回描き直すか（多いほど滑らかになり、パソコンの負荷が増える）",
            "How many times per second to redraw the piano roll (higher is smoother but uses more CPU)");

        /// <summary>ツールバーのボタンに出すエフェクトの強さ。</summary>
        public static string PianoRollEffectLevelShortName(int level) => level == 0 ? T("なし", "Off") : level.ToString();
        public static string PianoRollFullKeyboard => T("88 鍵(&K)", "88 &Keys");
        public static string PianoRollFullKeyboardTip => T("音域を曲に合わせず、ピアノの 88 鍵の幅で表示する", "Show the full 88-key range instead of fitting the song");
        public static string PianoRollOverview => T("全体図(&O)", "&Overview");
        public static string PianoRollOverviewTip => T("曲全体の縮小図を下に表示する（クリック・ドラッグでその位置へ移動）", "Show the whole song at the bottom (click or drag to move there)");
        public static string PianoRollOverviewBarTip => T("クリック・ドラッグでその位置へ移動（枠はピアノロールに出ている範囲）", "Click or drag to move there (the frame shows the range in the piano roll)");
        public static string PianoRollLyrics => T("歌詞・マーカー(&L)", "&Lyrics && Markers");
        public static string PianoRollLyricsTip => T("曲に入っている歌詞とマーカー（A メロ・サビなど）を表示する", "Show lyrics and markers stored in the song");
        public static string PianoRollFullScreen => T("全画面(&F)", "&Full Screen");
        public static string PianoRollFullScreenTip => T("全画面で表示する（F11。Esc で戻る）", "Full screen (F11; Esc to exit)");
        public static string PianoRollGpu => T("GPU で描く(&G)", "Draw with &GPU");
        public static string PianoRollGpuTip => T(
            "WPF の描画を使って、グラフィックス（GPU）で描く（CPU の負荷が下がる）。オフにすると、今までどおり CPU で描く",
            "Draw with the graphics processor (GPU) using WPF to reduce CPU load. Turn off to draw with the CPU as before");
        public static string PianoRollGpuUnavailable => T(
            "この PC（リモートデスクトップなど）では GPU で描けないため、CPU で描いています",
            "The GPU cannot be used on this PC (e.g. Remote Desktop), so the CPU is used");
        public static string PianoRollZoomLabel => T("倍率", "Zoom");
        public static string PianoRollZoomTip => T(
            "時間方向の倍率（20%～800%）。ホイール・矢印キーで 1 段ずつ、ダブルクリック・Home キーで 100% に戻す",
            "Zoom in time (20%-800%). Wheel or arrow keys change it step by step; double-click or Home resets to 100%");
        public static string SkipLeadingSilence => T("曲の頭の無音をとばす(&L)", "Skip &Leading Silence");
        public static string AlwaysOnTop => T("常に手前に表示(&T)", "Always on &Top");
        public static string TempoStatusTip => T("今のテンポ（テンポ欄の % を掛けた実際の速さ）と拍子", "Current tempo (including the tempo % setting) and time signature");
        public static string ResetTempoKeyPerSong => T("曲を切り替えたらテンポとキーを元に戻す(&K)", "Reset Tempo and &Key When the Song Changes");
        public static string LevelMeterSettings => T("レベルメーターの設定(&M)...", "Level &Meter...");
        public static string Preferences => T("環境設定(&P)...", "&Preferences...");

        // ---- メニュー: ヘルプ ----
        public static string HelpMenu => T("ヘルプ(&H)", "&Help");
        public static string About => T("バージョン情報(&A)...", "&About...");
        public static string AboutTitle => T("バージョン情報", "About");
        public static string AboutText(string version) => T($"KMIDI Player\nバージョン {version}", $"KMIDI Player\nVersion {version}");

        // ---- 操作部 ----
        public static string TempoLabel => T("テンポ:", "Tempo:");
        public static string KeyLabel => T("キー:", "Key:");
        public static string ModeLabel => T("モード:", "Mode:");
        public static string ResetLabel => T("リセット:", "Reset:");
        public static string RatingFilterLabel => T("評価:", "Rating:");
        public static string VelocityCurveLabel => T("ベロシティ:", "Velocity:");
        public static string SendResetButton => T("送信", "Send");

        /// <summary>再生モードの一覧（PlayMode の順）。</summary>
        public static string[] PlayModes => Current == UiLanguage.English
            ? new[] { "Play One", "Repeat One", "Play All", "Repeat All", "Shuffle" }
            : new[] { "1曲再生", "1曲リピート", "全曲再生", "全曲リピート", "シャッフル" };

        /// <summary>リセットの種類の一覧（MidiResetType の順）。</summary>
        public static string[] ResetTypes => new[] { T("送らない", "None"), "GM", "GS", "XG" };

        /// <summary>評価のフィルターの星の右に添える文字（0 のとき）。</summary>
        public static string RatingFilterAll => T("すべて", "All");

        /// <summary>評価のフィルターの星の右に添える文字（1～4 のとき）。</summary>
        public static string RatingFilterAtLeast => T("以上", "& up");

        public static string PositionDisplayTip => T(
            "TIME: 時:分:秒/100 分の 1 秒\nBAR: 今の小節/全小節数\nBEAT: 今の拍/1 小節の拍数　拍の中のティック/1 拍のティック数",
            "TIME: hours:minutes:seconds/hundredths\nBAR: current bar/total bars\nBEAT: current beat/beats per bar  tick in beat/ticks per beat");

        public static string RatingFilterTip => T(
            "この数の星以上の曲だけをプレイリストに表示する\nクリック・左右にドラッグで選ぶ（同じ数をクリックで「すべて」）",
            "Show only songs with at least this many stars\nClick or drag to choose (click the same star again for All)");

        public static string LoopATip(string time) => T($"A 地点: {time}（クリックで区間ループを解除）", $"Point A: {time} (click to cancel the A-B loop)");
        public static string LoopAEmptyTip => T("今の位置を区間ループの開始（A 地点）にする", "Set the current position as the loop start (A)");
        public static string LoopBTip(string time) => T($"B 地点: {time}（クリックで解除）", $"Point B: {time} (click to cancel)");
        public static string LoopBEmptyTip => T("今の位置を区間ループの終了（B 地点）にして、A～B を繰り返す", "Set the current position as the loop end (B) and repeat A to B");

        public static string VelocityCurveTip(int curve)
        {
            string value = (curve > 0 ? "+" : string.Empty) + curve;
            string effect = curve > 0 ? T("弱い音を強めに（強弱の差を小さく）", "softer notes louder (less dynamic range)")
                : curve < 0 ? T("弱い音をより弱く（強弱の差を大きく）", "softer notes quieter (more dynamic range)")
                : T("曲のまま", "as recorded");
            return T($"ベロシティカーブ: {value}（{effect}）\n上下にドラッグ・ホイールで変更、ダブルクリックで 0 に戻す",
                $"Velocity curve: {value} ({effect})\nDrag up/down or use the wheel to change; double-click to reset to 0");
        }

        // ---- プレイリスト ----
        public static string TitleColumn => T("タイトル", "Title");
        public static string DurationColumn => T("時間", "Time");
        public static string RatingColumn => T("評価", "Rating");
        public static string FileNameColumn => T("ファイル名", "File Name");
        public static string LoadError => T("（読み込みエラー）", " (load error)");
        public static string FileNotFound => T("ファイルが見つかりません。", "File not found.");
        public static string Untitled => T("無題", "Untitled");

        public static string AddFilesTitle => T("ファイルを追加", "Add Files");
        public static string MidiFileFilter => T("MIDI ファイル (*.mid;*.midi)|*.mid;*.midi", "MIDI Files (*.mid;*.midi)|*.mid;*.midi");
        public static string AddFolderDescription => T("MIDI ファイルが入ったフォルダを選んでください。サブフォルダ内のファイルも追加します。",
            "Choose a folder that contains MIDI files. Files in subfolders are also added.");
        public static string OpenPlaylistTitle => T("プレイリストを開く", "Open Playlist");
        public static string OpenPlaylistFilter => T(
            "プレイリスト (*.m3u;*.m3u8;*.to2)|*.m3u;*.m3u8;*.to2|M3U プレイリスト (*.m3u;*.m3u8)|*.m3u;*.m3u8|TMIDI Player プレイリスト (*.to2)|*.to2",
            "Playlists (*.m3u;*.m3u8;*.to2)|*.m3u;*.m3u8;*.to2|M3U Playlists (*.m3u;*.m3u8)|*.m3u;*.m3u8|TMIDI Player Playlists (*.to2)|*.to2");
        public static string SavePlaylistAsTitle => T("プレイリストに名前を付けて保存", "Save Playlist As");
        public static string SavePlaylistFilter => T("プレイリスト (*.m3u8)|*.m3u8", "Playlist (*.m3u8)|*.m3u8");
        public static string OpenPlaylistFailed(string reason) => T("プレイリストを開けませんでした。\n" + reason, "Could not open the playlist.\n" + reason);
        public static string SavePlaylistFailed(string reason) => T("プレイリストを保存できませんでした。\n" + reason, "Could not save the playlist.\n" + reason);
        public static string RatingsChangedSaveTo(string fileName) => T(
            $"お気に入りの評価が変更されています。\n「{fileName}」に保存しますか？",
            $"Ratings have been changed.\nSave them to \"{fileName}\"?");
        public static string RatingsChangedSaveNew => T(
            "お気に入りの評価が変更されていますが、プレイリストのファイルに保存されていません。\nプレイリストを保存しますか？",
            "Ratings have been changed but are not saved in a playlist file.\nSave the playlist?");

        // ---- プレイリストの右クリックメニュー ----
        public static string CopyItems => T("コピー(&C)", "&Copy");
        public static string CutItems => T("カット(&T)", "Cu&t");
        public static string PasteItems => T("貼り付け(&P)", "&Paste");
        public static string RemoveFromList => T("リストから削除(&R)", "&Remove from List");
        public static string DeleteFiles => T("リストとファイルを削除(&D)...", "&Delete from List and Disk...");
        public static string ClipboardFailed => T(
            "クリップボードを使えませんでした。ほかのアプリが使っている可能性があります。少し待ってからやり直してください。",
            "Could not access the clipboard. Another application may be using it. Please wait a moment and try again.");

        public static string ConfirmRemoveFromList(int count, string firstTitle) => count == 1
            ? T($"「{firstTitle}」をリストから削除しますか？\n（ファイルは削除しません）",
                $"Remove \"{firstTitle}\" from the list?\n(The file is not deleted.)")
            : T($"選択した {count} 曲をリストから削除しますか？\n（ファイルは削除しません）",
                $"Remove the {count} selected songs from the list?\n(The files are not deleted.)");

        /// <summary>ファイルを削除する 1 回目の確認。削除するファイルのパスを、<paramref name="maxListed"/> 個まで並べる。</summary>
        public static string ConfirmDeleteFiles(IReadOnlyList<string> paths, int maxListed) => T(
            $"次の {paths.Count} 個のファイルを、パソコンから削除します（ごみ箱に移動します）。\nリストからも削除します。\n\n{ListPaths(paths, maxListed)}\n\n削除してよろしいですか？",
            $"The following {paths.Count} file(s) will be deleted from your computer (moved to the Recycle Bin).\nThey are also removed from the list.\n\n{ListPaths(paths, maxListed)}\n\nDelete them?");

        /// <summary>ファイルを削除する 2 回目（最終）の確認。</summary>
        public static string ConfirmDeleteFilesFinal(int count) => T(
            $"最終確認です。\n\n本当に {count} 個のファイルを削除しますか？\n（リストから削除するだけなら「いいえ」を選び、「リストから削除」を使ってください）",
            $"Final confirmation.\n\nAre you sure you want to delete {count} file(s)?\n(To remove them from the list only, choose No and use \"Remove from List\".)");

        public static string DeleteFilesFailed(IReadOnlyList<string> paths, int maxListed) => T(
            $"次のファイルを削除できませんでした（使用中か、削除する権限が無い可能性があります）。リストには残しています。\n\n{ListPaths(paths, maxListed)}",
            $"The following file(s) could not be deleted (they may be in use, or you may not have permission). They remain in the list.\n\n{ListPaths(paths, maxListed)}");

        /// <summary>ファイルのパスを 1 行に 1 つ、<paramref name="maxListed"/> 個まで並べる。残りは「ほか N 個」とまとめる。</summary>
        private static string ListPaths(IReadOnlyList<string> paths, int maxListed)
        {
            var lines = new List<string>();
            for (int i = 0; i < paths.Count && i < maxListed; i++)
            {
                lines.Add("  " + paths[i]);
            }
            if (paths.Count > maxListed)
            {
                lines.Add(T($"  ……ほか {paths.Count - maxListed} 個", $"  ...and {paths.Count - maxListed} more"));
            }
            return string.Join("\n", lines);
        }

        // ---- 出力デバイス・状態 ----
        public static string NoOutputDevice => T("MIDI 出力デバイスが選ばれていません。\n「設定」→「出力デバイス」から選んでください。",
            "No MIDI output device is selected.\nChoose one from Settings > Output Device.");
        public static string OutputStatus(string deviceName) => T("出力: ", "Output: ") + (deviceName ?? T("（なし）", "(none)"));
        public static string MidiOutputName(int number) => T($"MIDI 出力 {number}", $"MIDI Output {number}");
        public static string DeviceNotFound => T("指定された MIDI 出力デバイスがありません。", "The specified MIDI output device does not exist.");
        public static string DeviceOpenFailed(string name, string reason) => T(
            $"MIDI 出力デバイス「{name}」を開けませんでした。\n{reason}",
            $"Could not open the MIDI output device \"{name}\".\n{reason}");
        public static string ErrorCode(int code) => T($"エラーコード {code}", $"Error code {code}");

        // ---- MIDI ファイルの読み込みエラー ----
        public static string NotMidiFile => T("MIDI ファイルではありません。", "Not a MIDI file.");
        public static string BrokenHeader => T("MIDI ファイルのヘッダーが壊れています。", "The MIDI file header is corrupt.");
        public static string Format2NotSupported => T("Format 2 の MIDI ファイルには対応していません。", "Format 2 MIDI files are not supported.");
        public static string UnknownFormat(int format) => T($"未知の MIDI ファイル形式です（Format {format}）。", $"Unknown MIDI file format (Format {format}).");
        public static string NoTracks => T("MIDI ファイルにトラックがありません。", "The MIDI file has no tracks.");

        // ---- チャンネルパネル ----
        public static string KeyboardHeader => T("鍵盤", "Keys");
        public static string PedalHeader => T("ペダル", "Pedal");
        public static string LevelHeader => T("レベル / 音量", "Level / Volume");
        public static string TransposeHeader => T("移調", "Trans.");
        public static string OctaveHeader => T("オクターブ", "Octave");
        public static string BendTip(int bend, int modulation) => T(
            $"ピッチベンド {bend:+0;-0;0}　モジュレーション {modulation}",
            $"Pitch bend {bend:+0;-0;0}  Modulation {modulation}");

        public static string VolumeTip(int percent) => T($"音量 {percent}%（ダブルクリックで 100%）", $"Volume {percent}% (double-click for 100%)");

        /// <summary>ペダルのランプのツールチップ（例: 「ソフトペダル（CC67）」）。送らない設定なら、その旨を付ける。</summary>
        public static string PedalTip(int controller, bool blocked)
        {
            return blocked
                ? T($"{ControllerName(controller)}（CC{controller}）: 音源に送らない（クリックで送る）",
                    $"{ControllerName(controller)} (CC{controller}): not sent to the device (click to send)")
                : T($"{ControllerName(controller)}（CC{controller}）: 音源に送る（クリックで送らない）",
                    $"{ControllerName(controller)} (CC{controller}): sent to the device (click to stop)");
        }

        /// <summary>ペダルのランプの右クリックメニュー（例: 「ソフトペダル（CC67）を送らない」）。</summary>
        public static string BlockPedalMenu(int controller) => T(
            $"{ControllerName(controller)}（CC{controller}）を音源に送らない(&B)",
            $"Do Not Send {ControllerName(controller)} (CC{controller}) (&B)");

        public static string ClearMuteSolo => T("ミュートとソロをすべて解除(&C)", "&Clear All Mute and Solo");
        public static string ResetOutputChannels => T("CH（送信先）をすべて TR と同じに戻す(&R)", "&Reset All CH (Destination) to TR");
        public static string ResetChannelTransposes => T("移調とオクターブをすべて 0 に戻す(&T)", "Reset All &Transpose and Octave to 0");
        public static string ResetChannelVolumes => T("音量をすべて 100% に戻す(&V)", "Reset All &Volumes to 100%");

        // ---- レベルメーターの設定 ----
        public static string LevelMeterTitle => T("レベルメーターの設定", "Level Meter");
        public static string FpsLabel => T("更新頻度(&F):", "Refresh &rate:");
        public static string FpsUnit => T("fps（1 秒あたりの更新回数）", "fps (updates per second)");
        public static string StyleLabel => T("表示タイプ(&T):", "&Type:");
        public static string FallLabel => T("下降速度(&S):", "&Fall time:");
        public static string FallUnit => T("秒で 10% まで下がる", "s to fall to 10%");
        public static string PeakHold => T("ピークホールド(&P)", "&Peak hold");
        public static string PeakHoldTimeLabel => T("ホールド時間(&H):", "&Hold time:");
        public static string PeakHoldUnit => T("秒（過ぎたら下がる）", "s (then falls)");
        public static string RestoreDefaults => T("初期値に戻す(&D)", "&Defaults");
        public static string Ok => "OK";
        public static string Cancel => T("キャンセル", "Cancel");

        /// <summary>レベルメーターの表示タイプの名前（LevelMeterStyle の順）。</summary>
        public static string[] MeterStyles => Current == UiLanguage.English
            ? new[] { "Solid bar", "Gradient (green→yellow→red)", "LED (3 colors)", "LED (single color)", "Blocks", "Dots", "Hardware (LEDs on black)", "Rainbow", "Center out", "Needle" }
            : new[] { "単色バー", "グラデーション（緑→黄→赤）", "LED（3 色）", "LED（単色）", "ブロック", "ドット", "ハードウェア風（黒地に LED）", "虹色", "中央から左右", "針" };

        // ---- 環境設定 ----
        public static string PreferencesTitle => T("環境設定", "Preferences");
        public static string LanguageLabel => T("表示言語(&L):", "&Language:");
        public static string ThemeLabel => T("配色(&T):", "Color &theme:");

        /// <summary>配色の一覧（ThemeKind の順）。</summary>
        public static string[] Themes => Current == UiLanguage.English ? new[] { "Light", "Dark" } : new[] { "ライト", "ダーク" };
        public static string BlockedControllersLabel => T("音源に送らない CC(&C):", "&CCs not sent to the device:");
        public static string BlockedControllersNote => T("チェックした CC は、曲を再生しても MIDI 音源に送りません（全チャンネル共通）。",
            "Checked CCs in the song are not sent to the MIDI device (all channels).");
        public static string ClearBlockedControllers => T("すべて解除(&A)", "Clear &All");

        /// <summary>送らない CC のまとめ（例: 「送らない CC: 64, 67」）。</summary>
        public static string BlockedControllersSummary(string list) => list.Length == 0
            ? T("送らない CC: なし", "Not sent: none")
            : T("送らない CC: ", "Not sent: ") + list;

        /// <summary>CC の一覧の 1 項目（例: 「067  ソフトペダル」）。</summary>
        public static string ControllerItem(int controller) => $"{controller:000}  {ControllerName(controller)}";

        /// <summary>CC の名前（GM・GS で一般的なもの）。</summary>
        public static string ControllerName(int controller)
        {
            switch (controller)
            {
                case 0: return T("バンクセレクト MSB", "Bank Select MSB");
                case 1: return T("モジュレーション", "Modulation");
                case 2: return T("ブレスコントローラー", "Breath Controller");
                case 4: return T("フットコントローラー", "Foot Controller");
                case 5: return T("ポルタメントタイム", "Portamento Time");
                case 6: return T("データエントリー MSB", "Data Entry MSB");
                case 7: return T("ボリューム", "Volume");
                case 8: return T("バランス", "Balance");
                case 10: return T("パン", "Pan");
                case 11: return T("エクスプレッション", "Expression");
                case 12: return T("エフェクトコントロール 1", "Effect Control 1");
                case 13: return T("エフェクトコントロール 2", "Effect Control 2");
                case 16: case 17: case 18: case 19:
                    return T($"汎用コントローラー {controller - 15}", $"General Purpose {controller - 15}");
                case 32: return T("バンクセレクト LSB", "Bank Select LSB");
                case 38: return T("データエントリー LSB", "Data Entry LSB");
                case 64: return T("ダンパーペダル", "Damper Pedal");
                case 65: return T("ポルタメント", "Portamento");
                case 66: return T("ソステヌートペダル", "Sostenuto Pedal");
                case 67: return T("ソフトペダル", "Soft Pedal");
                case 68: return T("レガートフットスイッチ", "Legato Footswitch");
                case 69: return T("ホールド 2", "Hold 2");
                case 70: return T("サウンドバリエーション", "Sound Variation");
                case 71: return T("レゾナンス", "Resonance");
                case 72: return T("リリースタイム", "Release Time");
                case 73: return T("アタックタイム", "Attack Time");
                case 74: return T("カットオフ", "Cutoff");
                case 75: return T("ディケイタイム", "Decay Time");
                case 76: return T("ビブラートレート", "Vibrato Rate");
                case 77: return T("ビブラートデプス", "Vibrato Depth");
                case 78: return T("ビブラートディレイ", "Vibrato Delay");
                case 79: return T("サウンドコントローラー 10", "Sound Controller 10");
                case 80: case 81: case 82: case 83:
                    return T($"汎用コントローラー {controller - 75}", $"General Purpose {controller - 75}");
                case 84: return T("ポルタメントコントロール", "Portamento Control");
                case 88: return T("高解像度ベロシティ", "High Resolution Velocity");
                case 91: return T("リバーブ", "Reverb");
                case 92: return T("トレモロ", "Tremolo");
                case 93: return T("コーラス", "Chorus");
                case 94: return T("セレステ（ディレイ）", "Celeste (Delay)");
                case 95: return T("フェイザー", "Phaser");
                case 96: return T("データインクリメント", "Data Increment");
                case 97: return T("データデクリメント", "Data Decrement");
                case 98: return "NRPN LSB";
                case 99: return "NRPN MSB";
                case 100: return "RPN LSB";
                case 101: return "RPN MSB";
                case 120: return T("オールサウンドオフ", "All Sound Off");
                case 121: return T("リセットオールコントローラー", "Reset All Controllers");
                case 122: return T("ローカルコントロール", "Local Control");
                case 123: return T("オールノートオフ", "All Notes Off");
                case 124: return T("オムニオフ", "Omni Off");
                case 125: return T("オムニオン", "Omni On");
                case 126: return T("モノモード", "Mono Mode");
                case 127: return T("ポリモード", "Poly Mode");
            }
            if (controller >= 33 && controller <= 63)
            {
                return T($"LSB（CC{controller - 32} の下位）", $"LSB for CC{controller - 32}");
            }
            return T("（未定義）", "(undefined)");
        }

        /// <summary>言語の一覧（UiLanguage の順）。それぞれの言語で書く。</summary>
        public static string[] Languages => new[] { "日本語", "English" };
    }
}
