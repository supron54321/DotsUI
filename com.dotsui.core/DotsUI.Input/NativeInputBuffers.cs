using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace DotsUI.Input
{
    public enum NativeInputEventType
    {
        PointerDown,
        PointerUp,
        KeyDown,
        KeyUp,
    }
    public struct NativePointerInputContainer : IComponentData
    {
    }
    public struct NativeKeyboardInputContainer : IComponentData
    {
    }

    public struct NativePointerButtonEvent : IBufferElementData
    {
        public NativeInputEventType EventType;
        public PointerButton Button;
    }

    /// <summary>
    /// TODO: pointer position support
    /// </summary>
    public struct NativePointerState : IBufferElementData
    {
        public int PointerId;
        public Vector2 Position;
        public Vector2 Delta;
    }

    public struct NativeKeyboardInputEvent : IBufferElementData
    {
        public NativeInputEventType EventType;
        public KeyboardEventType KbdEvent;
        public ushort KeyCode;
        public ushort Character;
    }
}
