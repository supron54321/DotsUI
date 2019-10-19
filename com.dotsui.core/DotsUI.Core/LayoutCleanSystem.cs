using Unity.Entities;

namespace DotsUI.Core
{
    [UnityEngine.ExecuteAlways]
    [UpdateInGroup(typeof(CleanupSystemGroup))]
    class LayoutCleanSystem : ComponentSystem
    {
        private ComponentType m_UpdateColorComponent = ComponentType.ReadOnly<UpdateElementColor>();
        private EntityQuery m_UpdateColorQuery;


        private ComponentType m_RebuildCanvasComponent = ComponentType.ReadOnly<RebuildCanvasHierarchyFlag>();
        private EntityQuery m_RebuildCanvasQuery;

        private ComponentType m_UpdateCanvasComponent = ComponentType.ReadOnly<UpdateCanvasVerticesFlag>();
        private EntityQuery m_UpdateCanvasQuery;

        protected override void OnCreate()
        {
            m_UpdateColorQuery = GetEntityQuery(m_UpdateColorComponent);
            m_RebuildCanvasQuery = GetEntityQuery(m_RebuildCanvasComponent);
            m_UpdateCanvasQuery = GetEntityQuery(m_UpdateCanvasComponent);
        }

        protected override void OnUpdate()
        {
            EntityManager.RemoveComponent(m_UpdateColorQuery, m_UpdateColorComponent);
            EntityManager.RemoveComponent(m_RebuildCanvasQuery, m_RebuildCanvasComponent);
            EntityManager.RemoveComponent(m_UpdateCanvasQuery, m_UpdateCanvasComponent);
        }
    }
}
