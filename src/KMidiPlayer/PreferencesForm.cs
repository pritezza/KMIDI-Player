using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KMidiPlayer.Controls;

namespace KMidiPlayer
{
    /// <summary>
    /// 環境設定の画面（レベルメーター、音源に送らない CC）。
    /// </summary>
    /// <remarks>
    /// レベルメーターの値を変えると、すぐに preview で知らせる（メーターで見ながら調整できるように）。
    /// OK なら呼び出し側で <see cref="MeterSettings"/> と <see cref="BlockedControllers"/> を反映し、
    /// キャンセルならレベルメーターを元の設定に戻すこと。
    /// 表示言語・配色・画面の拡大は、表示メニューで変える。
    /// </remarks>
    public partial class PreferencesForm : Form
    {
        private const int ControllerCount = 128;

        /// <summary>表示タイプの一覧の、見本のメーターの幅。</summary>
        private const int SampleWidth = 64;

        private readonly Action<LevelMeterSettings> _preview;

        /// <summary>レベルメーターの値を画面に入れている最中か（その間は preview を呼ばない）。</summary>
        private bool _loading;

        /// <summary>デザイナー用。</summary>
        public PreferencesForm()
            : this(new LevelMeterSettings(), null, new bool[ControllerCount])
        {
        }

        /// <param name="meterSettings">開いたときのレベルメーターの設定。</param>
        /// <param name="preview">レベルメーターの値が変わるたびに、その時点の設定を受け取る。</param>
        /// <param name="blockedControllers">送らない CC（添字が CC 番号）。</param>
        public PreferencesForm(LevelMeterSettings meterSettings, Action<LevelMeterSettings> preview, bool[] blockedControllers)
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
            ShowMeterValues(meterSettings);

            controllersListBox.BeginUpdate();
            for (int controller = 0; controller < ControllerCount; controller++)
            {
                controllersListBox.Items.Add(Texts.ControllerItem(controller), blockedControllers[controller]);
            }
            controllersListBox.EndUpdate();
            UpdateSummary(-1, false);

            UiScale.Apply(this);
            UiScale.CenterVertically(fpsLabel, fpsUpDown);
            UiScale.CenterVertically(fallLabel, fallUpDown);
            UiScale.CenterVertically(peakHoldTimeLabel, peakHoldUpDown);

            // 見出しは太字にする（画面の拡大を当てた後のフォントから作る。この画面を開いている間は拡大を変えられない）
            var headerFont = new Font(Font, FontStyle.Bold);
            meterHeaderLabel.Font = headerFont;
            controllersLabel.Font = headerFont;
            Disposed += (sender, e) => headerFont.Dispose(); // 部品が破棄された後に解放する

            ThemeStyler.SetMuted(controllersNoteLabel);
            ThemeStyler.Apply(this);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // 送らない CC があれば、最初のものが見えるようにする（一覧のウィンドウができてからでないと効かない）
            int first = controllersListBox.CheckedIndices.Count > 0 ? controllersListBox.CheckedIndices[0] : -1;
            if (first >= 0)
            {
                controllersListBox.TopIndex = Math.Max(0, first - 2);
            }
        }

        /// <summary>画面で選んだレベルメーターの設定。</summary>
        public LevelMeterSettings MeterSettings
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

        /// <summary>画面で選んだ、送らない CC（添字が CC 番号）。</summary>
        public bool[] BlockedControllers
        {
            get
            {
                var blocked = new bool[ControllerCount];
                foreach (int index in controllersListBox.CheckedIndices)
                {
                    blocked[index] = true;
                }
                return blocked;
            }
        }

        /// <summary>表示言語に合わせて、画面の文字列を設定する（表示タイプの一覧は <see cref="LevelMeterStyle"/> の順）。</summary>
        private void ApplyTexts()
        {
            Text = Texts.PreferencesTitle;
            meterHeaderLabel.Text = Texts.LevelMeterSection;
            fpsLabel.Text = Texts.FpsLabel;
            fpsUnitLabel.Text = Texts.FpsUnit;
            styleLabel.Text = Texts.StyleLabel;
            styleComboBox.Items.AddRange(Texts.MeterStyles);
            fallLabel.Text = Texts.FallLabel;
            fallUnitLabel.Text = Texts.FallUnit;
            peakHoldCheckBox.Text = Texts.PeakHold;
            peakHoldTimeLabel.Text = Texts.PeakHoldTimeLabel;
            peakHoldUnitLabel.Text = Texts.PeakHoldUnit;
            meterResetButton.Text = Texts.RestoreDefaults;
            controllersLabel.Text = Texts.BlockedControllersLabel;
            controllersNoteLabel.Text = Texts.BlockedControllersNote;
            clearControllersButton.Text = Texts.ClearBlockedControllers;
            okButton.Text = Texts.Ok;
            cancelButton.Text = Texts.Cancel;
        }

        // ---- レベルメーター ----

        private void ShowMeterValues(LevelMeterSettings settings)
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
            UpdateMeterEnabled();
        }

        private void UpdateMeterEnabled()
        {
            peakHoldTimeLabel.Enabled = peakHoldCheckBox.Checked;
            peakHoldUpDown.Enabled = peakHoldCheckBox.Checked;
            peakHoldUnitLabel.Enabled = peakHoldCheckBox.Checked;
        }

        private void Value_Changed(object sender, EventArgs e)
        {
            UpdateMeterEnabled();
            if (!_loading)
            {
                _preview?.Invoke(MeterSettings);
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

            int sampleWidth = UiScale.Scale(styleComboBox, SampleWidth);
            var meter = new Rectangle(e.Bounds.Left + 3, e.Bounds.Top + 4, sampleWidth, e.Bounds.Height - 8);
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

        private void meterResetButton_Click(object sender, EventArgs e)
        {
            ShowMeterValues(new LevelMeterSettings());
            _preview?.Invoke(MeterSettings);
        }

        // ---- 音源に送らない CC ----

        private void controllersListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // ItemCheck はチェックが変わる前に発生するので、変わった後の状態を渡す
            UpdateSummary(e.Index, e.NewValue == CheckState.Checked);
        }

        private void clearControllersButton_Click(object sender, EventArgs e)
        {
            foreach (int index in controllersListBox.CheckedIndices.Cast<int>().ToList())
            {
                controllersListBox.SetItemChecked(index, false);
            }
        }

        /// <summary>送らない CC の番号を、一覧の下にまとめて表示する。</summary>
        /// <param name="changingIndex">これから変わる項目（無ければ -1）。</param>
        /// <param name="willBeChecked">その項目の、変わった後のチェック。</param>
        private void UpdateSummary(int changingIndex, bool willBeChecked)
        {
            var numbers = new List<int>();
            for (int index = 0; index < controllersListBox.Items.Count; index++)
            {
                bool isChecked = index == changingIndex ? willBeChecked : controllersListBox.GetItemChecked(index);
                if (isChecked)
                {
                    numbers.Add(index);
                }
            }
            controllersSummaryLabel.Text = Texts.BlockedControllersSummary(string.Join(", ", numbers));
        }
    }
}
