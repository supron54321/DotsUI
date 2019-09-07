using Unity.Entities;
using Unity.Mathematics;

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

    public enum NativePointerId
    {
        Mouse = 0,
        TouchFinger0 = 1,
        TouchFinger1 = 2,
        TouchFinger2 = 3,
        TouchFinger3 = 4,
        TouchFinger4 = 5
    }

    public struct NativePointerState : IBufferElementData
    {
        public NativePointerId PointerId;
        public float2 Position;
        public float2 Delta;
    }

    public struct NativeKeyboardInputEvent : IBufferElementData
    {
        public NativeInputEventType EventType;
        public KeyboardEventType KbdEvent;
        public ushort KeyCode;
        public ushort Character;
    }
}
