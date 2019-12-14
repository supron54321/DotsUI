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
    class ScrollRectDragSystem : JobComponentSystem
    {
        private EntityQuery m_ScrollRectQuery;
        private PointerEventQuery m_ScrollEventQuery;
        private InputHandleBarrier m_Barrier;

        protected override void OnCreate()
        {
            m_ScrollRectQuery = GetEntityQuery(ComponentType.ReadOnly<ScrollRect>());
            m_ScrollEventQuery = PointerEventQuery.Create<ScrollRect>(EntityManager);
            m_Barrier = World.GetOrCreateSystem<InputHandleBarrier>();
        }

        [BurstCompile]
        struct ScrollBarHandleJob : IJob
        {
            [ReadOnly] public ComponentDataFromEntity<ScrollRect> ScrollRectFromEntity;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<ScrollBar> ScrollBarFormEntity;
            public FlagComponentCommandBuffer.ParallelWriter AddFlagCommandBuff;
            [ReadOnly]
            public InputEventReader<PointerInputBuffer> EventReader;

            public void Execute()
            {
                for (int i = 0; i < EventReader.EntityCount; i++)
                {
                    var target = EventReader[i];
                    EventReader.GetFirstEvent(target, out var pointerEvent, out var it);
                    var scrollRect = ScrollRectFromEntity[target];
                    do
                    {
                        HandleInputEvent(target, scrollRect, pointerEvent);
                    } while (EventReader.TryGetNextEvent(out pointerEvent, ref it));
                }
            }

            private void HandleInputEvent(Entity scrollRectEntity, ScrollRect scrollRect, PointerInputBuffer pointerInput)
            {
                if (pointerInput.EventType == PointerEventType.Drag)
                {
                    var horizontalBar = ScrollBarFormEntity[scrollRect.HorizontalBar];
                    var verticalBar = ScrollBarFormEntity[scrollRect.VerticalBar];
                    float horizontalValue = horizontalBar.Value;
                    float verticalValue = verticalBar.Value;
                    horizontalValue = math.saturate(horizontalBar.Value - pointerInput.EventData.Delta.x * horizontalBar.RectDragSensitivity);
                    verticalValue = math.saturate(verticalBar.Value - pointerInput.EventData.Delta.y * verticalBar.RectDragSensitivity);

                    if (math.abs(horizontalValue - horizontalBar.Value) >= horizontalBar.RectDragSensitivity ||
                        math.abs(verticalValue - verticalBar.Value) >= verticalBar.RectDragSensitivity)
                    {
                        AddFlagCommandBuff.TryAdd(scrollRectEntity);
                    }

                    horizontalBar.Value = horizontalValue;
                    verticalBar.Value = verticalValue;

                    ScrollBarFormEntity[scrollRect.HorizontalBar] = horizontalBar;
                    ScrollBarFormEntity[scrollRect.VerticalBar] = verticalBar;
                }

            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var commandBuff = m_Barrier.CreateFlagComponentCommandBuffer<DirtyElementFlag>();
            var eventReader = m_ScrollEventQuery.CreatePointerEventReader(Allocator.TempJob);
            if (eventReader.EntityCount > 0)
            {
                ScrollBarHandleJob scrollRectDragJob = new ScrollBarHandleJob()
                {
                    AddFlagCommandBuff = commandBuff.AsParallelWriter(),
                    EventReader = eventReader,
                    ScrollBarFormEntity = GetComponentDataFromEntity<ScrollBar>(),
                    ScrollRectFromEntity = GetComponentDataFromEntity<ScrollRect>(true),

                };
                inputDeps = scrollRectDragJob.Schedule(inputDeps);
                m_Barrier.AddJobHandleForProducer(inputDeps);
            }

            inputDeps = eventReader.Dispose(inputDeps);
            return inputDeps;
        }
    }
}
