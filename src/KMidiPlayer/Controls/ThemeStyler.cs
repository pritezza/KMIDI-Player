using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// 画面（フォームとその中のコントロール）に、配色（<see cref="Theme.Current"/>）を当てる。
    /// </summary>
    /// <remarks>
    /// 標準のコントロールは、色と平らな見た目（FlatStyle.Flat、枠なし）を設定する。
    /// 自分で描くコントロールは、<see cref="IThemeable"/> で自分の配色を決めるか、<see cref="Theme.Current"/> の色で描く。
    /// メニュー・ツールバー・ステータスバーは、<see cref="ToolStripManager.Renderer"/>（すべての ToolStrip 共通）で描く。
    /// </remarks>
    public static class ThemeStyler
    {
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19; // Windows 10 1809～1909
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;     // Windows 10 2004 以降・Windows 11
        private const int WM_NCACTIVATE = 0x0086;
        private const int LVM_GETHEADER = 0x101F;

        /// <summary>文字を控えめな色（<see cref="Theme.MutedText"/>）で書くラベル。</summary>
        private static readonly ConditionalWeakTable<Control, object> MutedLabels = new ConditionalWeakTable<Control, object>();

        /// <summary>
        /// 配色を変え、開いているすべての画面に反映する。起動時（画面を作る前）にも呼ぶこと。
        /// </summary>
        public static void SetTheme(ThemeKind kind)
        {
            Theme.Current = Theme.FromKind(kind);
            ToolStripManager.Renderer = new FlatToolStripRenderer(Theme.Current);
            foreach (Form form in Application.OpenForms)
            {
                Apply(form);
            }
        }

        /// <summary>ラベルの文字を、控えめな色にする（見出し・補足用）。<see cref="Apply"/> の前に呼ぶ。</summary>
        public static void SetMuted(Control label)
        {
            MutedLabels.Remove(label);
            MutedLabels.Add(label, null);
        }

        /// <summary>コントロールとその子に、今の配色を当てて描き直す。</summary>
        public static void Apply(Control control)
        {
            Theme theme = Theme.Current;
            if (control is IThemeable themeable)
            {
                themeable.ApplyTheme();
                return;
            }

            switch (control)
            {
                case Form form:
                    form.BackColor = theme.Background;
                    form.ForeColor = theme.Text;
                    form.HandleCreated -= Form_HandleCreated;
                    form.HandleCreated += Form_HandleCreated;
                    ApplyTitleBar(form);
                    break;

                case ToolStrip strip:
                    strip.BackColor = theme.Background;
                    strip.ForeColor = theme.Text;
                    break;

                case Button button:
                    ApplyButton(button, button.FindForm()?.AcceptButton == button);
                    break;

                case CheckBox _:
                case RadioButton _:
                    control.ForeColor = theme.Text;
                    break;

                case Label label:
                    label.ForeColor = MutedLabels.TryGetValue(label, out _) ? theme.MutedText : theme.Text;
                    break;

                case ListBox _:
                case TextBoxBase _:
                case ListView _:
                case TreeView _:
                    control.BackColor = theme.Surface;
                    control.ForeColor = theme.Text;
                    ApplyScrollBarTheme(control);
                    break;

                case ScrollBar _:
                    ApplyScrollBarTheme(control);
                    break;

                case Panel _:
                case SplitContainer _:
                case GroupBox _:
                case UserControl _:
                case TrackBar _:
                    control.BackColor = theme.Background;
                    control.ForeColor = theme.Text;
                    break;
            }

            foreach (Control child in control.Controls)
            {
                Apply(child);
            }
            control.Invalidate();
        }

        /// <summary>
        /// スクロールバーの見た目を配色に合わせる（暗い配色では、Windows のダークモードのスクロールバーにする）。
        /// ウィンドウができる前なら、できたときに設定する。
        /// </summary>
        internal static void ApplyScrollBarTheme(Control control)
        {
            if (!control.IsHandleCreated)
            {
                control.HandleCreated -= ScrollBarOwner_HandleCreated;
                control.HandleCreated += ScrollBarOwner_HandleCreated;
                return;
            }

            bool dark = Theme.Current.IsDark;
            SetWindowTheme(control.Handle, dark ? "DarkMode_Explorer" : "Explorer", null);
            if (control is ListView)
            {
                // 列の見出しの、列の無い部分（右の余白）も暗くする
                IntPtr header = SendMessage(control.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
                if (header != IntPtr.Zero)
                {
                    SetWindowTheme(header, dark ? "DarkMode_ItemsView" : null, null);
                }
            }
        }

        /// <summary>標準のボタンを、平らな見た目にする。<paramref name="isDefault"/> なら強調の色で塗る（OK ボタンなど）。</summary>
        private static void ApplyButton(Button button, bool isDefault)
        {
            Theme theme = Theme.Current;
            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
            button.FlatAppearance.BorderSize = 1;
            if (isDefault)
            {
                button.BackColor = theme.Accent;
                button.ForeColor = theme.AccentText;
                button.FlatAppearance.BorderColor = theme.Accent;
                button.FlatAppearance.MouseOverBackColor = theme.AccentHover;
                button.FlatAppearance.MouseDownBackColor = theme.AccentPressed;
            }
            else
            {
                button.BackColor = theme.ControlFill;
                button.ForeColor = theme.Text;
                button.FlatAppearance.BorderColor = theme.Border;
                button.FlatAppearance.MouseOverBackColor = theme.ControlHover;
                button.FlatAppearance.MouseDownBackColor = theme.Pressed;
            }
        }

        /// <summary>タイトルバーを配色に合わせる（暗い配色では暗くする。Windows 10 の古い版では変わらない）。</summary>
        private static void ApplyTitleBar(Form form)
        {
            if (!form.IsHandleCreated)
            {
                return;
            }
            int dark = Theme.Current.IsDark ? 1 : 0;
            if (DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref dark, sizeof(int));
            }

            // タイトルバーは、次に描き直すまで前の色のままなので、今の状態（アクティブかどうか）のまま描き直させる
            if (form.Visible)
            {
                SendMessage(form.Handle, WM_NCACTIVATE, (IntPtr)(Form.ActiveForm == form ? 1 : 0), IntPtr.Zero);
            }
        }

        private static void Form_HandleCreated(object sender, EventArgs e)
        {
            ApplyTitleBar((Form)sender);
        }

        private static void ScrollBarOwner_HandleCreated(object sender, EventArgs e)
        {
            var control = (Control)sender;
            control.HandleCreated -= ScrollBarOwner_HandleCreated;
            ApplyScrollBarTheme(control);
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string subAppName, string subIdList);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);
    }
}
