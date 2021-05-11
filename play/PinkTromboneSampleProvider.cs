namespace Vocal {
    using System;
    using NAudio.Wave;

    public sealed class PinkTromboneSampleProvider : ISampleProvider {
        public WaveFormat WaveFormat { get; }
        public PinkThrombone Thrombone { get; }

        public PinkTromboneSampleProvider(int sampleRate) {
            this.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
            this.Thrombone = new PinkThrombone(sampleRate);
        }

        public int Read(float[] buffer, int offset, int count) {
            this.Thrombone.Synthesize(buffer.AsSpan().Slice(offset, count));
            return count;
        }
    }
}
