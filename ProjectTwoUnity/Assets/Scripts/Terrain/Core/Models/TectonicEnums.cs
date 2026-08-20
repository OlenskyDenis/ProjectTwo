namespace ProjectTwo.Terrain.Core.Models
{
    /// <summary>
    /// Classification of tectonic plate boundary interactions.
    /// </summary>
    public enum TectonicBoundaryType
    {
        /// <summary>
        /// Plates collide or subduct, creating massive mountain ridges and orogenic belts.
        /// </summary>
        Convergent = 0,

        /// <summary>
        /// Plates pull apart, creating sunken rift valleys and spreading trenches.
        /// </summary>
        Divergent = 1,

        /// <summary>
        /// Plates slide laterally past each other, creating sheared fault hills and fracture zones.
        /// </summary>
        Transform = 2
    }

    /// <summary>
    /// Type of crustal material forming a tectonic plate.
    /// </summary>
    public enum PlateCrustType
    {
        /// <summary>
        /// Buoyant, thicker crust forming landmasses and mountain bases.
        /// </summary>
        Continental = 0,

        /// <summary>
        /// Dense, lower-elevation crust forming ocean floors and deep basins.
        /// </summary>
        Oceanic = 1
    }
}
