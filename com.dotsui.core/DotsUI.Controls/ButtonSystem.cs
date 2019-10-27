using DotsUI.Core;
using DotsUI.Core.Utils;
using DotsUI.Input;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

namespace DotsUI.Controls
{
    [UpdateInGroup(typeof(InputSystemGroup))]
    [UpdateAfter(typeof(ControlsInputSystem))]
    public class ButtonSystem : JobComponentSystem
    {
        private EntityQuery m_EventGroup;
        private InputHandleBarrier m_Barrier;

        protected override void OnCreate()
        {
            m_Barrier = World.GetOrCreateSystem<InputHandleBarrier>();
            m_EventGroup = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[]
                {
                    ComponentType.ReadWrite<PointerInputBuffer>(),
                    ComponentType.ReadWrite<PointerEvent>()
                }
            });
        }

        protected override void OnDestroy()
        {
        }
        [BurstCompile]
        private struct ProcessClicks : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkComponentType<PointerEvent> EventType;
            [ReadOnly] public ArchetypeChunkBufferType<PointerInputBuffer> BufferType;

            [WriteOnly] public AddFlagComponentCommandBuffer.ParallelWriter ClickedButtons;
            [ReadOnly] public ComponentDataFromEntity<Button> ButtonTargetType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var eventArray = chunk.GetNativeArray(EventType);
                var bufferAccessor = chunk.GetBufferAccessor(BufferType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    if (ButtonTargetType.Exists(eventArray[i].Target))
                    {
                        var buff = bufferAccessor[i];
                        for (int j = 0; j < buff.Length; j++)
                        {
                            if (buff[j].EventType == PointerEventType.Click)
                            {
                                ClickedButtons.TryAdd(eventArray[i].Target);
                            }
                        }
                    }

                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var commandBuff = m_Barrier.CreateAddFlagComponentCommandBuffer<ButtonClickedEvent>();
            ProcessClicks clicksJob = new ProcessClicks
            {
                BufferType = GetArchetypeChunkBufferType<PointerInputBuffer>(),
                EventType = GetArchetypeChunkComponentType<PointerEvent>(),
                ButtonTargetType = GetComponentDataFromEntity<Button>(),
                ClickedButtons = commandBuff.AsParallelWriter()
            };
            inputDeps = clicksJob.Schedule(m_EventGroup, inputDeps);
            m_Barrier.AddJobHandleForProducer(inputDeps);
            return inputDeps;
        }
    }
}
