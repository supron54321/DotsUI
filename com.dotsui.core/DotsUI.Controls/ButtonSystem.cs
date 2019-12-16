using System.Diagnostics;
using DotsUI.Core;
using DotsUI.Core.Utils;
using DotsUI.Input;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

namespace DotsUI.Controls
{
    [UpdateInGroup(typeof(InputSystemGroup))]
    [UpdateAfter(typeof(ControlsInputSystemGroup))]
    public class ButtonSystem : JobComponentSystem
    {
        private InputHandleBarrier m_Barrier;

        private PointerEventQuery m_PointerQuery;

        protected override void OnCreate()
        {
            m_Barrier = World.GetOrCreateSystem<InputHandleBarrier>();
            GetEntityQuery(ComponentType.ReadOnly<Button>());
            m_PointerQuery = PointerEventQuery.Create<Button>(EntityManager);
        }

        protected override void OnDestroy()
        {
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var reader = m_PointerQuery.CreatePointerEventReader(Allocator.TempJob);

            for (int i = 0; i < reader.EntityCount; i++)
            {
                foreach (var ev in reader.GetEventsForTargetEntity(reader[i]))
                {
                    if (ev.EventType == PointerEventType.Click)
                        EntityManager.AddComponent<ButtonClickedEvent>(reader[i]);
                }
            }

            inputDeps = reader.Dispose(inputDeps);
            return inputDeps;
        }
    }
}