namespace ProjectTwo.Terrain.Core.Models
{
    /// <summary>
    /// Classification of topological nodes within the hydrological river graph.
    /// </summary>
    public enum RiverNodeType
    {
        /// <summary>
        /// Origin spring of a river in high mountain catchment areas.
        /// </summary>
        Source = 0,

        /// <summary>
        /// Intermediate navigation waypoint along the river channel.
        /// </summary>
        Waypoint = 1,

        /// <summary>
        /// Confluence junction where multiple tributaries merge into a larger stream.
        /// </summary>
        Confluence = 2,

        /// <summary>
        /// Inlet point where a river enters an inland lake basin.
        /// </summary>
        LakeInlet = 3,

        /// <summary>
        /// Overflow spillover outlet from a lake continuing downstream.
        /// </summary>
        LakeOutlet = 4,

        /// <summary>
        /// Terminal river mouth discharging into the sea / ocean at base water level.
        /// </summary>
        OceanMouth = 5,

        /// <summary>
        /// Steep cliff-conforming vertical cascade / waterfall ribbon.
        /// </summary>
        Waterfall = 6,

        /// <summary>
        /// Fast turbulent rocky spillway or rapids connecting lake tiers.
        /// </summary>
        Rapids = 7,

        /// <summary>
        /// Inflow channel entering a lake basin.
        /// </summary>
        LakeInflow = 8,

        /// <summary>
        /// Outflow spillway emerging from a lake saddle rim.
        /// </summary>
        LakeOutflow = 9,

        /// <summary>
        /// Lowland bifurcation point splitting a river channel into multiple branches.
        /// </summary>
        Bifurcation = 10,

        /// <summary>
        /// Distributary delta mouth discharging into coastal flats or ocean.
        /// </summary>
        DeltaMouth = 11
    }
}
