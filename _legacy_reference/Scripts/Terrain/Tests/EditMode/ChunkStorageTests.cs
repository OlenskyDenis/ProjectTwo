namespace ProjectTwo.Terrain.Tests.EditMode
{
    using System.Threading.Tasks;
    using NUnit.Framework;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Core.Services;

    [TestFixture]
    public class ChunkStorageTests
    {
        private MemoryChunkStorage _storage;

        [SetUp]
        public void SetUp()
        {
            _storage = new MemoryChunkStorage();
        }

        [Test]
        public async Task SaveChunkAsync_And_TryGetChunk_PreservesData()
        {
            ChunkCoordinate coord = new ChunkCoordinate(5, -3);
            float[,] data = new float[4, 4];
            data[1, 1] = 0.75f;
            HeightMap map = new HeightMap(data);

            await _storage.SaveChunkAsync(coord, map);

            bool found = _storage.TryGetChunk(coord, out HeightMap retrievedMap);

            Assert.IsTrue(found);
            Assert.IsNotNull(retrievedMap);
            Assert.AreEqual(0.75f, retrievedMap.Values[1, 1]);
        }

        [Test]
        public void TryGetChunk_ReturnsFalse_ForNonExistentChunk()
        {
            ChunkCoordinate coord = new ChunkCoordinate(99, 99);
            bool found = _storage.TryGetChunk(coord, out _);

            Assert.IsFalse(found);
        }
    }
}
