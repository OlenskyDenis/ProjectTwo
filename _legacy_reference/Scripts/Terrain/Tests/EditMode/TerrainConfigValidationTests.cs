namespace ProjectTwo.Terrain.Tests.EditMode
{
    using NUnit.Framework;
    using UnityEngine;
    using ProjectTwo.Terrain.Presentation.Config;
    using ProjectTwo.Terrain.Core.Models;

    [TestFixture]
    public class TerrainConfigValidationTests
    {
        private TerrainDataConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<TerrainDataConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_config != null)
            {
                Object.DestroyImmediate(_config);
            }
        }

        [Test]
        public void Validate_SnapsChunkResolutionToMultipleOf12()
        {
            _config.ChunkResolution = 55; // Not divisible by 12
            _config.Validate();

            Assert.AreEqual(60, _config.ChunkResolution);
            Assert.AreEqual(0, _config.ChunkResolution % 12);
        }

        [Test]
        public void Validate_EnforcesMinChunkResolution()
        {
            _config.ChunkResolution = 10; // Below min 24
            _config.Validate();

            Assert.AreEqual(24, _config.ChunkResolution);
        }

        [Test]
        public void Validate_SnapsChunkSizeToMultipleOf12()
        {
            _config.ChunkSize = 250;
            _config.Validate();

            Assert.AreEqual(252, _config.ChunkSize);
            Assert.AreEqual(0, _config.ChunkSize % 12);
        }

        [Test]
        public void Validate_InitializesDefaultRegionsIfNull()
        {
            _config.Regions = null;
            _config.Validate();

            Assert.IsNotNull(_config.Regions);
            Assert.GreaterOrEqual(_config.Regions.Length, 1);
        }

        [Test]
        public void Validate_EnforcesPositiveViewDistance()
        {
            _config.MaxViewDistance = -50f;
            _config.Validate();

            Assert.GreaterOrEqual(_config.MaxViewDistance, 50f);
        }
    }
}
