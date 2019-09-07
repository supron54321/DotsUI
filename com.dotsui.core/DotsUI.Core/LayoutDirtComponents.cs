using Unity.Entities;

namespace DotsUI.Core
{
    /// <summary>
    /// Special "flag" component attached to canvas. Forces entire layout to be rebuilt.
    /// </summary>
    public struct RebuildCanvasHierarchyFlag : IComponentData
    {

    }

    /// <summary>
    /// Only update vertices. Used for fast color update
    /// </summary>
    public struct UpdateCanvasVerticesFlag : IComponentData
    {

    }

    /// <summary>
    /// Updates element color with "Fast path"
    /// </summary>
    public struct UpdateElementColor : IComponentData
    {

    }
    public struct RebuildElementMeshFlag : IComponentData
    {
        public bool Rebuild;
    }
    public struct DirtyElementFlag : IComponentData
    {

    }
}