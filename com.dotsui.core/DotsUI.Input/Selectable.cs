using System;
using Unity.Mathematics;
using Unity.Entities;

namespace DotsUI.Input
{
    [Flags]
    public enum SelectableState
    {
        Hovered = 1,
        Pressed = 2,
        Selected = 4,
    }
    public struct SelectableColor : IComponentData
    {
        public Entity Target;
        public float4 Normal;
        public float4 Hover;
        public float4 Pressed;
        public float4 Selected;
        public float4 Disabled;
        public float TransitionTime;
    }

    public struct Selectable : IComponentData
    {
        public SelectableState Value;
    }
    
    public struct SelectableColorMultiTarget : IBufferElementData
    {
        public Entity Target;
        public float4 Normal;
        public float4 Hover;
        public float4 Pressed;
        public float4 Selected;
        public float4 Disabled;
        public float TransitionTime;
    }
}