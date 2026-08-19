namespace ProjectTwo.Terrain.Core.Services
{
    using System.Collections.Generic;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Pure C# domain service for constructing 3D terrain mesh data from heightmaps.
    /// Handles multi-LOD vertex skipping, seamless gradient normals, and vertical terrain skirts to eliminate LOD cracks.
    /// </summary>
    public static class TerrainMeshBuilder
    {
        public static TerrainMeshData GenerateTerrainMesh(
            HeightMap heightMap,
            float chunkSize,
            float heightMultiplier,
            int lodStep = 1,
            TerrainRegion[] regions = null)
        {
            if (lodStep < 1) lodStep = 1;

            int width = heightMap.Width;
            int height = heightMap.Height;

            int numSegmentsX = width - 1;
            int numSegmentsZ = height - 1;

            int verticesPerLineX = numSegmentsX / lodStep + 1;
            int verticesPerLineZ = numSegmentsZ / lodStep + 1;

            int gridVertexCount = verticesPerLineX * verticesPerLineZ;
            int gridTriangleCount = (verticesPerLineX - 1) * (verticesPerLineZ - 1) * 6;

            // Perimeter segment count for skirt
            int perimeterSegments = ((verticesPerLineX - 1) + (verticesPerLineZ - 1)) * 2;
            int skirtVertexCount = perimeterSegments * 2;
            int skirtTriangleIndexCount = perimeterSegments * 6;

            int totalVertices = gridVertexCount + skirtVertexCount;
            int totalTriangles = gridTriangleCount + skirtTriangleIndexCount;

            TerrainMeshData meshData = new TerrainMeshData(totalVertices, totalTriangles);
            Color[] colors = new Color[totalVertices];

            float halfChunkSize = chunkSize * 0.5f;
            float stepDistX = (float)lodStep / numSegmentsX * chunkSize;
            float stepDistZ = (float)lodStep / numSegmentsZ * chunkSize;

            // Skirt extends downwards below the lowest possible terrain point
            float skirtDepth = Mathf.Max(15f, heightMultiplier * 0.6f);

            // 1. Generate Surface Grid Vertices, UVs, Colors, and Gradient Normals
            for (int zIndex = 0; zIndex < verticesPerLineZ; zIndex++)
            {
                int sampleZ = Mathf.Min(zIndex * lodStep, numSegmentsZ);
                float percentZ = (float)sampleZ / numSegmentsZ;
                float posZ = -halfChunkSize + percentZ * chunkSize;

                for (int xIndex = 0; xIndex < verticesPerLineX; xIndex++)
                {
                    int sampleX = Mathf.Min(xIndex * lodStep, numSegmentsX);
                    float percentX = (float)sampleX / numSegmentsX;
                    float posX = -halfChunkSize + percentX * chunkSize;

                    float normalizedHeight = heightMap.Values[sampleX, sampleZ];
                    float currentElevation = normalizedHeight * heightMultiplier;

                    int vertexIndex = zIndex * verticesPerLineX + xIndex;

                    meshData.Vertices[vertexIndex] = new Vector3(posX, currentElevation, posZ);
                    meshData.UVs[vertexIndex] = new Vector2(percentX, percentZ);
                    colors[vertexIndex] = EvaluateRegionColor(normalizedHeight, regions);

                    // Compute smooth seamless surface normal via height gradient
                    meshData.Normals[vertexIndex] = CalculateSmoothNormal(
                        heightMap, sampleX, sampleZ, numSegmentsX, numSegmentsZ, lodStep, stepDistX, stepDistZ, heightMultiplier);

                    // Add surface grid triangles (clockwise winding for upward normal)
                    if (xIndex < verticesPerLineX - 1 && zIndex < verticesPerLineZ - 1)
                    {
                        int current = vertexIndex;
                        int right = vertexIndex + 1;
                        int top = vertexIndex + verticesPerLineX;
                        int topRight = vertexIndex + verticesPerLineX + 1;

                        meshData.AddTriangle(current, top, topRight);
                        meshData.AddTriangle(current, topRight, right);
                    }
                }
            }

            // 2. Generate Vertical Skirt around the 4 outer edges to seal LOD cracks
            int skirtVertIndex = gridVertexCount;

            // Helper to append a skirt wall along a boundary segment (vA -> vB)
            void AddSkirtWall(int vA, int vB)
            {
                Vector3 posA = meshData.Vertices[vA];
                Vector3 posB = meshData.Vertices[vB];

                Vector3 skirtA = new Vector3(posA.x, posA.y - skirtDepth, posA.z);
                Vector3 skirtB = new Vector3(posB.x, posB.y - skirtDepth, posB.z);

                int idxSA = skirtVertIndex++;
                int idxSB = skirtVertIndex++;

                meshData.Vertices[idxSA] = skirtA;
                meshData.Vertices[idxSB] = skirtB;

                meshData.UVs[idxSA] = meshData.UVs[vA];
                meshData.UVs[idxSB] = meshData.UVs[vB];

                colors[idxSA] = colors[vA];
                colors[idxSB] = colors[vB];

                meshData.Normals[idxSA] = meshData.Normals[vA];
                meshData.Normals[idxSB] = meshData.Normals[vB];

                // Skirt quad: (vA, skirtA, skirtB) and (vA, skirtB, vB) facing outward
                meshData.AddTriangle(vA, idxSA, idxSB);
                meshData.AddTriangle(vA, idxSB, vB);
            }

            // South edge (Z = 0, moving +X): (x, 0) -> (x+1, 0)
            for (int x = 0; x < verticesPerLineX - 1; x++)
            {
                int vA = x;
                int vB = x + 1;
                AddSkirtWall(vB, vA);
            }

            // East edge (X = Max, moving +Z): (X_max, z) -> (X_max, z+1)
            for (int z = 0; z < verticesPerLineZ - 1; z++)
            {
                int vA = z * verticesPerLineX + (verticesPerLineX - 1);
                int vB = (z + 1) * verticesPerLineX + (verticesPerLineX - 1);
                AddSkirtWall(vB, vA);
            }

            // North edge (Z = Max, moving -X): (x+1, Z_max) -> (x, Z_max)
            for (int x = verticesPerLineX - 1; x > 0; x--)
            {
                int vA = (verticesPerLineZ - 1) * verticesPerLineX + x;
                int vB = (verticesPerLineZ - 1) * verticesPerLineX + (x - 1);
                AddSkirtWall(vB, vA);
            }

            // West edge (X = 0, moving -Z): (0, z+1) -> (0, z)
            for (int z = verticesPerLineZ - 1; z > 0; z--)
            {
                int vA = z * verticesPerLineX;
                int vB = (z - 1) * verticesPerLineX;
                AddSkirtWall(vB, vA);
            }

            meshData.Colors = colors;
            return meshData;
        }

        private static Vector3 CalculateSmoothNormal(
            HeightMap heightMap,
            int sampleX,
            int sampleZ,
            int maxSegX,
            int maxSegZ,
            int lodStep,
            float stepDistX,
            float stepDistZ,
            float heightMultiplier)
        {
            float hL = heightMap.GetNormalizedValue(sampleX - lodStep, sampleZ) * heightMultiplier;
            float hR = heightMap.GetNormalizedValue(sampleX + lodStep, sampleZ) * heightMultiplier;
            float hD = heightMap.GetNormalizedValue(sampleX, sampleZ - lodStep) * heightMultiplier;
            float hU = heightMap.GetNormalizedValue(sampleX, sampleZ + lodStep) * heightMultiplier;

            float spanX = (sampleX == 0 || sampleX == maxSegX) ? stepDistX : (2f * stepDistX);
            float spanZ = (sampleZ == 0 || sampleZ == maxSegZ) ? stepDistZ : (2f * stepDistZ);

            float dX = (hL - hR) / spanX;
            float dZ = (hD - hU) / spanZ;

            return new Vector3(dX, 2f, dZ).normalized;
        }

        private static Color EvaluateRegionColor(float normalizedHeight, TerrainRegion[] regions)
        {
            if (regions == null || regions.Length == 0)
            {
                return Color.Lerp(new Color(0.2f, 0.6f, 0.2f), new Color(0.8f, 0.8f, 0.8f), normalizedHeight);
            }

            for (int i = 0; i < regions.Length; i++)
            {
                if (normalizedHeight <= regions[i].HeightThreshold)
                {
                    return regions[i].ColorTint;
                }
            }

            return regions[regions.Length - 1].ColorTint;
        }
    }
}
