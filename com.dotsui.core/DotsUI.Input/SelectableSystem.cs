using DotsUI.Core;
using DotsUI.Core.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace DotsUI.Input
{
    [UpdateInGroup(typeof(InputSystemGroup))]
    [UpdateAfter(typeof(ControlsInputSystem))]
    public class SelectableSystem : PointerInputComponentSystem<Selectable>
    {
        private EntityQuery m_SelectableGroup;

        private InputHandleBarrier m_Barrier;

        protected override void OnCreateInput()
        {
            m_Barrier = World.GetOrCreateSystem<InputHandleBarrier>();
            m_SelectableGroup = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadWrite<Selectable>(), 
                    ComponentType.ReadOnly<SelectableColor>(),
                    ComponentType.ReadWrite<VertexColorValue>()
                }
            });
            RequireForUpdate(m_SelectableGroup);
        }

        protected override void OnDestroyInput()
        {
        }

        [BurstCompile]
        struct SetColorValueJob : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkComponentType<SelectableColor> SelectableColorType;
            public ArchetypeChunkComponentType<Selectable> SelectableType;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public BufferFromEntity<PointerInputBuffer> PointerInputType;
            [ReadOnly] public NativeHashMap<Entity, Entity> TargetToEvent;

            // This is probably not the best idea to disable this restriction, since different selecatables can point to the same target
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<VertexColorMultiplier> ColorMultiplierFromEntity;
            public AddFlagComponentCommandBuffer.ParallelWriter ToUpdate;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var selectableColorArray = chunk.GetNativeArray(SelectableColorType);
                var selectableArray = chunk.GetNativeArray(SelectableType);
                var entityArray = chunk.GetNativeArray(EntityType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var selectableState = selectableArray[i];

                    if (TargetToEvent.TryGetValue(entityArray[i], out Entity eventEntity))
                    {
                        var inputBuffer = PointerInputType[eventEntity];
                        for (int j = 0; j < inputBuffer.Length; j++)
                        {
                            var input = inputBuffer[j];
                            if (input.EventType == PointerEventType.Down && input.EventData.Button == PointerButton.Left)
                                selectableState.Value |= SelectableState.Pressed;
                            else if (input.EventType == PointerEventType.Up && input.EventData.Button == PointerButton.Left)
                                selectableState.Value &= (~SelectableState.Pressed);
                            else if (input.EventType == PointerEventType.Enter)
                                selectableState.Value |= SelectableState.Hovered;
                            else if (input.EventType == PointerEventType.Exit)
                                selectableState.Value &= (~SelectableState.Hovered);
                            else if (input.EventType == PointerEventType.Selected)
                                selectableState.Value |= SelectableState.Selected;
                            else if (input.EventType == PointerEventType.Deselected)
                                selectableState.Value &= (~SelectableState.Selected);
                        }
                    }

                    var target = selectableColorArray[i].Target;
                    if (ColorMultiplierFromEntity.Exists(target))
                    {
                        var currentColor = ColorMultiplierFromEntity[target].Value;
                        float4 newColor = currentColor;
                        if ((selectableState.Value & SelectableState.Pressed) == SelectableState.Pressed)
                        {
                            newColor = selectableColorArray[i].Pressed;
                        }
                        else if ((selectableState.Value & SelectableState.Selected) == SelectableState.Selected)
                        {
                            newColor = selectableColorArray[i].Selected;
                        }
                        else if ((selectableState.Value & SelectableState.Hovered) == SelectableState.Hovered)
                        {
                            newColor = selectableColorArray[i].Hover;
                        }
                        else
                        {
                            newColor = selectableColorArray[i].Normal;
                        }

                        if (!currentColor.Equals(newColor))
                        {
                            ColorMultiplierFromEntity[selectableColorArray[i].Target] = new VertexColorMultiplier()
                            {
                                Value = newColor
                            };
                        }

                        ToUpdate.TryAdd(target);
                        selectableArray[i] = selectableState;
                    }

                }
            }
        };
        protected override JobHandle OnUpdateInput(JobHandle inputDeps, NativeHashMap<Entity, Entity> targetToEvent, BufferFromEntity<PointerInputBuffer> pointerBufferFromEntity)
        {
            var selectableColorType = GetArchetypeChunkComponentType<SelectableColor>(true);
            var selectableType = GetArchetypeChunkComponentType<Selectable>();
            var entityType = GetArchetypeChunkEntityType();

            NativeHashMap<Entity, int> updateQueue = new NativeHashMap<Entity, int>(m_SelectableGroup.CalculateEntityCount(), Allocator.TempJob);
            SetColorValueJob setJob = new SetColorValueJob()
            {
                SelectableColorType = selectableColorType,
                SelectableType = selectableType,
                ColorMultiplierFromEntity = GetComponentDataFromEntity<VertexColorMultiplier>(),
                EntityType = entityType,
                PointerInputType = pointerBufferFromEntity,
                TargetToEvent = targetToEvent,
                ToUpdate = m_Barrier.CreateAddFlagComponentCommandBuffer<UpdateElementColor>().AsParallelWriter()
            };
            inputDeps = setJob.Schedule(m_SelectableGroup, inputDeps);
            m_Barrier.AddJobHandleForProducer(inputDeps);
            inputDeps = updateQueue.Dispose(inputDeps);
            return inputDeps;
        }
    }
}