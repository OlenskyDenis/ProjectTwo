namespace ProjectTwo.Terrain.Tests.EditMode
{
    using NUnit.Framework;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Core.Services;

    [TestFixture]
    public class PerlinNoiseGeneratorTests
    {
        private PerlinNoiseGenerator _generator;

        [SetUp]
        public void SetUp()
        {
            _generator = new PerlinNoiseGenerator();
        }

        [Test]
        public void SampleNoise_IsDeterministic_ForSameSeedAndCoordinates()
        {
            NoiseSettings settings = NoiseSettings.Default;
            settings.Seed = 42;

            float val1 = _generator.SampleNoise(15.5f, 25.3f, settings);
            float val2 = _generator.SampleNoise(15.5f, 25.3f, settings);

            Assert.AreEqual(val1, val2, 1e-6f, "Noise generation must be 100% deterministic for identical inputs.");
        }

        [Test]
        public void SampleNoise_ReturnsDifferentValues_ForDifferentSeeds()
        {
            NoiseSettings settingsA = NoiseSettings.Default;
            settingsA.Seed = 101;

            NoiseSettings settingsB = NoiseSettings.Default;
            settingsB.Seed = 999;

            float valA = _generator.SampleNoise(12.0f, 34.0f, settingsA);
            float valB = _generator.SampleNoise(12.0f, 34.0f, settingsB);

            Assert.AreNotEqual(valA, valB, "Different seeds must produce different noise samples.");
        }

        [Test]
        public void SampleNoise_OutputIsBoundedBetweenZeroAndOne()
        {
            NoiseSettings settings = NoiseSettings.Default;
            settings.Octaves = 4;

            for (float x = -50f; x <= 50f; x += 10.5f)
            {
                for (float y = -50f; y <= 50f; y += 10.5f)
                {
                    float sample = _generator.SampleNoise(x, y, settings);
                    Assert.GreaterOrEqual(sample, 0f, $"Noise sample at ({x}, {y}) should be >= 0");
                    Assert.LessOrEqual(sample, 1f, $"Noise sample at ({x}, {y}) should be <= 1");
                }
            }
        }
    }
}
