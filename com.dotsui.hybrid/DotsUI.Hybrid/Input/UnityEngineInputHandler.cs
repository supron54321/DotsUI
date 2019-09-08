using DotsUI.Core;
using UnityEngine;
using DotsUI.Input;
using Unity.Entities;
using Unity.Mathematics;

namespace DotsUI.Hybrid
{
    [UpdateInGroup(typeof(InputSystemGroup))]
    [UpdateBefore(typeof(ControlsInputSystem))]
    class UnityEngineInputHandler : ComponentSystem
    {
        private readonly Event m_Events = new Event();
        private float2 m_LastFrameMousePos;

        protected override void OnCreate()
        {
            m_LastFrameMousePos = (Vector2) UnityEngine.Input.mousePosition;
        }

        protected override void OnUpdate()
        {
            var pointerContainerEntity = GetSingletonEntity<NativePointerInputContainer>();
            var keyboardContainerEntity = GetSingletonEntity<NativeKeyboardInputContainer>();

            var pointerButtonBuffer = EntityManager.GetBuffer<NativePointerButtonEvent>(pointerContainerEntity);
            var pointerStateBuffer = EntityManager.GetBuffer<NativePointerState>(pointerContainerEntity);
            var keyboardBuffer = EntityManager.GetBuffer<NativeKeyboardInputEvent>(keyboardContainerEntity);

            int currentPointerOffset = 0;
            if (UnityEngine.Input.mousePresent)
            {
                pointerStateBuffer.ResizeUninitialized(1 + UnityEngine.Input.touchCount);
                float2 mousePos = (Vector2)UnityEngine.Input.mousePosition;
                pointerStateBuffer[0] = new NativePointerState()
                {
                    PointerId = NativePointerId.Mouse,
                    Position = mousePos,
                    Delta = mousePos-m_LastFrameMousePos
                };
                m_LastFrameMousePos = mousePos;
                currentPointerOffset++;
            }
            else
            {
                pointerStateBuffer.ResizeUninitialized(UnityEngine.Input.touchCount);
            }

            for (int i = 0; i < UnityEngine.Input.touchCount; i++)
            {
                var touch = UnityEngine.Input.GetTouch(i);
                pointerStateBuffer[i+currentPointerOffset] = new NativePointerState()
                {
                    PointerId = (NativePointerId)(1 + (touch.fingerId)),
                    Position = touch.position,
                    Delta = touch.deltaPosition,
                };
            }


            for (int i = 0; i < 3; i++)
            {
                if (UnityEngine.Input.GetMouseButtonDown(i))
                    pointerButtonBuffer.Add(new NativePointerButtonEvent()
                    {
                        EventType = NativeInputEventType.PointerDown,
                        Button = (PointerButton)i
                    });
                if (UnityEngine.Input.GetMouseButtonUp(i))
                    pointerButtonBuffer.Add(new NativePointerButtonEvent()
                    {
                        EventType = NativeInputEventType.PointerUp,
                        Button = (PointerButton)i
                    });
            }
            // I had a problem with this loop, so I put this max loop count to avoid deadlocks. I'll investigate this later. Probably misunderstood documentation :)
            for (int i = 0; i < 100 && Event.PopEvent(m_Events); i++)
            {
                if (m_Events.type == EventType.KeyDown)
                {
                    keyboardBuffer.Add(new NativeKeyboardInputEvent()
                    {
                        EventType = NativeInputEventType.KeyDown,
                        Character = m_Events.character,
                        KbdEvent = m_Events.character != 0 ? KeyboardEventType.Character : KeyboardEventType.Key,
                        KeyCode = (ushort)m_Events.keyCode
                    });
                }
                if (m_Events.type == EventType.KeyUp)
                {
                    keyboardBuffer.Add(new NativeKeyboardInputEvent()
                    {
                        EventType = NativeInputEventType.KeyUp,
                        Character = m_Events.character,
                        KbdEvent = m_Events.character != 0 ? KeyboardEventType.Character : KeyboardEventType.Key,
                        KeyCode = (ushort)m_Events.keyCode
                    });
                }
            }

        }
    }
}
