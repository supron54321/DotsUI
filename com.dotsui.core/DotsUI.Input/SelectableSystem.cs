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
    [UpdateAfter(typeof(ControlsInputSystemGroup))]
    public class SelectableSystem : JobComponentSystem
    {
        private EntityQuery m_SelectableGroup;

        private InputHandleBarrier m_Barrier;
        private InputEventQuery m_EventQuery;

        protected override void OnCreate()
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
            m_EventQuery = InputEventQuery.Create<Selectable>(EntityManager);
            RequireForUpdate(m_SelectableGroup);
        }

        protected override void OnDestroy()
        {
        }

        [BurstCompile]
        struct SetColorValueJob : IJobChunk
        {
            [ReadOnly] public ComponentDataFromEntity<SelectableColor> SelectableColorType;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<Selectable> SelectableType;

            // This is probably not the best idea to disable this restriction, since different selecatables can point to the same target
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<VertexColorMultiplier> ColorMultiplierFromEntity;
            public AddFlagComponentCommandBuffer.ParallelWriter ToUpdate;
            public PointerInputEventReader EventReader;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {

                for (int i = 0; i < EventReader.EntityCount; i++)
                {
                    var entity = EventReader[i];
                    var selectableColor = SelectableColorType[entity];
                    var selectableState = SelectableType[entity];

                    selectableState = ParseEntityEvents(entity, selectableState);

                    var target = selectableColor.Target;
                    if (ColorMultiplierFromEntity.Exists(target))
                    {
                        var currentColor = ColorMultiplierFromEntity[target].Value;
                        float4 newColor = currentColor;
                        if ((selectableState.Value & SelectableState.Pressed) == SelectableState.Pressed)
                        {
                            newColor = selectableColor.Pressed;
                        }
                        else if ((selectableState.Value & SelectableState.Selected) == SelectableState.Selected)
                        {
                            newColor = selectableColor.Selected;
                        }
                        else if ((selectableState.Value & SelectableState.Hovered) == SelectableState.Hovered)
                        {
                            newColor = selectableColor.Hover;
                        }
                        else
                        {
                            newColor = selectableColor.Normal;
                        }

                        if (!currentColor.Equals(newColor))
                        {
                            ColorMultiplierFromEntity[selectableColor.Target] = new VertexColorMultiplier()
                            {
                                Value = newColor
                            };
                        }

                        ToUpdate.TryAdd(target);
                    }

                    SelectableType[entity] = selectableState;
                }

            }

            private Selectable ParseEntityEvents(Entity entity, Selectable selectableState)
            {
                EventReader.GetFirstEvent(entity, out var pointerEvent, out var it);
                selectableState = ParseEvent(pointerEvent, selectableState);
                while (EventReader.TryGetNextEvent(out pointerEvent, ref it))
                    selectableState = ParseEvent(pointerEvent, selectableState);
                return selectableState;
            }

            private static Selectable ParseEvent(PointerInputBuffer pointerEvent, Selectable selectableState)
            {
                if (pointerEvent.EventType == PointerEventType.Down && pointerEvent.EventData.Button == PointerButton.Left)
                    selectableState.Value |= SelectableState.Pressed;
                else if (pointerEvent.EventType == PointerEventType.Up && pointerEvent.EventData.Button == PointerButton.Left)
                    selectableState.Value &= (~SelectableState.Pressed);
                else if (pointerEvent.EventType == PointerEventType.Enter)
                    selectableState.Value |= SelectableState.Hovered;
                else if (pointerEvent.EventType == PointerEventType.Exit)
                    selectableState.Value &= (~SelectableState.Hovered);
                else if (pointerEvent.EventType == PointerEventType.Selected)
                    selectableState.Value |= SelectableState.Selected;
                else if (pointerEvent.EventType == PointerEventType.Deselected)
                    selectableState.Value &= (~SelectableState.Selected);
                return selectableState;
            }
        };
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var eventReader = m_EventQuery.CreateEventReader(Allocator.TempJob);
            if (eventReader.EntityCount == 0)
                return inputDeps;

            NativeHashMap<Entity, int> updateQueue = new NativeHashMap<Entity, int>(m_SelectableGroup.CalculateEntityCount(), Allocator.TempJob);
            SetColorValueJob setJob = new SetColorValueJob()
            {
                SelectableColorType = GetComponentDataFromEntity<SelectableColor>(true),
                SelectableType = GetComponentDataFromEntity<Selectable>(),
                ColorMultiplierFromEntity = GetComponentDataFromEntity<VertexColorMultiplier>(),
                EventReader = eventReader,
                ToUpdate = m_Barrier.CreateAddFlagComponentCommandBuffer<UpdateElementColor>().AsParallelWriter()
            };
            inputDeps = setJob.Schedule(m_SelectableGroup, inputDeps);
            m_Barrier.AddJobHandleForProducer(inputDeps);
            inputDeps = updateQueue.Dispose(inputDeps);
            inputDeps = eventReader.Dispose(inputDeps);
            return inputDeps;
        }
    }
}