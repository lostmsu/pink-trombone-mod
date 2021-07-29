namespace Vocal {
    using System;
    using System.Collections.Generic;

    using Troschuetz.Random;

    internal class Tract {
        readonly Glottis glottis;
        readonly int sampleRate;
        readonly Func<float> fricationNoiseSource;

        public const int n = 44;
        public const int BladeStart = 10;
        public const int TipStart = 32;
        public const int LipStart = 39;
        public const int NoseLength = 28;
        public const int NoseStart = n - NoseLength + 1;

        const double GlottalReflection = 0.75;
        const double LipReflection = -0.85;

        long sampleCount = 0;
        internal float time = 0;

        readonly double[] right, left;
        readonly double[] reflection;
        readonly double[] newReflection;
        readonly double[] junctionOutputRight, junctionOutputLeft;
        readonly double[] maxAmplitude;
        /// vocal tract cell diameters
        internal readonly double[] diameter;

        internal readonly List<Transient> transients = new();
        internal readonly List<TurbulencePoint> turbulencePoints = new();

        readonly double[] noseRight, noseLeft;
        readonly double[] noseJunctionOutputRight, noseJunctionOutputLeft;
        readonly double[] noseReflection;
        /// nose diameters, [0] = velum opening
        internal readonly double[] noseDiameter;
        /// max amplitudes per waveguide cell for nose (read-only from outside)
        readonly double[] noseMaxAmplitude;

        double reflectionLeft, reflectionRight;
        double newReflectionLeft, newReflectionRight;
        double reflectionNose, newReflectionNose;

        public Tract(Glottis glottis, int sampleRate, IGenerator random) {
            if (glottis is null) throw new ArgumentNullException(nameof(glottis));
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
            if (random is null) throw new ArgumentNullException(nameof(random));

            this.glottis = glottis;
            this.sampleRate = sampleRate;
            this.fricationNoiseSource = Noise.CreateFilteredNoiseSource(1000, 0.5, sampleRate, 0x8000, random);
            this.diameter = new double[n];
            this.right = new double[n];
            this.left = new double[n];
            this.reflection = new double[n];
            this.newReflection = new double[n];
            this.junctionOutputRight = new double[n];
            this.junctionOutputLeft = new double[n+1];
            this.maxAmplitude = new double[n];
            this.noseRight = new double[NoseLength];
            this.noseLeft = new double[NoseLength];
            this.noseJunctionOutputRight = new double[NoseLength];
            this.noseJunctionOutputLeft = new double[NoseLength + 1];
            this.noseReflection = new double[NoseLength];
            this.noseDiameter = new double[NoseLength];
            this.noseMaxAmplitude = new double[NoseLength];
            this.newReflectionLeft = this.newReflectionRight = this.newReflectionNose = 0;
        }

        public void CalculateNoseReflections() {
            double[] a = new double[NoseLength];
            for (int i = 0; i < NoseLength; i++) {
                a[i] = Math.Max(1e-6, this.noseDiameter[i] * this.noseDiameter[i]);
            }
            for (int i = 1; i < NoseLength; i++) {
                this.noseReflection[i] = (a[i - 1] - a[i]) / (a[i - 1] + a[i]);
            }
        }

        public void CalculateNewBlockParameters() {
            this.CalculateMainTractReflections();
            this.CalculateNoseJunctionReflections();
        }

        void CalculateMainTractReflections() {
            Span<double> a = stackalloc double[n];
            for (int i = 0; i < n; i++) {
                a[i] = this.diameter[i] * this.diameter[i];
            }

            for (int i = 1; i < n; i++) {
                this.reflection[i] = this.newReflection[i];
                double sum = a[i - 1] + a[i];
                this.newReflection[i] = (Math.Abs(sum) > 1e-6) ? (a[i - 1] - a[i]) / sum : 1;
            }
        }

        void CalculateNoseJunctionReflections() {
            this.reflectionLeft = this.newReflectionLeft;
            this.reflectionRight = this.newReflectionRight;
            this.reflectionNose = this.newReflectionNose;

            double velumA = this.noseDiameter[0] * this.noseDiameter[0];
            double an0 = this.diameter[NoseStart] * this.diameter[NoseStart];
            double an1 = this.diameter[NoseStart + 1] * this.diameter[NoseStart + 1];
            double sum = an0 + an1 + velumA;

            this.newReflectionLeft = (Math.Abs(sum) > 1E-6) ? (2 * an0 - sum) / sum : 1;
            this.newReflectionRight = (Math.Abs(sum) > 1E-6) ? (2 * an1 - sum) / sum : 1;
            this.newReflectionNose = (Math.Abs(sum) > 1E-6) ? (2 * velumA - sum) / sum : 1;
        }

        public float Step(double glottalOutput, double lambda) {

            // mouth
            this.ProcessTransients();
            this.AddTurbulenceNoise();

            // this.glottalReflection = -0.8 + 1.6 * this.glottis.newTenseness;
            this.junctionOutputRight[0] = this.left[0] * GlottalReflection + glottalOutput;
            this.junctionOutputLeft[n] = this.right[n - 1] * LipReflection;

            for (int i = 1; i < n; i++) {
                double r = MathX.Interpolate(this.reflection[i], this.newReflection[i], lambda);
                double w = r * (this.right[i - 1] + this.left[i]);
                this.junctionOutputRight[i] = this.right[i - 1] - w;
                this.junctionOutputLeft[i] = this.left[i] + w;
            }

            // now at junction with nose
            {
                const int i = NoseStart;
                double r = MathX.Interpolate(this.reflectionLeft, this.newReflectionLeft, lambda);
                this.junctionOutputLeft[i] = r * this.right[i - 1] + (1 + r) * (this.noseLeft[0] + this.left[i]);
                r = MathX.Interpolate(this.reflectionRight, this.newReflectionRight, lambda);
                this.junctionOutputRight[i] = r * this.left[i] + (1 + r) * (this.right[i - 1] + this.noseLeft[0]);
                r = MathX.Interpolate(this.reflectionNose, this.newReflectionNose, lambda);
                this.noseJunctionOutputRight[0] = r * this.noseLeft[0] + (1 + r) * (this.left[i] + this.right[i - 1]);
            }

            for (int i = 0; i < n; i++) {
                double right = this.junctionOutputRight[i] * 0.999;
                double left = this.junctionOutputLeft[i + 1] * 0.999;
                this.right[i] = right;
                this.left[i] = left;
                double amplitude = Math.Abs(right + left);
                this.maxAmplitude[i] = Math.Max(this.maxAmplitude[i] *= 0.9999, amplitude);
            }

            double lipOutput = this.right[n - 1];

            // nose
            this.noseJunctionOutputLeft[NoseLength] = this.noseRight[NoseLength - 1] * LipReflection;

            for (int i = 1; i < NoseLength; i++) {
                double w = this.noseReflection[i] * (this.noseRight[i - 1] + this.noseLeft[i]);
                this.noseJunctionOutputRight[i] = this.noseRight[i - 1] - w;
                this.noseJunctionOutputLeft[i] = this.noseLeft[i] + w;
            }

            for (int i = 0; i < NoseLength; i++) {
                double right = this.noseJunctionOutputRight[i];
                double left = this.noseJunctionOutputLeft[i + 1];
                this.noseRight[i] = right;
                this.noseLeft[i] = left;
                double amplitude = Math.Abs(right + left);
                this.noseMaxAmplitude[i] = Math.Max(this.noseMaxAmplitude[i] *= 0.9999, amplitude);
            }

            double noseOutput = this.noseRight[NoseLength - 1];

            this.sampleCount++;
            this.time = this.sampleCount * 1f / this.sampleRate;

            return (float)(lipOutput + noseOutput);
        }

        void ProcessTransients() {
            for (int i = this.transients.Count - 1; i >= 0; i--) {
                var trans = this.transients[i];
                float timeAlive = this.time - trans.StartTime;
                if (timeAlive > trans.LifeTime) {
                    this.transients.RemoveAt(i);
                    continue;
                }
                double amplitude = trans.Strength * Math.Pow(2, -trans.Exponent * timeAlive);
                this.right[trans.Position] += amplitude * 0.5;
                this.left[trans.Position] += amplitude * 0.5;
            }
        }

        void AddTurbulenceNoise() {
            const double fricativeAttackTime = 0.1;                     // 0.1 seconds
            foreach (var p in this.turbulencePoints) {
                if (p.Position < 2 || p.Position > n) {
                    continue;
                }
                if (p.Diameter <= 0) {
                    continue;
                }
                double intensity;
                if (double.IsNaN(p.EndTime)) {
                    intensity = MathX.Clamp((this.time - p.StartTime) / fricativeAttackTime, 0, 1);
                } else {                                          // point has been released
                    intensity = MathX.Clamp(1 - (this.time - p.EndTime) / fricativeAttackTime, 0, 1);
                }
                if (intensity <= 0) {
                    continue;
                }
                double turbulenceNoise = 0.66 * this.fricationNoiseSource() * intensity * this.glottis.GetNoiseModulator();
                this.AddTurbulenceNoiseAtPosition(turbulenceNoise, p.Position, p.Diameter);
            }
        }

        void AddTurbulenceNoiseAtPosition(double turbulenceNoise, double position, double diameter) {
            int i = (int)Math.Floor(position);
            double delta = position - i;
            double thinness0 = MathX.Clamp(8 * (0.7 - diameter), 0, 1);
            double openness = MathX.Clamp(30 * (diameter - 0.3), 0, 1);
            double noise0 = turbulenceNoise * (1 - delta) * thinness0 * openness;
            double noise1 = turbulenceNoise * delta * thinness0 * openness;
            if (i + 1 < n) {
                this.right[i + 1] += noise0 * 0.5;
                this.left[i + 1] += noise0 * 0.5;
            }
            if (i + 2 < n) {
                this.right[i + 2] += noise1 * 0.5;
                this.left[i + 2] += noise1 * 0.5;
            }
        }
    }
}