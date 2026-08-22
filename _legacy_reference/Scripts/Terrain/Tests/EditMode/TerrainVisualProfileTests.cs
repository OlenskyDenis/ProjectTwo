namespace ProjectTwo.Terrain.Tests.EditMode
{
    using NUnit.Framework;
    using UnityEngine;
    using ProjectTwo.Terrain.Presentation.Config;
    using ProjectTwo.Terrain.Presentation.Materials;

    [TestFixture]
    public class TerrainVisualProfileTests
    {
        private TerrainVisualProfileSO _terrainProfile;
        private WaterVisualProfileSO _waterProfile;
        private TerrainMaterialService _service;

        [SetUp]
        public void SetUp()
        {
            _terrainProfile = TerrainVisualProfileSO.CreateDefaultProfile();
            _waterProfile = WaterVisualProfileSO.CreateDefaultProfile();
            _service = new TerrainMaterialService();
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
            if (_terrainProfile != null) Object.DestroyImmediate(_terrainProfile);
            if (_waterProfile != null) Object.DestroyImmediate(_waterProfile);
        }

        [Test]
        public void TerrainVisualProfile_FiresOnProfileChanged_WhenNotifyCalled()
        {
            int eventCount = 0;
            _terrainProfile.OnProfileChanged += () => eventCount++;

            _terrainProfile.NotifyProfileChanged();

            Assert.AreEqual(1, eventCount);
        }

        [Test]
        public void WaterVisualProfile_FiresOnProfileChanged_WhenNotifyCalled()
        {
            int eventCount = 0;
            _waterProfile.OnProfileChanged += () => eventCount++;

            _waterProfile.NotifyProfileChanged();

            Assert.AreEqual(1, eventCount);
        }

        [Test]
        public void Service_UpdatesMaterialProperties_WhenProfileModified()
        {
            Material mat = _service.GetOrCreateTerrainMaterial(_terrainProfile);
            _terrainProfile.GlobalTint = new Color(0.8f, 0.4f, 0.1f, 1f);

            _terrainProfile.NotifyProfileChanged();

            if (mat.HasProperty("_BaseColor"))
            {
                Assert.AreEqual(new Color(0.8f, 0.4f, 0.1f, 1f), mat.GetColor("_BaseColor"));
            }
            else if (mat.HasProperty("_Color"))
            {
                Assert.AreEqual(new Color(0.8f, 0.4f, 0.1f, 1f), mat.GetColor("_Color"));
            }
        }
    }
}
