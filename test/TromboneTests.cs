namespace Vocal {
    using System;

    using Troschuetz.Random.Generators;

    using Xunit;
    public class TromboneTests {
        [Fact]
        public void Reproducible() {
            var xorshift = new XorShift128Generator(9452);
            const int sampleRate = 48000;
            var trombone = new PinkThrombone(sampleRate, xorshift);

            float[] vals = new float[sampleRate * 15];
            trombone.Synthesize(vals);

            Assert.Equal("0.0385491103", vals[^1].ToString("F10"));
        }
    }
}
