using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Sprites;
using static Unity.Mathematics.math;

namespace DotsUI.Core
{
    internal struct RectMaskCut
    {
        public float2 Min;
        public float2 Max;
        public float2 UvMin;
        public float2 UvMax;

        public float2 Size
        {
            get
            {
                return Max - Min;
            }
        }
    }

    internal struct SpriteUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SpriteVertexData GetSpriteVertexData(Sprite sprite)
        {
            if (sprite != null)
            {
                return new SpriteVertexData
                {
                    Outer = DataUtility.GetOuterUV(sprite),
                    Inner = DataUtility.GetInnerUV(sprite),
                    Padding = DataUtility.GetPadding(sprite),
                    Border = sprite.border,
                    PixelsPerUnit = sprite.pixelsPerUnit
                };
            }

            return new SpriteVertexData
            {
                Outer = float4(0.0f),
                Inner = float4(0.0f),
                Padding = float4(0.0f),
                Border = float4(0.0f),
                PixelsPerUnit = 100
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void PopulateSpriteVertices(ref WorldSpaceMask rectMask, ref DynamicBuffer<ControlVertexData> vertices,
            ref DynamicBuffer<ControlVertexIndex> triangles, ref WorldSpaceRect rectData,
            ref SpriteVertexData spriteVertexData, float4 color)
        {
            float pixelsPerUnit = spriteVertexData.PixelsPerUnit / 100.0f;
            Rect rect = new Rect(rectData.Min, rectData.Max - rectData.Min);
            float4 adjustedBorders = GetAdjustedBorders(spriteVertexData.Border / pixelsPerUnit, rect);
            var padding = spriteVertexData.Padding / pixelsPerUnit;

            float2* vertScratch = stackalloc float2[4];
            float2* uvScratch = stackalloc float2[4];

            vertScratch[0] = new float2(padding.x, padding.y);
            vertScratch[3] = new float2(rect.width - padding.z, rect.height - padding.w);

            vertScratch[1].x = adjustedBorders.x;
            vertScratch[1].y = adjustedBorders.y;

            vertScratch[2].x = rect.width - adjustedBorders.z;
            vertScratch[2].y = rect.height - adjustedBorders.w;

            for (int i = 0; i < 4; ++i)
            {
                vertScratch[i].x += rect.x;
                vertScratch[i].y += rect.y;
            }

            uvScratch[0] = new Vector2(spriteVertexData.Outer.x, spriteVertexData.Outer.y);
            uvScratch[1] = new Vector2(spriteVertexData.Inner.x, spriteVertexData.Inner.y);
            uvScratch[2] = new Vector2(spriteVertexData.Inner.z, spriteVertexData.Inner.w);
            uvScratch[3] = new Vector2(spriteVertexData.Outer.z, spriteVertexData.Outer.w);

            // rect mask support
            var cut = GetRectangleMaskCut(vertScratch[0], vertScratch[3], ref rectMask);
            vertScratch[0] = vertScratch[0] + cut.Min - clamp(cut.Max-float2(rect.size), 0.0f, float.PositiveInfinity);
            vertScratch[1] = vertScratch[1] + math.clamp(cut.Min - adjustedBorders.xy, 0.0f, float.PositiveInfinity) - clamp(cut.Max - (float2(rect.size) - adjustedBorders.xy), 0.0f, float.PositiveInfinity);
            vertScratch[2] = vertScratch[2] + math.clamp(cut.Min - (float2(rect.size)-adjustedBorders.zw), 0.0f, float.PositiveInfinity) - math.clamp(cut.Max - adjustedBorders.wz, 0.0f, float.PositiveInfinity);
            vertScratch[3] = vertScratch[3] + clamp(cut.Min - float2(rect.size), 0.0f, float.PositiveInfinity) - cut.Max;


            int vertexOffset = vertices.Length;

            for (int x = 0; x < 4; ++x)
            {
                for (int y = 0; y < 4; ++y)
                {
                    vertices.Add(new ControlVertexData()
                    {
                        Position = new float3(vertScratch[x].x, vertScratch[y].y, 0.0f),
                        TexCoord0 = new float2(uvScratch[x].x, uvScratch[y].y),
                        Color = color
                    });
                }
            }


            for (int x = 0; x < 3; ++x)
            {
                int x2 = x + 1;

                for (int y = 0; y < 3; ++y)
                {
                    int y2 = y + 1;

                    // Ignore empty
                    if (vertScratch[x2].x - vertScratch[x].x <= 0.0f)
                        continue;
                    if (vertScratch[y2].y - vertScratch[y].y <= 0.0f)
                        continue;

                    triangles.Add(vertexOffset + x * 4 + y);
                    triangles.Add(vertexOffset + x * 4 + y2);
                    triangles.Add(vertexOffset + x2 * 4 + y);

                    triangles.Add(vertexOffset + x2 * 4 + y2);
                    triangles.Add(vertexOffset + x2 * 4 + y);
                    triangles.Add(vertexOffset + x * 4 + y2);
                }
            }


            //            for (int x = 0; x < 3; ++x)
            //            {
            //                int x2 = x + 1;
            //
            //                for (int y = 0; y < 3; ++y)
            //                {
            //                    int y2 = y + 1;
            //
            //
            //                    AddQuad(ref rectMask, ref vertices, ref triangles,
            //                        new float2(vertScratch[x].x, vertScratch[y].y),
            //                        new float2(vertScratch[x2].x, vertScratch[y2].y),
            //                        color,
            //                        new float2(uvScratch[x].x, uvScratch[y].y),
            //                        new float2(uvScratch[x2].x, uvScratch[y2].y));
            //                }
            //            }

        }

        private static float4 GetAdjustedBorders(float4 border, Rect adjustedRect)
        {
            Rect originalRect = adjustedRect;

            for (int axis = 0; axis <= 1; axis++)
            {
                float borderScaleRatio;

                if (originalRect.size[axis] != 0)
                {
                    borderScaleRatio = adjustedRect.size[axis] / originalRect.size[axis];
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }

                float combinedBorders = border[axis] + border[axis + 2];
                if (adjustedRect.size[axis] < combinedBorders && combinedBorders != 0)
                {
                    borderScaleRatio = adjustedRect.size[axis] / combinedBorders;
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }
            }

            return border;
        }

        private static void AddQuad(ref WorldSpaceMask rectMask, ref DynamicBuffer<MeshVertex> vertices,
            ref DynamicBuffer<MeshVertexIndex> triangles, float2 posMin, float2 posMax, float4 color, float2 uvMin,
            float2 uvMax)
        {
            RectMaskCut cut = SpriteUtility.GetRectangleMaskCut(posMin, posMax, ref rectMask);
            var cutSize = cut.Max + cut.Min;
            var rectSize = posMax - posMin;
            if (cutSize.x > rectSize.x || cutSize.y > rectSize.y)
                return;

            int startIndex = vertices.Length;

            vertices.Add(new MeshVertex()
            {
                Position = new float3(new float2(posMin) + cut.Min, 0.0f),
                TexCoord0 = new float2(uvMin) + cut.UvMin,
                Color = color
            });
            vertices.Add(new MeshVertex()
            {
                Position = new float3(new float2(posMin.x + cut.Min.x, posMax.y - cut.Max.y), 0.0f),
                TexCoord0 = new float2(uvMin.x + cut.UvMin.x, uvMax.y - cut.UvMax.y),
                Color = color
            });
            vertices.Add(new MeshVertex()
            {
                Position = new float3(new float2(posMax.x, posMax.y) - cut.Max, 0.0f),
                TexCoord0 = new float2(uvMax.x, uvMax.y) - cut.UvMax,
                Color = color
            });
            vertices.Add(new MeshVertex()
            {
                Position = new float3(new float2(posMax.x - cut.Max.x, posMin.y + cut.Min.y), 0.0f),
                TexCoord0 = new float2(uvMax.x - cut.UvMax.x, uvMin.y + cut.UvMin.y),
                Color = color
            });

            triangles.Add(new MeshVertexIndex {Value = startIndex});
            triangles.Add(new MeshVertexIndex {Value = startIndex + 1});
            triangles.Add(new MeshVertexIndex {Value = startIndex + 2});
            triangles.Add(new MeshVertexIndex {Value = startIndex + 2});
            triangles.Add(new MeshVertexIndex {Value = startIndex + 3});
            triangles.Add(new MeshVertexIndex {Value = startIndex});
        }

        public static RectMaskCut GetRectangleMaskCut(float2 posMin, float2 posMax, ref WorldSpaceMask rectMask)
        {
            float2 min, max;
            min.x = math.clamp(rectMask.Min.x - posMin.x, 0.0f, float.PositiveInfinity);
            min.y = math.clamp(rectMask.Min.y - posMin.y, 0.0f, float.PositiveInfinity);
            max.x = math.clamp(posMax.x - rectMask.Max.x, 0.0f, float.PositiveInfinity);
            max.y = math.clamp(posMax.y - rectMask.Max.y, 0.0f, float.PositiveInfinity);

            var rectSize = posMax - posMin;
            return new RectMaskCut()
            {
                Min = min,
                Max = max,
                UvMin = min / rectSize,
                UvMax = max / rectSize,
            };
        }
        public static RectMaskCut GetRectangleMaskCut(float2 posMin, float2 posMax, float2 uvMin, float2 uvMax, ref WorldSpaceMask rectMask)
        {
            float2 min, max;
            min.x = math.clamp(rectMask.Min.x - posMin.x, 0.0f, float.PositiveInfinity);
            min.y = math.clamp(rectMask.Min.y - posMin.y, 0.0f, float.PositiveInfinity);
            max.x = math.clamp(posMax.x - rectMask.Max.x, 0.0f, float.PositiveInfinity);
            max.y = math.clamp(posMax.y - rectMask.Max.y, 0.0f, float.PositiveInfinity);

            var rectSize = posMax - posMin;
            var uvSize = uvMax - uvMin;
            return new RectMaskCut()
            {
                Min = min,
                Max = max,
                UvMin = min * uvSize / rectSize,
                UvMax = max * uvSize / rectSize,
            };
        }
    }
}