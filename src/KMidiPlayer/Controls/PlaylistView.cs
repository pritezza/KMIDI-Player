using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using KMidiPlayer.Playlist;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// プレイリストの一覧。お気に入りの評価の列に星を描き、星の上でのクリックや左右のドラッグで評価を変えられるようにする。
    /// 再生中の曲の行には、最初の列に再生状態の印（▶ ❚❚ ■）を描く。
    /// </summary>
    /// <remarks>
    /// 各行の <see cref="ListViewItem.Tag"/> には <see cref="PlaylistItem"/> を入れておくこと。
    /// 評価の列の上でマウスのボタンを押したときは、ListView 本来の処理（行の選択やドラッグの開始）をさせず、
    /// 押している間の左右の動きで評価を選ばせる。評価は離したときに確定し、<see cref="RatingChanged"/> で知らせる。
    /// </remarks>
    public class PlaylistView : ListView, IThemeable
    {
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_CAPTURECHANGED = 0x0215;
        private const int LVM_GETHEADER = 0x101F;

        /// <summary>再生状態の印（▶ ❚❚ ■）の大きさ。</summary>
        private const int MarkSize = 9;

        private ListViewItem _currentItem;
        private PlaybackState _currentState;

        // 評価をドラッグ中の行（していなければ null）と、その間に表示する評価
        private ListViewItem _ratingItem;
        private int _ratingValue;

        /// <summary>ボタンを押したときの評価。</summary>
        private int _ratingStart;

        /// <summary>押してから、表示する評価が一度でも変わったか。</summary>
        private bool _ratingMoved;

        private HeaderPainter _headerPainter;

        public PlaylistView()
        {
            OwnerDraw = true;
            DoubleBuffered = true; // 再描画のちらつきを抑える
            BorderStyle = BorderStyle.None; // 枠は描かず、背景の色の違いで区切る
        }

        /// <summary>評価が操作で変わったときに発生する。</summary>
        [Category("Action")]
        public event EventHandler<RatingEventArgs> RatingChanged;

        /// <summary>評価の星を描く列の番号（-1 なら描かない）。</summary>
        [Category("Behavior")]
        [DefaultValue(-1)]
        public int RatingColumnIndex { get; set; } = -1;

        /// <summary>再生中の曲の行。最初の列の左端に、再生状態の印を描く。無ければ null。</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ListViewItem CurrentItem
        {
            get => _currentItem;
            set
            {
                if (value == _currentItem)
                {
                    return;
                }
                ListViewItem old = _currentItem;
                _currentItem = value;
                InvalidateMark(old);
                InvalidateMark(value);
            }
        }

        /// <summary>再生中の曲の状態（印の形）。</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PlaybackState CurrentState
        {
            get => _currentState;
            set
            {
                if (value != _currentState)
                {
                    _currentState = value;
                    InvalidateMark(_currentItem);
                }
            }
        }

        /// <summary>星 5 つがちょうど収まる、評価の列の幅。</summary>
        [Browsable(false)]
        public int RatingColumnWidth => RatingStars.TotalWidth(Font);

        /// <summary>その行の評価の欄だけを描き直す。</summary>
        public void InvalidateRating(ListViewItem item)
        {
            if (item.ListView == this && RatingColumnIndex >= 0)
            {
                Invalidate(item.SubItems[RatingColumnIndex].Bounds);
            }
        }

        /// <summary>その行の最初の列（再生状態の印）を描き直す。</summary>
        private void InvalidateMark(ListViewItem item)
        {
            if (item != null && item.ListView == this)
            {
                Invalidate(item.SubItems[0].Bounds);
            }
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_LBUTTONDOWN:
                case WM_LBUTTONDBLCLK: // 星を続けてクリックしても、ダブルクリックとして曲を再生しない
                    if (BeginRatingDrag(PointFromLParam(m.LParam), m.Msg == WM_LBUTTONDBLCLK))
                    {
                        return;
                    }
                    break;

                case WM_MOUSEMOVE:
                    if (_ratingItem != null)
                    {
                        UpdateRatingDrag(PointFromLParam(m.LParam).X);
                        return;
                    }
                    break;

                case WM_LBUTTONUP:
                    if (_ratingItem != null)
                    {
                        EndRatingDrag(true);
                        return;
                    }
                    break;

                case WM_CAPTURECHANGED:
                    // ウィンドウの切り替えなどで、離したことが届かなかった
                    if (_ratingItem != null)
                    {
                        EndRatingDrag(false);
                    }
                    break;
            }
            base.WndProc(ref m);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _headerPainter?.ReleaseHandle();
            IntPtr header = SendMessage(Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
            _headerPainter = header != IntPtr.Zero ? new HeaderPainter(this, header) : null;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _headerPainter?.ReleaseHandle();
            _headerPainter = null;
            base.OnHandleDestroyed(e);
        }

        public void ApplyTheme()
        {
            BackColor = Theme.Current.Surface;
            ForeColor = Theme.Current.Text;
            ThemeStyler.ApplyScrollBarTheme(this);
            _headerPainter?.Invalidate();
            Invalidate();
        }

        /// <summary>列の見出しを、区切りの縦線を短くした平らな見た目で描く。</summary>
        protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
        {
            Theme theme = Theme.Current;
            Graphics g = e.Graphics;
            using (var back = new SolidBrush(theme.Surface))
            using (var line = new Pen(theme.Divider))
            {
                g.FillRectangle(back, e.Bounds);
                g.DrawLine(line, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                int top = e.Bounds.Top + e.Bounds.Height / 4;
                g.DrawLine(line, e.Bounds.Right - 1, top, e.Bounds.Right - 1, e.Bounds.Bottom - top);
            }

            TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
            switch (e.Header.TextAlign)
            {
                case HorizontalAlignment.Right:
                    flags |= TextFormatFlags.Right;
                    break;
                case HorizontalAlignment.Center:
                    flags |= TextFormatFlags.HorizontalCenter;
                    break;
            }
            var textBounds = Rectangle.FromLTRB(e.Bounds.Left + 6, e.Bounds.Top, e.Bounds.Right - 6, e.Bounds.Bottom);
            TextRenderer.DrawText(g, e.Header.Text, Font, textBounds, theme.MutedText, flags);
        }

        protected override void OnDrawItem(DrawListViewItemEventArgs e)
        {
            // 列ごとに OnDrawSubItem で描く
            base.OnDrawItem(e);
        }

        /// <remarks>
        /// すべての列を自分で描く。OwnerDraw のとき、DrawDefault で ListView に任せた列には、
        /// 行の背景色（<see cref="ListViewItem.BackColor"/>）や太字（<see cref="ListViewItem.Font"/>）が反映されないため。
        /// </remarks>
        protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
        {
            bool selected = e.Item.Selected;
            bool focused = Focused;
            Theme theme = Theme.Current;
            Color backColor = !selected ? e.Item.BackColor : focused ? theme.Selection : theme.SelectionInactive;
            Color foreColor = e.Item.ForeColor;

            // 塗るのは列の範囲だけにする。最後の列の右の余白は、選択が変わっても ListView が描き直さないので、
            // そこまで塗ると、前に選んでいた行の色が残ってしまう
            using (var brush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            if (e.ColumnIndex == RatingColumnIndex && e.Item.Tag is PlaylistItem item)
            {
                RatingStars.Draw(e.Graphics, e.Bounds, Font, e.Item == _ratingItem ? _ratingValue : item.Rating, theme.StarEmpty);
                return;
            }

            TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
                | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
            switch (Columns[e.ColumnIndex].TextAlign)
            {
                case HorizontalAlignment.Right:
                    flags |= TextFormatFlags.Right;
                    break;
                case HorizontalAlignment.Center:
                    flags |= TextFormatFlags.HorizontalCenter;
                    break;
            }
            Font font = e.Item.UseItemStyleForSubItems ? e.Item.Font : e.SubItem.Font;
            var textBounds = Rectangle.FromLTRB(e.Bounds.Left + 2, e.Bounds.Top, e.Bounds.Right - 2, e.Bounds.Bottom);
            if (e.ColumnIndex == 0 && e.Item == _currentItem)
            {
                // フォントの「▶」などは大きさがそろわないので、図形で描く
                DrawMark(e.Graphics, new Rectangle(e.Bounds.Left + 5, e.Bounds.Top + (e.Bounds.Height - MarkSize) / 2, MarkSize, MarkSize), foreColor);
                textBounds.X += MarkSize + 6;
                textBounds.Width -= MarkSize + 6;
            }
            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, font, textBounds, foreColor, flags);
        }

        /// <summary>再生状態の印（再生中 ▶、一時停止中 ❚❚、停止中 ■）を描く。</summary>
        private void DrawMark(Graphics g, Rectangle bounds, Color color)
        {
            using (var brush = new SolidBrush(color))
            {
                switch (_currentState)
                {
                    case PlaybackState.Playing:
                        SmoothingMode oldMode = g.SmoothingMode;
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.FillPolygon(brush, new[]
                        {
                            new PointF(bounds.Left, bounds.Top),
                            new PointF(bounds.Left, bounds.Bottom),
                            new PointF(bounds.Right, bounds.Top + bounds.Height / 2f),
                        });
                        g.SmoothingMode = oldMode;
                        break;

                    case PlaybackState.Paused:
                        int barWidth = (bounds.Width - 3) / 2;
                        g.FillRectangle(brush, bounds.Left, bounds.Top, barWidth, bounds.Height);
                        g.FillRectangle(brush, bounds.Right - barWidth, bounds.Top, barWidth, bounds.Height);
                        break;

                    default:
                        g.FillRectangle(brush, bounds.Left + 1, bounds.Top + 1, bounds.Width - 2, bounds.Height - 2);
                        break;
                }
            }
        }

        /// <summary>評価の欄の上で押されたら、評価のドラッグを始める。</summary>
        /// <param name="isDoubleClick">
        /// ダブルクリックの 2 回目か。1 回目で付けた評価を 2 回目で未評価に戻さないよう、押しただけで動かなくても評価を変えたものとして扱う。
        /// </param>
        private bool BeginRatingDrag(Point point, bool isDoubleClick)
        {
            // Shift・Ctrl を押しながらのクリックは、複数選択の操作に任せる
            if (RatingColumnIndex < 0 || ModifierKeys != Keys.None)
            {
                return false;
            }
            ListViewHitTestInfo hit = HitTest(point);
            if (hit.Item == null || hit.SubItem == null || hit.Item.SubItems.IndexOf(hit.SubItem) != RatingColumnIndex
                || !(hit.Item.Tag is PlaylistItem item))
            {
                return false;
            }

            Focus();
            _ratingItem = hit.Item;
            _ratingStart = item.Rating;
            _ratingValue = RatingFromX(hit.SubItem.Bounds, point.X);
            _ratingMoved = isDoubleClick;
            Capture = true;
            InvalidateRating(_ratingItem);
            return true;
        }

        private void UpdateRatingDrag(int x)
        {
            int rating = RatingFromX(_ratingItem.SubItems[RatingColumnIndex].Bounds, x);
            if (rating != _ratingValue)
            {
                _ratingValue = rating;
                _ratingMoved = true;
                InvalidateRating(_ratingItem);
            }
        }

        /// <param name="commit">評価を確定するか（false なら元に戻す）。</param>
        private void EndRatingDrag(bool commit)
        {
            ListViewItem item = _ratingItem;
            int rating = _ratingValue;
            if (!_ratingMoved && rating == _ratingStart)
            {
                rating = 0; // 今と同じ数の星をクリックしただけなら、未評価に戻す
            }

            // Capture を外すと WM_CAPTURECHANGED が届くので、先に終わった状態にしておく
            _ratingItem = null;
            Capture = false;
            InvalidateRating(item);

            if (commit && rating != _ratingStart)
            {
                RatingChanged?.Invoke(this, new RatingEventArgs(item, rating));
            }
        }

        /// <summary>
        /// 評価の欄の中の横位置から評価を求める。最初の星より左なら 0、5 つ目の星より右なら 5。
        /// </summary>
        private int RatingFromX(Rectangle cell, int x)
        {
            return RatingStars.RatingFromX(Font, cell.Left, x);
        }

        private static Point PointFromLParam(IntPtr lParam)
        {
            int value = unchecked((int)lParam.ToInt64());
            return new Point((short)(value & 0xFFFF), (short)((value >> 16) & 0xFFFF));
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// 列の見出しの、最後の列より右の余白を、配色に合わせて塗り直す（余白は OwnerDraw でも Windows が描くため）。
        /// 見出しのウィンドウのメッセージを横取りし、Windows が描いた後に上から描く。
        /// </summary>
        private sealed class HeaderPainter : NativeWindow
        {
            private const int WM_PAINT = 0x000F;
            private const int WM_PRINT = 0x0317;
            private const int WM_PRINTCLIENT = 0x0318;

            private readonly PlaylistView _owner;

            public HeaderPainter(PlaylistView owner, IntPtr header)
            {
                _owner = owner;
                AssignHandle(header);
            }

            public void Invalidate()
            {
                InvalidateRect(Handle, IntPtr.Zero, true);
            }

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);
                switch (m.Msg)
                {
                    case WM_PAINT:
                        using (Graphics g = Graphics.FromHwnd(Handle))
                        {
                            PaintRemainder(g);
                        }
                        break;
                    case WM_PRINT:
                    case WM_PRINTCLIENT:
                        using (Graphics g = Graphics.FromHdc(m.WParam))
                        {
                            PaintRemainder(g);
                        }
                        break;
                }
            }

            private void PaintRemainder(Graphics g)
            {
                GetClientRect(Handle, out RECT client);
                int right = 0;
                foreach (ColumnHeader column in _owner.Columns)
                {
                    right += column.Width;
                }
                if (client.Right <= right || client.Bottom <= 0)
                {
                    return;
                }

                Theme theme = Theme.Current;
                using (var back = new SolidBrush(theme.Surface))
                using (var line = new Pen(theme.Divider))
                {
                    g.FillRectangle(back, right, 0, client.Right - right, client.Bottom);
                    g.DrawLine(line, right, client.Bottom - 1, client.Right, client.Bottom - 1);
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct RECT
            {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;
            }

            [DllImport("user32.dll")]
            private static extern bool GetClientRect(IntPtr hwnd, out RECT rect);

            [DllImport("user32.dll")]
            private static extern bool InvalidateRect(IntPtr hwnd, IntPtr rect, bool erase);
        }
    }
}
