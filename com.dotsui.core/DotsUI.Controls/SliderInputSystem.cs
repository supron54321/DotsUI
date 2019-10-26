using DotsUI.Core;
using DotsUI.Core.Utils;
using DotsUI.Input;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsUI.Controls
{
    [UpdateInGroup(typeof(InputSystemGroup))]
    [UpdateAfter(typeof(ControlsInputSystem))]
    class SliderInputSystem : PointerInputComponentSystem<Slider>
    {
        private EntityQuery m_SliderQuery;
        private InputHandleBarrier m_Barrier;

        [BurstCompile]
        struct SliderInputJob : IJobChunk
        {
            public ArchetypeChunkComponentType<Slider> SliderComponentType;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            public AddFlagComponentCommandBuffer.ParallelWriter AddFlagCommandBuff;
            public AddFlagComponentCommandBuffer.ParallelWriter AddOnChangeFlagCommandBuff;
            [ReadOnly] public NativeHashMap<Entity, Entity> TargetToEvent;
            [ReadOnly] public ComponentDataFromEntity<WorldSpaceRect> WorldSpaceRectFromEntity;
            [ReadOnly] public BufferFromEntity<PointerInputBuffer> PointerBufferFromEntity;
            [ReadOnly] public ComponentDataFromEntity<Parent> ParentFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var sliderArray = chunk.GetNativeArray(SliderComponentType);
                var entityArray = chunk.GetNativeArray(EntityType);
                for (int i = 0; i < sliderArray.Length; i++)
                {
                    if (TargetToEvent.TryGetValue(entityArray[i], out var eventEntity))
                    {
                        sliderArray[i] = HandleEvents(sliderArray[i], entityArray[i], eventEntity);
                    }
                }
            }

            private Slider HandleEvents(Slider slider, Entity entity, Entity eventEntity)
            {
                var pointerBuffer = PointerBufferFromEntity[eventEntity];
                for (int i = 0; i < pointerBuffer.Length; i++)
                {
                    var eventData = pointerBuffer[i];
                    if (eventData.EventType == PointerEventType.Down)
                        OnPointerDown(ref slider, entity, eventData);
                    else if (eventData.EventType == PointerEventType.Drag)
                        OnDrag(ref slider, entity, eventData);
                }

                return slider;
            }

            private void OnDrag(ref Slider slider, Entity entity, PointerInputBuffer eventData)
            {
                var sliderRect = GetSliderInputRect(ref slider);
                MoveValueTo(ref slider, entity, sliderRect.WorldSpaceToNormalizedLocalPoint(eventData.EventData.Position));
            }

            private WorldSpaceRect GetSliderInputRect(ref Slider slider)
            {
                if (slider.HandleRect != Entity.Null && ParentFromEntity.Exists(slider.HandleRect))
                    return WorldSpaceRectFromEntity[ParentFromEntity[slider.HandleRect].Value];
                return WorldSpaceRectFromEntity[ParentFromEntity[slider.FillRect].Value];
            }

            private void OnPointerDown(ref Slider slider, Entity entity, PointerInputBuffer eventData)
            {
                var sliderRect = GetSliderInputRect(ref slider);
                if (slider.HandleRect != Entity.Null)
                {
                    var handleRect = WorldSpaceRectFromEntity[slider.HandleRect];
                    if (handleRect.ContainsPoint(eventData.EventData.Position))
                        return;
                }
                MoveValueTo(ref slider, entity, sliderRect.WorldSpaceToNormalizedLocalPoint(eventData.EventData.Position));
            }

            private void MoveValueTo(ref Slider slider, Entity entity, float2 normalizedLocalPoint)
            {
                var oldValue = slider.Value;

                if(slider.Reversed)
                    slider.NormalizedValue = 1 - normalizedLocalPoint[slider.GetAxis()];
                else
                    slider.NormalizedValue = normalizedLocalPoint[slider.GetAxis()];

                if(slider.Value != oldValue)
                    AddOnChangeFlagCommandBuff.TryAdd(entity);
                AddFlagCommandBuff.TryAdd(entity);
            }
        }

        protected override JobHandle OnUpdateInput(JobHandle inputDeps, NativeHashMap<Entity, Entity> targetToEvent, BufferFromEntity<PointerInputBuffer> pointerBufferFromEntity)
        {
            SliderInputJob sliderJob = new SliderInputJob()
            {
                SliderComponentType = GetArchetypeChunkComponentType<Slider>(),
                EntityType = GetArchetypeChunkEntityType(),
                AddFlagCommandBuff = m_Barrier.CreateAddFlagComponentCommandBuffer<DirtyElementFlag>().AsParallelWriter(),
                AddOnChangeFlagCommandBuff = m_Barrier.CreateAddFlagComponentCommandBuffer<SliderValueChangedEvent>().AsParallelWriter(),
                TargetToEvent = targetToEvent,
                WorldSpaceRectFromEntity = GetComponentDataFromEntity<WorldSpaceRect>(true),
                PointerBufferFromEntity = GetBufferFromEntity<PointerInputBuffer>(true),
                ParentFromEntity = GetComponentDataFromEntity<Parent>(true)
            };
            inputDeps = sliderJob.Schedule(m_SliderQuery, inputDeps);
            m_Barrier.AddJobHandleForProducer(inputDeps);
            return inputDeps;
        }

        protected override void OnCreateInput()
        {
            m_SliderQuery = GetEntityQuery(typeof(Slider));
            m_Barrier = World.GetOrCreateSystem<InputHandleBarrier>();
        }

        protected override void OnDestroyInput()
        {
        }
    }
}
