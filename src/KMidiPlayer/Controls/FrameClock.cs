using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using KMidiPlayer.Midi;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// 決めた fps で、UI スレッドに描き直しの処理（<see cref="Tick"/>）を起こす時計。
    /// </summary>
    /// <remarks>
    /// WinForms の Timer（WM_TIMER）は、タイマーの精度を上げても約 15.6ms 刻みでしか動かない
    /// （33ms を指定すると 31.2ms ではなく 46.8ms ごとになり、30fps のつもりが 21fps になる）。
    /// そのため、専用のスレッドで時間を測り（その間だけタイマーの精度を 1ms にする）、UI スレッドに BeginInvoke で頼む。
    /// 前に頼んだ処理がまだ終わっていなければ、その回はとばす（描くのが間に合わないときに、頼みがたまらないように）。
    /// </remarks>
    internal sealed class FrameClock : IDisposable
    {
        private readonly Control _owner;
        private readonly Action _invokeTick;

        /// <summary>1 フレームの長さ（Stopwatch の単位）。</summary>
        private long _interval;

        /// <summary>動いているスレッドの止める合図（止まっていれば null）。</summary>
        private ManualResetEvent _stopSignal;

        /// <summary>UI スレッドに頼んだ処理が、まだ終わっていないか（1 = 終わっていない）。</summary>
        private int _pending;

        /// <param name="owner">処理を実行する UI スレッドのコントロール（ハンドルを作ってから動かすこと）。</param>
        public FrameClock(Control owner, int framesPerSecond)
        {
            _owner = owner;
            _invokeTick = RunTick;
            FramesPerSecond = framesPerSecond;
        }

        /// <summary>UI スレッドで、1 フレームごとに発生する。</summary>
        public event EventHandler Tick;

        public int FramesPerSecond
        {
            set => Interlocked.Exchange(ref _interval, Stopwatch.Frequency / Math.Max(1, value));
        }

        public bool Enabled
        {
            get => _stopSignal != null;
            set
            {
                if (value == Enabled)
                {
                    return;
                }
                if (value)
                {
                    // スレッドごとに止める合図を作る（止めてすぐ動かし直しても、前のスレッドが確実に止まるように）
                    var stopSignal = new ManualResetEvent(false);
                    _stopSignal = stopSignal;
                    new Thread(() => Run(stopSignal)) { IsBackground = true, Name = "FrameClock" }.Start();
                }
                else
                {
                    _stopSignal.Set();
                    _stopSignal = null;
                }
            }
        }

        public void Dispose()
        {
            Enabled = false;
        }

        private void Run(ManualResetEvent stopSignal)
        {
            NativeMethods.timeBeginPeriod(1); // 待ち時間の精度を 1ms にする
            try
            {
                long next = Stopwatch.GetTimestamp() + Interlocked.Read(ref _interval);
                while (true)
                {
                    long now = Stopwatch.GetTimestamp();
                    if (now < next)
                    {
                        int waitMilliseconds = (int)Math.Max(1, (next - now) * 1000 / Stopwatch.Frequency);
                        if (stopSignal.WaitOne(waitMilliseconds))
                        {
                            break;
                        }
                        continue;
                    }

                    // 遅れが 1 フレームを超えたら、遅れを取り戻そうとせず、今から数え直す
                    long interval = Interlocked.Read(ref _interval);
                    next = now - next > interval ? now + interval : next + interval;

                    if (Interlocked.CompareExchange(ref _pending, 1, 0) == 0)
                    {
                        try
                        {
                            _owner.BeginInvoke(_invokeTick);
                        }
                        catch (InvalidOperationException)
                        {
                            _pending = 0; // ハンドルがまだ無い・もう無い
                        }
                    }
                }
            }
            finally
            {
                NativeMethods.timeEndPeriod(1);
                stopSignal.Dispose();
            }
        }

        private void RunTick()
        {
            try
            {
                if (Enabled)
                {
                    Tick?.Invoke(this, EventArgs.Empty);
                }
            }
            finally
            {
                _pending = 0;
            }
        }
    }
}
