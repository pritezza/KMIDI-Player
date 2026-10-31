using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using KMidiPlayer.Controls;

namespace KMidiPlayer
{
    /// <summary>
    /// 環境設定の画面（表示言語、配色、音源に送らない CC）。OK のとき、呼び出し側で <see cref="Language"/>・<see cref="Theme"/> と
    /// <see cref="BlockedControllers"/> を反映すること。
    /// </summary>
    public partial class PreferencesForm : Form
    {
        private const int ControllerCount = 128;

        /// <summary>デザイナー用。</summary>
        public PreferencesForm()
            : this(UiLanguage.Japanese, ThemeKind.Light, new bool[ControllerCount])
        {
        }

        /// <param name="blockedControllers">送らない CC（添字が CC 番号）。</param>
        public PreferencesForm(UiLanguage language, ThemeKind theme, bool[] blockedControllers)
        {
            InitializeComponent();
            Text = Texts.PreferencesTitle;
            languageLabel.Text = Texts.LanguageLabel;
            themeLabel.Text = Texts.ThemeLabel;
            themeComboBox.Items.AddRange(Texts.Themes);
            themeComboBox.SelectedIndex = (int)theme;
            controllersLabel.Text = Texts.BlockedControllersLabel;
            controllersNoteLabel.Text = Texts.BlockedControllersNote;
            clearControllersButton.Text = Texts.ClearBlockedControllers;
            okButton.Text = Texts.Ok;
            cancelButton.Text = Texts.Cancel;

            languageComboBox.Items.AddRange(Texts.Languages);
            languageComboBox.SelectedIndex = (int)language;

            controllersListBox.BeginUpdate();
            for (int controller = 0; controller < ControllerCount; controller++)
            {
                controllersListBox.Items.Add(Texts.ControllerItem(controller), blockedControllers[controller]);
            }
            controllersListBox.EndUpdate();
            UpdateSummary(-1, false);

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

        /// <summary>画面で選んだ表示言語。</summary>
        public UiLanguage Language => (UiLanguage)Math.Max(0, languageComboBox.SelectedIndex);

        /// <summary>画面で選んだ配色。</summary>
        public ThemeKind Theme => (ThemeKind)Math.Max(0, themeComboBox.SelectedIndex);

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
