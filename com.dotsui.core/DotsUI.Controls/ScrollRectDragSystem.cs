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
    class ScrollRectDragSystem : PointerInputComponentSystem<ScrollRect>
    {
        private EntityQuery m_ScrollRectQuery;
        private InputHandleBarrier m_Barrier;

        protected override void OnCreateInput()
        {
            m_ScrollRectQuery = GetEntityQuery(ComponentType.ReadOnly<ScrollRect>());
            m_Barrier = World.GetOrCreateSystem<InputHandleBarrier>();
        }

        [BurstCompile]
        struct ScrollBarHandleJob : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkComponentType<ScrollRect> ScrollRectType;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<ScrollBar> ScrollBarFormEntity;
            [ReadOnly] public NativeHashMap<Entity, Entity> TargetToEvent;
            [ReadOnly] public BufferFromEntity<PointerInputBuffer> PointerBufferFromEntity;
            public AddFlagComponentCommandBuffer.ParallelWriter AddFlagCommandBuff;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var scrollRectArray = chunk.GetNativeArray(ScrollRectType);
                var entityArray = chunk.GetNativeArray(EntityType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    if (TargetToEvent.TryGetValue(entityArray[i], out var eventEntity))
                    {
                        var scrollRect = scrollRectArray[i];
                        var pointerBuff = PointerBufferFromEntity[eventEntity];
                        for (int j = 0; j < pointerBuff.Length; j++)
                            HandleInputEvent(entityArray[i], scrollRect, pointerBuff[j]);
                    }
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

        protected override JobHandle OnUpdateInput(JobHandle inputDeps, NativeHashMap<Entity, Entity> targetToEvent, BufferFromEntity<PointerInputBuffer> pointerBufferFromEntity)
        {
            var commandBuff = m_Barrier.CreateAddFlagComponentCommandBuffer<DirtyElementFlag>();
            ScrollBarHandleJob scrollRectDragJob = new ScrollBarHandleJob()
            {
                AddFlagCommandBuff = commandBuff.AsParallelWriter(),
                EntityType = GetArchetypeChunkEntityType(),
                PointerBufferFromEntity = GetBufferFromEntity<PointerInputBuffer>(true),
                ScrollBarFormEntity = GetComponentDataFromEntity<ScrollBar>(),
                ScrollRectType = GetArchetypeChunkComponentType<ScrollRect>(true),
                TargetToEvent = targetToEvent

            };
            inputDeps = scrollRectDragJob.Schedule(m_ScrollRectQuery, inputDeps);
            m_Barrier.AddJobHandleForProducer(inputDeps);
            return inputDeps;
        }

        protected override void OnDestroyInput()
        {
        }
    }
}
