namespace KMidiPlayer
{
    partial class LevelMeterSettingsForm
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
            this.resetButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.fpsUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fallUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.peakHoldUpDown)).BeginInit();
            this.SuspendLayout();
            //
            // fpsLabel
            //
            this.fpsLabel.Location = new System.Drawing.Point(12, 14);
            this.fpsLabel.Name = "fpsLabel";
            this.fpsLabel.Size = new System.Drawing.Size(120, 20);
            this.fpsLabel.TabIndex = 0;
            this.fpsLabel.Text = "更新頻度(&F):";
            this.fpsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // fpsUpDown
            //
            this.fpsUpDown.Location = new System.Drawing.Point(140, 15);
            this.fpsUpDown.Name = "fpsUpDown";
            this.fpsUpDown.Size = new System.Drawing.Size(56, 19);
            this.fpsUpDown.TabIndex = 1;
            this.fpsUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.fpsUpDown.ValueChanged += new System.EventHandler(this.Value_Changed);
            //
            // fpsUnitLabel
            //
            this.fpsUnitLabel.Location = new System.Drawing.Point(200, 14);
            this.fpsUnitLabel.Name = "fpsUnitLabel";
            this.fpsUnitLabel.Size = new System.Drawing.Size(150, 20);
            this.fpsUnitLabel.TabIndex = 2;
            this.fpsUnitLabel.Text = "fps（1 秒あたりの更新回数）";
            this.fpsUnitLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // styleLabel
            //
            this.styleLabel.Location = new System.Drawing.Point(12, 44);
            this.styleLabel.Name = "styleLabel";
            this.styleLabel.Size = new System.Drawing.Size(120, 20);
            this.styleLabel.TabIndex = 3;
            this.styleLabel.Text = "表示タイプ(&T):";
            this.styleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // styleComboBox
            //
            this.styleComboBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.styleComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.styleComboBox.FormattingEnabled = true;
            this.styleComboBox.ItemHeight = 18;
            this.styleComboBox.Location = new System.Drawing.Point(140, 43);
            this.styleComboBox.MaxDropDownItems = 10;
            this.styleComboBox.Name = "styleComboBox";
            this.styleComboBox.Size = new System.Drawing.Size(250, 24);
            this.styleComboBox.TabIndex = 4;
            this.styleComboBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.styleComboBox_DrawItem);
            this.styleComboBox.SelectedIndexChanged += new System.EventHandler(this.Value_Changed);
            //
            // fallLabel
            //
            this.fallLabel.Location = new System.Drawing.Point(12, 74);
            this.fallLabel.Name = "fallLabel";
            this.fallLabel.Size = new System.Drawing.Size(120, 20);
            this.fallLabel.TabIndex = 5;
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
            this.fallUpDown.Location = new System.Drawing.Point(140, 75);
            this.fallUpDown.Name = "fallUpDown";
            this.fallUpDown.Size = new System.Drawing.Size(56, 19);
            this.fallUpDown.TabIndex = 6;
            this.fallUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.fallUpDown.ValueChanged += new System.EventHandler(this.Value_Changed);
            //
            // fallUnitLabel
            //
            this.fallUnitLabel.Location = new System.Drawing.Point(200, 74);
            this.fallUnitLabel.Name = "fallUnitLabel";
            this.fallUnitLabel.Size = new System.Drawing.Size(150, 20);
            this.fallUnitLabel.TabIndex = 7;
            this.fallUnitLabel.Text = "秒で 10% まで下がる";
            this.fallUnitLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // peakHoldCheckBox
            //
            this.peakHoldCheckBox.AutoSize = true;
            this.peakHoldCheckBox.Location = new System.Drawing.Point(14, 108);
            this.peakHoldCheckBox.Name = "peakHoldCheckBox";
            this.peakHoldCheckBox.Size = new System.Drawing.Size(108, 16);
            this.peakHoldCheckBox.TabIndex = 8;
            this.peakHoldCheckBox.Text = "ピークホールド(&P)";
            this.peakHoldCheckBox.UseVisualStyleBackColor = true;
            this.peakHoldCheckBox.CheckedChanged += new System.EventHandler(this.Value_Changed);
            //
            // peakHoldTimeLabel
            //
            this.peakHoldTimeLabel.Location = new System.Drawing.Point(30, 132);
            this.peakHoldTimeLabel.Name = "peakHoldTimeLabel";
            this.peakHoldTimeLabel.Size = new System.Drawing.Size(102, 20);
            this.peakHoldTimeLabel.TabIndex = 9;
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
            this.peakHoldUpDown.Location = new System.Drawing.Point(140, 133);
            this.peakHoldUpDown.Name = "peakHoldUpDown";
            this.peakHoldUpDown.Size = new System.Drawing.Size(56, 19);
            this.peakHoldUpDown.TabIndex = 10;
            this.peakHoldUpDown.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.peakHoldUpDown.ValueChanged += new System.EventHandler(this.Value_Changed);
            //
            // peakHoldUnitLabel
            //
            this.peakHoldUnitLabel.Location = new System.Drawing.Point(200, 132);
            this.peakHoldUnitLabel.Name = "peakHoldUnitLabel";
            this.peakHoldUnitLabel.Size = new System.Drawing.Size(150, 20);
            this.peakHoldUnitLabel.TabIndex = 11;
            this.peakHoldUnitLabel.Text = "秒（過ぎたら下がる）";
            this.peakHoldUnitLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // resetButton
            //
            this.resetButton.Location = new System.Drawing.Point(12, 176);
            this.resetButton.Name = "resetButton";
            this.resetButton.Size = new System.Drawing.Size(110, 26);
            this.resetButton.TabIndex = 12;
            this.resetButton.Text = "初期値に戻す(&D)";
            this.resetButton.UseVisualStyleBackColor = true;
            this.resetButton.Click += new System.EventHandler(this.resetButton_Click);
            //
            // okButton
            //
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(224, 176);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(80, 26);
            this.okButton.TabIndex = 13;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            //
            // cancelButton
            //
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(310, 176);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(80, 26);
            this.cancelButton.TabIndex = 14;
            this.cancelButton.Text = "キャンセル";
            this.cancelButton.UseVisualStyleBackColor = true;
            //
            // LevelMeterSettingsForm
            //
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(402, 214);
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
            this.Controls.Add(this.resetButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LevelMeterSettingsForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "レベルメーターの設定";
            ((System.ComponentModel.ISupportInitialize)(this.fpsUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fallUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.peakHoldUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

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
        private System.Windows.Forms.Button resetButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
    }
}
