using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;

namespace DotsUI.Input
{
    /// <summary>
    /// This struct encapsulates internal <see cref="ControlsInputSystem"/> buffers.
    /// These buffers are managed by <see cref="ControlsInputSystem"/> system, so we hide them behind safe wrapper.
    /// Buffers are marked as [WriteOnly]. It's safe to use this wrapper in job.
    /// </summary>
    public readonly struct NativeEventWriter
    {
        [WriteOnly]
        private readonly NativeList<NativePointerButtonEvent> m_PointerEventBuffer;
        [WriteOnly]
        private readonly NativeList<NativeKeyboardInputEvent> m_KeyboardEventBuffer;

        internal NativeEventWriter(NativeList<NativePointerButtonEvent> pointerEventBuffer,
            NativeList<NativeKeyboardInputEvent> keyboardEventBuffer)
        {
            m_PointerEventBuffer = pointerEventBuffer;
            m_KeyboardEventBuffer = keyboardEventBuffer;
        }
        
        /// <summary>
        /// Add pointer event to the queue.
        /// </summary>
        /// <param name="pointerEvent"></param>
        public void AddPointerEvent(NativePointerButtonEvent pointerEvent)
        {
            m_PointerEventBuffer.Add(pointerEvent);
        }

        /// <summary>
        /// Add keyboard event to the queue.
        /// </summary>
        /// <param name="pointerEvent"></param>
        public void AddKeyboardEvent(NativeKeyboardInputEvent keyboardEvent)
        {
            m_KeyboardEventBuffer.Add(keyboardEvent);
        }
    }
}
