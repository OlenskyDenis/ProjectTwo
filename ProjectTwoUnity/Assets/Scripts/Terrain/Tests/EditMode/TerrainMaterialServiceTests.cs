namespace ProjectTwo.Terrain.Tests.EditMode
{
    using NUnit.Framework;
    using UnityEngine;
    using ProjectTwo.Terrain.Presentation.Materials;
    using ProjectTwo.Terrain.Presentation.Config;

    [TestFixture]
    public class TerrainMaterialServiceTests
    {
        private TerrainMaterialService _service;
        private TerrainVisualProfileSO _terrainProfile;
        private WaterVisualProfileSO _waterProfile;

        [SetUp]
        public void SetUp()
        {
            _service = new TerrainMaterialService();
            _terrainProfile = TerrainVisualProfileSO.CreateDefaultProfile();
            _waterProfile = WaterVisualProfileSO.CreateDefaultProfile();
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
            if (_terrainProfile != null) Object.DestroyImmediate(_terrainProfile);
            if (_waterProfile != null) Object.DestroyImmediate(_waterProfile);
        }

        [Test]
        public void GetOrCreateTerrainMaterial_GeneratesValidMaterial_WhenProfileProvided()
        {
            Material mat = _service.GetOrCreateTerrainMaterial(_terrainProfile);
            Assert.IsNotNull(mat);
            Assert.IsNotNull(mat.shader);
        }

        [Test]
        public void GetOrCreateTerrainMaterial_ReturnsSameInstance_OnSubsequentCallsWithSameProfile()
        {
            Material mat1 = _service.GetOrCreateTerrainMaterial(_terrainProfile);
            Material mat2 = _service.GetOrCreateTerrainMaterial(_terrainProfile);

            Assert.AreSame(mat1, mat2);
        }

        [Test]
        public void GetOrCreateTerrainMaterial_ReturnsFallbackMaterial_WhenProfileIsNull()
        {
            Material mat = _service.GetOrCreateTerrainMaterial(null);
            Assert.IsNotNull(mat);
            Assert.IsNotNull(mat.shader);
        }

        [Test]
        public void GetOrCreateWaterMaterial_GeneratesValidWaterMaterial_WhenProfileProvided()
        {
            Material waterMat = _service.GetOrCreateWaterMaterial(_waterProfile);
            Assert.IsNotNull(waterMat);
            Assert.IsNotNull(waterMat.shader);
        }

        [Test]
        public void GetOrCreateWaterMaterial_ReturnsSameInstance_OnSubsequentCallsWithSameProfile()
        {
            Material mat1 = _service.GetOrCreateWaterMaterial(_waterProfile);
            Material mat2 = _service.GetOrCreateWaterMaterial(_waterProfile);

            Assert.AreSame(mat1, mat2);
        }
    }
}
