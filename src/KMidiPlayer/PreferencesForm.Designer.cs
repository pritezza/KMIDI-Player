namespace KMidiPlayer
{
    partial class PreferencesForm
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
            this.languageLabel = new System.Windows.Forms.Label();
            this.languageComboBox = new KMidiPlayer.Controls.FlatComboBox();
            this.themeLabel = new System.Windows.Forms.Label();
            this.themeComboBox = new KMidiPlayer.Controls.FlatComboBox();
            this.controllersLabel = new System.Windows.Forms.Label();
            this.controllersNoteLabel = new System.Windows.Forms.Label();
            this.controllersListBox = new System.Windows.Forms.CheckedListBox();
            this.controllersSummaryLabel = new System.Windows.Forms.Label();
            this.clearControllersButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // languageLabel
            //
            this.languageLabel.Location = new System.Drawing.Point(12, 16);
            this.languageLabel.Name = "languageLabel";
            this.languageLabel.Size = new System.Drawing.Size(100, 20);
            this.languageLabel.TabIndex = 0;
            this.languageLabel.Text = "表示言語(&L):";
            this.languageLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // languageComboBox
            //
            this.languageComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.languageComboBox.FormattingEnabled = true;
            this.languageComboBox.Location = new System.Drawing.Point(116, 16);
            this.languageComboBox.Name = "languageComboBox";
            this.languageComboBox.Size = new System.Drawing.Size(150, 20);
            this.languageComboBox.TabIndex = 1;
            //
            // themeLabel
            //
            this.themeLabel.Location = new System.Drawing.Point(12, 48);
            this.themeLabel.Name = "themeLabel";
            this.themeLabel.Size = new System.Drawing.Size(100, 20);
            this.themeLabel.TabIndex = 2;
            this.themeLabel.Text = "配色(&T):";
            this.themeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // themeComboBox
            //
            this.themeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.themeComboBox.FormattingEnabled = true;
            this.themeComboBox.Location = new System.Drawing.Point(116, 48);
            this.themeComboBox.Name = "themeComboBox";
            this.themeComboBox.Size = new System.Drawing.Size(150, 20);
            this.themeComboBox.TabIndex = 3;
            //
            // controllersLabel
            //
            this.controllersLabel.Location = new System.Drawing.Point(12, 84);
            this.controllersLabel.Name = "controllersLabel";
            this.controllersLabel.Size = new System.Drawing.Size(316, 18);
            this.controllersLabel.TabIndex = 4;
            this.controllersLabel.Text = "音源に送らない CC(&C):";
            this.controllersLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // controllersNoteLabel
            //
            this.controllersNoteLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.controllersNoteLabel.Location = new System.Drawing.Point(12, 102);
            this.controllersNoteLabel.Name = "controllersNoteLabel";
            this.controllersNoteLabel.Size = new System.Drawing.Size(316, 28);
            this.controllersNoteLabel.TabIndex = 5;
            this.controllersNoteLabel.Text = "チェックした CC は、曲を再生しても MIDI 音源に送りません（全チャンネル共通）。";
            //
            // controllersListBox
            //
            this.controllersListBox.CheckOnClick = true;
            this.controllersListBox.FormattingEnabled = true;
            this.controllersListBox.IntegralHeight = false;
            this.controllersListBox.Location = new System.Drawing.Point(12, 132);
            this.controllersListBox.Name = "controllersListBox";
            this.controllersListBox.Size = new System.Drawing.Size(316, 220);
            this.controllersListBox.TabIndex = 6;
            this.controllersListBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.controllersListBox_ItemCheck);
            //
            // controllersSummaryLabel
            //
            this.controllersSummaryLabel.AutoEllipsis = true;
            this.controllersSummaryLabel.Location = new System.Drawing.Point(12, 358);
            this.controllersSummaryLabel.Name = "controllersSummaryLabel";
            this.controllersSummaryLabel.Size = new System.Drawing.Size(220, 24);
            this.controllersSummaryLabel.TabIndex = 7;
            this.controllersSummaryLabel.Text = "送らない CC: なし";
            this.controllersSummaryLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // clearControllersButton
            //
            this.clearControllersButton.Location = new System.Drawing.Point(238, 358);
            this.clearControllersButton.Name = "clearControllersButton";
            this.clearControllersButton.Size = new System.Drawing.Size(90, 24);
            this.clearControllersButton.TabIndex = 8;
            this.clearControllersButton.Text = "すべて解除(&A)";
            this.clearControllersButton.UseVisualStyleBackColor = true;
            this.clearControllersButton.Click += new System.EventHandler(this.clearControllersButton_Click);
            //
            // okButton
            //
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(162, 396);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(80, 26);
            this.okButton.TabIndex = 9;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            //
            // cancelButton
            //
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(248, 396);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(80, 26);
            this.cancelButton.TabIndex = 10;
            this.cancelButton.Text = "キャンセル";
            this.cancelButton.UseVisualStyleBackColor = true;
            //
            // PreferencesForm
            //
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(340, 434);
            this.Controls.Add(this.languageLabel);
            this.Controls.Add(this.languageComboBox);
            this.Controls.Add(this.themeLabel);
            this.Controls.Add(this.themeComboBox);
            this.Controls.Add(this.controllersLabel);
            this.Controls.Add(this.controllersNoteLabel);
            this.Controls.Add(this.controllersListBox);
            this.Controls.Add(this.controllersSummaryLabel);
            this.Controls.Add(this.clearControllersButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PreferencesForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "環境設定";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label languageLabel;
        private KMidiPlayer.Controls.FlatComboBox languageComboBox;
        private System.Windows.Forms.Label themeLabel;
        private KMidiPlayer.Controls.FlatComboBox themeComboBox;
        private System.Windows.Forms.Label controllersLabel;
        private System.Windows.Forms.Label controllersNoteLabel;
        private System.Windows.Forms.CheckedListBox controllersListBox;
        private System.Windows.Forms.Label controllersSummaryLabel;
        private System.Windows.Forms.Button clearControllersButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
    }
}
