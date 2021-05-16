namespace Vocal {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using static PinkTrombone.Arg;

    public sealed class PinkThrombone {
        const int maxBlockLength = 512;

        readonly Glottis glottis;
        readonly Tract tract;
        readonly TractShaper shaper;
        readonly int sampleRate;

        public PinkThrombone(int sampleRate) {
            if (sampleRate <= 0 || sampleRate >= int.MaxValue / 2)
                throw new ArgumentOutOfRangeException(nameof(sampleRate));

            this.sampleRate = sampleRate;
            this.glottis = new Glottis(sampleRate);
            // tract runs at twice the sample rate
            this.tract = new Tract(this.glottis, sampleRate: 2 * sampleRate);
            this.shaper = new TractShaper(this.tract);
        }

        /// <summary>
        /// -1..+1
        /// </summary>
        [Obsolete("NotImplemented", error: true)]
        public float Noise {
            get => throw new NotImplementedException();
            set {
                if (value < -1 || value > 1)
                    throw new ArgumentOutOfRangeException(nameof(this.Noise));
                throw new NotImplementedException();
            }
        }
        /// <summary>
        /// 0..1
        /// </summary>
        public float Intensity {
            get => this.glottis.Intensity;
            set => this.glottis.Intensity = Check01(value);
        }
        /// <summary>
        /// 0..1
        /// </summary>
        public float Loudness {
            get => this.glottis.Loudness;
            set => this.glottis.Loudness = Check01(value);
        }
        /// <summary>
        /// 0..
        /// </summary>
        public float TargetFrequency {
            get => this.glottis.TargetFrequency;
            set {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(this.TargetFrequency));
                this.glottis.TargetFrequency = value;
            }
        }
        /// <summary>
        /// 0..1
        /// </summary>
        public float TargetTenseness {
            get => this.glottis.TargetTenseness;
            set => this.glottis.TargetTenseness = Check01(value);
        }
        /// <summary>
        /// 0..44 <see cref="Tract.n"/>
        /// </summary>
        public double TongueIndex {
            get => this.shaper.TongueIndex;
            set {
                if (value < -1 || value > Tract.n + 1)
                    throw new ArgumentOutOfRangeException(nameof(this.TongueIndex));
                this.shaper.TongueIndex = value;
            }
        }
        /// <summary>
        /// 0..3(?)
        /// </summary>
        public float TongueDiameter {
            get => this.shaper.TongueDiameter;
            set => this.shaper.TongueDiameter = value;
        }

        /// <summary>
        /// 0..
        /// </summary>
        public float VibratoGain {
            get => this.glottis.VibratoAmount;
            set {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(this.VibratoGain));
                this.glottis.VibratoAmount = value;
            }
        }
        /// <summary>
        /// 0..
        /// </summary>
        public float VibratoFrequency {
            get => this.glottis.VibratoFrequency;
            set {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(this.VibratoFrequency));
                this.glottis.VibratoFrequency = value;
            }
        }
        public bool VibratoWobble {
            get => this.glottis.AutoWobble;
            set => this.glottis.AutoWobble = value;
        }
        public IList<TurbulencePoint> TurbulencePoints => this.tract.turbulencePoints;

        /// <summary>
        /// Set <see cref="TargetFrequency"/> to the specified musical note.
        /// </summary>
        /// <param name="semitone">Semitone, based at A4.</param>
        public void SetMusicalNote(float semitone) {
            this.glottis.SetMusicalNote(semitone);
        }

        public void Synthesize(Span<float> buf) {
            int p = 0;
            while (p < buf.Length) {
                int blockLength = Math.Min(maxBlockLength, buf.Length - p);
                var blockBuf = buf.Slice(p, blockLength);
                this.SynthesizeBlock(blockBuf);
                p += blockLength;
            }
        }

        public void Reset() {
            this.CalculateNewBlockParameters(0);
        }

        int totalBlocks = 0;
        void SynthesizeBlock(Span<float> buf) {
            float deltaTime = buf.Length * 1f / this.sampleRate;
            this.CalculateNewBlockParameters(deltaTime);
            for (int i = 0; i < buf.Length; i++) {
                float lambda1 = i * 1f / buf.Length;
                float lambda2 = (i + 0.5f) / buf.Length;
                float glottalOutput = this.glottis.Step(lambda1);
                float vocalOutput1 = this.tract.Step(glottalOutput, lambda1);
                float vocalOutput2 = this.tract.Step(glottalOutput, lambda2);
                buf[i] = (vocalOutput1 + vocalOutput2) * 0.125f;
            }
            this.totalBlocks++;
            Debug.WriteLine($"Block: {this.totalBlocks}");
        }

        void CalculateNewBlockParameters(float deltaTime) {
            this.glottis.AdjustParameters(deltaTime);
            this.shaper.AdjustTractShape(deltaTime);
            this.tract.CalculateNewBlockParameters();
        }
    }
}
