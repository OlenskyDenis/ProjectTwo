namespace ProjectTwo.Terrain.Tests.EditMode
{
    using NUnit.Framework;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Presentation.Config;

    [TestFixture]
    public class TerrainPresetTests
    {
        private TerrainPreset _preset;
        private TerrainDataConfig _config;

        [SetUp]
        public void SetUp()
        {
            _preset = ScriptableObject.CreateInstance<TerrainPreset>();
            _config = ScriptableObject.CreateInstance<TerrainDataConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_preset != null) Object.DestroyImmediate(_preset);
            if (_config != null) Object.DestroyImmediate(_config);
        }

        [Test]
        public void Preset_CanStoreAndApplyToConfig()
        {
            _preset.PresetName = "Alpine Test";
            _preset.ChunkSize = 360;
            _preset.ChunkResolution = 96;
            _preset.NoiseSettings = new NoiseSettings
            {
                Type = NoiseType.RidgedMultifractal,
                Seed = 4242,
                Scale = 80f,
                Octaves = 6,
                Persistence = 0.55f,
                Lacunarity = 2.1f,
                HeightMultiplier = 60f,
                Offset = Vector2.zero
            };

            // Apply preset fields onto config
            _config.ChunkSize = _preset.ChunkSize;
            _config.ChunkResolution = _preset.ChunkResolution;
            _config.NoiseSettings = _preset.NoiseSettings;
            _config.Validate();

            Assert.AreEqual(360, _config.ChunkSize);
            Assert.AreEqual(96, _config.ChunkResolution);
            Assert.AreEqual(NoiseType.RidgedMultifractal, _config.NoiseSettings.Type);
            Assert.AreEqual(4242, _config.NoiseSettings.Seed);
        }
    }
}
