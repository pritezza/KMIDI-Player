namespace KMidiPlayer.Controls
{
    /// <summary>
    /// 配色（<see cref="Theme.Current"/>）を自分で反映するコントロール。
    /// <see cref="ThemeStyler.Apply"/> は、このコントロールの子コントロールには手を出さない（子の配色もこのコントロールが決める）。
    /// </summary>
    internal interface IThemeable
    {
        /// <summary>今の配色を反映して、描き直す。</summary>
        void ApplyTheme();
    }
}
