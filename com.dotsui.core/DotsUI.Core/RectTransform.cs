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

    public struct ElementCanvasReference : ISharedComponentData
    {
        public Entity Canvas;

        public override int GetHashCode()
        {
            return Canvas.Index;
        }
    }

    public struct ElementHierarchyIndex : IComponentData
    {
        public int Value;
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