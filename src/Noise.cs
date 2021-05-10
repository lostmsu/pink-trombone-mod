using System;

namespace Vocal {
    internal class Noise {
        internal static Func<float> CreateFilteredNoiseSource(double f0, double q, int sampleRate, int bufferSize) {
            Func<double>? whiteNoise = CreateBufferedWhiteNoiseSource(bufferSize);
            Func<double, float>? filter = CreateBandPassFilter(f0, q, sampleRate);
            return () => filter(whiteNoise());
        }

        static Func<double> CreateBufferedWhiteNoiseSource(int bufferSize) {
            double[]? buf = new double[bufferSize];
            var random = new Random();
            for (int i = 0; i < bufferSize; i++)
                // -1 .. 1
                buf[i] = 2 * random.NextDouble() - 1;

            int currentIndex = 0;
            return () => {
                if (currentIndex >= bufferSize) currentIndex = 0;
                return buf[currentIndex++];
            };
        }

        static Func<double, float> CreateBandPassFilter(double f0, double q, int sampleRate) {
            double w0 = 2 * Math.PI * f0 / sampleRate;
            double alpha = Math.Sin(w0) / (2 * q);
            double b0 = alpha;
            double b1 = 0;
            double b2 = -alpha;
            double a0 = 1 + alpha;
            double a1 = -2 * Math.Cos(w0);
            double a2 = 1 - alpha;
            return CreateBiquadIirFilter(b0, b1, b2, a0, a1, a2);
        }

        static Func<double, float> CreateBiquadIirFilter(double b0, double b1, double b2, double a0, double a1, double a2) {
            double nb0 = b0 / a0;                                   // normalized coefficients...
            double nb1 = b1 / a0;
            double nb2 = b2 / a0;
            double na1 = a1 / a0;
            double na2 = a2 / a0;
            double x1 = 0;                                            // x[n-1], last input value
            double x2 = 0;                                            // x[n-2], second-last input value
            double y1 = 0;                                            // y[n-1], last output value
            double y2 = 0;                                            // y[n-2], second-last output value
            return (double x) => {
                double y = nb0 * x + nb1 * x1 + nb2 * x2 - na1 * y1 - na2 * y2;
                x2 = x1;
                x1 = x;
                y2 = y1;
                y1 = y;
                return (float)y;
            };
        }
    }
}