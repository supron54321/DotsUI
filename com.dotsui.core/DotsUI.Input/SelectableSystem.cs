using DotsUI.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace DotsUI.Input
{
    [UpdateInGroup(typeof(InputSystemGroup))]
    [UpdateAfter(typeof(ControlsInputSystem))]
    public class SelectableSystem : JobComponentSystem
    {
        class SystemBarrier : EntityCommandBufferSystem
        {

        }
        private EntityQuery m_SelectableGroup;
        private EntityQuery m_EventGroup;

        private NativeHashMap<Entity, Entity> m_TargetToEvent;
        private SystemBarrier m_Barrier;

        protected override void OnCreateManager()
        {
            m_TargetToEvent = new NativeHashMap<Entity, Entity>(10, Allocator.Persistent);
            m_Barrier = World.GetOrCreateSystem<SystemBarrier>();
            m_EventGroup = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<PointerEvent>(),
                    ComponentType.ReadOnly<PointerInputBuffer>()
                }
            });
            m_SelectableGroup = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<SelectableColor>(),
                    ComponentType.ReadWrite<VertexColorValue>()
                }
            });
        }

        protected override void OnDestroyManager()
        {
            m_TargetToEvent.Dispose();
        }
        
        
        [BurstCompile]
        struct CreateTargetToEventMap : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkComponentType<PointerEvent> EventType;
            [WriteOnly] public NativeHashMap<Entity, Entity>.Concurrent TargetToEvent;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);
                var eventArray = chunk.GetNativeArray(EventType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    TargetToEvent.TryAdd(eventArray[i].Target, entityArray[i]);
                }
            }
        }
        
        [BurstCompile]
        struct SetColorValueJob : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkComponentType<SelectableColor> SelectableColorType;
            public ArchetypeChunkComponentType<Selectable> SelectableType;
            public ArchetypeChunkComponentType<VertexColorMultiplier> ColorMultiplierType;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public BufferFromEntity<PointerInputBuffer> PointerInputType;
            [ReadOnly] public NativeHashMap<Entity, Entity> TargetToEvent;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var selectableColorArray = chunk.GetNativeArray(SelectableColorType);
                var selectableArray = chunk.GetNativeArray(SelectableType);
                var colorArray = chunk.GetNativeArray(ColorMultiplierType);
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

                    var currentColor = colorArray[i].Value;
                    float4 newColor = currentColor;
                    if ((selectableState.Value&SelectableState.Pressed) == SelectableState.Pressed)
                    {
                        newColor = selectableColorArray[i].Pressed;
                    }
                    else if ((selectableState.Value&SelectableState.Selected) == SelectableState.Selected)
                    {
                        newColor = selectableColorArray[i].Selected;
                    }
                    else if ((selectableState.Value&SelectableState.Hovered) == SelectableState.Hovered)
                    {
                        newColor = selectableColorArray[i].Hover;
                    }
                    else
                    {
                        newColor = selectableColorArray[i].Normal;
                    }

                    if (!currentColor.Equals(newColor))
                    {
                        colorArray[i] = new VertexColorMultiplier()
                        {
                            Value = newColor
                        };
                    }

                    selectableArray[i] = selectableState;
                }
            }
        };

        //struct UpdateColorFlagging : IJob
        //{
        //    public NativeHashMap<Entity, int> UpdateEntities;
        //    public EntityCommandBuffer CommandBuffer;

        //    public void Execute()
        //    {
        //        UpdateEntities.GetKeyArray(Allocator.Temp);
        //        CommandBuffer.AddComponent()
        //    }
        //}

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (m_EventGroup.CalculateLength() > 0)
            {
                m_TargetToEvent.Clear();
                var selectableColorType = GetArchetypeChunkComponentType<SelectableColor>(true);
                var selectableType = GetArchetypeChunkComponentType<Selectable>();
                var colorMultiplierType = GetArchetypeChunkComponentType<VertexColorMultiplier>();
                var entityType = GetArchetypeChunkEntityType();
                CreateTargetToEventMap createTargetToEvent = new CreateTargetToEventMap()
                {
                    EntityType = GetArchetypeChunkEntityType(),
                    EventType = GetArchetypeChunkComponentType<PointerEvent>(true),
                    TargetToEvent = m_TargetToEvent.ToConcurrent()
                };
                inputDeps = createTargetToEvent.Schedule(m_EventGroup, inputDeps);
            
                SetColorValueJob setJob = new SetColorValueJob()
                {
                    SelectableColorType = selectableColorType,
                    SelectableType = selectableType,
                    ColorMultiplierType = colorMultiplierType,
                    EntityType = entityType,
                    PointerInputType = GetBufferFromEntity<PointerInputBuffer>(),
                    TargetToEvent = m_TargetToEvent,
                };
                inputDeps = setJob.Schedule(m_SelectableGroup, inputDeps);
                inputDeps.Complete();
                EntityManager.AddComponent(m_TargetToEvent.GetKeyArray(Allocator.Temp), typeof(UpdateElementColor));
            }

            return inputDeps;
        }
    }
}