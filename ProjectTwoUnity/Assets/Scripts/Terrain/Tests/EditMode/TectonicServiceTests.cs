namespace ProjectTwo.Terrain.Tests.EditMode
{
    using NUnit.Framework;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Contracts;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Core.Services;

    [TestFixture]
    public class TectonicServiceTests
    {
        private ITectonicService _tectonicService;

        [SetUp]
        public void SetUp()
        {
            _tectonicService = new TectonicService();
        }

        [Test]
        public void GenerateTectonicPartition_ValidSettings_CreatesPlatesAndBoundaries()
        {
            var settings = TectonicSettings.Default;
            settings.PlateCount = 16;
            settings.PlateScale = 1000f;

            _tectonicService.GenerateTectonicPartition(settings, out var plates, out var boundaries);

            Assert.IsNotNull(plates);
            Assert.IsNotNull(boundaries);
            Assert.AreEqual(settings.PlateCount, plates.Length);
            Assert.Greater(boundaries.Length, 0, "Boundaries should be formed between adjacent Voronoi plates.");

            // Verify each plate has valid centroid and drift velocity
            for (int i = 0; i < plates.Length; i++)
            {
                Assert.AreEqual(i, plates[i].Id);
                Assert.Greater(plates[i].DriftVelocity.sqrMagnitude, 0f, "Plate drift velocity should be non-zero.");
            }
        }

        [Test]
        public void GenerateTectonicPartition_BoundaryClassification_ContainsConvergentAndDivergent()
        {
            var settings = TectonicSettings.Default;
            settings.PlateCount = 24;

            _tectonicService.GenerateTectonicPartition(settings, out _, out var boundaries);

            bool hasConvergent = false;
            bool hasDivergentOrTransform = false;

            for (int i = 0; i < boundaries.Length; i++)
            {
                if (boundaries[i].BoundaryType == TectonicBoundaryType.Convergent)
                {
                    hasConvergent = true;
                }
                else
                {
                    hasDivergentOrTransform = true;
                }
            }

            Assert.IsTrue(hasConvergent, "Partition should classify some boundaries as convergent.");
            Assert.IsTrue(hasDivergentOrTransform, "Partition should classify some boundaries as divergent or transform.");
        }

        [Test]
        public void SampleTectonicUplift_Disabled_ReturnsZero()
        {
            var settings = TectonicSettings.Default;
            settings.Enabled = false;

            _tectonicService.GenerateTectonicPartition(settings, out _, out var boundaries);
            float uplift = _tectonicService.SampleTectonicUplift(100f, 200f, settings, boundaries);

            Assert.AreEqual(0f, uplift);
        }

        [Test]
        public void SampleTectonicUplift_ConvergentBoundaryCenter_ProducesHighUplift()
        {
            var settings = TectonicSettings.Default;
            settings.MountainUplift = 100f;
            settings.BoundaryInfluenceWidth = 200f;

            // Artificial convergent boundary segment along X axis
            var boundaries = new[]
            {
                new TectonicBoundary(
                    0, 1,
                    new Vector2(-500f, 0f),
                    new Vector2(500f, 0f),
                    TectonicBoundaryType.Convergent,
                    collisionIntensity: 1f,
                    influenceRadius: 200f,
                    maxUplift: 100f)
            };

            // Exactly on the boundary crest
            float crestUplift = _tectonicService.SampleTectonicUplift(0f, 0f, settings, boundaries);

            // Far from boundary
            float farUplift = _tectonicService.SampleTectonicUplift(0f, 600f, settings, boundaries);

            Assert.Greater(crestUplift, 50f, "Crest uplift should be significant near boundary centerline.");
            Assert.Less(farUplift, 5f, "Uplift should decay to near zero far away from influence radius.");
            Assert.Greater(crestUplift, farUplift, "Crest elevation must be strictly higher than distance falloff.");
        }
    }
}
