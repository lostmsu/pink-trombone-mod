namespace Vocal {
    using System;

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
    }
}
