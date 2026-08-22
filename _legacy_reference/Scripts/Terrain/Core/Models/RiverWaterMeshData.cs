namespace ProjectTwo.Terrain.Core.Models
{
    using UnityEngine;

    /// <summary>
    /// Pure data structure containing procedural vertex, normal, UV, and triangle data for river ribbons.
    /// </summary>
    public class RiverWaterMeshData
    {
        public readonly Vector3[] Vertices;
        public readonly Vector3[] Normals;
        public readonly Vector2[] UVs;
        public readonly int[] Triangles;
        public readonly Color32[] Colors;

        public RiverWaterMeshData(
            Vector3[] vertices,
            Vector3[] normals,
            Vector2[] uvs,
            int[] triangles,
            Color32[] colors = null)
        {
            Vertices = vertices ?? System.Array.Empty<Vector3>();
            Normals = normals ?? System.Array.Empty<Vector3>();
            UVs = uvs ?? System.Array.Empty<Vector2>();
            Triangles = triangles ?? System.Array.Empty<int>();
            Colors = colors;
        }

        public static RiverWaterMeshData Empty => new RiverWaterMeshData(null, null, null, null);

        public bool IsEmpty => Vertices == null || Vertices.Length == 0;

        /// <summary>
        /// Creates a UnityEngine.Mesh instance from this pure mesh data.
        /// </summary>
        public Mesh CreateMesh(string meshName = "ProceduralRiverWaterMesh")
        {
            if (IsEmpty) return null;

            var mesh = new Mesh
            {
                name = meshName,
                vertices = Vertices,
                normals = Normals,
                uv = UVs,
                triangles = Triangles
            };

            if (Colors != null && Colors.Length == Vertices.Length)
            {
                mesh.colors32 = Colors;
            }

            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
