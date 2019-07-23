using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Jobs;

namespace DotsUI.Core
{

    [UpdateInGroup(typeof(CleanupSystemGroup))]
    class LayoutCleanSystem : ComponentSystem
    {
        private ComponentType m_ElementComponent = ComponentType.ReadOnly<RebuildElementMeshFlag>();
        private EntityQuery m_ElementMeshDirtGroup;

        private ComponentType m_UpdateColorComponent = ComponentType.ReadOnly<UpdateElementColor>();
        private EntityQuery m_UpdateColorQuery;

        protected override void OnCreate()
        {
            m_ElementMeshDirtGroup = GetEntityQuery(m_ElementComponent);
            m_UpdateColorQuery = GetEntityQuery(m_UpdateColorComponent);
        }

        protected override void OnUpdate()
        {
            //EntityManager.RemoveComponent(m_ElementMeshDirtGroup, m_ElementComponent);
            EntityManager.RemoveComponent(m_UpdateColorQuery, m_UpdateColorComponent);
        }
    }
}
