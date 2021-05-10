using System;

namespace Vocal {
    internal class MathFX {
        public static float Clamp(float v, float min, float max)
            => MathF.Min(max, MathF.Max(v, min));

        public static float Interpolate(float a, float b, float lambda) => a + lambda * (b - a);
    }

    internal class MathX {
        public static double Clamp(double v, double min, double max)
            => Math.Min(max, Math.Max(v, min));

        public static double Interpolate(double a, double b, double lambda) => a + lambda * (b - a);

        public static double MoveTowards(double current, double target, double amountUp, double amountDown)
            => (current < target) ? Math.Min(current + amountUp, target) : Math.Max(current - amountDown, target);
    }
}