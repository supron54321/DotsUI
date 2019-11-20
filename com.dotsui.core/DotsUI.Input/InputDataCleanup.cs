using DotsUI.Core;
using Unity.Entities;
using Unity.Jobs;

namespace DotsUI.Input
{
    [UpdateInGroup(typeof(CleanupSystemGroup))]
    internal class InputDataCleanup : JobComponentSystem
    {
        private EntityQuery m_FocusGroup;
        private EntityQuery m_LostFocusGroup;

        protected override void OnCreate()
        {
            m_FocusGroup = GetEntityQuery(ComponentType.ReadWrite<OnFocusEvent>());
            m_LostFocusGroup = GetEntityQuery(ComponentType.ReadWrite<OnLostFocusEvent>());
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            EntityManager.RemoveComponent(m_FocusGroup, ComponentType.ReadWrite<OnFocusEvent>());
            EntityManager.RemoveComponent(m_LostFocusGroup, ComponentType.ReadWrite<OnLostFocusEvent>());

            return inputDeps;
        }
    }
}