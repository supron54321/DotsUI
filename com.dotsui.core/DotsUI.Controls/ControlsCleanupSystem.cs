using DotsUI.Core;
using Unity.Entities;

namespace DotsUI.Controls
{
    [UpdateInGroup(typeof(CleanupSystemGroup))]
    class ControlsCleanupSystem : ComponentSystem
    {
        private EntityQuery m_ButtonClickedQuery;
        private EntityQuery m_InputFieldEndEditQuery;
        private EntityQuery m_InputFieldReturnQuery;
        private EntityQuery m_SliderValueChangedQuery;

        protected override void OnCreate()
        {
            m_ButtonClickedQuery = GetEntityQuery(typeof(ButtonClickedEvent));
            m_SliderValueChangedQuery = GetEntityQuery(typeof(SliderValueChangedEvent));
            m_InputFieldEndEditQuery = GetEntityQuery(typeof(InputFieldEndEditEvent));
            m_InputFieldReturnQuery = GetEntityQuery(typeof(InputFieldReturnEvent));
        }

        protected override void OnUpdate()
        {
            EntityManager.RemoveComponent(m_ButtonClickedQuery, typeof(ButtonClickedEvent));
            EntityManager.RemoveComponent(m_SliderValueChangedQuery, typeof(SliderValueChangedEvent));
            EntityManager.RemoveComponent(m_InputFieldEndEditQuery, typeof(InputFieldEndEditEvent));
            EntityManager.RemoveComponent(m_InputFieldReturnQuery, typeof(InputFieldReturnEvent));
        }
    }
}
