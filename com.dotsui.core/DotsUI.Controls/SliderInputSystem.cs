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
    [UpdateAfter(typeof(ControlsInputSystemGroup))]
    class SliderInputSystem : JobComponentSystem
    {
        private EntityQuery m_SliderQuery;
        private PointerEventQuery m_PointerQuery;
        private InputHandleBarrier m_Barrier;


        protected override void OnCreate()
        {
            m_SliderQuery = GetEntityQuery(typeof(Slider));
            m_Barrier = World.GetOrCreateSystem<InputHandleBarrier>();
            m_PointerQuery = PointerEventQuery.Create<Slider>(EntityManager);
        }

        protected override void OnDestroy()
        {
        }

        [BurstCompile]
        struct SliderInputJob : IJob
        {
            public ComponentDataFromEntity<Slider> SliderComponentFromEntity;
            public AddFlagComponentCommandBuffer.ParallelWriter AddFlagCommandBuff;
            public AddFlagComponentCommandBuffer.ParallelWriter AddOnChangeFlagCommandBuff;
            [ReadOnly] public ComponentDataFromEntity<WorldSpaceRect> WorldSpaceRectFromEntity;
            [ReadOnly] public InputEventReader<PointerInputBuffer> EventReader;
            [ReadOnly] public ComponentDataFromEntity<Parent> ParentFromEntity;

            public void Execute()
            {
                for (int i = 0; i < EventReader.EntityCount; i++)
                {
                    var entity = EventReader[i];
                    SliderComponentFromEntity[entity] = HandleEvents(SliderComponentFromEntity[entity], entity);
                }
            }

            private Slider HandleEvents(Slider slider, Entity entity)
            {
                EventReader.GetFirstEvent(entity, out var pointerEvent, out var it);
                do
                {
                    if (pointerEvent.EventType == PointerEventType.Down)
                        OnPointerDown(ref slider, entity, pointerEvent);
                    else if (pointerEvent.EventType == PointerEventType.Drag)
                        OnDrag(ref slider, entity, pointerEvent);
                } while (EventReader.TryGetNextEvent(out pointerEvent, ref it));
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

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var eventReader = m_PointerQuery.CreatePointerEventReader(Allocator.TempJob);
            if (eventReader.EntityCount > 0)
            {
                SliderInputJob sliderJob = new SliderInputJob()
                {
                    SliderComponentFromEntity = GetComponentDataFromEntity<Slider>(),
                    AddFlagCommandBuff = m_Barrier.CreateAddFlagComponentCommandBuffer<DirtyElementFlag>().AsParallelWriter(),
                    AddOnChangeFlagCommandBuff = m_Barrier.CreateAddFlagComponentCommandBuffer<SliderValueChangedEvent>().AsParallelWriter(),
                    WorldSpaceRectFromEntity = GetComponentDataFromEntity<WorldSpaceRect>(true),
                    EventReader = eventReader,
                    ParentFromEntity = GetComponentDataFromEntity<Parent>(true)
                };
                inputDeps = sliderJob.Schedule(inputDeps);
                m_Barrier.AddJobHandleForProducer(inputDeps);
            }

            inputDeps = eventReader.Dispose(inputDeps);
            return inputDeps;
        }
    }
}
