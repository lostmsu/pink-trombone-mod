namespace Vocal {
    using System;

    internal class Glottis {
        public bool AlwaysVoice { get; set; } = true;
        public bool AutoWobble { get; set; } = true;
        public bool IsTouched { get; set; } = false;
        float targetTenseness = 0.6f;
        public float TargetTenseness {
            get => this.targetTenseness;
            set {
                if (value < 0 || value > 1)
                    throw new ArgumentOutOfRangeException(nameof(this.TargetTenseness));
                this.targetTenseness = value;
            }
        }
        public float TargetFrequency { get; set; } = 140;
        public float VibratoAmount { get; set; } = 0.005f;
        public float VibratoFrequency { get; set; } = 6;

        public NoiseGenerator NoiseGenerator { get; } = new NoiseGenerator();

        readonly int sampleRate;
        long sampleCount;
        float intensity = 0;
        float loudness = 1;
        float smoothFrequency = 140;
        float timeInWaveform;

        float oldTenseness = 0.6f, newTenseness = 0.6f;
        float oldFrequency = 140, newFrequency = 140;

        readonly Func<float> aspirationNoiseSource;

        float waveformLength;

        public Glottis(int sampleRate) {
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
            this.sampleRate = sampleRate;
            this.aspirationNoiseSource = Noise.CreateFilteredNoiseSource(500, 0.5f, sampleRate, 0x8000);
            this.SetupWaveform(0);
        }

        const float A4 = 440;
        /// <summary>
        /// Set <see cref="TargetFrequency"/> to the specified musical note.
        /// </summary>
        /// <param name="semitone">Semitone, based at A4.</param>
        public void SetMusicalNote(float semitone) {
            this.TargetFrequency = A4 * MathF.Pow(2, semitone * (1f / 12));
        }

        /// <param name="lambda">used for linear interpolation between the calculated frequency and tenseness values.</param>
        public float Step(float lambda) {
            float time = this.sampleCount * 1f / this.sampleRate;

            if (this.timeInWaveform > this.waveformLength) {
                this.timeInWaveform -= this.waveformLength;
                this.SetupWaveform(lambda);
            }

            float out1 = this.NormalizedLFWaveform(this.timeInWaveform / this.waveformLength);
            float aspirationNoise = this.aspirationNoiseSource();
            float aspiration1 = this.intensity * (1 - MathF.Sqrt(this.TargetTenseness))
                * this.GetNoiseModulator() * aspirationNoise;
            float aspiration2 = aspiration1 * (0.2f + 0.02f * this.NoiseGenerator.Simplex(time * 1.99f));
            float result = out1 + aspiration2;
            this.sampleCount++;
            this.timeInWaveform += 1f / this.sampleRate;
            return result;
        }

        public float GetNoiseModulator() {
            float voiced = 0.1f + 0.2f * MathF.Max(0, MathF.Sin(MathF.PI * 2 * this.timeInWaveform / this.waveformLength));
            return this.TargetTenseness * this.intensity * voiced + (1 - this.TargetTenseness * this.intensity) * 0.3f;
        }

        public void AdjustParameters(float deltaTime) {
            float delta = deltaTime * this.sampleRate / 512;
            float oldTime = this.sampleCount * 1f / this.sampleRate;
            float newTime = oldTime + deltaTime;
            this.AdjustIntensity(delta);
            this.CalculateNewFrequency(newTime, delta);
            this.CalculateNewTenseness(newTime);
        }

        void CalculateNewFrequency(float time, float deltaTime) {
            if (this.intensity == 0) {
                this.smoothFrequency = this.TargetFrequency;
            } else if (this.TargetFrequency > this.smoothFrequency) {
                this.smoothFrequency = MathF.Min(this.smoothFrequency * (1 + 0.1f * deltaTime), this.TargetFrequency);
            } else if (this.TargetFrequency < this.smoothFrequency) {
                this.smoothFrequency = Math.Max(this.smoothFrequency / (1 + 0.1f * deltaTime), this.TargetFrequency);
            }
            this.oldFrequency = this.newFrequency;
            this.newFrequency = MathF.Max(10, this.smoothFrequency * (1 + this.CalculateVibrato(time)));
        }

        void CalculateNewTenseness(float time) {
            this.oldTenseness = this.newTenseness;
            this.newTenseness = MathF.Max(0, this.TargetTenseness + 0.1f * this.NoiseGenerator.Simplex(time * 0.46f) + 0.05f * this.NoiseGenerator.Simplex(time * 0.36f));
            if (!this.IsTouched && this.AlwaysVoice) { // attack
                this.newTenseness += (3 - this.TargetTenseness) * (1 - this.intensity);
            }
        }

        void AdjustIntensity(float delta) {
            if (this.IsTouched || this.AlwaysVoice) {
                this.intensity += 0.13f * delta;
            } else {
                this.intensity -= 0.05f * delta;
            }

            this.intensity = MathFX.Clamp(this.intensity, 0, 1);
        }

        float CalculateVibrato(float time) {
            float vibrato = this.VibratoAmount * MathF.Sin(2 * MathF.PI * time * this.VibratoFrequency);
            vibrato += 0.02f * this.NoiseGenerator.Simplex(time * 4.07f);
            vibrato += 0.04f * this.NoiseGenerator.Simplex(time * 2.15f);
            if (this.AutoWobble) {
                vibrato += 0.2f * this.NoiseGenerator.Simplex(time * 0.96f);
                vibrato += 0.4f * this.NoiseGenerator.Simplex(time * 0.5f);
            }
            return vibrato;
        }

        void SetupWaveform(float lambda) {
            float frequency = MathFX.Interpolate(this.oldFrequency, this.newFrequency, lambda);
            float tenseness = MathFX.Interpolate(this.oldTenseness, this.newTenseness, lambda);
            this.waveformLength = 1f / frequency;
            this.loudness = MathF.Pow(Math.Max(0, tenseness), 0.25f);

            float rd = MathFX.Clamp(3 * (1 - tenseness), 0.5f, 2.7f);

            // normalized to time = 1, Ee = 1
            float ra = -0.01f + 0.048f * rd;
            float rk = 0.224f + 0.118f * rd;
            float rg = (rk / 4) * (0.5f + 1.2f * rk) / (0.11f * rd - ra * (0.5f + 1.2f * rk));

            float ta = ra;
            float tp = 1 / (2 * rg);
            float te = tp + tp * rk;

            float epsilon = 1 / ta;
            float shift = MathF.Exp(-epsilon * (1 - te));
            float delta = 1 - shift;                       // divide by this to scale RHS

            float rhsIntegral = ((1 / epsilon) * (shift - 1) + (1 - te) * shift) / delta;
            float totalLowerIntegral = rhsIntegral - (te - tp) / 2;
            float totalUpperIntegral = -totalLowerIntegral;

            float omega = MathF.PI / tp;
            float s = MathF.Sin(omega * te);

            // need E0*e^(alpha*Te)*s = -1 (to meet the return at -1)
            // and E0*e^(alpha*Tp/2) * Tp*2/pi = totalUpperIntegral
            //             (our approximation of the integral up to Tp)
            // writing x for e^alpha,
            // have E0*x^Te*s = -1 and E0 * x^(Tp/2) * Tp*2/pi = totalUpperIntegral
            // dividing the second by the first,
            // letting y = x^(Tp/2 - Te),
            // y * Tp*2 / (pi*s) = -totalUpperIntegral;

            float y = -MathF.PI * s * totalUpperIntegral / (tp * 2);
            float z = MathF.Log(y);
            float alpha = z / (tp / 2 - te);
            float e0 = -1 / (s * MathF.Exp(alpha * te));

            this.alpha = alpha;
            this.e0 = e0;
            this.epsilon = epsilon;
            this.shift = shift;
            this.delta = delta;
            this.te = te;
            this.omega = omega;
        }

        float alpha, e0, epsilon, shift, delta, te, omega;

        float NormalizedLFWaveform(float t) {
            float output = t > this.te
                ? (-MathF.Exp(-this.epsilon * (t - this.te)) + this.shift) / this.delta
                : this.e0 * MathF.Exp(this.alpha * t) * MathF.Sin(this.omega * t);
            return output * this.intensity * this.loudness;
        }
    }
}