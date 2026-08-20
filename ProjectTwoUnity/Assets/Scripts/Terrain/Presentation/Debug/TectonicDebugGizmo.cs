namespace ProjectTwo.Terrain.Presentation.Debug
{
    using UnityEngine;
    using ProjectTwo.Terrain.Core.Models;
    using ProjectTwo.Terrain.Core.Services;

    /// <summary>
    /// Utility for rendering tectonic plate polygons, centroids, and boundary lines via Gizmos.
    /// </summary>
    public static class TectonicDebugGizmo
    {
        public static void DrawTectonicGizmos(TectonicSettings settings, Vector3 origin)
        {
            if (!settings.Enabled) return;

            var tectonicService = new TectonicService();
            tectonicService.GenerateTectonicPartition(settings, out var plates, out var boundaries);

            if (plates != null)
            {
                for (int i = 0; i < plates.Length; i++)
                {
                    ref readonly TectonicPlate plate = ref plates[i];
                    Vector3 center = origin + new Vector3(plate.Centroid.x, 15f, plate.Centroid.y);

                    // Draw Plate Centroid
                    Gizmos.color = plate.CrustType == PlateCrustType.Continental ? Color.yellow : Color.cyan;
                    Gizmos.DrawSphere(center, 8f);

                    // Draw Drift Velocity Arrow
                    Vector3 driftEnd = center + new Vector3(plate.DriftVelocity.x, 0f, plate.DriftVelocity.y) * 40f;
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(center, driftEnd);
                }
            }

            if (boundaries != null)
            {
                for (int i = 0; i < boundaries.Length; i++)
                {
                    ref readonly TectonicBoundary b = ref boundaries[i];
                    Vector3 start = origin + new Vector3(b.StartPoint.x, 10f, b.StartPoint.y);
                    Vector3 end = origin + new Vector3(b.EndPoint.x, 10f, b.EndPoint.y);

                    if (b.BoundaryType == TectonicBoundaryType.Convergent)
                    {
                        Gizmos.color = Color.red;
                    }
                    else if (b.BoundaryType == TectonicBoundaryType.Divergent)
                    {
                        Gizmos.color = Color.blue;
                    }
                    else
                    {
                        Gizmos.color = Color.green;
                    }

                    Gizmos.DrawLine(start, end);
                }
            }
        }
    }
}
