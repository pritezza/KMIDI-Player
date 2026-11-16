using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using KMidiPlayer.Controls;

namespace KMidiPlayer
{
    /// <summary>
    /// バージョン情報の画面（アプリの名前・バージョンと、作者のページへのリンク）。
    /// </summary>
    /// <remarks>
    /// メッセージボックスにはリンクを置けないので、小さな画面を作る。配置は 100%・96 DPI のときの値で書き、
    /// ほかの画面と同じく自動拡大（AutoScaleMode.Font）と <see cref="UiScale.Apply"/> で画面の拡大に合わせる。
    /// </remarks>
    public class AboutForm : Form
    {
        /// <summary>作者のページ。</summary>
        public const string AuthorUrl = "https://github.com/pritezza";

        private readonly LinkLabel _linkLabel;

        // この画面で作った画像とフォント（画面を閉じたら解放する）
        private readonly Bitmap _iconImage;
        private readonly Font _nameFont;

        /// <summary>アプリのバージョン（プロジェクトの Version。例: 1.1.0）。</summary>
        public static string AppVersion =>
            typeof(AboutForm).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Application.ProductVersion;

        /// <param name="appIcon">左に出すアプリのアイコン。無ければ null。</param>
        public AboutForm(Icon appIcon)
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(6F, 12F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(300, 128);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = Texts.AboutTitle;

            if (appIcon != null)
            {
                using (var icon = new Icon(appIcon, 48, 48))
                {
                    _iconImage = icon.ToBitmap();
                }
            }
            var iconBox = new PictureBox
            {
                Location = new Point(16, 16),
                Size = new Size(48, 48),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = _iconImage,
            };
            var nameLabel = new Label
            {
                AutoSize = true,
                Location = new Point(76, 16),
                Text = "KMIDI Player",
            };
            var versionLabel = new Label
            {
                AutoSize = true,
                Location = new Point(76, 38),
                Text = Texts.AboutVersion(AppVersion),
            };
            _linkLabel = new LinkLabel
            {
                AutoSize = true,
                Location = new Point(76, 58),
                Text = AuthorUrl,
                TabIndex = 0,
            };
            _linkLabel.LinkClicked += LinkLabel_LinkClicked;
            var okButton = new Button
            {
                DialogResult = DialogResult.OK,
                Location = new Point(208, 92),
                Size = new Size(80, 26),
                Text = Texts.Ok,
                TabIndex = 1,
            };
            AcceptButton = okButton;
            CancelButton = okButton;
            Controls.AddRange(new Control[] { iconBox, nameLabel, versionLabel, _linkLabel, okButton });
            ResumeLayout(false);
            PerformLayout();

            UiScale.Apply(this);

            // アプリの名前は大きめの太字にする（画面の拡大を当てた後のフォントから作る。この画面を開いている間は拡大を変えられない）
            _nameFont = new Font(Font.FontFamily, Font.SizeInPoints * 1.4f, FontStyle.Bold, GraphicsUnit.Point);
            nameLabel.Font = _nameFont;
            ThemeStyler.Apply(this);
            ApplyLinkColors();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing); // 先に部品を破棄してから、部品が使っていた画像とフォントを解放する
            if (disposing)
            {
                _iconImage?.Dispose();
                _nameFont?.Dispose();
            }
        }

        /// <summary>リンクの色を配色に合わせる（ThemeStyler はラベルの文字の色しか変えないため）。</summary>
        private void ApplyLinkColors()
        {
            Theme theme = Theme.Current;
            _linkLabel.LinkColor = theme.Accent;
            _linkLabel.ActiveLinkColor = theme.AccentPressed;
            _linkLabel.VisitedLinkColor = theme.Accent;
        }

        private void LinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(AuthorUrl); // 既定のブラウザで開く
            }
            catch (Win32Exception ex)
            {
                MessageBox.Show(this, ex.Message, Texts.AboutTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
