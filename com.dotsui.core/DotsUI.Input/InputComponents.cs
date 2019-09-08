using System;
using Unity.Entities;

namespace DotsUI.Input
{
    [Flags]
    public enum PointerEventType
    {
        /// <summary>
        /// Represents raycast target without event consumption. In example text inside a button
        /// </summary>
        None = 0,
        /// <summary>
        /// Finger touch began or mouse button pressed over this entity
        /// </summary>
        Down = 1,
        /// <summary>
        /// Finger touch ended or mouse button released over this entity
        /// </summary>
        Up = 2,
        /// <summary>
        /// Click or click
        /// </summary>
        Click = 4,
        Enter = 8,
        Exit = 16,
        BeginDrag = 32,
        Drag = 64,
        EndDrag = 128,
        Drop = 256,
        Selected = 512,
        Deselected = 1024,
        
        SelectableGroup = Down | Up | Click | Enter | Exit | Selected | Deselected,
    }
    public struct OnLostFocusEvent : IComponentData
    {

    }

    public struct OnFocusEvent : IComponentData
    {

    }
    public struct PointerInputReceiver : IComponentData
    {
        public PointerEventType ListenerTypes;
    }

    public struct KeyboardInputReceiver : IComponentData
    {

    }


    public struct PointerEvent : IComponentData
    {
        public Entity Target;
    }
    public struct KeyboardEvent : IComponentData
    {
        public Entity Target;
    }

    

    public enum KeyboardEventType
    {
        Character,
        Key
    }

    public struct PointerInputBuffer : IBufferElementData
    {
        public int EventId;
        public PointerEventType EventType;
        public PointerEventData EventData;
    }

    public struct KeyboardInputBuffer : IBufferElementData
    {
        public KeyboardEventType EventType;
        public ushort KeyCode;
        public ushort Character;
    }
}
