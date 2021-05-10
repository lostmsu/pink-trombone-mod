namespace Vocal {
    using System;
    using NAudio.Wave;

    public sealed class PinkTromboneSampleProvider : ISampleProvider {
        public WaveFormat WaveFormat { get; }

        readonly PinkThrombone pinkThrombone;

        public PinkTromboneSampleProvider(int sampleRate) {
            this.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
            this.pinkThrombone = new PinkThrombone(sampleRate);
        }

        public int Read(float[] buffer, int offset, int count) {
            this.pinkThrombone.Synthesize(buffer.AsSpan().Slice(offset, count));
            return count;
        }
    }
}
