using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.TextCore;

namespace DotsUI.Core
{
    public struct GlyphInfo
    {
        public float Scale;
        public GlyphRect Rect;
        public GlyphMetrics Metrics;
    }

    public struct FontInfo
    {
        public float LineHeight;
        public float NormalSpace;
        public float BoldSpace;
        public float AscentLine;
        public float CapLine;
        public float MeanLine;
        public float Baseline;
        public float DescentLine;
        public float PointSize;
        public float BoldStyle;
        public float NormalStyle;
    }

    [UnityEngine.ExecuteAlways]
    [UpdateInGroup(typeof(ElementMeshUpdateSystemGroup))]
    public class TextMeshBuildSystem : JobComponentSystem
    {
        [BurstCompile(FloatMode = FloatMode.Fast)]
        private struct TextChunkBuilder : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkEntityType TextEntities;
            [ReadOnly] public ArchetypeChunkComponentType<WorldSpaceRect> WorldSpaceRectType;
            [ReadOnly] public ArchetypeChunkComponentType<VertexColorValue> ColorValueType;
            [ReadOnly] public ArchetypeChunkComponentType<VertexColorMultiplier> ColorMultiplierType;
            [ReadOnly] public ArchetypeChunkComponentType<TextRenderer> TextRendererType;
            [ReadOnly] public ArchetypeChunkComponentType<ElementScale> ElementScaleType;
            [ReadOnly] public ArchetypeChunkComponentType<WorldSpaceMask> WorldSpaceMaskType;
            public ArchetypeChunkComponentType<RebuildElementMeshFlag> RebuildElementMeshFlagArray;

            [ReadOnly] public ComponentDataFromEntity<TextFontAsset> FontAssetFromEntity;
            [ReadOnly] public BufferFromEntity<FontGlyphData> FontGlyphDataFromEntity;

            [NativeDisableContainerSafetyRestriction]
            public ArchetypeChunkBufferType<ControlVertexData> VertexDataType;
            [NativeDisableContainerSafetyRestriction]
            public ArchetypeChunkBufferType<ControlVertexIndex> IndexDataType;

            [ReadOnly]
            public ArchetypeChunkBufferType<TextData> TextBufferType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var rebuildMeshArray = chunk.GetNativeArray(RebuildElementMeshFlagArray);
                var vertexBufferAccessor = chunk.GetBufferAccessor(VertexDataType);
                var indexBufferAccessor = chunk.GetBufferAccessor(IndexDataType);
                var textBufferAccessor = chunk.GetBufferAccessor(TextBufferType);
                var worldSpaceRectArray = chunk.GetNativeArray(WorldSpaceRectType);
                var textRendererArray = chunk.GetNativeArray(TextRendererType);
                var vertexColorArray = chunk.GetNativeArray(ColorValueType);
                var vertexColorMultiplierArray = chunk.GetNativeArray(ColorMultiplierType);
                var elementScaleArray = chunk.GetNativeArray(ElementScaleType);
                var worldSpaceMaskArray = chunk.GetNativeArray(WorldSpaceMaskType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    if (rebuildMeshArray[i].Rebuild)
                    {
                        var vertices = vertexBufferAccessor[i];
                        var triangles = indexBufferAccessor[i];
                        var textBuffer = textBufferAccessor[i];
                        var rect = worldSpaceRectArray[i];
                        var fontSettings = textRendererArray[i];
                        var elementScale = elementScaleArray[i];
                        var mask = worldSpaceMaskArray[i];

                        vertices.Clear();
                        triangles.Clear();

                        float4 color = vertexColorArray[i].Value * vertexColorMultiplierArray[i].Value;

                        PopulateMesh(rect, elementScale, mask, fontSettings, color, ref textBuffer, ref vertices, ref triangles);
                        rebuildMeshArray[i] = new RebuildElementMeshFlag() { Rebuild = false };
                    }
                }
            }


