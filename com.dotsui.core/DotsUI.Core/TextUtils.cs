using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace DotsUI.Core
{
    public static class TextUtils
    {
        public struct TextLineInfo
        {
            public int CharacterOffset;
            public float LineWidth;
        }

        public static void SetupFontAssetFromTmp(EntityManager mgr, Entity entity, TMP_FontAsset font)
		{ 
			mgr.SetSharedComponentData(entity, new LegacyTextFontAsset
            {
                Asset = font,
                FontMaterial = font.material
            });

            mgr.SetComponentData(entity, new TextFontAsset
            {
                LineHeight = font.faceInfo.lineHeight,
                AscentLine = font.faceInfo.ascentLine,
                Baseline = font.faceInfo.baseline,
                BoldSpace = font.boldSpacing,
                CapLine = font.faceInfo.capLine,
                DescentLine = font.faceInfo.descentLine,
                MeanLine = font.faceInfo.meanLine,
                NormalSpace = font.normalSpacingOffset,
                PointSize = font.faceInfo.pointSize,
                BoldStyle = font.boldStyle,
                NormalStyle = font.normalStyle,
                AtlasSize = new float2(font.atlasWidth, font.atlasHeight),
            });


            var buffer = mgr.GetBuffer<FontGlyphData>(entity);
            buffer.Reserve(font.glyphLookupTable.Count);
            //foreach (var glyph in font.characterLookupTable)
            //{
            //    buffer.Add(new FontGlyphData()
            //    {
            //        Character = (ushort)glyph.Key,
            //        Scale = glyph.Value.scale,
            //        Rect = glyph.Value.glyph.glyphRect,
            //        Metrics = glyph.Value.glyph.metrics
            //    });
            //}
        }

        public static bool GetGlyph(ushort character, ref DynamicBuffer<FontGlyphData> glyphData, out FontGlyphData glyph)
        {
            for (int i = 0; i < glyphData.Length; i++)
            {
                if (glyphData[i].Character == character)
                {
                    glyph = glyphData[i];
                    return true;
                }
            }

            glyph = default;
            return false;

        }

        struct CurrentLineData
        {
            public float LineWidth;
            public float WordWidth;
            public int LineWordIndex;
            public int WordCharacterCount;
            public int CharacterOffset;
        }
        public static void CalculateLines(ref WorldSpaceRect rect, float2 canvasScale, float styleSpaceMultiplier, ref DynamicBuffer<FontGlyphData> glyphData,
                ref DynamicBuffer<TextData> textBuffer, NativeList<TextLineInfo> ret)
        {
            float maxLineWidth = rect.Max.x - rect.Min.x;

            CurrentLineData currentLine = default;

            for (int i = 0; i < textBuffer.Length; i++)
            {
                var character = textBuffer[i].Value;
                if (character == '\n')
                {
                    ret.Add(new TextLineInfo()
                    {
                        CharacterOffset = currentLine.CharacterOffset,
                        LineWidth = currentLine.LineWidth,
                    });
                    currentLine.CharacterOffset = i + 1;
                    currentLine.LineWidth = 0.0f;
                    currentLine.LineWordIndex = 0;
                    currentLine.WordCharacterCount = 0;
                    currentLine.WordWidth = 0.0f;
                    continue;
                }
                if (character == ' ')
                {
                    currentLine.LineWordIndex++;
                    currentLine.WordCharacterCount = -1;
                    currentLine.WordWidth = 0.0f;
                }
                if (GetGlyph(character, ref glyphData, out var ch))
                {
                    if (((ch.Metrics.width * styleSpaceMultiplier *
                           canvasScale.x) < rect.Width))
                    {
                        currentLine.WordCharacterCount++;
                        float characterWidth = ch.Metrics.horizontalAdvance * styleSpaceMultiplier *
                                               canvasScale.x;
                        currentLine.LineWidth += characterWidth;
                        currentLine.WordWidth += characterWidth;

                        if (currentLine.LineWidth > maxLineWidth)
                        {
                            if (currentLine.LineWordIndex != 0)
                            {
                                ret.Add(new TextLineInfo()
                                {
                                    CharacterOffset = currentLine.CharacterOffset,
                                    LineWidth = currentLine.LineWidth - currentLine.WordWidth,
                                });
                                currentLine.CharacterOffset = i - currentLine.WordCharacterCount + 1;
                                currentLine.LineWidth = 0.0f;
                                currentLine.WordWidth = 0.0f;
                                i = i - currentLine.WordCharacterCount + 1;
                                currentLine.LineWordIndex = 0;
                                currentLine.WordCharacterCount = 0;
                            }
                            else
                            {
                                ret.Add(new TextLineInfo()
                                {
                                    CharacterOffset = currentLine.CharacterOffset,
                                    LineWidth = currentLine.LineWidth,
                                });
                                currentLine.CharacterOffset = i;
                                currentLine.LineWidth = 0.0f;
                                currentLine.WordWidth = 0.0f;
                                currentLine.LineWordIndex = 0;
                                currentLine.WordCharacterCount = 0;
                            }
                        }
                        continue;
                    }
                    ret.Add(new TextLineInfo()
                    {
                        CharacterOffset = currentLine.CharacterOffset,
                        LineWidth = currentLine.LineWidth,
                    });
                    currentLine.CharacterOffset = i;
                    currentLine.LineWidth = 0.0f;
                    currentLine.WordWidth = 0.0f;
                    currentLine.LineWordIndex = 0;
                    currentLine.WordCharacterCount = 0;
                }
            }
            ret.Add(new TextLineInfo() { CharacterOffset = currentLine.CharacterOffset, LineWidth = currentLine.LineWidth });
        }

        static public WorldSpaceRect GetCaretRect(Entity targetText, EntityManager mgr, int caretPos)
        {
            var rect = mgr.GetComponentData<WorldSpaceRect>(targetText);
            var elementScale = mgr.GetComponentData<ElementScale>(targetText);
            TextRenderer settings = mgr.GetComponentData<TextRenderer>(targetText);
            var textData = mgr.GetBuffer<TextData>(targetText);
            
            _VerticalAlignmentOptions verticalAlignment = (_VerticalAlignmentOptions)settings.Alignment;
            _HorizontalAlignmentOptions horizontalAlignment = (_HorizontalAlignmentOptions)settings.Alignment;

            var font = mgr.GetComponentData<TextFontAsset>(settings.Font);
            var glyphData = mgr.GetBuffer<FontGlyphData>(settings.Font);
            float styleSpaceMultiplier = 1.0f + (settings.Bold ? font.BoldSpace * 0.01f : font.NormalSpace * 0.01f);

            float2 canvasScale = settings.Size * elementScale.Value / font.PointSize;
            NativeList<TextLineInfo> lines = new NativeList<TextLineInfo>(Allocator.Temp);
            CalculateLines(ref rect, canvasScale, styleSpaceMultiplier, ref glyphData, ref textData, lines);

            float textBlockHeight = lines.Length * font.LineHeight * canvasScale.y;
            float2 alignedStartPosition = TextUtils.GetAlignedStartPosition(ref rect, ref settings, ref font, textBlockHeight, canvasScale);
            
            for(int i = lines.Length-1; i >= 0; i--)
            {
                if(lines[i].CharacterOffset <= caretPos)
                {
                    float2 currentCharacter = new float2(GetAlignedLinePosition(ref rect, lines[i].LineWidth, horizontalAlignment),
                        alignedStartPosition.y - font.LineHeight * canvasScale.y * i);
                    
                    for(int j = lines[i].CharacterOffset; j < math.min(caretPos, textData.Length); j++)
                    {
                        if (TextUtils.GetGlyph(textData[j].Value, ref glyphData, out FontGlyphData ch))
                            currentCharacter += (new float2(ch.Metrics.horizontalAdvance * styleSpaceMultiplier, 0.0f) *
                                             canvasScale);
                    }
                    return new WorldSpaceRect(){
                        Min = currentCharacter+new float2(0.0f, font.DescentLine*canvasScale.y),
                        Max = new float2(currentCharacter.x+(2.0f), currentCharacter.y+font.LineHeight*canvasScale.y)
                    };
                    
                }
            }

            return new WorldSpaceRect(){
                Min = rect.Min,
                Max = new float2(rect.Min.x+2.0f, rect.Max.y)
            };
        }
        public static float GetAlignedLinePosition(ref WorldSpaceRect rect, float lineWidth, _HorizontalAlignmentOptions horizontalAlignment)
        {
            if ((horizontalAlignment & _HorizontalAlignmentOptions.Right) == _HorizontalAlignmentOptions.Right)
                return rect.Min.x + rect.Width - lineWidth;
            if ((horizontalAlignment & _HorizontalAlignmentOptions.Center) == _HorizontalAlignmentOptions.Center)
                return rect.Min.x + rect.Width * 0.5f - lineWidth * 0.5f;
            return rect.Min.x;
        }

        public static float2 GetAlignedStartPosition(ref WorldSpaceRect rect, ref TextRenderer settings, ref TextFontAsset font, float textBlockHeight, float2 scale)
        {
            float startY = 0.0f;
            _VerticalAlignmentOptions vertical = (_VerticalAlignmentOptions)settings.Alignment;
            _HorizontalAlignmentOptions horizontal = (_HorizontalAlignmentOptions)settings.Alignment;
            if ((vertical & _VerticalAlignmentOptions.Bottom) == _VerticalAlignmentOptions.Bottom)
                startY = rect.Min.y - font.DescentLine * scale.y + textBlockHeight - font.LineHeight * scale.y;
            else if ((vertical & _VerticalAlignmentOptions.Middle) == _VerticalAlignmentOptions.Middle)
                startY = (rect.Min.y + rect.Max.y) * 0.5f - (font.AscentLine) * scale.y + textBlockHeight * 0.5f;
            else if ((vertical & _VerticalAlignmentOptions.Top) == _VerticalAlignmentOptions.Top)
                startY = rect.Max.y - (font.AscentLine) * scale.y;
            return new float2(rect.Min.x, startY);
        }
    }
}