using System;
using System.Drawing;
using System.Windows.Forms;
using KMidiPlayer.Controls;

namespace KMidiPlayer
{
    /// <summary>
    /// レベルメーターの設定画面。値を変えるとすぐに preview で知らせる（メーターで見ながら調整できるように）。
    /// </summary>
    /// <remarks>
    /// OK なら <see cref="Settings"/> を使い、キャンセルなら呼び出し側で元の設定に戻すこと。
    /// </remarks>
    public partial class LevelMeterSettingsForm : Form
    {
        /// <summary>一覧の見本のメーターの幅。</summary>
        private const int SampleWidth = 64;

        private readonly Action<LevelMeterSettings> _preview;

        /// <summary>画面に値を入れている最中か（その間は preview を呼ばない）。</summary>
        private bool _loading;

        /// <summary>デザイナー用。</summary>
        public LevelMeterSettingsForm()
            : this(new LevelMeterSettings(), null)
        {
        }

        /// <param name="settings">開いたときの設定。</param>
        /// <param name="preview">値が変わるたびに、その時点の設定を受け取る。</param>
        public LevelMeterSettingsForm(LevelMeterSettings settings, Action<LevelMeterSettings> preview)
        {
            InitializeComponent();
            ApplyTexts();
            fpsUpDown.Minimum = LevelMeterSettings.MinFramesPerSecond;
            fpsUpDown.Maximum = LevelMeterSettings.MaxFramesPerSecond;
            fallUpDown.Minimum = (decimal)LevelMeterSettings.MinFallSeconds;
            fallUpDown.Maximum = (decimal)LevelMeterSettings.MaxFallSeconds;
            peakHoldUpDown.Minimum = (decimal)LevelMeterSettings.MinPeakHoldSeconds;
            peakHoldUpDown.Maximum = (decimal)LevelMeterSettings.MaxPeakHoldSeconds;

            _preview = preview;
            ShowValues(settings);
            ThemeStyler.Apply(this);
        }

        /// <summary>画面で選んだ設定。</summary>
        public LevelMeterSettings Settings
        {
            get
            {
                var settings = new LevelMeterSettings
                {
                    FramesPerSecond = (int)fpsUpDown.Value,
                    Style = (LevelMeterStyle)Math.Max(0, styleComboBox.SelectedIndex),
                    FallSeconds = (double)fallUpDown.Value,
                    PeakHold = peakHoldCheckBox.Checked,
                    PeakHoldSeconds = (double)peakHoldUpDown.Value,
                };
                settings.Normalize();
                return settings;
            }
        }

        /// <summary>表示言語に合わせて、画面の文字列を設定する（表示タイプの一覧は <see cref="LevelMeterStyle"/> の順）。</summary>
        private void ApplyTexts()
        {
            Text = Texts.LevelMeterTitle;
            fpsLabel.Text = Texts.FpsLabel;
            fpsUnitLabel.Text = Texts.FpsUnit;
            styleLabel.Text = Texts.StyleLabel;
            styleComboBox.Items.AddRange(Texts.MeterStyles);
            fallLabel.Text = Texts.FallLabel;
            fallUnitLabel.Text = Texts.FallUnit;
            peakHoldCheckBox.Text = Texts.PeakHold;
            peakHoldTimeLabel.Text = Texts.PeakHoldTimeLabel;
            peakHoldUnitLabel.Text = Texts.PeakHoldUnit;
            resetButton.Text = Texts.RestoreDefaults;
            okButton.Text = Texts.Ok;
            cancelButton.Text = Texts.Cancel;
        }

        private void ShowValues(LevelMeterSettings settings)
        {
            settings = settings.Clone();
            settings.Normalize();
            _loading = true;
            try
            {
                fpsUpDown.Value = settings.FramesPerSecond;
                styleComboBox.SelectedIndex = (int)settings.Style;
                fallUpDown.Value = (decimal)Math.Round(settings.FallSeconds, 1);
                peakHoldCheckBox.Checked = settings.PeakHold;
                peakHoldUpDown.Value = (decimal)Math.Round(settings.PeakHoldSeconds, 1);
            }
            finally
            {
                _loading = false;
            }
            UpdateEnabled();
        }

        private void UpdateEnabled()
        {
            peakHoldTimeLabel.Enabled = peakHoldCheckBox.Checked;
            peakHoldUpDown.Enabled = peakHoldCheckBox.Checked;
            peakHoldUnitLabel.Enabled = peakHoldCheckBox.Checked;
        }

        private void Value_Changed(object sender, EventArgs e)
        {
            UpdateEnabled();
            if (!_loading)
            {
                _preview?.Invoke(Settings);
            }
        }

        /// <summary>表示タイプの一覧の各項目に、見本のメーター（レベル 70%・ピーク 90%）と名前を描く。</summary>
        private void styleComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            // 地の色は FlatComboBox が塗ってある
            if (e.Index < 0)
            {
                return;
            }

            Theme theme = Theme.Current;
            bool isEdit = (e.State & DrawItemState.ComboBoxEdit) != 0;
            bool selected = !isEdit && (e.State & DrawItemState.Selected) != 0;
            using (var back = new SolidBrush(selected ? theme.Selection : e.BackColor))
            {
                e.Graphics.FillRectangle(back, e.Bounds);
            }

            var meter = new Rectangle(e.Bounds.Left + 3, e.Bounds.Top + 4, SampleWidth, e.Bounds.Height - 8);
            using (var meterBack = new SolidBrush(theme.MeterBack))
            using (var meterBorder = new Pen(theme.MeterBorder))
            {
                e.Graphics.FillRectangle(meterBack, meter);
                e.Graphics.DrawRectangle(meterBorder, meter.Left, meter.Top, meter.Width - 1, meter.Height - 1);
            }
            var inner = Rectangle.Inflate(meter, -1, -1);
            LevelMeterRenderer.Draw(e.Graphics, inner, (LevelMeterStyle)e.Index, 0.7f, 0.9f, true);

            var textBounds = Rectangle.FromLTRB(meter.Right + 6, e.Bounds.Top, e.Bounds.Right, e.Bounds.Bottom);
            TextRenderer.DrawText(e.Graphics, styleComboBox.Items[e.Index].ToString(), e.Font, textBounds, theme.Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        private void resetButton_Click(object sender, EventArgs e)
        {
            ShowValues(new LevelMeterSettings());
            _preview?.Invoke(Settings);
        }
    }
}
