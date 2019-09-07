using DotsUI.Core;
using Unity.Entities;

namespace DotsUI.Controls
{
    [UpdateInGroup(typeof(CleanupSystemGroup))]
    class ControlsCleanupSystem : ComponentSystem
    {
        private EntityQuery m_ButtonClickedGroup;
        private EntityQuery m_InputFieldEndEditGroup;
        private EntityQuery m_InputFieldReturnGroup;
        protected override void OnCreate()
        {
            m_ButtonClickedGroup = GetEntityQuery(typeof(ButtonClickedEvent));
            m_InputFieldEndEditGroup = GetEntityQuery(typeof(InputFieldEndEditEvent));
            m_InputFieldReturnGroup = GetEntityQuery(typeof(InputFieldReturnEvent));
        }

        protected override void OnUpdate()
        {
            EntityManager.RemoveComponent(m_ButtonClickedGroup, typeof(ButtonClickedEvent));
            EntityManager.RemoveComponent(m_InputFieldEndEditGroup, typeof(InputFieldEndEditEvent));
            EntityManager.RemoveComponent(m_InputFieldReturnGroup, typeof(InputFieldReturnEvent));
        }
    }
}
