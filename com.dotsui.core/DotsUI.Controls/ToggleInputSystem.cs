using DotsUI.Core;
using DotsUI.Core.Utils;
using DotsUI.Input;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DotsUI.Controls
{
    [UpdateInGroup(typeof(InputSystemGroup))]
    [UpdateAfter(typeof(ControlsInputSystemGroup))]
    class ToggleInputSystem : JobComponentSystem
    {
        private EntityQuery m_ToggleQuery;
        private PointerEventQuery m_EventQuery;
        private InputHandleBarrier m_Barrier;

        protected override void OnCreate()
        {
            m_ToggleQuery = GetEntityQuery(typeof(Toggle));
            m_Barrier = World.GetOrCreateSystem<InputHandleBarrier>();
            m_EventQuery = PointerEventQuery.Create<Toggle>(EntityManager);
        }

        struct UpdateToggle : IJob
        {
            public ComponentDataFromEntity<Toggle> ToggleComponentFromEntity;
            public InputEventReader<PointerInputBuffer> EventReader;
            public FlagComponentCommandBuffer.ParallelWriter DisableCommandBuff;
            public FlagComponentCommandBuffer.ParallelWriter RebuildMeshCommandBuff;
            public FlagComponentCommandBuffer.ParallelWriter ToggleEventCommandBuff;

            public void Execute()
            {
                for (int i = 0; i < EventReader.EntityCount; i++)
                {
                    var entity = EventReader[i];
                    var toggle = ToggleComponentFromEntity[entity];
                    ToggleComponentFromEntity[entity] = HandleEvents(entity, toggle);
                }
            }

            private Toggle HandleEvents(Entity entity, Toggle toggle)
            {
                var ret = toggle;
                EventReader.GetFirstEvent(entity, out var pointerEvent, out var it);
                do
                {
                    if ((pointerEvent.EventType & PointerEventType.Click) != 0)
                    {
                        ret.IsOn = !ret.IsOn;
                    }
                } while (EventReader.TryGetNextEvent(out pointerEvent, ref it));

                if (ret.IsOn != toggle.IsOn)
                {
                    if(ret.IsOn)
                        DisableCommandBuff.TryRemove(toggle.TargetGraphic);
                    else
                        DisableCommandBuff.TryAdd(toggle.TargetGraphic);
                    RebuildMeshCommandBuff.TryAdd(entity);
                    ToggleEventCommandBuff.TryAdd(entity);
                }

                return ret;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var eventReader = m_EventQuery.CreatePointerEventReader(Allocator.TempJob);
            if (eventReader.EntityCount > 0)
            {
                UpdateToggle toggleJob = new UpdateToggle()
                {
                    ToggleComponentFromEntity = GetComponentDataFromEntity<Toggle>(),
                    EventReader = eventReader,
                    DisableCommandBuff = m_Barrier.CreateFlagComponentCommandBuffer<Disabled>().AsParallelWriter(),
                    RebuildMeshCommandBuff = m_Barrier.CreateFlagComponentCommandBuffer<DirtyElementFlag>().AsParallelWriter(),
                    ToggleEventCommandBuff = m_Barrier.CreateFlagComponentCommandBuffer<ToggleValueChangedEvent>().AsParallelWriter(),
                };
                inputDeps = toggleJob.Schedule(inputDeps);
                m_Barrier.AddJobHandleForProducer(inputDeps);
            }
            inputDeps = eventReader.Dispose(inputDeps);
            return inputDeps;
        }
    }
}
