using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// 画面の拡大（100～200%）。すべてのウィンドウの文字と、それに合わせた配置（ボタン・入力欄・一覧の列など）の大きさを変える。
    /// </summary>
    /// <remarks>
    /// フォームのフォントを大きくすると、WinForms の自動拡大（AutoScaleMode.Font）で、部品の位置・大きさも同じ比率で広がる。
    /// 次のものは自動では変わらないので、<see cref="Apply"/> でまとめて合わせる。
    /// <list type="bullet">
    /// <item>メニュー・ツールバー・ステータスバー・右クリックメニュー（フォームのフォントを引き継がない）</item>
    /// <item>フォントを個別に指定している部品（再生ボタンなどの記号のフォント）</item>
    /// </list>
    /// 自分で描く部品は、<see cref="Of"/> で倍率を求めて、大きさを計算する（フォントが変わると描き直す）。
    /// </remarks>
    public static class UiScale
    {
        public const int MinPercent = 100;
        public const int MaxPercent = 200;
        public const int DefaultPercent = 100;

        /// <summary>環境設定で選べる大きさ（%）。</summary>
        public static readonly int[] Choices = { 100, 110, 125, 150, 175, 200 };

        /// <summary>フォントを個別に指定している部品の、100% のときの大きさ（pt）。</summary>
        private static readonly ConditionalWeakTable<Control, object> ExplicitFontSizes = new ConditionalWeakTable<Control, object>();

        /// <summary>フォームごとの、今当てている大きさ（%）。</summary>
        private static readonly ConditionalWeakTable<Form, object> AppliedPercents = new ConditionalWeakTable<Form, object>();

        private static int _percent = DefaultPercent;

        /// <summary>今の画面の拡大（%）。変えたら、開いている画面に <see cref="Apply"/> すること。</summary>
        public static int Percent
        {
            get => _percent;
            set => _percent = Math.Max(MinPercent, Math.Min(MaxPercent, value));
        }

        /// <summary>
        /// 100% のときの、画面の基本のフォント。画面を作る前に設定すること。
        /// 初期値は、Windows の UI のフォント（メッセージのフォント。日本語の Windows では Yu Gothic UI、英語では Segoe UI）を、
        /// .NET の既定のフォント（<see cref="Control.DefaultFont"/>。日本語の Windows では MS UI Gothic）と同じ大きさにしたもの。
        /// </summary>
        /// <remarks>
        /// 大きさを Windows の設定（「テキストを大きくする」など）に合わせないのは、画面の配置を 100% のときと同じに保つため。
        /// 大きくしたい人は、画面の拡大で大きくする。
        /// </remarks>
        public static Font BaseFont { get; set; } = CreateDefaultBaseFont();

        private static Font CreateDefaultBaseFont()
        {
            using (Font systemFont = SystemFonts.MessageBoxFont) // 読むたびに新しいフォントが作られる
            {
                return new Font(systemFont.FontFamily, Control.DefaultFont.SizeInPoints, FontStyle.Regular, GraphicsUnit.Point);
            }
        }

        /// <summary>今の大きさの、画面の基本のフォント（<see cref="BaseFont"/> を、大きさの分だけ大きくしたもの）。</summary>
        public static Font CreateFont()
        {
            return CreateFont(_percent);
        }

        private static Font CreateFont(int percent)
        {
            Font baseFont = BaseFont;
            return GetFont(baseFont.FontFamily, baseFont.SizeInPoints * percent / 100f, baseFont.Style);
        }

        /// <summary>
        /// 作ったフォント（フォント名・大きさ・スタイルごと）。拡大を変えるたびに作り直すと、前のフォントが解放されずに残るので、同じものは使い回す
        /// （使う大きさは拡大の選択肢の数ほどしか無いので、増え続けることはない。どの画面がまだ使っているか分からないので、解放はしない）。
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<string, Font> Fonts = new System.Collections.Generic.Dictionary<string, Font>();

        /// <summary>フォントを、作ったものがあれば使い回して返す（返したフォントは呼び出し側で解放しないこと）。</summary>
        private static Font GetFont(FontFamily family, float sizeInPoints, FontStyle style)
        {
            string key = family.Name + "|" + Math.Round(sizeInPoints, 2).ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + (int)style;
            if (!Fonts.TryGetValue(key, out Font font))
            {
                font = new Font(family, sizeInPoints, style, GraphicsUnit.Point);
                Fonts.Add(key, font);
            }
            return font;
        }

        /// <summary>Windows のメニューのフォント（SystemFonts.MenuFont は読むたびに新しいフォントを作るので、1 度だけ読む）。</summary>
        private static readonly Font SystemMenuFont = SystemFonts.MenuFont;

        /// <summary>
        /// 自分で描く部品の、大きさの倍率（部品のフォント ÷ 基本のフォント × 画面の DPI ÷ 96）。
        /// 100% で DPI が 96 なら 1。
        /// </summary>
        public static float Of(Control control)
        {
            return control.Font.SizeInPoints / BaseFont.SizeInPoints * control.DeviceDpi / 96f;
        }

        /// <summary>100%・96 DPI のときの大きさ（px）を、部品の倍率に合わせる。</summary>
        public static int Scale(Control control, int pixels)
        {
            return (int)Math.Round(pixels * Of(control));
        }

        /// <summary>
        /// フォームに今の大きさを当てる（文字・配置・メニュー・記号のフォント）。ウィンドウが画面より大きくなるときは、画面に収める。
        /// フォームを作った直後（保存した位置・大きさを戻す前）と、大きさを変えたときに呼ぶ。
        /// </summary>
        public static void Apply(Form form)
        {
            int oldPercent = AppliedPercents.TryGetValue(form, out object applied) ? (int)applied : DefaultPercent;
            RememberExplicitFonts(form, oldPercent);

            Font font = CreateFont();
            if (oldPercent != _percent || !form.Font.Equals(font))
            {
                // 自動拡大は、フォントの平均の文字幅・高さの比率で、部品の位置・大きさを広げる。
                // そのままでは、フォントの種類を替えただけでも配置が変わる（Yu Gothic UI は MS UI Gothic より行が高いので、全体が大きくなる）。
                // そこで、自動拡大の後に、実際に広がった倍率と拡大の比率（例: 100% → 150% なら 1.5 倍）の差だけ、全体を拡大・縮小し直す
                // （フォントの文字幅・高さは整数の px で測るので、フォントどうしの比率では、縦横で少しずれた倍率にもなる）
                float ratio = _percent / (float)oldPercent;
                Size oldClientSize = form.ClientSize;
                SizeF before = form.AutoScaleDimensions; // 自動拡大の基準（今の配置を作ったときのフォントの大きさ）
                form.Font = font;
                SizeF after = form.AutoScaleDimensions;  // 自動拡大の後は、新しいフォントの大きさになる
                if (before.Width > 0 && before.Height > 0 && after.Width > 0 && after.Height > 0)
                {
                    var correction = new SizeF(ratio * before.Width / after.Width, ratio * before.Height / after.Height);
                    if (Math.Abs(correction.Width - 1) > 0.001f || Math.Abs(correction.Height - 1) > 0.001f)
                    {
                        Point location = form.Location;
                        form.Scale(correction);
                        form.Location = location; // ウィンドウの位置は動かさない
                    }
                }

                // 小さくするとき、ウィンドウの大きさは、前の（大きい）最小の大きさに止められて縮まないことがあるので、縮め直す
                if (ratio < 1 && form.WindowState == FormWindowState.Normal)
                {
                    var expected = new Size((int)Math.Round(oldClientSize.Width * ratio), (int)Math.Round(oldClientSize.Height * ratio));
                    if (form.ClientSize.Width > expected.Width || form.ClientSize.Height > expected.Height)
                    {
                        form.ClientSize = expected;
                    }
                }
            }
            ApplyExplicitFonts(form);
            ApplyMenus(form);
            FitToScreen(form);

            AppliedPercents.Remove(form);
            AppliedPercents.Add(form, _percent);
        }

        /// <summary>
        /// 部品の縦の中央を、<paramref name="reference"/> の縦の中央にそろえる。
        /// 数値の欄（NumericUpDown）のように、高さが Windows によってフォントの行の高さで決まる部品を、行の中央に置き直すのに使う
        /// （フォントの種類で行の高さが違うので、配置で決めた位置のままでは上下にずれる）。<see cref="Apply"/> の後に呼ぶ。
        /// </summary>
        public static void CenterVertically(Control reference, params Control[] controls)
        {
            int center = reference.Top + reference.Height / 2;
            foreach (Control control in controls)
            {
                control.Top = center - control.Height / 2;
            }
        }

        /// <summary>
        /// メニュー・ツールバー・ステータスバー・右クリックメニューの文字を、今の大きさにする（作り直したメニューにも呼ぶ）。
        /// </summary>
        public static void ApplyMenus(Control root)
        {
            ApplyMenus(root, CreateMenuFont());
        }

        /// <summary>メニュー類のフォント（Windows のメニューのフォントを、大きさの分だけ大きくしたもの）。</summary>
        private static Font CreateMenuFont()
        {
            Font menuFont = SystemMenuFont;
            return GetFont(menuFont.FontFamily, menuFont.SizeInPoints * _percent / 100f, menuFont.Style);
        }

        private static void ApplyMenus(Control control, Font font)
        {
            if (control is ToolStrip strip)
            {
                ApplyToolStrip(strip, font);
            }
            if (control.ContextMenuStrip != null)
            {
                ApplyToolStrip(control.ContextMenuStrip, font);
            }
            foreach (Control child in control.Controls)
            {
                ApplyMenus(child, font);
            }
        }

        private static void ApplyToolStrip(ToolStrip strip, Font font)
        {
            if (!strip.Font.Equals(font))
            {
                strip.Font = font;
            }
            // チェックの印・アイコンの欄も、文字に合わせて広げる
            int icon = (int)Math.Round(16 * _percent / 100f * strip.DeviceDpi / 96f);
            if (strip.ImageScalingSize.Width != icon)
            {
                strip.ImageScalingSize = new Size(icon, icon);
            }
            foreach (ToolStripItem item in strip.Items)
            {
                if (item is ToolStripDropDownItem dropDownItem && dropDownItem.HasDropDownItems)
                {
                    ApplyToolStrip(dropDownItem.DropDown, font);
                }
            }
        }

        /// <summary>フォントを個別に指定している部品（親と違うフォント）の、100% のときの大きさを覚えておく。</summary>
        private static void RememberExplicitFonts(Control parent, int currentPercent)
        {
            foreach (Control child in parent.Controls)
            {
                // メニュー類は親のフォントを引き継がない作りなので、別に合わせる（ApplyMenus）
                if (!(child is ToolStrip) && !ExplicitFontSizes.TryGetValue(child, out _) && !child.Font.Equals(parent.Font))
                {
                    ExplicitFontSizes.Add(child, child.Font.SizeInPoints * 100f / currentPercent);
                }
                RememberExplicitFonts(child, currentPercent);
            }
        }

        private static void ApplyExplicitFonts(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                if (ExplicitFontSizes.TryGetValue(child, out object baseSize))
                {
                    float size = (float)baseSize * _percent / 100f;
                    if (Math.Abs(child.Font.SizeInPoints - size) > 0.01f)
                    {
                        child.Font = GetFont(child.Font.FontFamily, size, child.Font.Style);
                    }
                }
                ApplyExplicitFonts(child);
            }
        }

        /// <summary>大きくした結果、ウィンドウ（と最小の大きさ）が画面より大きくなったら、画面に収める。</summary>
        private static void FitToScreen(Form form)
        {
            Rectangle area = Screen.FromControl(form).WorkingArea;
            Size minimum = form.MinimumSize;
            if (minimum.Width > area.Width || minimum.Height > area.Height)
            {
                form.MinimumSize = new Size(Math.Min(minimum.Width, area.Width), Math.Min(minimum.Height, area.Height));
            }
            if (form.WindowState == FormWindowState.Normal && (form.Width > area.Width || form.Height > area.Height))
            {
                form.Size = new Size(Math.Min(form.Width, area.Width), Math.Min(form.Height, area.Height));
            }
        }
    }
}
