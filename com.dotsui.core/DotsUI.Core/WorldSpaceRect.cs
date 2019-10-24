using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DotsUI.Core
{
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

    public static class WorldSpaceRectExtensions
    {
        public static bool ContainsPoint(this WorldSpaceRect rect, float2 point)
        {
            return (point.x >= rect.Min.x && point.x <= rect.Max.x) &&
                   (point.y >= rect.Min.y && point.y <= rect.Max.y);
        }

        public static float2 WorldSpaceToLocalPoint(this WorldSpaceRect rect, float2 worldSpacePoint)
        {
            return worldSpacePoint - rect.Min;
        }
        public static float2 WorldSpaceToNormalizedLocalPoint(this WorldSpaceRect rect, float2 worldSpacePoint)
        {
            return (worldSpacePoint - rect.Min)/rect.Size;
        }
    }
}