using System.Drawing;
using DotsUI.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace DotsUI.Input
{
    //[BurstCompile]
    internal struct UpdatePointerEvents : IJob
    {
        [ReadOnly] public Entity StateEntity;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<Entity> Hits;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<MouseInputFrameData> PointerFrameData;

        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<(NativeInputEventType, PointerButton)> PointerEvents;

        public NativeArray<MouseButtonState> ButtonStates;
        public EntityCommandBuffer Manager;
        public EntityArchetype EventArchetype;
        [ReadOnly] public ComponentDataFromEntity<PointerInputReceiver> ReceiverFromEntity;
        [ReadOnly] public ComponentDataFromEntity<UIParent> ParentFromEntity;
        public ComponentDataFromEntity<InputSystemState> StateFromEntity;
        [ReadOnly] public float DragThreshold;


        public void Execute()
        {
            NativeHashMap<Entity, DynamicBuffer<PointerInputBuffer>> targetToEvent =
                new NativeHashMap<Entity, DynamicBuffer<PointerInputBuffer>>(8, Allocator.Temp);
            var state = StateFromEntity[StateEntity];
            // Currently only mouse is fully supported. That's why I pick only the first hit entity (mouse) and skip touches
            Entity mouseHit = Hits[0];
            UpdateHover(mouseHit, ref targetToEvent, ref state);
            UpdateButtons(mouseHit, ref targetToEvent, ref state);
            UpdateDrag(mouseHit, ref targetToEvent, ref state);
            StateFromEntity[StateEntity] = state;
        }

        private void UpdateDrag(Entity mouseHit, ref NativeHashMap<Entity, DynamicBuffer<PointerInputBuffer>> targetToEvent, ref InputSystemState state)
        {
            for (int i = 0; i < ButtonStates.Length; i++)
            {
                var buttonState = ButtonStates[(int)i];
                if (buttonState.Pressed && buttonState.PressEntity != default)
                {
                    var dragDelta = PointerFrameData[0].Position - buttonState.PressPosition;
                    var dragLength = math.length(dragDelta);
                    if (!buttonState.IsDragging)
                    {
                        if (dragLength > DragThreshold)
                        {
                            buttonState.IsDragging = true;
                            CreateEvent(buttonState.PressEntity, PointerEventType.BeginDrag, true, (PointerButton) i,
                                ref targetToEvent);
                        }
                    }
                    else if(dragLength > 0.0f)
                    {
                        CreateEvent(buttonState.PressEntity, PointerEventType.Drag, true, (PointerButton)i,
                            ref targetToEvent);
                    }
                }
                ButtonStates[(int)i] = buttonState;
            }
        }

        private void UpdateButtons(Entity mouseHit,
            ref NativeHashMap<Entity, DynamicBuffer<PointerInputBuffer>> targetToEvent,
            ref InputSystemState state)
        {
            for (int i = 0; i < PointerEvents.Length; i++)
            {
                var (eventType, button) = PointerEvents[i];
                if (eventType == NativeInputEventType.PointerDown)
                {
                    HandleButtonDown(mouseHit, ref targetToEvent, ref state, button);
                }
                else if (eventType == NativeInputEventType.PointerUp)
                {
                    HandleButtonUp(mouseHit, ref targetToEvent, ref state, button);
                }
            }
        }

        private void HandleButtonUp(Entity mouseHit,
            ref NativeHashMap<Entity, DynamicBuffer<PointerInputBuffer>> targetToEvent,
            ref InputSystemState state, PointerButton button)
        {
            var buttonState = ButtonStates[(int)button];

            buttonState.Pressed = false;


            if (buttonState.PressEntity != default)
                CreateEvent(buttonState.PressEntity, PointerEventType.Up, true, button, ref targetToEvent);

            if (buttonState.IsDragging)
            {
                buttonState.IsDragging = false;
                CreateEvent(buttonState.PressEntity, PointerEventType.EndDrag, true, button, ref targetToEvent);
            }

            if (button == PointerButton.Left)
            {
                if (mouseHit == buttonState.PressEntity)
                {
                    CreateEvent(buttonState.PressEntity, PointerEventType.Click, true, button, ref targetToEvent);
                }
            }

            //buttonState.PressPosition = PointerFrameData[0].Position;
            buttonState.PressEntity = default;
            ButtonStates[(int)button] = buttonState;
        }

        private void HandleButtonDown(Entity mouseHit,
            ref NativeHashMap<Entity, DynamicBuffer<PointerInputBuffer>> targetToEvent,
            ref InputSystemState state, PointerButton button)
        {
            var buttonState = ButtonStates[(int)button];

            buttonState.Pressed = true;
            buttonState.PressPosition = PointerFrameData[0].Position;
            buttonState.PressEntity = mouseHit;
            if (button == PointerButton.Left)
            {
                if (mouseHit != state.SelectedEntity)
                {
                    //TODO: Selection event
                    if (state.SelectedEntity != default)
                        CreateEvent(state.SelectedEntity, PointerEventType.Deselected, true, PointerButton.Left, ref targetToEvent);
                    if (mouseHit != default)
                        CreateEvent(mouseHit, PointerEventType.Selected, true, PointerButton.Left, ref targetToEvent);
                    state.SelectedEntity = mouseHit;
                }
            }

            if (mouseHit != default)
                CreateEvent(mouseHit, PointerEventType.Down, true, button, ref targetToEvent);

            ButtonStates[(int)button] = buttonState;
        }

        private void UpdateHover(Entity mouseHit,
            ref NativeHashMap<Entity, DynamicBuffer<PointerInputBuffer>> targetToEvent,
            ref InputSystemState state)
        {
            if (state.HoveredEntity != mouseHit)
            {
                if (ReceiverFromEntity.Exists(state.HoveredEntity))
                    CreateEvent(state.HoveredEntity, PointerEventType.Exit, true,
                        PointerButton.Invalid, ref targetToEvent);
                if (mouseHit != default)
                    CreateEvent(mouseHit, PointerEventType.Enter, true, PointerButton.Invalid,
                        ref targetToEvent);
                state.HoveredEntity = mouseHit;
            }
        }

        private void CreateEvent(Entity target, PointerEventType type, bool propagateParent,
            PointerButton button,
            ref NativeHashMap<Entity, DynamicBuffer<PointerInputBuffer>> targetToEvent)
        {
            Debug.Log($"{target} {type}");
            if (ReceiverFromEntity.Exists(target) && (ReceiverFromEntity[target].ListenerTypes & type) == type)
            {
                if (!targetToEvent.TryGetValue(target, out var eventBuffer))
                {
                    var eventEntity = Manager.CreateEntity(EventArchetype);
                    var data = new PointerEvent()
                    {
                        Target = target,
                    };
                    Manager.SetComponent(eventEntity, data);
                    eventBuffer = Manager.SetBuffer<PointerInputBuffer>(eventEntity);
                    targetToEvent.TryAdd(target, eventBuffer);
                }

                PointerEventData eventData = new PointerEventData()
                {
                    Button = button,
                    ClickCount = -1,
                    ClickTime = 0.0f,
                    Delta = PointerFrameData[0].Delta,
                    PointerId = -1,
                    Position = PointerFrameData[0].Position,
                    ScrollDelta = float2.zero,
                    UseDragThreshold = false,
                };
                if (button != PointerButton.Invalid)
                {
                    eventData.IsDragging = ButtonStates[(int) button].IsDragging;
                    eventData.PressPosition = ButtonStates[(int) button].PressPosition;
                    eventData.PressEntity = ButtonStates[(int) button].PressEntity;
                }
                eventBuffer.Add(new PointerInputBuffer()
                {
                    EventType = type,
                    EventData = eventData
                });
            }

            if (!propagateParent)
                return;
            Entity parent = GetParent(target);
            while (parent != default)
            {
                if (!ReceiverFromEntity.Exists(parent))
                {
                    parent = GetParent(parent);
                }
                else
                {
                    if (IsConsumableType(type))
                    {
                        var receiver = ReceiverFromEntity[parent];
                        if ((receiver.ListenerTypes & type) == type)
                            propagateParent = false;
                    }

                    CreateEvent(parent, type, propagateParent, button, ref targetToEvent);
                    break;
                }
            }
        }

        private bool IsConsumableType(PointerEventType type)
        {
            switch (type)
            {
                case PointerEventType.Down:
                case PointerEventType.Up:
                case PointerEventType.Click:
                case PointerEventType.Selected:
                case PointerEventType.Deselected:
                    return true;
                default:
                    return false;
            }
        }

        private Entity GetParent(Entity target)
        {
            if (ParentFromEntity.Exists(target))
                return ParentFromEntity[target].Value;
            return default;
        }
    }
}