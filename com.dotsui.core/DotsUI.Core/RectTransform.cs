using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace DotsUI.Core
{
    public struct RectTransform : IComponentData
    {
        public float2 AnchorMin;
        public float2 AnchorMax;
        public float2 Position;
        public float2 SizeDelta;
        public float2 Pivot;
    }

    /// <summary>
    /// This component is set by canvas scaler. Do not modify it manually
    /// </summary>
    public struct ElementScale : IComponentData
    {
        public float2 Value;
    }

    public struct WorldSpaceMask : IComponentData
    {
        public float2 Min;
        public float2 Max;
    }

    /// <summary>
    /// Updated by RectTransformSystem. Consider it read-only
    /// </summary>
    public struct WorldSpaceRect : IComponentData
    {
        public float2 Min;
        public float2 Max;

        public float Width
        {
            get { return Max.x - Min.x; }
        }
        public float Height
        {
            get { return Max.y - Min.y; }
        }
        public float2 Center
        {
            get { return (Min + Max) * 0.5f; }
        }

        public float2 TopLeft
        {
            get { return new float2(Min.x, Max.y); }
        }

        public float2 TopCenter
        {
            get { return new float2(Center.x, Max.y); }
        }

        public float2 TopRight
        {
            get { return new float2(Max.x, Max.y); }
        }
        
        
        public float2 BottomLeft
        {
            get { return new float2(Min.x, Min.y); }
        }

        public float2 BottomCenter
        {
            get { return new float2(Center.x, Min.y); }
        }

        public float2 BottomRight
        {
            get { return new float2(Max.x, Min.y); }
        }

        public float2 Size
        {
            get
            {
                return new float2(Max-Min);
            }
        }

        [Obsolete]
        public float CalculateWidth()
        {
            return Max.x - Min.x;
        }

        [Obsolete]
        public float CalculateHeight()
        {
            return Max.y - Min.y;
        }

        public Rect CreateRect()
        {
            return new Rect(Min, Max - Min);
        }
    }

    [Obsolete("Parent removed. Use Unity.Transforms.Parent instead", true)]
    public struct UIParent : IComponentData
    {
        public Entity Value;
    }

    [Obsolete("UIPreviousParent removed. Use Unity.Transforms.PreviousParent instead", true)]
    public struct UIPreviousParent : ISystemStateComponentData
    {
        public Entity Value;
    }

    [InternalBufferCapacity(8)]
    [Obsolete("UIChild removed. Use Unity.Transforms.Child instead", true)]
    public struct UIChild : ISystemStateBufferElementData
    {
        public Entity Value;
    }

    /// <summary>
    /// Used as color multiplier for VertexColorValue. Useful for selectable transition effects
    /// </summary>
    public struct VertexColorMultiplier : IComponentData
    {
        public float4 Value;
    }

    /// <summary>
    /// Actual element color. Equivalent of Graphics.Color
    /// </summary>
    public struct VertexColorValue : IComponentData
    {
        public float4 Value;
    }

    public static class ColorExtensions
    {
        public static float4 ToFloat4(this Color color)
        {
            return new float4(color.r, color.g, color.b, color.a);
        }

        public static float4 ToFloat4(this Color32 color)
        {
            return new float4(color.r, color.g, color.b, color.a) / 255.0f;
        }
    }
}