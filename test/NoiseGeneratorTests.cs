namespace Vocal {
    using System;

    using Troschuetz.Random.Generators;

    using Xunit;
    public class NoiseGeneratorTests {
        [Fact]
        public void Reproducible() {
            var generator = new NoiseGenerator(seed: 15122);
            float[] vals = new float[112341];
            for (int i = 0; i < vals.Length; i++) {
                vals[i] = generator.Simplex(i);
            }
            Assert.Equal("0.8608906865", vals[^1].ToString("F10"));
        }

        [Fact]
        public void RngReproducible() {
            var xorshift = new XorShift128Generator(9452);
            double[] vals = new double[461456];
            for (int i = 0; i < vals.Length; i++) {
                vals[i] = xorshift.NextDouble();
            }
            Assert.Equal("0.5612585810", vals[^1].ToString("F10"));
        }
    }
}
