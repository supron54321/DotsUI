using DotsUI.Core;
using DotsUI.Core.Utils;
using DotsUI.Input;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace DotsUI.Controls
{
    [UpdateInGroup(typeof(InputSystemGroup))]
    [UpdateAfter(typeof(ControlsInputSystemGroup))]
    class ScrollBarHandleSystem : JobComponentSystem
    {
        private EntityQuery m_ScrollBarQuery;
        private InputHandleBarrier m_Barrier;
        private PointerEventQuery m_EventQuery;

        protected override void OnCreate()
        {
            m_ScrollBarQuery = GetEntityQuery(ComponentType.ReadOnly<ScrollBar>());
            RequireForUpdate(m_ScrollBarQuery);
            m_EventQuery = PointerEventQuery.Create<ScrollBar>(EntityManager);
            m_Barrier = World.GetOrCreateSystem<InputHandleBarrier>();
        }

        protected override void OnDestroy()
        {
        }

        [BurstCompile]
        struct ScrollBarHandleJob : IJob
        {
            public ComponentDataFromEntity<ScrollBar> ScrollBarFromEntity;
            [ReadOnly] public InputEventReader<PointerInputBuffer> EventReader;
            [ReadOnly] public ComponentDataFromEntity<ScrollRect> ScrollRectFromEntity;
            public AddFlagComponentCommandBuffer.ParallelWriter AddFlagCommandBuff;

            public void Execute()
            {
                for (int i = 0; i < EventReader.EntityCount; i++)
                {
                    var entity = EventReader[i];
                    ScrollBarFromEntity[entity] = HandleInputEvent(entity, ScrollBarFromEntity[entity]);
                }
            }

            private ScrollBar HandleInputEvent(Entity scrollBarEntity, ScrollBar scrollBar)
            {
                EventReader.GetFirstEvent(scrollBarEntity, out var pointerEvent, out var it);
                do
                {
                    var scrollRect = ScrollRectFromEntity[scrollBar.ParentScrollRect];
                    float value = scrollBar.Value;
                    if (scrollRect.HorizontalBar == scrollBarEntity)
                    {
                        value = math.saturate(scrollBar.Value +
                                              pointerEvent.EventData.Delta.x * scrollBar.HandleDragSensitivity);
                    }
                    else if (scrollRect.VerticalBar == scrollBarEntity)
                    {
                        value = math.saturate(scrollBar.Value +
                                              pointerEvent.EventData.Delta.y * scrollBar.HandleDragSensitivity);
                    }

                    if (math.abs(value - scrollBar.Value) >= scrollBar.HandleDragSensitivity)
                    {
                        AddFlagCommandBuff.TryAdd(scrollBarEntity);
                    }

                    scrollBar.Value = value;
                } while (EventReader.TryGetNextEvent(out pointerEvent, ref it));

                return scrollBar;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var eventReader = m_EventQuery.CreatePointerEventReader(Allocator.TempJob);
            ScrollBarHandleJob scrollBarJob = new ScrollBarHandleJob()
            {
                ScrollBarFromEntity = GetComponentDataFromEntity<ScrollBar>(),
                EventReader = eventReader,
                ScrollRectFromEntity = GetComponentDataFromEntity<ScrollRect>(true),
                AddFlagCommandBuff = m_Barrier.CreateAddFlagComponentCommandBuffer<DirtyElementFlag>().AsParallelWriter()
            };
            inputDeps = scrollBarJob.Schedule(inputDeps);
            m_Barrier.AddJobHandleForProducer(inputDeps);
            inputDeps = eventReader.Dispose(inputDeps);
            return inputDeps;
        }
    }
}
