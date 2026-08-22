namespace ProjectTwo.Terrain.Tests.EditMode
{
    using NUnit.Framework;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;

    [TestFixture]
    public class ChunkCoordinateTests
    {
        [Test]
        public void FromWorldPosition_CalculatesCorrectChunkCoordinates()
        {
            Vector3 worldPos = new Vector3(240f, 0f, 480f);
            ChunkCoordinate coord = ChunkCoordinate.FromWorldPosition(worldPos, 240f);

            Assert.AreEqual(1, coord.X);
            Assert.AreEqual(2, coord.Z);
        }

        [Test]
        public void FromWorldPosition_HandlesNegativeCoordinatesAndZeroChunkSize()
        {
            Vector3 worldPos = new Vector3(-240f, 10f, -480f);
            ChunkCoordinate coordZeroSize = ChunkCoordinate.FromWorldPosition(worldPos, 0f);

            Assert.AreEqual(-1, coordZeroSize.X);
            Assert.AreEqual(-2, coordZeroSize.Z);
        }

        [Test]
        public void ToWorldPosition_ReturnsCenterAlignedWorldCoordinates()
        {
            ChunkCoordinate coord = new ChunkCoordinate(3, -2);
            Vector3 worldPos = coord.ToWorldPosition(100f);

            Assert.AreEqual(300f, worldPos.x);
            Assert.AreEqual(0f, worldPos.y);
            Assert.AreEqual(-200f, worldPos.z);
        }

        [Test]
        public void DistanceTo_Calculates2DDistanceAccurately()
        {
            ChunkCoordinate coord = new ChunkCoordinate(0, 0);
            Vector3 target = new Vector3(300f, 100f, 400f); // 3-4-5 triangle

            float dist = coord.DistanceTo(target, 100f);
            Assert.AreEqual(500f, dist, 1e-4f);
        }

        [Test]
        public void EqualsAndHashCode_WorkCorrectly()
        {
            ChunkCoordinate coordA = new ChunkCoordinate(5, 7);
            ChunkCoordinate coordB = new ChunkCoordinate(5, 7);
            ChunkCoordinate coordC = new ChunkCoordinate(7, 5);

            Assert.IsTrue(coordA == coordB);
            Assert.IsFalse(coordA != coordB);
            Assert.IsTrue(coordA != coordC);
            Assert.AreEqual(coordA.GetHashCode(), coordB.GetHashCode());
            Assert.IsTrue(coordA.Equals((object)coordB));
            Assert.IsFalse(coordA.Equals("NotAChunkCoordinate"));
            Assert.AreEqual("Chunk(5, 7)", coordA.ToString());
        }
    }
}
