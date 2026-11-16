using System;
using System.ComponentModel;

namespace KMidiPlayer.Controls
{
    /// <summary>
    /// 拡大率を選ぶスライダー（ピアノロールの時間方向の倍率用）。目盛りは対数で、どの倍率でも同じ感覚で動かせる。
    /// </summary>
    /// <remarks>
    /// 操作は <see cref="FlatSlider"/> と同じ。ホイール・矢印キー 1 回で、値に <see cref="FlatSlider.Step"/> を掛ける（割る）。
    /// </remarks>
    public class ZoomSlider : FlatSlider
    {
        public ZoomSlider()
        {
            Minimum = 20;
            Maximum = 800;
            Step = 1.25;
        }

        [DefaultValue(20.0)]
        public new double Minimum
        {
            get => base.Minimum;
            set => base.Minimum = value;
        }

        [DefaultValue(800.0)]
        public new double Maximum
        {
            get => base.Maximum;
            set => base.Maximum = value;
        }

        /// <summary>ホイール・矢印キー 1 回で、値に掛ける（割る）倍率。</summary>
        [DefaultValue(1.25)]
        public new double Step
        {
            get => base.Step;
            set => base.Step = value;
        }

        protected override float PositionOf(double value)
        {
            if (Maximum <= Minimum)
            {
                return 0;
            }
            return (float)(Math.Log(Clamp(value) / Minimum) / Math.Log(Maximum / Minimum));
        }

        protected override double ValueAtPosition(double position)
        {
            return Minimum * Math.Pow(Maximum / Minimum, position);
        }

        protected override double StepValue(double value, int steps)
        {
            return value * Math.Pow(Step, steps);
        }
    }
}
