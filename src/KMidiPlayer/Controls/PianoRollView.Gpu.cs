using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace KMidiPlayer.Controls
{
    // GPU で描く: ピアノロールの上に、WPF で描く子ウィンドウ（HwndSource）を重ね、同じ描画のコード（Render）を WPF で描く。
    // WPF の描画は GPU で行われる（GPU を使えない環境では、今までどおり GDI+ で描く）。
    //
    // 子ウィンドウは表示するだけにし、マウスの判定に「透過」と答えて、マウスの操作はすべて下のピアノロールに届ける
    // （クリック・ドラッグ・ホイール・右クリックメニュー・ツールチップ・カーソルは、GDI+ で描くときと同じ処理で動く）
    public partial class PianoRollView
    {
        private const int WM_NCHITTEST = 0x0084;
        private const int HTTRANSPARENT = -1;
        private const int WS_CHILD = 0x40000000;
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_CLIPSIBLINGS = 0x04000000;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        private HwndSource _gpuSource;
        private DrawingVisual _gpuVisual;
        private WpfRollResources _gpuResources;
        private bool _gpuRenderPending;
        private bool _useGpu;

        /// <summary>この PC で、WPF の描画が GPU（ハードウェア）で行われるか。</summary>
        public static bool IsGpuAvailable => (RenderCapability.Tier >> 16) >= 1;

        /// <summary>GPU で描くか。GPU を使えない環境では、true にしても GDI+（CPU）で描く。</summary>
        [Category("Behavior")]
        [DefaultValue(false)]
        public bool UseGpu
        {
            get => _useGpu;
            set
            {
                _useGpu = value;
                UpdateGpuSource();
            }
        }

        /// <summary>今、GPU で描いているか。</summary>
        [Browsable(false)]
        public bool IsUsingGpu => _gpuSource != null;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateGpuSource();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            DestroyGpuSource();
            base.OnHandleDestroyed(e);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (_gpuSource != null)
            {
                SetWindowPos(_gpuSource.Handle, IntPtr.Zero, 0, 0, ClientSize.Width, ClientSize.Height, SWP_NOZORDER | SWP_NOACTIVATE);
                RequestGpuRender();
            }
        }

        protected override void OnInvalidated(Forms.InvalidateEventArgs e)
        {
            base.OnInvalidated(e);
            RequestGpuRender();
        }

        /// <summary>GPU で描くかどうかに合わせて、WPF の子ウィンドウを作る・壊す。</summary>
        private void UpdateGpuSource()
        {
            bool use = _useGpu && IsHandleCreated && IsGpuAvailable;
            if (use == (_gpuSource != null))
            {
                return;
            }
            if (!use)
            {
                DestroyGpuSource();
                Invalidate();
                return;
            }

            var parameters = new HwndSourceParameters("KMidiPlayer.PianoRoll.Gpu")
            {
                ParentWindow = Handle,
                WindowStyle = WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS,
                PositionX = 0,
                PositionY = 0,
                Width = Math.Max(1, ClientSize.Width),
                Height = Math.Max(1, ClientSize.Height),
            };
            _gpuSource = new HwndSource(parameters);
            _gpuSource.AddHook(GpuWindowHook);
            _gpuSource.CompositionTarget.BackgroundColor = WpfRollCanvas.ToWpf(PianoRollPalette.DarkBackColor);
            _gpuVisual = new DrawingVisual();
            _gpuResources = new WpfRollResources();
            _gpuSource.RootVisual = _gpuVisual;
            RequestGpuRender();
        }

        private void DestroyGpuSource()
        {
            if (_gpuSource == null)
            {
                return;
            }
            _gpuSource.RemoveHook(GpuWindowHook);
            _gpuSource.Dispose();
            _gpuSource = null;
            _gpuVisual = null;
            _gpuResources = null;
        }

        /// <summary>
        /// GPU での描き直しを頼む。何度頼まれても、WPF の次の描画の前に 1 回だけ描く。
        /// </summary>
        private void RequestGpuRender()
        {
            if (_gpuSource == null || _gpuRenderPending)
            {
                return;
            }
            _gpuRenderPending = true;
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, new Action(RenderGpu));
        }

        private void RenderGpu()
        {
            _gpuRenderPending = false;
            if (_gpuSource == null || IsDisposed)
            {
                return;
            }
            double pixelsPerDip = _gpuSource.CompositionTarget.TransformToDevice.M11;
            using (DrawingContext dc = _gpuVisual.RenderOpen())
            {
                var canvas = new WpfRollCanvas(dc, _gpuResources, pixelsPerDip, ClientSize.Width, ClientSize.Height);
                Render(canvas);
                canvas.Finish();
            }
        }

        /// <summary>WPF の子ウィンドウは、マウスの判定に「透過」と答える（マウスの操作は下のピアノロールが受ける）。</summary>
        private static IntPtr GpuWindowHook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message == WM_NCHITTEST)
            {
                handled = true;
                return (IntPtr)HTTRANSPARENT;
            }
            return IntPtr.Zero;
        }

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
    }
}
