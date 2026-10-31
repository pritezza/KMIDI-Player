namespace KMidiPlayer
{
    partial class PianoRollForm
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
            this.pianoRollView = new KMidiPlayer.Controls.PianoRollView();
            this.viewMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.overview = new KMidiPlayer.Controls.PianoRollOverview();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            //
            // pianoRollView
            //
            this.pianoRollView.ContextMenuStrip = this.viewMenu;
            this.pianoRollView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pianoRollView.Location = new System.Drawing.Point(0, 25);
            this.pianoRollView.Name = "pianoRollView";
            this.pianoRollView.Size = new System.Drawing.Size(784, 300);
            this.pianoRollView.TabIndex = 0;
            this.pianoRollView.SeekRequested += new System.EventHandler<KMidiPlayer.Controls.SeekRequestedEventArgs>(this.pianoRollView_SeekRequested);
            //
            // viewMenu
            //
            this.viewMenu.Name = "viewMenu";
            this.viewMenu.Size = new System.Drawing.Size(61, 4);
            //
            // overview
            //
            this.overview.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.overview.Location = new System.Drawing.Point(0, 325);
            this.overview.Name = "overview";
            this.overview.Size = new System.Drawing.Size(784, 36);
            this.overview.TabIndex = 1;
            this.overview.SeekRequested += new System.EventHandler<KMidiPlayer.Controls.SeekRequestedEventArgs>(this.pianoRollView_SeekRequested);
            //
            // toolStrip
            //
            this.toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(784, 25);
            this.toolStrip.TabIndex = 2;
            //
            // PianoRollForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 361);
            this.Controls.Add(this.pianoRollView);
            this.Controls.Add(this.overview);
            this.Controls.Add(this.toolStrip);
            this.MinimumSize = new System.Drawing.Size(300, 200);
            this.Name = "PianoRollForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "ピアノロール";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private KMidiPlayer.Controls.PianoRollView pianoRollView;
        private KMidiPlayer.Controls.PianoRollOverview overview;
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ContextMenuStrip viewMenu;
        private System.Windows.Forms.ToolTip toolTip;
    }
}
