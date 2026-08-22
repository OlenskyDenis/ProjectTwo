namespace ProjectTwo.Terrain.Tests.EditMode
{
    using NUnit.Framework;
    using UnityEngine;
    using ProjectTwo.Terrain.Presentation.Materials;

    [TestFixture]
    public class TerrainMaterialCacheTests
    {
        private TerrainMaterialCache _cache;

        [SetUp]
        public void SetUp()
        {
            _cache = new TerrainMaterialCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache?.Dispose();
        }

        [Test]
        public void GetOrAdd_CreatesAndReturnsNewMaterial_WhenNotCached()
        {
            Shader shader = Shader.Find("Hidden/InternalErrorShader") ?? Shader.Find("Sprites/Default");
            int factoryCallCount = 0;

            Material mat = _cache.GetOrAdd("test_mat", () =>
            {
                factoryCallCount++;
                return new Material(shader) { name = "TestMat" };
            });

            Assert.IsNotNull(mat);
            Assert.AreEqual(1, factoryCallCount);
            Assert.IsTrue(_cache.TryGet("test_mat", out Material cached));
            Assert.AreSame(mat, cached);
        }

        [Test]
        public void GetOrAdd_ReturnsCachedInstance_WithoutInvokingFactoryTwice()
        {
            Shader shader = Shader.Find("Hidden/InternalErrorShader") ?? Shader.Find("Sprites/Default");
            int factoryCallCount = 0;

            Material mat1 = _cache.GetOrAdd("test_mat_reuse", () =>
            {
                factoryCallCount++;
                return new Material(shader) { name = "TestMatReuse" };
            });

            Material mat2 = _cache.GetOrAdd("test_mat_reuse", () =>
            {
                factoryCallCount++;
                return new Material(shader) { name = "TestMatReuseDuplicate" };
            });

            Assert.AreSame(mat1, mat2);
            Assert.AreEqual(1, factoryCallCount);
        }

        [Test]
        public void Clear_RemovesAllEntriesAndDestroysMaterials()
        {
            Shader shader = Shader.Find("Hidden/InternalErrorShader") ?? Shader.Find("Sprites/Default");
            Material mat = _cache.GetOrAdd("test_clear", () => new Material(shader) { name = "ClearMat" });

            Assert.IsTrue(_cache.TryGet("test_clear", out _));
            _cache.Clear();

            Assert.IsFalse(_cache.TryGet("test_clear", out _));
        }
    }
}