            private void PopulateMesh(WorldSpaceRect rect, ElementScale scale, WorldSpaceMask mask, TextRenderer settings, float4 color, ref DynamicBuffer<TextData> textBuffer, ref DynamicBuffer<ControlVertexData> vertices, ref DynamicBuffer<ControlVertexIndex> triangles)
            {
                _VerticalAlignmentOptions verticalAlignment = (_VerticalAlignmentOptions)settings.Alignment;
                _HorizontalAlignmentOptions horizontalAlignment = (_HorizontalAlignmentOptions)settings.Alignment;

                var font = FontAssetFromEntity[settings.Font];
                var glyphData = FontGlyphDataFromEntity[settings.Font];

                float2 canvasScale = settings.Size * scale.Value / font.PointSize;


                float stylePadding = 1.25f + (settings.Bold ? font.BoldStyle / 4.0f : font.NormalStyle / 4.0f);
                float styleSpaceMultiplier = 1.0f + (settings.Bold ? font.BoldSpace * 0.01f : font.NormalSpace * 0.01f);

                NativeList<TextUtils.TextLineInfo> lines = new NativeList<TextUtils.TextLineInfo>(Allocator.Temp);
                TextUtils.CalculateLines(ref rect, canvasScale, styleSpaceMultiplier, ref glyphData, ref textBuffer, lines);
                float textBlockHeight = lines.Length * font.LineHeight * canvasScale.y;

                float2 alignedStartPosition = TextUtils.GetAlignedStartPosition(ref rect, ref settings, ref font, textBlockHeight, canvasScale);
                float2 currentCharacter = alignedStartPosition;

                int lineIdx = 0;
                for (int i = 0; i < textBuffer.Length; i++)
                {
                    if (lineIdx < lines.Length && i == lines[lineIdx].CharacterOffset)
                    {
                        currentCharacter = new float2(TextUtils.GetAlignedLinePosition(ref rect, lines[lineIdx].LineWidth, horizontalAlignment),
                            alignedStartPosition.y - font.LineHeight * canvasScale.y * lineIdx);
                        lineIdx++;
                    }

                    var character = textBuffer[i].Value;
                    if (character == (ushort)'\n')
                    {
                        // It's handled in GetLinesOffsets
                        continue;
                    }
                    if (TextUtils.GetGlyph(character, ref glyphData, out FontGlyphData ch))
                    {
                        int startIndex = vertices.Length;

                        float2 uv2 = new float2(ch.Scale, ch.Scale) * math.select(canvasScale, -canvasScale, settings.Bold);

                        float2 vMin = currentCharacter + new float2(ch.Metrics.horizontalBearingX - stylePadding, ch.Metrics.horizontalBearingY - ch.Metrics.height - stylePadding) * canvasScale;
                        float2 vMax = vMin + new float2(ch.Metrics.width + stylePadding * 2.0f, ch.Metrics.height + stylePadding * 2.0f) * canvasScale;

                        float4 uv = new float4(ch.Rect.x - stylePadding, ch.Rect.y - stylePadding, (ch.Rect.x + ch.Rect.width + stylePadding), (ch.Rect.y + ch.Rect.height + stylePadding)) / new float4(font.AtlasSize, font.AtlasSize);

                        RectMaskCut cut = SpriteUtility.GetRectangleMaskCut(vMin, vMax, uv.xy, uv.zw, ref mask);
                        var cutSize = cut.Max + cut.Min;
                        if (!(cutSize.x > vMax.x - vMin.x) && !(cutSize.y > vMax.y - vMin.y))
                        {
                            float4 vertexPos = new float4(vMin + cut.Min, vMax - cut.Max);
                            uv.xy = uv.xy + cut.UvMin;
                            uv.zw = uv.zw - cut.UvMax;

                            triangles.Add(new ControlVertexIndex() { Value = startIndex + 2 });
                            triangles.Add(new ControlVertexIndex() { Value = startIndex + 1 });
                            triangles.Add(new ControlVertexIndex() { Value = startIndex });

                            triangles.Add(new ControlVertexIndex() { Value = startIndex + 3 });
                            triangles.Add(new ControlVertexIndex() { Value = startIndex + 2 });
                            triangles.Add(new ControlVertexIndex() { Value = startIndex });

                            vertices.Add(new ControlVertexData()
                            {
                                Position = new float3(vertexPos.xy, 0.0f),
                                Normal = new float3(0.0f, 0.0f, -1.0f),
                                TexCoord0 = uv.xy,
                                TexCoord1 = uv2,
                                Color = color
                            });
                            vertices.Add(new ControlVertexData()
                            {
                                Position = new float3(vertexPos.zy, 0.0f),
                                Normal = new float3(0.0f, 0.0f, -1.0f),
                                TexCoord0 = uv.zy,
                                TexCoord1 = uv2,
                                Color = color
                            });
                            vertices.Add(new ControlVertexData()
                            {
                                Position = new float3(vertexPos.zw, 0.0f),
                                Normal = new float3(0.0f, 0.0f, -1.0f),
                                TexCoord0 = uv.zw,
                                TexCoord1 = uv2,
                                Color = color
                            });
                            vertices.Add(new ControlVertexData()
                            {
                                Position = new float3(vertexPos.xw, 0.0f),
                                Normal = new float3(0.0f, 0.0f, -1.0f),
                                TexCoord0 = uv.xw,
                                TexCoord1 = uv2,
                                Color = color
                            });
                        }

                        currentCharacter += (new float2(ch.Metrics.horizontalAdvance * styleSpaceMultiplier, 0.0f) *
                                             canvasScale);
                    }
                }
            }


        }
        private EntityQuery m_TextGroup;

        protected override void OnDestroy()
        {
        }

        protected override void OnCreate()
        {
            m_TextGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<WorldSpaceRect>(),
                    ComponentType.ReadWrite<ControlVertexData>(),
                    ComponentType.ReadWrite<ControlVertexIndex>(),
                    ComponentType.ReadOnly<TextRenderer>(),
                    ComponentType.ReadOnly<TextData>(),
                    ComponentType.ReadOnly<RebuildElementMeshFlag>(),
                },
                Options = EntityQueryOptions.FilterWriteGroup
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            TextChunkBuilder chunkJob = new TextChunkBuilder()
            {
                TextEntities = GetArchetypeChunkEntityType(),
                TextBufferType = GetArchetypeChunkBufferType<TextData>(true),
                WorldSpaceRectType = GetArchetypeChunkComponentType<WorldSpaceRect>(true),
                ColorValueType = GetArchetypeChunkComponentType<VertexColorValue>(true),
                ColorMultiplierType = GetArchetypeChunkComponentType<VertexColorMultiplier>(true),
                TextRendererType = GetArchetypeChunkComponentType<TextRenderer>(true),
                VertexDataType = GetArchetypeChunkBufferType<ControlVertexData>(),
                IndexDataType = GetArchetypeChunkBufferType<ControlVertexIndex>(),
                RebuildElementMeshFlagArray = GetArchetypeChunkComponentType<RebuildElementMeshFlag>(),
                FontAssetFromEntity = GetComponentDataFromEntity<TextFontAsset>(true),
                FontGlyphDataFromEntity = GetBufferFromEntity<FontGlyphData>(true),
                ElementScaleType = GetArchetypeChunkComponentType<ElementScale>(true),
                WorldSpaceMaskType = GetArchetypeChunkComponentType<WorldSpaceMask>(true)
            };
            //inputDeps = chunkJob.Schedule(chunkJob.TextEntities.Length, 1, inputDeps);
            inputDeps = chunkJob.Schedule(m_TextGroup, inputDeps);
            //}

            return inputDeps;
        }

        private NativeHashMap<ushort, GlyphInfo> CreateGlyphDataLookupTable(TMP_FontAsset font, Allocator allocator)
        {
            NativeHashMap<ushort, GlyphInfo> ret = new NativeHashMap<ushort, GlyphInfo>(font.glyphLookupTable.Count, allocator);
            foreach (var glyph in font.characterLookupTable)
            {
                ret.TryAdd((ushort)glyph.Key, new GlyphInfo()
                {
                    Scale = glyph.Value.scale,
                    Rect = glyph.Value.glyph.glyphRect,
                    Metrics = glyph.Value.glyph.metrics
                });
            }

            return ret;
        }
    }
}