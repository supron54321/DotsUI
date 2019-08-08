using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotsUI.Core;
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
            [ReadOnly] public ArchetypeChunkComponentType<WorldSpaceRect> ScrollBarRectType;
            [ReadOnly] public NativeHashMap<Entity, Entity> TargetToEvent;
            [ReadOnly] public BufferFromEntity<PointerInputBuffer> PointerBufferFromEntity;
            [ReadOnly] public ComponentDataFromEntity<ScrollRect> ScrollRectFromEntity;
            public NativeHashMap<Entity, int>.ParallelWriter MarkForRebuild;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var scrollBarArray = chunk.GetNativeArray(ScrollBarType);
                var entityArray = chunk.GetNativeArray(EntityType);
                var rectArray = chunk.GetNativeArray(ScrollBarRectType);

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
                if(pointerInput.EventType == PointerEventType.Drag)
                {
                    var scrollRect = ScrollRectFromEntity[scrollBar.ParentScrollRect];
                    float value = scrollBar.Value;
                    if (scrollRect.HorizontalBar == scrollBarEntity)
                    {
                        value = math.saturate(scrollBar.Value + pointerInput.EventData.Delta.x * scrollBar.DragSensitivity);
                    }
                    else if (scrollRect.VerticalBar == scrollBarEntity)
                    {
                        value = math.saturate(scrollBar.Value + pointerInput.EventData.Delta.y * scrollBar.DragSensitivity);
                    }

                    if (math.abs(value - scrollBar.Value) >= scrollBar.DragSensitivity)
                    {
                        MarkForRebuild.TryAdd(scrollBarEntity, 1);
                    }

                    scrollBar.Value = value;
                }

                return scrollBar;
            }
        }

        struct MarkForRebuildJob : IJob
        {
            public EntityCommandBuffer CommandBuffer;
            [ReadOnly] public NativeHashMap<Entity, int> MarkForRebuild;


            public void Execute()
            {
                var entityArray = MarkForRebuild.GetKeyArray(Allocator.Temp);
                for(int i = 0; i < entityArray.Length; i++)
                    CommandBuffer.AddComponent<DirtyElementFlag>(entityArray[i]);
            }
        }

        protected override JobHandle OnUpdateInput(JobHandle inputDeps, NativeHashMap<Entity, Entity> targetToEvent, BufferFromEntity<PointerInputBuffer> pointerBufferFromEntity)
        {
            NativeHashMap<Entity, int> markForRebuild = new NativeHashMap<Entity, int>(4, Allocator.TempJob);
            ScrollBarHandleJob scrollBarJob = new ScrollBarHandleJob()
            {
                EntityType = GetArchetypeChunkEntityType(),
                ScrollBarType = GetArchetypeChunkComponentType<ScrollBar>(),
                TargetToEvent = targetToEvent,
                PointerBufferFromEntity = pointerBufferFromEntity,
                ScrollBarRectType = GetArchetypeChunkComponentType<WorldSpaceRect>(true),
                ScrollRectFromEntity = GetComponentDataFromEntity<ScrollRect>(true),
                MarkForRebuild = markForRebuild.AsParallelWriter(),
            };
            inputDeps = scrollBarJob.Schedule(m_ScrollBarQuery, inputDeps);
            var ecb = m_Barrier.CreateCommandBuffer();
            MarkForRebuildJob rebuildJob = new MarkForRebuildJob()
            {
                MarkForRebuild = markForRebuild,
                CommandBuffer = ecb
            };
            inputDeps = rebuildJob.Schedule(inputDeps);
            m_Barrier.AddJobHandleForProducer(inputDeps);
            return inputDeps;
        }
    }
}
