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
    [UpdateAfter(typeof(ControlsInputSystem))]
    class ScrollBarHandleSystem : PointerInputComponentSystem<ScrollBar>
    {
        private EntityQuery m_ScrollBarQuery;
        private InputHandleBarrier m_Barrier;

        protected override void OnCreateInput()
        {
            m_ScrollBarQuery = GetEntityQuery(ComponentType.ReadOnly<ScrollBar>());
            RequireForUpdate(m_ScrollBarQuery);
            m_Barrier = World.GetOrCreateSystem<InputHandleBarrier>();
        }

        protected override void OnDestroyInput()
        {
        }

        [BurstCompile]
        struct ScrollBarHandleJob : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            public ArchetypeChunkComponentType<ScrollBar> ScrollBarType;
            [ReadOnly] public NativeHashMap<Entity, Entity> TargetToEvent;
            [ReadOnly] public BufferFromEntity<PointerInputBuffer> PointerBufferFromEntity;
            [ReadOnly] public ComponentDataFromEntity<ScrollRect> ScrollRectFromEntity;
            public AddFlagComponentCommandBuffer.ParallelWriter AddFlagCommandBuff;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var scrollBarArray = chunk.GetNativeArray(ScrollBarType);
                var entityArray = chunk.GetNativeArray(EntityType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var scrollBar = scrollBarArray[i];
                    if (TargetToEvent.TryGetValue(entityArray[i], out var eventEntity))
                    {
                        var pointerBuff = PointerBufferFromEntity[eventEntity];
                        for (int j = 0; j < pointerBuff.Length; j++)
                            scrollBarArray[i] = HandleInputEvent(entityArray[i], scrollBar, pointerBuff[j]);
                    }
                }
            }

            private ScrollBar HandleInputEvent(Entity scrollBarEntity, ScrollBar scrollBar, PointerInputBuffer pointerInput)
            {
                if (pointerInput.EventType == PointerEventType.Drag)
                {
                    var scrollRect = ScrollRectFromEntity[scrollBar.ParentScrollRect];
                    float value = scrollBar.Value;
                    if (scrollRect.HorizontalBar == scrollBarEntity)
                    {
                        value = math.saturate(scrollBar.Value + pointerInput.EventData.Delta.x * scrollBar.HandleDragSensitivity);
                    }
                    else if (scrollRect.VerticalBar == scrollBarEntity)
                    {
                        value = math.saturate(scrollBar.Value + pointerInput.EventData.Delta.y * scrollBar.HandleDragSensitivity);
                    }

                    if (math.abs(value - scrollBar.Value) >= scrollBar.HandleDragSensitivity)
                    {
                        AddFlagCommandBuff.TryAdd(scrollBarEntity);
                    }

                    scrollBar.Value = value;
                }

                return scrollBar;
            }
        }

        protected override JobHandle OnUpdateInput(JobHandle inputDeps, NativeHashMap<Entity, Entity> targetToEvent, BufferFromEntity<PointerInputBuffer> pointerBufferFromEntity)
        {
            ScrollBarHandleJob scrollBarJob = new ScrollBarHandleJob()
            {
                EntityType = GetArchetypeChunkEntityType(),
                ScrollBarType = GetArchetypeChunkComponentType<ScrollBar>(),
                TargetToEvent = targetToEvent,
                PointerBufferFromEntity = pointerBufferFromEntity,
                ScrollRectFromEntity = GetComponentDataFromEntity<ScrollRect>(true),
                AddFlagCommandBuff = m_Barrier.CreateAddFlagComponentCommandBuffer<DirtyElementFlag>().AsParallelWriter()
            };
            inputDeps = scrollBarJob.Schedule(m_ScrollBarQuery, inputDeps);
            m_Barrier.AddJobHandleForProducer(inputDeps);
            return inputDeps;
        }
    }
}
