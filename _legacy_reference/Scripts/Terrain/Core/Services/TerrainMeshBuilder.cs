namespace ProjectTwo.Terrain.Core.Services
{
    using System.Collections.Generic;
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Pure C# domain service for constructing 3D terrain mesh data from heightmaps.
    /// Supports optional skirt generation (true for seamless visuals, false for physics colliders).
    /// </summary>
    public static class TerrainMeshBuilder
    {
        public static TerrainMeshData GenerateTerrainMesh(
            HeightMap heightMap,
            float chunkSize,
            float heightMultiplier,
            int lodStep = 1,
            TerrainRegion[] regions = null,
            bool includeSkirt = true)
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

            int skirtVertexCount = 0;
            int skirtTriangleIndexCount = 0;

            if (includeSkirt)
            {
                int perimeterSegments = ((verticesPerLineX - 1) + (verticesPerLineZ - 1)) * 2;
                skirtVertexCount = perimeterSegments * 2;
                skirtTriangleIndexCount = perimeterSegments * 6;
            }

            int totalVertices = gridVertexCount + skirtVertexCount;
            int totalTriangles = gridTriangleCount + skirtTriangleIndexCount;

            TerrainMeshData meshData = new TerrainMeshData(totalVertices, totalTriangles);
            Color[] colors = new Color[totalVertices];

            float halfChunkSize = chunkSize * 0.5f;
            float stepDistX = (float)lodStep / numSegmentsX * chunkSize;
            float stepDistZ = (float)lodStep / numSegmentsZ * chunkSize;

            float skirtDepth = Mathf.Clamp(heightMultiplier * 0.08f, 5f, 20f);

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

                    meshData.Normals[vertexIndex] = CalculateSmoothNormal(
                        heightMap, sampleX, sampleZ, numSegmentsX, numSegmentsZ, lodStep, stepDistX, stepDistZ, heightMultiplier);

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

            // 2. Generate Vertical Skirt (visuals only)
            if (includeSkirt)
            {
                int skirtVertIndex = gridVertexCount;

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

                    meshData.AddTriangle(vA, idxSA, idxSB);
                    meshData.AddTriangle(vA, idxSB, vB);
                }

                for (int x = 0; x < verticesPerLineX - 1; x++)
                {
                    AddSkirtWall(x + 1, x);
                }

                for (int z = 0; z < verticesPerLineZ - 1; z++)
                {
                    int vA = z * verticesPerLineX + (verticesPerLineX - 1);
                    int vB = (z + 1) * verticesPerLineX + (verticesPerLineX - 1);
                    AddSkirtWall(vB, vA);
                }

                for (int x = verticesPerLineX - 1; x > 0; x--)
                {
                    int vA = (verticesPerLineZ - 1) * verticesPerLineX + x;
                    int vB = (verticesPerLineZ - 1) * verticesPerLineX + (x - 1);
                    AddSkirtWall(vB, vA);
                }

                for (int z = verticesPerLineZ - 1; z > 0; z--)
                {
                    int vA = z * verticesPerLineX;
                    int vB = (z - 1) * verticesPerLineX;
                    AddSkirtWall(vB, vA);
                }
            }

            meshData.Colors = colors;
            return meshData;
        }

        private static Vector3 CalculateSmoothNormal(
            HeightMap heightMap,
            int x,
            int z,
            int numSegmentsX,
            int numSegmentsZ,
            int lodStep,
            float stepDistX,
            float stepDistZ,
            float heightMultiplier)
        {
            int leftX = Mathf.Max(0, x - lodStep);
            int rightX = Mathf.Min(numSegmentsX, x + lodStep);
            int downZ = Mathf.Max(0, z - lodStep);
            int upZ = Mathf.Min(numSegmentsZ, z + lodStep);

            float hL = heightMap.Values[leftX, z] * heightMultiplier;
            float hR = heightMap.Values[rightX, z] * heightMultiplier;
            float hD = heightMap.Values[x, downZ] * heightMultiplier;
            float hU = heightMap.Values[x, upZ] * heightMultiplier;

            float spanX = (rightX - leftX) > 0 ? (float)(rightX - leftX) / lodStep * stepDistX : 1f;
            float spanZ = (upZ - downZ) > 0 ? (float)(upZ - downZ) / lodStep * stepDistZ : 1f;

            Vector3 normal = new Vector3(hL - hR, spanX + spanZ, hD - hU);
            return normal.normalized;
        }

        private static Color EvaluateRegionColor(float normalizedHeight, TerrainRegion[] regions)
        {
            if (regions == null || regions.Length == 0)
            {
                return Color.white;
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
