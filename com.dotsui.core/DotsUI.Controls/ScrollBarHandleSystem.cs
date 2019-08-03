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
            [ReadOnly] public ArchetypeChunkComponentType<ScrollBar> ScrollBarType;
            [ReadOnly] public NativeHashMap<Entity, Entity> TargetToEvent;
            [ReadOnly] public BufferFromEntity<PointerInputBuffer> PointerBufferFromEntity;

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
                            HandleInputEvent(scrollBar, pointerBuff[j]);
                    }
                }
            }

            private void HandleInputEvent(ScrollBar scrollBar, PointerInputBuffer pointerInput)
            {
                if(pointerInput.EventType == PointerEventType.Drag)
                    UnityEngine.Debug.Log($"{pointerInput.EventType} Handle entity: {scrollBar.ScrollHandle}, dragDelta: {pointerInput.EventData.Delta}");
            }
        }

        protected override JobHandle OnUpdateInput(JobHandle inputDeps, NativeHashMap<Entity, Entity> targetToEvent, BufferFromEntity<PointerInputBuffer> pointerBufferFromEntity)
        {
            ScrollBarHandleJob scrollBarJob = new ScrollBarHandleJob()
            {
                EntityType = GetArchetypeChunkEntityType(),
                ScrollBarType = GetArchetypeChunkComponentType<ScrollBar>(true),
                TargetToEvent = targetToEvent,
                PointerBufferFromEntity = pointerBufferFromEntity,
            };
            inputDeps = scrollBarJob.Schedule(m_ScrollBarQuery, inputDeps);
            return inputDeps;
        }
    }
}
