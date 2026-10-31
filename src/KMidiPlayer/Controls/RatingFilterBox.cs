using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using KMidiPlayer.Playlist;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// お気に入りの評価のフィルター。プレイリストの評価と同じく星 5 つで表し、「この数の星以上の曲だけを表示する」を選ぶ。
    /// </summary>
    /// <remarks>
    /// 星をクリックするとその数（今と同じ数の星なら 0 = すべて表示）、押したまま左右に動かすとその位置の数にする。
    /// 選んでいる間は星の表示だけを変え、ボタンを離したときに <see cref="ValueChanged"/> で知らせる（リストの絞り込みを何度もしないため）。
    /// マウスホイールと ↑ ↓ キーで 1 ずつ、0～5 キーでその数にする。星の右には「すべて」「以上」を添える。
    /// </remarks>
    public class RatingFilterBox : Control
    {
        private int _value;

        // 押している間に表示する値（押していなければ -1）と、押したときの値
        private int _dragValue = -1;
        private int _dragStart;
        private bool _dragMoved;

        /// <summary>マウスホイールの、1 目盛りに満たない分。</summary>
        private int _wheelDelta;

        public RatingFilterBox()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.Selectable,
                true);
            TabStop = true;
        }

        /// <summary>値が操作で変わったときに発生する。</summary>
        [Category("Action")]
        public event EventHandler ValueChanged;

        /// <summary>表示する曲の評価の下限（0 = すべて表示、1～5）。</summary>
        [Category("Behavior")]
        [DefaultValue(0)]
        public int Value
        {
            get => _value;
            set
            {
                value = Math.Max(0, Math.Min(PlaylistItem.MaxRating, value));
                if (value != _value)
                {
                    _value = value;
                    Invalidate();
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        protected override Size DefaultSize => new Size(112, 20);

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }
            Focus();
            _dragStart = _value;
            _dragValue = RatingStars.RatingFromX(Font, 0, e.X);
            _dragMoved = e.Clicks >= 2; // ダブルクリックの 2 回目で、1 回目に選んだ数を 0 に戻さない
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragValue < 0)
            {
                return;
            }
            int value = RatingStars.RatingFromX(Font, 0, e.X);
            if (value != _dragValue)
            {
                _dragValue = value;
                _dragMoved = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && _dragValue >= 0)
            {
                EndDrag(true);
            }
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (_dragValue >= 0 && !Capture)
            {
                EndDrag(false); // ウィンドウの切り替えなどで、離したことが届かなかった
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            _wheelDelta += e.Delta;
            int steps = _wheelDelta / 120;
            _wheelDelta -= steps * 120;
            Value += steps;
            if (e is HandledMouseEventArgs handled)
            {
                handled.Handled = true;
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            return keyData == Keys.Up || keyData == Keys.Down || base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Up)
            {
                Value++;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                Value--;
                e.Handled = true;
            }
            else if (e.Modifiers == Keys.None && e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D0 + PlaylistItem.MaxRating)
            {
                Value = e.KeyCode - Keys.D0;
                e.Handled = true;
            }
            else if (e.Modifiers == Keys.None && e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad0 + PlaylistItem.MaxRating)
            {
                Value = e.KeyCode - Keys.NumPad0;
                e.Handled = true;
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            Theme theme = Theme.Current;
            FlatPainter.PaintField(g, ClientRectangle, Parent?.BackColor ?? theme.Background, Focused);

            int value = _dragValue >= 0 ? _dragValue : _value;
            RatingStars.Draw(g, ClientRectangle, Font, value, theme.StarEmpty);

            // 星の右に、意味を添える（0 = すべて、1～4 = 以上）
            string suffix = value == 0 ? Texts.RatingFilterAll : value < PlaylistItem.MaxRating ? Texts.RatingFilterAtLeast : string.Empty;
            int left = RatingStars.TotalWidth(Font) - RatingStars.Padding + 2;
            var suffixBounds = new Rectangle(left, 0, Math.Max(0, Width - left - 2), Height);
            TextRenderer.DrawText(g, suffix, Font, suffixBounds, theme.MutedText,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        /// <param name="commit">選んだ値を確定するか（false なら元に戻す）。</param>
        private void EndDrag(bool commit)
        {
            int value = _dragValue;
            if (!_dragMoved && value == _dragStart)
            {
                value = 0; // 今と同じ数の星をクリックしただけなら、すべて表示に戻す
            }
            _dragValue = -1;
            Invalidate();
            if (commit)
            {
                Value = value;
            }
        }
    }
}
