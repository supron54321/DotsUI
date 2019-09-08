using TMPro;
using Unity.Entities;
using Unity.Collections.LowLevel.Unsafe;

namespace DotsUI.Core
{
    public struct TextRenderer : IComponentData
    {
        public float Size;
        public TextAlignmentOptions Alignment;
        public bool Bold;
        public bool Italic;
        public Entity Font;
    }

    public struct TextData : IBufferElementData
    {
        public ushort Value;

        public static unsafe void Set(DynamicBuffer<TextData> textBuffer, string value)
        {
            textBuffer.ResizeUninitialized(value.Length);
            fixed (char* str = value)
            {
                UnsafeUtility.MemCpy(textBuffer.GetUnsafePtr(), str, value.Length * 2);
            }
        }
    }
}
