using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotsUI.Core;
using DotsUI.Input;
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

        protected override void OnCreateInput()
        {
            m_ScrollBarQuery = GetEntityQuery(ComponentType.ReadOnly<ScrollBar>());
            RequireForUpdate(m_ScrollBarQuery);
        }

        protected override void OnDestroyInput()
        {
        }

        struct ScrollBarHandleJob : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            public ArchetypeChunkComponentType<ScrollBar> ScrollBarType;
            [ReadOnly] public ArchetypeChunkComponentType<WorldSpaceRect> ScrollBarRectType;
            [ReadOnly] public NativeHashMap<Entity, Entity> TargetToEvent;
            [ReadOnly] public BufferFromEntity<PointerInputBuffer> PointerBufferFromEntity;
            [ReadOnly] public ComponentDataFromEntity<ScrollRect> ScrollRectFromEntity;

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
                    UnityEngine.Debug.Log(
                        $"{pointerInput.EventType} Handle entity: {scrollBar.ScrollHandle}, dragDelta: {pointerInput.EventData.Delta}");
                    var scrollRect = ScrollRectFromEntity[scrollBar.ParentScrollRect];
                    if (scrollRect.HorizontalBar == scrollBarEntity)
                    {
                        scrollBar.Value = math.saturate(scrollBar.Value + pointerInput.EventData.Delta.x * scrollBar.DragSensitivity);
                    }
                    else if (scrollRect.VerticalBar == scrollBarEntity)
                    {
                        scrollBar.Value = math.saturate(scrollBar.Value + pointerInput.EventData.Delta.y * scrollBar.DragSensitivity);
                    }

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
                ScrollBarRectType = GetArchetypeChunkComponentType<WorldSpaceRect>(true),
                ScrollRectFromEntity = GetComponentDataFromEntity<ScrollRect>(true)
            };
            inputDeps = scrollBarJob.Schedule(m_ScrollBarQuery, inputDeps);
            return inputDeps;
        }
    }
}
