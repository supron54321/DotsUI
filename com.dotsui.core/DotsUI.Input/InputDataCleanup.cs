using DotsUI.Core;
using Unity.Entities;
using Unity.Jobs;

namespace DotsUI.Input
{
    [UpdateInGroup(typeof(CleanupSystemGroup))]
    internal class InputDataCleanup : JobComponentSystem
    {
        private EntityQuery m_PointerGroup;
        private EntityQuery m_FocusGroup;
        private EntityQuery m_LostFocusGroup;
        private EntityQuery m_KeyboardGroup;

        protected override void OnCreate()
        {
            m_PointerGroup = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[]
                {
                    ComponentType.ReadWrite<PointerInputBuffer>(),
                    ComponentType.ReadWrite<PointerEvent>()
                }
            });
            m_KeyboardGroup = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[]
                {
                    ComponentType.ReadWrite<KeyboardInputBuffer>(),
                    ComponentType.ReadWrite<KeyboardEvent>()
                }
            });
            m_FocusGroup = GetEntityQuery(ComponentType.ReadWrite<OnFocusEvent>());
            m_LostFocusGroup = GetEntityQuery(ComponentType.ReadWrite<OnLostFocusEvent>());
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            EntityManager.DestroyEntity(m_PointerGroup);
            EntityManager.DestroyEntity(m_KeyboardGroup);
            EntityManager.RemoveComponent(m_FocusGroup, ComponentType.ReadWrite<OnFocusEvent>());
            EntityManager.RemoveComponent(m_LostFocusGroup, ComponentType.ReadWrite<OnLostFocusEvent>());

            return inputDeps;
        }
    }
}