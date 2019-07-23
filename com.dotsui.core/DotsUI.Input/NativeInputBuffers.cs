using System;
using System.Collections.Generic;
using System.Linq;
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

    public struct NativePointerInputBuffer : IBufferElementData
    {
        public NativeInputEventType EventType;
        public PointerButton Button;
    }
    public struct NativeKeyboardInputBuffer : IBufferElementData
    {
        public NativeInputEventType EventType;
        public KeyboardEventType KbdEvent;
        public ushort KeyCode;
        public ushort Character;
    }
}
