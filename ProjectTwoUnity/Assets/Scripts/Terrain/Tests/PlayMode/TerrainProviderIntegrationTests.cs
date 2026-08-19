namespace ProjectTwo.Terrain.Tests.PlayMode
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using ProjectTwo.Terrain.Core.Contracts;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Presentation.Components;
    using ProjectTwo.Terrain.Presentation.Config;

    [TestFixture]
    public class TerrainProviderIntegrationTests
    {
        private GameObject _generatorGameObject;
        private TerrainGenerator _generator;
        private GameObject _viewerGameObject;
        private TerrainDataConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<TerrainDataConfig>();
            _config.ChunkSize = 100;
            _config.ChunkResolution = 30;
            _config.MaxViewDistance = 250f;
            _config.NoiseSettings = NoiseSettings.Default;
            _config.NoiseSettings.HeightMultiplier = 40f;
            _config.Regions = TerrainRegion.CreateDefaultRegions();
            _config.Validate();

            _viewerGameObject = new GameObject("TestViewer");
            _viewerGameObject.transform.position = new Vector3(0f, 50f, 0f);

            _generatorGameObject = new GameObject("TestTerrainGenerator");
            _generator = _generatorGameObject.AddComponent<TerrainGenerator>();
            _generator.Configuration = _config;
            _generator.Viewer = _viewerGameObject.transform;
            _generator.AutoUpdate = false;
        }

        [TearDown]
        public void TearDown()
        {
            if (_generatorGameObject != null)
            {
                Object.DestroyImmediate(_generatorGameObject);
            }

            if (_viewerGameObject != null)
            {
                Object.DestroyImmediate(_viewerGameObject);
            }

            if (_config != null)
            {
                Object.DestroyImmediate(_config);
            }
        }

        [UnityTest]
        public IEnumerator ITerrainProvider_StreamsChunksAndFiresLifecycleEvents()
        {
            bool chunkLoadedFired = false;
            ChunkEventArgs loadedArgs = default;

            _generator.OnChunkLoaded += (args) =>
            {
                chunkLoadedFired = true;
                loadedArgs = args;
            };

            _generator.Regenerate();

            // Wait for background tasks to complete and queue to process in Update
            float timeout = 2.0f;
            float elapsed = 0f;
            while (!chunkLoadedFired && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(chunkLoadedFired, "OnChunkLoaded event must fire when chunks are loaded asynchronously.");
            Assert.AreEqual(_config.ChunkSize, loadedArgs.ChunkSize);

            // Test spatial queries on loaded terrain
            float height = _generator.GetHeight(0f, 0f);
            Assert.GreaterOrEqual(height, 0f, "Height query must return non-negative elevation.");
            Assert.LessOrEqual(height, _config.NoiseSettings.HeightMultiplier + 1f);

            Vector3 normal = _generator.GetNormal(0f, 0f);
            Assert.Greater(normal.magnitude, 0.9f, "Surface normal must be a normalized unit vector.");

            float slope = _generator.GetSlope(0f, 0f);
            Assert.GreaterOrEqual(slope, 0f, "Slope angle must be >= 0 degrees.");
            Assert.LessOrEqual(slope, 90f, "Slope angle must be <= 90 degrees.");

            string biome = _generator.GetBiomeAt(0f, 0f);
            Assert.IsNotNull(biome, "GetBiomeAt must return a valid region name.");
            Assert.IsNotEmpty(biome);

            bool isLoaded = _generator.IsPositionLoaded(0f, 0f);
            Assert.IsTrue(isLoaded, "IsPositionLoaded should return true for origin coordinate.");
        }

        [UnityTest]
        public IEnumerator ITerrainProvider_FiresOnTerrainRegenerated()
        {
            bool regeneratedFired = false;
            _generator.OnTerrainRegenerated += () =>
            {
                regeneratedFired = true;
            };

            _generator.Regenerate();
            yield return null;

            Assert.IsTrue(regeneratedFired, "OnTerrainRegenerated must fire when Regenerate() is invoked.");
        }

        [UnityTest]
        public IEnumerator ITerrainProvider_UnloadsDistantChunksWhenViewerMoves()
        {
            _generator.Regenerate();

            // Wait for initial chunks to spawn
            yield return new WaitForSeconds(0.5f);

            bool chunkUnloadedFired = false;
            _generator.OnChunkUnloaded += (args) =>
            {
                chunkUnloadedFired = true;
            };

            // Teleport viewer far away
            _viewerGameObject.transform.position = new Vector3(2000f, 50f, 2000f);
            _generator.UpdateVisibleChunks();

            yield return new WaitForSeconds(0.5f);

            Assert.IsTrue(chunkUnloadedFired, "OnChunkUnloaded must fire when chunks exit the view radius.");
        }
    }
}
