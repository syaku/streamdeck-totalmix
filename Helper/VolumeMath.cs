// ダイヤル回転の数値ロジック。状態を持たない純関数だけを置く。

namespace streamdeck_totalmix
{
    using System;

    public static class VolumeMath
    {
        // 1 tick あたりの基準増分。既存 OscChannel の Raise/Lower と同じ刻み。
        public const decimal Step = 0.02M;

        // 倍率は正で乗算、負で 1/|m| 倍、0 は 1 として扱う(例外を投げない)。
        public static decimal Increment(int ticks, int multiplier)
        {
            var step = Step;
            if (multiplier > 0)
            {
                step *= multiplier;
            }
            else if (multiplier < 0)
            {
                // int.MinValue は -multiplier が int に収まらず符号が戻ってしまうので long に広げてから割る。
                step /= -(long)multiplier;
            }

            return step * ticks;
        }

        // 0..1 の両端で clamp する。既存 16/17 は下限のみだが、こちらは新アクション内の判断。
        public static decimal NextValue(decimal current, int ticks, int multiplier)
        {
            var next = current + Increment(ticks, multiplier);
            if (next < 0M) return 0M;
            if (next > 1M) return 1M;
            return next;
        }

        public static int ToDisplay(decimal value)
        {
            return (int)Math.Round(value * 100M, MidpointRounding.AwayFromZero);
        }

        public static decimal FromDisplay(int display)
        {
            return Math.Round(display / 100M, 2);
        }
    }
}
