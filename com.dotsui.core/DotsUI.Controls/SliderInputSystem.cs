using System;
using DotsUI.Core;
using DotsUI.Core.Utils;
using DotsUI.Input;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DotsUI.Controls
{
    [UpdateInGroup(typeof(InputSystemGroup))]
    [UpdateAfter(typeof(ControlsInputSystem))]
    class SliderInputSystem : PointerInputComponentSystem<Slider>
    {
        private EntityQuery m_SliderQuery;

        // There is possibility that two different sliders share same handle or fill rect
        // In this scenario multithreading can lead to undefined behavior. We do not care, because sharing rects is bad design anyway.
        struct SliderInputJob : IJobChunk
        {
            public ArchetypeChunkComponentType<Slider> SliderComponentType;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            //public AddFlagComponentCommandBuffer.ParallelWriter AddFlagCommandBuff;
            [ReadOnly] public NativeHashMap<Entity, Entity> TargetToEvent;
            [ReadOnly] public ComponentDataFromEntity<WorldSpaceRect> WorldSpaceRectFromEntity;
            [ReadOnly] public BufferFromEntity<PointerInputBuffer> PointerBufferFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var sliderArray = chunk.GetNativeArray(SliderComponentType);
                var entityArray = chunk.GetNativeArray(EntityType);
                for (int i = 0; i < sliderArray.Length; i++)
                {
                    if (TargetToEvent.TryGetValue(entityArray[i], out var eventEntity))
                    {
                        HandleEvents(sliderArray[i], entityArray[i], eventEntity);
                    }
                }
            }

            private void HandleEvents(Slider slider, Entity entity, Entity eventEntity)
            {
                var pointerBuffer = PointerBufferFromEntity[eventEntity];
                for (int i = 0; i < pointerBuffer.Length; i++)
                {
                    var eventData = pointerBuffer[i];
                    if (eventData.EventType == PointerEventType.Click)
                        OnPointerClick(slider, entity, eventData);
                }
            }

            private void OnPointerClick(Slider slider, Entity entity, PointerInputBuffer eventData)
            {
                if (slider.HandleRect != Entity.Null)
                {
                    var handleRect = WorldSpaceRectFromEntity[slider.HandleRect];
                    if(handleRect.ContainsPoint(eventData.EventData.Position))
                        UnityEngine.Debug.Log($"handle clicked");
                    else
                        UnityEngine.Debug.Log($"slider clicked");
                }
            }
        }

        protected override JobHandle OnUpdateInput(JobHandle inputDeps, NativeHashMap<Entity, Entity> targetToEvent, BufferFromEntity<PointerInputBuffer> pointerBufferFromEntity)
        {
            SliderInputJob sliderJob = new SliderInputJob()
            {
                SliderComponentType = GetArchetypeChunkComponentType<Slider>(),
                EntityType = GetArchetypeChunkEntityType(),
                TargetToEvent = targetToEvent,
                WorldSpaceRectFromEntity = GetComponentDataFromEntity<WorldSpaceRect>(true),
                PointerBufferFromEntity = GetBufferFromEntity<PointerInputBuffer>(true),
            };
            inputDeps = sliderJob.Schedule(m_SliderQuery, inputDeps);
            return inputDeps;
        }

        protected override void OnCreateInput()
        {
            m_SliderQuery = GetEntityQuery(typeof(Slider));
        }

        protected override void OnDestroyInput()
        {
        }
    }
}
