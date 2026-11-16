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
            this.meterHeaderLabel = new System.Windows.Forms.Label();
            this.fpsLabel = new System.Windows.Forms.Label();
            this.fpsUpDown = new KMidiPlayer.Controls.StepNumericUpDown();
            this.fpsUnitLabel = new System.Windows.Forms.Label();
            this.styleLabel = new System.Windows.Forms.Label();
            this.styleComboBox = new KMidiPlayer.Controls.FlatComboBox();
            this.fallLabel = new System.Windows.Forms.Label();
            this.fallUpDown = new KMidiPlayer.Controls.StepNumericUpDown();
            this.fallUnitLabel = new System.Windows.Forms.Label();
            this.peakHoldCheckBox = new System.Windows.Forms.CheckBox();
            this.peakHoldTimeLabel = new System.Windows.Forms.Label();
            this.peakHoldUpDown = new KMidiPlayer.Controls.StepNumericUpDown();
            this.peakHoldUnitLabel = new System.Windows.Forms.Label();
            this.meterResetButton = new System.Windows.Forms.Button();
            this.controllersLabel = new System.Windows.Forms.Label();
            this.controllersNoteLabel = new System.Windows.Forms.Label();
            this.controllersListBox = new System.Windows.Forms.CheckedListBox();
            this.controllersSummaryLabel = new System.Windows.Forms.Label();
            this.clearControllersButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.fpsUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fallUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.peakHoldUpDown)).BeginInit();
            this.SuspendLayout();
            //
            // meterHeaderLabel
            //
            this.meterHeaderLabel.Location = new System.Drawing.Point(12, 10);
            this.meterHeaderLabel.Name = "meterHeaderLabel";
            this.meterHeaderLabel.Size = new System.Drawing.Size(378, 20);
            this.meterHeaderLabel.TabIndex = 0;
            this.meterHeaderLabel.Text = "レベルメーター";
            this.meterHeaderLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // fpsLabel
            //
            this.fpsLabel.Location = new System.Drawing.Point(12, 36);
            this.fpsLabel.Name = "fpsLabel";
            this.fpsLabel.Size = new System.Drawing.Size(120, 20);
            this.fpsLabel.TabIndex = 1;
            this.fpsLabel.Text = "更新頻度(&F):";
            this.fpsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // fpsUpDown
            //
            this.fpsUpDown.Location = new System.Drawing.Point(140, 37);
            this.fpsUpDown.Name = "fpsUpDown";
            this.fpsUpDown.Size = new System.Drawing.Size(56, 19);
            this.fpsUpDown.TabIndex = 2;
            this.fpsUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.fpsUpDown.ValueChanged += new System.EventHandler(this.Value_Changed);
            //
            // fpsUnitLabel
            //
            this.fpsUnitLabel.Location = new System.Drawing.Point(200, 36);
            this.fpsUnitLabel.Name = "fpsUnitLabel";
            this.fpsUnitLabel.Size = new System.Drawing.Size(190, 20);
            this.fpsUnitLabel.TabIndex = 3;
            this.fpsUnitLabel.Text = "fps（1 秒あたりの更新回数）";
            this.fpsUnitLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // styleLabel
            //
            this.styleLabel.Location = new System.Drawing.Point(12, 66);
            this.styleLabel.Name = "styleLabel";
            this.styleLabel.Size = new System.Drawing.Size(120, 20);
            this.styleLabel.TabIndex = 4;
            this.styleLabel.Text = "表示タイプ(&T):";
            this.styleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // styleComboBox
            //
            this.styleComboBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.styleComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.styleComboBox.FormattingEnabled = true;
            this.styleComboBox.BaseItemHeight = 18;
            this.styleComboBox.Location = new System.Drawing.Point(140, 65);
            this.styleComboBox.MaxDropDownItems = 10;
            this.styleComboBox.Name = "styleComboBox";
            this.styleComboBox.Size = new System.Drawing.Size(250, 24);
            this.styleComboBox.TabIndex = 5;
            this.styleComboBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.styleComboBox_DrawItem);
            this.styleComboBox.SelectedIndexChanged += new System.EventHandler(this.Value_Changed);
            //
            // fallLabel
            //
            this.fallLabel.Location = new System.Drawing.Point(12, 96);
            this.fallLabel.Name = "fallLabel";
            this.fallLabel.Size = new System.Drawing.Size(120, 20);
            this.fallLabel.TabIndex = 6;
            this.fallLabel.Text = "下降速度(&S):";
            this.fallLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // fallUpDown
            //
            this.fallUpDown.DecimalPlaces = 1;
            this.fallUpDown.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.fallUpDown.Location = new System.Drawing.Point(140, 97);
            this.fallUpDown.Name = "fallUpDown";
            this.fallUpDown.Size = new System.Drawing.Size(56, 19);
            this.fallUpDown.TabIndex = 7;
            this.fallUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.fallUpDown.ValueChanged += new System.EventHandler(this.Value_Changed);
            //
            // fallUnitLabel
            //
            this.fallUnitLabel.Location = new System.Drawing.Point(200, 96);
            this.fallUnitLabel.Name = "fallUnitLabel";
            this.fallUnitLabel.Size = new System.Drawing.Size(190, 20);
            this.fallUnitLabel.TabIndex = 8;
            this.fallUnitLabel.Text = "秒で 10% まで下がる";
            this.fallUnitLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // peakHoldCheckBox
            //
            this.peakHoldCheckBox.AutoSize = true;
            this.peakHoldCheckBox.Location = new System.Drawing.Point(14, 128);
            this.peakHoldCheckBox.Name = "peakHoldCheckBox";
            this.peakHoldCheckBox.Size = new System.Drawing.Size(108, 16);
            this.peakHoldCheckBox.TabIndex = 9;
            this.peakHoldCheckBox.Text = "ピークホールド(&P)";
            this.peakHoldCheckBox.UseVisualStyleBackColor = true;
            this.peakHoldCheckBox.CheckedChanged += new System.EventHandler(this.Value_Changed);
            //
            // peakHoldTimeLabel
            //
            this.peakHoldTimeLabel.Location = new System.Drawing.Point(30, 150);
            this.peakHoldTimeLabel.Name = "peakHoldTimeLabel";
            this.peakHoldTimeLabel.Size = new System.Drawing.Size(102, 20);
            this.peakHoldTimeLabel.TabIndex = 10;
            this.peakHoldTimeLabel.Text = "ホールド時間(&H):";
            this.peakHoldTimeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // peakHoldUpDown
            //
            this.peakHoldUpDown.DecimalPlaces = 1;
            this.peakHoldUpDown.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.peakHoldUpDown.Location = new System.Drawing.Point(140, 151);
            this.peakHoldUpDown.Name = "peakHoldUpDown";
            this.peakHoldUpDown.Size = new System.Drawing.Size(56, 19);
            this.peakHoldUpDown.TabIndex = 11;
            this.peakHoldUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.peakHoldUpDown.ValueChanged += new System.EventHandler(this.Value_Changed);
            //
            // peakHoldUnitLabel
            //
            this.peakHoldUnitLabel.Location = new System.Drawing.Point(200, 150);
            this.peakHoldUnitLabel.Name = "peakHoldUnitLabel";
            this.peakHoldUnitLabel.Size = new System.Drawing.Size(190, 20);
            this.peakHoldUnitLabel.TabIndex = 12;
            this.peakHoldUnitLabel.Text = "秒（過ぎたら下がる）";
            this.peakHoldUnitLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // meterResetButton
            //
            this.meterResetButton.Location = new System.Drawing.Point(12, 180);
            this.meterResetButton.Name = "meterResetButton";
            this.meterResetButton.Size = new System.Drawing.Size(130, 24);
            this.meterResetButton.TabIndex = 13;
            this.meterResetButton.Text = "初期値に戻す(&D)";
            this.meterResetButton.UseVisualStyleBackColor = true;
            this.meterResetButton.Click += new System.EventHandler(this.meterResetButton_Click);
            //
            // controllersLabel
            //
            this.controllersLabel.Location = new System.Drawing.Point(12, 220);
            this.controllersLabel.Name = "controllersLabel";
            this.controllersLabel.Size = new System.Drawing.Size(378, 20);
            this.controllersLabel.TabIndex = 14;
            this.controllersLabel.Text = "音源に送らない CC(&C)";
            this.controllersLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // controllersNoteLabel
            //
            this.controllersNoteLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.controllersNoteLabel.Location = new System.Drawing.Point(12, 242);
            this.controllersNoteLabel.Name = "controllersNoteLabel";
            this.controllersNoteLabel.Size = new System.Drawing.Size(378, 32);
            this.controllersNoteLabel.TabIndex = 15;
            this.controllersNoteLabel.Text = "チェックした CC は、曲を再生しても MIDI 音源に送りません（全チャンネル共通）。";
            //
            // controllersListBox
            //
            this.controllersListBox.CheckOnClick = true;
            this.controllersListBox.FormattingEnabled = true;
            this.controllersListBox.IntegralHeight = false;
            this.controllersListBox.Location = new System.Drawing.Point(12, 276);
            this.controllersListBox.Name = "controllersListBox";
            this.controllersListBox.Size = new System.Drawing.Size(378, 166);
            this.controllersListBox.TabIndex = 16;
            this.controllersListBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.controllersListBox_ItemCheck);
            //
            // controllersSummaryLabel
            //
            this.controllersSummaryLabel.AutoEllipsis = true;
            this.controllersSummaryLabel.Location = new System.Drawing.Point(12, 448);
            this.controllersSummaryLabel.Name = "controllersSummaryLabel";
            this.controllersSummaryLabel.Size = new System.Drawing.Size(282, 24);
            this.controllersSummaryLabel.TabIndex = 17;
            this.controllersSummaryLabel.Text = "送らない CC: なし";
            this.controllersSummaryLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // clearControllersButton
            //
            this.clearControllersButton.Location = new System.Drawing.Point(300, 448);
            this.clearControllersButton.Name = "clearControllersButton";
            this.clearControllersButton.Size = new System.Drawing.Size(90, 24);
            this.clearControllersButton.TabIndex = 18;
            this.clearControllersButton.Text = "すべて解除(&A)";
            this.clearControllersButton.UseVisualStyleBackColor = true;
            this.clearControllersButton.Click += new System.EventHandler(this.clearControllersButton_Click);
            //
            // okButton
            //
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(224, 488);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(80, 26);
            this.okButton.TabIndex = 19;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            //
            // cancelButton
            //
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(310, 488);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(80, 26);
            this.cancelButton.TabIndex = 20;
            this.cancelButton.Text = "キャンセル";
            this.cancelButton.UseVisualStyleBackColor = true;
            //
            // PreferencesForm
            //
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(402, 526);
            this.Controls.Add(this.meterHeaderLabel);
            this.Controls.Add(this.fpsLabel);
            this.Controls.Add(this.fpsUpDown);
            this.Controls.Add(this.fpsUnitLabel);
            this.Controls.Add(this.styleLabel);
            this.Controls.Add(this.styleComboBox);
            this.Controls.Add(this.fallLabel);
            this.Controls.Add(this.fallUpDown);
            this.Controls.Add(this.fallUnitLabel);
            this.Controls.Add(this.peakHoldCheckBox);
            this.Controls.Add(this.peakHoldTimeLabel);
            this.Controls.Add(this.peakHoldUpDown);
            this.Controls.Add(this.peakHoldUnitLabel);
            this.Controls.Add(this.meterResetButton);
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
            ((System.ComponentModel.ISupportInitialize)(this.fpsUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fallUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.peakHoldUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label meterHeaderLabel;
        private System.Windows.Forms.Label fpsLabel;
        private KMidiPlayer.Controls.StepNumericUpDown fpsUpDown;
        private System.Windows.Forms.Label fpsUnitLabel;
        private System.Windows.Forms.Label styleLabel;
        private KMidiPlayer.Controls.FlatComboBox styleComboBox;
        private System.Windows.Forms.Label fallLabel;
        private KMidiPlayer.Controls.StepNumericUpDown fallUpDown;
        private System.Windows.Forms.Label fallUnitLabel;
        private System.Windows.Forms.CheckBox peakHoldCheckBox;
        private System.Windows.Forms.Label peakHoldTimeLabel;
        private KMidiPlayer.Controls.StepNumericUpDown peakHoldUpDown;
        private System.Windows.Forms.Label peakHoldUnitLabel;
        private System.Windows.Forms.Button meterResetButton;
        private System.Windows.Forms.Label controllersLabel;
        private System.Windows.Forms.Label controllersNoteLabel;
        private System.Windows.Forms.CheckedListBox controllersListBox;
        private System.Windows.Forms.Label controllersSummaryLabel;
        private System.Windows.Forms.Button clearControllersButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
    }
}
