using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsUI.Input
{
    [BurstCompile]
    struct ClearNativeBuffers : IJob
    {
        [WriteOnly]
        public NativeList<NativePointerButtonEvent> PointerEventBuffer;
        [WriteOnly]
        public NativeList<NativeKeyboardInputEvent> KeyboardEventBuffer;

        public void Execute()
        {
            PointerEventBuffer.Clear();
            KeyboardEventBuffer.Clear();
        }
    }
    [BurstCompile]
    internal struct UpdatePointerEvents : IJob
    {
        [ReadOnly] public Entity StateEntity;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<Entity> Hits;
        [ReadOnly] public NativeArray<MouseInputFrameData> PointerFrameData;

        [ReadOnly]
        public NativeArray<NativePointerButtonEvent> PointerEvents;

        public NativeArray<MouseButtonState> ButtonStates;
        [ReadOnly] public ComponentDataFromEntity<PointerInputReceiver> ReceiverFromEntity;
        [ReadOnly] public ComponentDataFromEntity<Parent> ParentFromEntity;
        public ComponentDataFromEntity<InputSystemState> StateFromEntity;
        [ReadOnly] public float DragThreshold;

        public NativeMultiHashMap<Entity, PointerInputBuffer> TargetToEvent;
        public NativeList<Entity> PointerEventList;

        // used to preserve order in NativeMultiHashMap
        private int m_EventIdCounter;

        public void Execute()
        {
            m_EventIdCounter = 0;
            TargetToEvent.Clear();
            PointerEventList.Clear();
            var state = StateFromEntity[StateEntity];
            // Currently only mouse is fully supported. That's why I pick only the first hit entity (mouse) and skip touches
            Entity mouseHit = Hits[0];
            UpdateHover(mouseHit, ref state);
            UpdateButtons(mouseHit, ref state);
            UpdateDrag(mouseHit, ref state);
            StateFromEntity[StateEntity] = state;
            SaveTargetList();
        }

        private void SaveTargetList()
        {
            // NativeMultiHashMap duplicates keys. We need unique key list
            // TODO: Need more efficient implementation
            var tempKeyArray = TargetToEvent.GetKeyArray(Allocator.Temp);
            var uniqueMap = new NativeHashMap<Entity, int>(tempKeyArray.Length, Allocator.Temp);
            for (int i = 0; i < tempKeyArray.Length; i++)
            {
                uniqueMap.TryAdd(tempKeyArray[i], 0);
            }
            PointerEventList.AddRange(uniqueMap.GetKeyArray(Allocator.Temp));
            tempKeyArray.Dispose();
            uniqueMap.Dispose();
        }

        private void UpdateDrag(Entity mouseHit, ref InputSystemState state)
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
                            CreateEvent(buttonState.PressEntity, PointerEventType.BeginDrag, true, (PointerButton)i);
                        }
                    }
                    else if (dragLength > 0.0f)
                    {
                        CreateEvent(buttonState.PressEntity, PointerEventType.Drag, true, (PointerButton)i);
                    }
                }
                ButtonStates[(int)i] = buttonState;
            }
        }

        private void UpdateButtons(Entity mouseHit, ref InputSystemState state)
        {
            for (int i = 0; i < PointerEvents.Length; i++)
            {
                var eventType = PointerEvents[i].EventType;
                var button = PointerEvents[i].Button;
                if (eventType == NativeInputEventType.PointerDown)
                {
                    HandleButtonDown(mouseHit, ref state, button);
                }
                else if (eventType == NativeInputEventType.PointerUp)
                {
                    HandleButtonUp(mouseHit, ref state, button);
                }
            }
        }

        private void HandleButtonUp(Entity mouseHit, ref InputSystemState state, PointerButton button)
        {
            var buttonState = ButtonStates[(int)button];

            buttonState.Pressed = false;


            if (buttonState.PressEntity != default)
                CreateEvent(buttonState.PressEntity, PointerEventType.Up, true, button);

            if (buttonState.IsDragging)
            {
                CreateEvent(buttonState.PressEntity, PointerEventType.EndDrag, true, button);
                if (mouseHit != buttonState.PressEntity)
                {
                    if (mouseHit != default)
                        CreateEvent(mouseHit, PointerEventType.Drop, true, button);
                }
            }

            if (button == PointerButton.Left)
            {
                if (mouseHit == buttonState.PressEntity)
                {
                    CreateEvent(buttonState.PressEntity, PointerEventType.Click, true, button);
                }
            }

            buttonState.IsDragging = false;
            buttonState.PressEntity = default;
            ButtonStates[(int)button] = buttonState;
        }

        private void HandleButtonDown(Entity mouseHit, ref InputSystemState state, PointerButton button)
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
                        CreateEvent(state.SelectedEntity, PointerEventType.Deselected, true, PointerButton.Left);
                    if (mouseHit != default)
                        CreateEvent(mouseHit, PointerEventType.Selected, true, PointerButton.Left);
                    state.SelectedEntity = mouseHit;
                }
            }

            if (mouseHit != default)
                CreateEvent(mouseHit, PointerEventType.Down, true, button);

            ButtonStates[(int)button] = buttonState;
        }

        private void UpdateHover(Entity mouseHit, ref InputSystemState state)
        {
            if (state.HoveredEntity != mouseHit)
            {
                if (ReceiverFromEntity.Exists(state.HoveredEntity))
                    CreateEvent(state.HoveredEntity, PointerEventType.Exit, true,
                        PointerButton.Invalid);
                if (mouseHit != default)
                    CreateEvent(mouseHit, PointerEventType.Enter, true, PointerButton.Invalid);
                state.HoveredEntity = mouseHit;
            }
        }

        private void CreateEvent(Entity target, PointerEventType type, bool propagateParent, PointerButton button)
        {
            if (ReceiverFromEntity.Exists(target) && (ReceiverFromEntity[target].ListenerTypes & type) == type)
            {
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
                    eventData.IsDragging = ButtonStates[(int)button].IsDragging;
                    eventData.PressPosition = ButtonStates[(int)button].PressPosition;
                    eventData.PressEntity = ButtonStates[(int)button].PressEntity;
                }
                TargetToEvent.Add(target, new PointerInputBuffer()
                {
                    EventId = m_EventIdCounter++,
                    EventType = type,
                    EventData = eventData
                });
            }

            if (!propagateParent || IsConsumableType(type))
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

                    CreateEvent(parent, type, propagateParent, button);
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
                case PointerEventType.BeginDrag:
                case PointerEventType.EndDrag:
                case PointerEventType.Drag:
                case PointerEventType.Drop:
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