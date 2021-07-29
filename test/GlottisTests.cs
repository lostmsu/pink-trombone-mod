namespace Vocal {
    using Troschuetz.Random.Generators;

    using Xunit;
    public class GlottisTests {
        [Fact(Skip = "It's reproducible, but outputs zeros. Some tweaking needed for meaningful testing.")]
        public void Reproducible() {
            var rng = new XorShift128Generator(939);
            var glottis = new Glottis(48000, rng);
            float[] outputs = new float[512];
            for (int i = 0; i < 512; i++) {
                float lambda = (i % 256) / 256f;
                outputs[i] = glottis.Step(lambda);
            }
            Assert.Equal("0.8608906865", outputs[^1].ToString("F10"));
        }
    }
}
