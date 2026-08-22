namespace ProjectTwo.Terrain.Presentation.Components
{
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Transfer payload containing pre-calculated heightmaps, visual meshes (with skirts),
    /// collision surface meshes, and river ribbon water meshes generated entirely off the Main Thread.
    /// Ready for instantaneous native buffer upload with zero geometry computation on the Main Thread.
    /// </summary>
    public readonly struct ChunkGenerationPayload
    {
        public readonly ChunkCoordinate Coordinate;
        public readonly HeightMap HeightMap;
        public readonly TerrainMeshData VisualMeshData;
        public readonly TerrainMeshData CollisionMeshData;
        public readonly RiverWaterMeshData RiverMeshData;
        public readonly int TargetLOD;
        public readonly bool HasCollider;
        public readonly int PreBakedMeshInstanceID;

        public ChunkGenerationPayload(
            ChunkCoordinate coordinate,
            HeightMap heightMap,
            TerrainMeshData visualMeshData,
            TerrainMeshData collisionMeshData,
            RiverWaterMeshData riverMeshData,
            int targetLOD,
            bool hasCollider,
            int preBakedMeshInstanceID = 0)
        {
            Coordinate = coordinate;
            HeightMap = heightMap;
            VisualMeshData = visualMeshData;
            CollisionMeshData = collisionMeshData;
            RiverMeshData = riverMeshData;
            TargetLOD = targetLOD;
            HasCollider = hasCollider;
            PreBakedMeshInstanceID = preBakedMeshInstanceID;
        }
    }
}
