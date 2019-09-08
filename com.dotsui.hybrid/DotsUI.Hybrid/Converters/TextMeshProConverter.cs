using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using DotsUI.Core;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;

namespace DotsUI.Hybrid{
    internal class TextMeshProConverter : TypedConverter<TextMeshProUGUI>
    {
        protected override void ConvertComponent(TextMeshProUGUI unityComponent, Entity entity, RectTransformToEntity rectTransformToEntity, Dictionary<UnityEngine.Object, Entity> assetToEntity, EntityManager commandBuffer)
        {
            if(unityComponent.font == null)
            {
                Debug.LogError($"TextMeshProConverter - font asset cannot be null reference. Object: {unityComponent}", unityComponent);
                return;
            }
            if (!assetToEntity.TryGetValue(unityComponent.font, out var fontAsset))
            {
                fontAsset = TextUtils.CreateFontAssetFromTmp(commandBuffer, unityComponent.font);
                assetToEntity.Add(unityComponent.font, fontAsset);
            }
            commandBuffer.AddComponentData(entity, new TextRenderer()
            {
                Font = fontAsset,
                Size = unityComponent.fontSize,
                Alignment = unityComponent.alignment,
                Bold = (unityComponent.fontStyle & FontStyles.Bold) == FontStyles.Bold,
                Italic = (unityComponent.fontStyle & FontStyles.Italic) == FontStyles.Italic
            });
            var textBuffer = commandBuffer.AddBuffer<TextData>(entity);
            var content = unityComponent.text;
            textBuffer.ResizeUninitialized(content.Length);
            unsafe
            {
                fixed (char* textPtr = content)
                    UnsafeUtility.MemCpy(textBuffer.GetUnsafePtr(), textPtr, content.Length * sizeof(char));
            }
            commandBuffer.AddBuffer<ControlVertexData>(entity);
            commandBuffer.AddBuffer<ControlVertexIndex>(entity);
            commandBuffer.AddComponent(entity, typeof(RebuildElementMeshFlag));
        }
    }
}