namespace ProjectTwo.Terrain.Core.Services
{
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Pure C# domain service for constructing 3D terrain mesh data from heightmaps.
    /// Handles multi-LOD vertex skipping and normal generation.
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

            float topLeftX = (chunkSize) / -2f;
            float topLeftZ = (chunkSize) / 2f;

            int meshSimplificationIncrement = lodStep;
            int verticesPerLineX = (width - 1) / meshSimplificationIncrement + 1;
            int verticesPerLineZ = (height - 1) / meshSimplificationIncrement + 1;

            int totalVertices = verticesPerLineX * verticesPerLineZ;
            int totalTriangles = (verticesPerLineX - 1) * (verticesPerLineZ - 1) * 6;

            TerrainMeshData meshData = new TerrainMeshData(totalVertices, totalTriangles);
            Color[] colors = new Color[totalVertices];

            int vertexIndex = 0;

            for (int y = 0; y < height; y += meshSimplificationIncrement)
            {
                for (int x = 0; x < width; x += meshSimplificationIncrement)
                {
                    float normalizedHeight = heightMap.Values[x, y];
                    float currentElevation = normalizedHeight * heightMultiplier;

                    float percentX = (float)x / (width - 1);
                    float percentZ = (float)y / (height - 1);

                    float posX = topLeftX + percentX * chunkSize;
                    float posZ = topLeftZ - percentZ * chunkSize;

                    meshData.Vertices[vertexIndex] = new Vector3(posX, currentElevation, posZ);
                    meshData.UVs[vertexIndex] = new Vector2(percentX, percentZ);

                    // Assign color based on elevation regions
                    colors[vertexIndex] = EvaluateRegionColor(normalizedHeight, regions);

                    if (x < width - 1 && y < height - 1)
                    {
                        int current = vertexIndex;
                        int right = vertexIndex + 1;
                        int bottom = vertexIndex + verticesPerLineX;
                        int bottomRight = vertexIndex + verticesPerLineX + 1;

                        meshData.AddTriangle(current, right, bottom);
                        meshData.AddTriangle(right, bottomRight, bottom);
                    }

                    vertexIndex++;
                }
            }

            meshData.Colors = colors;
            meshData.RecalculateNormals();
            return meshData;
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
