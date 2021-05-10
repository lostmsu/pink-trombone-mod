﻿namespace Vocal {
    using System;
    using System.Diagnostics;

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

        public void Reset() {
            this.CalculateNewBlockParameters(0);
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
            Debug.WriteLine($"Block: {totalBlocks}");
        }

        void CalculateNewBlockParameters(float deltaTime) {
            this.glottis.AdjustParameters(deltaTime);
            this.shaper.AdjustTractShape(deltaTime);
            this.tract.CalculateNewBlockParameters();
        }
    }
}