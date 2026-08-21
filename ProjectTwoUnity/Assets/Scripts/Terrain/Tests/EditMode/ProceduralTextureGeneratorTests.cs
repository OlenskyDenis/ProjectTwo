namespace ProjectTwo.Terrain.Tests.EditMode
{
    using NUnit.Framework;
    using UnityEngine;
    using ProjectTwo.Terrain.Presentation.Materials;

    [TestFixture]
    public class ProceduralTextureGeneratorTests
    {
        [Test]
        public void GenerateSeamlessAlbedo_CreatesValidTextureWithExactResolution()
        {
            var p = ProceduralTextureGenerator.TextureGenerationParams.CreateGrass();
            p.Resolution = 64;

            Texture2D tex = ProceduralTextureGenerator.GenerateSeamlessAlbedo(p);

            Assert.IsNotNull(tex);
            Assert.AreEqual(64, tex.width);
            Assert.AreEqual(64, tex.height);
            Assert.AreEqual(TextureWrapMode.Repeat, tex.wrapMode);

            Color centerColor = tex.GetPixel(32, 32);
            Assert.Greater(centerColor.a, 0.99f);

            Object.DestroyImmediate(tex);
        }

        [Test]
        public void GenerateSeamlessNormalMap_CreatesValidNormalVectors()
        {
            var p = ProceduralTextureGenerator.TextureGenerationParams.CreateRock();
            p.Resolution = 64;

            Texture2D normalTex = ProceduralTextureGenerator.GenerateSeamlessNormalMap(p);

            Assert.IsNotNull(normalTex);
            Assert.AreEqual(64, normalTex.width);
            Assert.AreEqual(64, normalTex.height);

            // In tangent-space normal maps, Z (blue channel) should be positive pointing up (>= 0.5 when packed)
            Color sample = normalTex.GetPixel(32, 32);
            Assert.GreaterOrEqual(sample.b, 0.45f);

            Object.DestroyImmediate(normalTex);
        }

        [Test]
        public void SeamlessNoise_OppositeEdges_MatchContinuousToroidalBoundary()
        {
            var p = ProceduralTextureGenerator.TextureGenerationParams.CreateSand();

            // Sample u = 0.0 vs u = 1.0 (should be mathematically identical on a torus)
            float valLeft = ProceduralTextureGenerator.SampleSeamlessNoise(0.0f, 0.5f, p);
            float valRight = ProceduralTextureGenerator.SampleSeamlessNoise(1.0f, 0.5f, p);

            Assert.AreEqual(valLeft, valRight, 0.001f, "Seamless toroidal noise must match at u=0.0 and u=1.0 boundary");

            float valBottom = ProceduralTextureGenerator.SampleSeamlessNoise(0.5f, 0.0f, p);
            float valTop = ProceduralTextureGenerator.SampleSeamlessNoise(0.5f, 1.0f, p);

            Assert.AreEqual(valBottom, valTop, 0.001f, "Seamless toroidal noise must match at v=0.0 and v=1.0 boundary");
        }

        [Test]
        public void AllSurfacePresets_GenerateWithoutExceptions()
        {
            var grass = ProceduralTextureGenerator.TextureGenerationParams.CreateGrass();
            var rock = ProceduralTextureGenerator.TextureGenerationParams.CreateRock();
            var sand = ProceduralTextureGenerator.TextureGenerationParams.CreateSand();
            var snow = ProceduralTextureGenerator.TextureGenerationParams.CreateSnow();
            var dirt = ProceduralTextureGenerator.TextureGenerationParams.CreateDirt();

            grass.Resolution = 32;
            rock.Resolution = 32;
            sand.Resolution = 32;
            snow.Resolution = 32;
            dirt.Resolution = 32;

            Texture2D t1 = ProceduralTextureGenerator.GenerateSeamlessAlbedo(grass);
            Texture2D t2 = ProceduralTextureGenerator.GenerateSeamlessAlbedo(rock);
            Texture2D t3 = ProceduralTextureGenerator.GenerateSeamlessAlbedo(sand);
            Texture2D t4 = ProceduralTextureGenerator.GenerateSeamlessAlbedo(snow);
            Texture2D t5 = ProceduralTextureGenerator.GenerateSeamlessAlbedo(dirt);

            Assert.IsNotNull(t1);
            Assert.IsNotNull(t2);
            Assert.IsNotNull(t3);
            Assert.IsNotNull(t4);
            Assert.IsNotNull(t5);

            Object.DestroyImmediate(t1);
            Object.DestroyImmediate(t2);
            Object.DestroyImmediate(t3);
            Object.DestroyImmediate(t4);
            Object.DestroyImmediate(t5);
        }
    }
}
