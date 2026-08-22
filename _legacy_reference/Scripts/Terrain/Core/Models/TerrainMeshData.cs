namespace ProjectTwo.Terrain.Core.Models
{
    using UnityEngine;

    /// <summary>
    /// Holds computed raw geometry buffers before GPU/Unity Mesh upload.
    /// </summary>
    public class TerrainMeshData
    {
        public Vector3[] Vertices { get; set; }
        public int[] Triangles { get; set; }
        public Vector2[] UVs { get; set; }
        public Vector3[] Normals { get; set; }
        public Color[] Colors { get; set; }

        private int _triangleIndex;

        public TerrainMeshData(int vertexCount, int triangleIndexCount)
        {
            Vertices = new Vector3[vertexCount];
            UVs = new Vector2[vertexCount];
            Normals = new Vector3[vertexCount];
            Triangles = new int[triangleIndexCount];
            _triangleIndex = 0;
        }

        public void AddTriangle(int a, int b, int c)
        {
            if (_triangleIndex + 2 < Triangles.Length)
            {
                Triangles[_triangleIndex] = a;
                Triangles[_triangleIndex + 1] = b;
                Triangles[_triangleIndex + 2] = c;
                _triangleIndex += 3;
            }
        }

        public void RecalculateNormals()
        {
            for (int i = 0; i < Normals.Length; i++)
            {
                Normals[i] = Vector3.zero;
            }

            for (int i = 0; i < Triangles.Length; i += 3)
            {
                int iA = Triangles[i];
                int iB = Triangles[i + 1];
                int iC = Triangles[i + 2];

                Vector3 edgeAB = Vertices[iB] - Vertices[iA];
                Vector3 edgeAC = Vertices[iC] - Vertices[iA];
                Vector3 normal = Vector3.Cross(edgeAB, edgeAC).normalized;

                Normals[iA] += normal;
                Normals[iB] += normal;
                Normals[iC] += normal;
            }

            for (int i = 0; i < Normals.Length; i++)
            {
                Normals[i] = Normals[i].normalized;
            }
        }

        public Mesh CreateMesh()
        {
            Mesh mesh = new Mesh();
            if (Vertices.Length > 65535)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            mesh.vertices = Vertices;
            mesh.triangles = Triangles;
            mesh.uv = UVs;
            if (Normals != null && Normals.Length == Vertices.Length)
            {
                mesh.normals = Normals;
            }
            else
            {
                mesh.RecalculateNormals();
            }

            if (Colors != null && Colors.Length == Vertices.Length)
            {
                mesh.colors = Colors;
            }

            mesh.RecalculateBounds();
            return mesh;
        }

        public void ApplyToMesh(Mesh mesh)
        {
            if (mesh == null) return;
            mesh.Clear();
            if (Vertices.Length > 65535)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            mesh.vertices = Vertices;
            mesh.triangles = Triangles;
            mesh.uv = UVs;
            if (Normals != null && Normals.Length == Vertices.Length)
            {
                mesh.normals = Normals;
            }
            else
            {
                mesh.RecalculateNormals();
            }

            if (Colors != null && Colors.Length == Vertices.Length)
            {
                mesh.colors = Colors;
            }

            mesh.RecalculateBounds();
        }
    }
}
