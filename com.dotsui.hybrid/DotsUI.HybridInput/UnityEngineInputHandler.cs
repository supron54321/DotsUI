using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotsUI.Core;
using UnityEngine;
using DotsUI.Input;
using Unity.Entities;

namespace DotsUI.Hybrid
{
    [UpdateInGroup(typeof(InputSystemGroup))]
    [UpdateBefore(typeof(ControlsInputSystem))]
    class UnityEngineInputHandler : ComponentSystem
    {
        private readonly Event m_Events = new Event();

        protected override void OnUpdate()
        {
            var pointerContainerEntity = GetSingletonEntity<NativePointerInputContainer>();
            var keyboardContainerEntity = GetSingletonEntity<NativeKeyboardInputContainer>();

            var pointerBuffer = EntityManager.GetBuffer<NativePointerButtonEvent>(pointerContainerEntity);
            var keyboardBuffer = EntityManager.GetBuffer<NativeKeyboardInputEvent>(keyboardContainerEntity);

            GetSingletonEntity<NativeKeyboardInputContainer>();
            for (int i = 0; i < 3; i++)
            {
                if (UnityEngine.Input.GetMouseButtonDown(i))
                    pointerBuffer.Add(new NativePointerButtonEvent()
                    {
                        EventType = NativeInputEventType.PointerDown,
                        Button = (PointerButton)i
                    });
                if (UnityEngine.Input.GetMouseButtonUp(i))
                    pointerBuffer.Add(new NativePointerButtonEvent()
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
