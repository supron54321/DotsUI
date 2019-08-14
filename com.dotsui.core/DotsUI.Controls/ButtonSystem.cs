using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotsUI.Core;
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

        protected override void OnCreate()
        {
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

            [WriteOnly] public NativeQueue<Entity>.ParallelWriter ClickedButtons;
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
                                ClickedButtons.Enqueue(eventArray[i].Target);
                            }
                        }
                    }

                }
            }
        }

        [BurstCompile]
        struct AddComponentJob : IJob
        {
            public EntityCommandBuffer CommandBuffer;
            public NativeQueue<Entity> ToAdd;

            public void Execute()
            {
                ComponentType component = typeof(ButtonClickedEvent);
                while (ToAdd.TryDequeue(out var entity))
                    CommandBuffer.AddComponent(entity, component);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            NativeQueue<Entity> clickedButtons = new NativeQueue<Entity>(Allocator.TempJob);
            ProcessClicks clicksJob = new ProcessClicks
            {
                BufferType = GetArchetypeChunkBufferType<PointerInputBuffer>(),
                EventType = GetArchetypeChunkComponentType<PointerEvent>(),
                ButtonTargetType = GetComponentDataFromEntity<Button>(),
                ClickedButtons = clickedButtons.AsParallelWriter()
            };
            inputDeps = clicksJob.Schedule(m_EventGroup, inputDeps);
            var barrier = World.GetOrCreateSystem<InputHandleBarrier>();
            AddComponentJob addComponentJob = new AddComponentJob
            {
                CommandBuffer = barrier.CreateCommandBuffer(),
                ToAdd = clickedButtons,
            };
            inputDeps = addComponentJob.Schedule(inputDeps);
            barrier.AddJobHandleForProducer(inputDeps);
            inputDeps = clickedButtons.Dispose(inputDeps);
            return inputDeps;
        }
    }
}
