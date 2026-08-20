namespace ProjectTwo.Terrain.Presentation.Debug
{
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;

    /// <summary>
    /// Utility for rendering river splines, nodes, confluences, and lake basins via Gizmos.
    /// </summary>
    public static class RiverGraphDebugGizmo
    {
        public static void DrawRiverGizmos(RiverGraph graph, Vector3 origin)
        {
            if (graph == null || graph.SegmentCount == 0) return;

            // Draw Segments
            for (int i = 0; i < graph.Segments.Length; i++)
            {
                ref readonly RiverSegment seg = ref graph.Segments[i];
                Gizmos.color = new Color(0.1f, 0.7f, 1f, 0.9f);

                Vector3 p0 = origin + seg.StartPosition;
                Vector3 p1 = origin + seg.ControlPoint;
                Vector3 p2 = origin + seg.EndPosition;

                Vector3 prev = p0;
                const int steps = 8;
                for (int s = 1; s <= steps; s++)
                {
                    float t = (float)s / steps;
                    float u = 1f - t;
                    Vector3 current = u * u * p0 + 2f * u * t * p1 + t * t * p2;
                    Gizmos.DrawLine(prev, current);
                    prev = current;
                }
            }

            // Draw Nodes
            for (int i = 0; i < graph.Nodes.Length; i++)
            {
                ref readonly RiverNode node = ref graph.Nodes[i];
                Vector3 pos = origin + node.Position;

                if (node.NodeType == RiverNodeType.Source)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawSphere(pos, 4f);
                }
                else if (node.NodeType == RiverNodeType.OceanMouth)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawCube(pos, Vector3.one * 6f);
                }
                else if (node.NodeType == RiverNodeType.Confluence)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(pos, 3.5f);
                }
            }

            // Draw Lakes
            for (int i = 0; i < graph.Lakes.Length; i++)
            {
                ref readonly LakeBasin lake = ref graph.Lakes[i];
                Gizmos.color = new Color(0.2f, 0.5f, 0.9f, 0.4f);
                Gizmos.DrawSphere(origin + lake.Center, lake.Radius);
            }
        }
    }
}
