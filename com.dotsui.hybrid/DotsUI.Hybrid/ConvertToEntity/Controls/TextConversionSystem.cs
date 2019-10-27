using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotsUI.Core;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace DotsUI.Hybrid
{
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    class TextConversionSystem : GraphicConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach<TextMeshProUGUI>(ConvertText);
        }

        void ConvertText(TextMeshProUGUI text)
        {
            var entity = GetPrimaryEntity(text);
            var fontAsset = GetPrimaryEntity(text.font);

            DstEntityManager.AddComponentData(entity, new TextRenderer()
            {
                Font = fontAsset,
                Size = text.fontSize,
                Alignment = text.alignment,
                Bold = (text.fontStyle & FontStyles.Bold) == FontStyles.Bold,
                Italic = (text.fontStyle & FontStyles.Italic) == FontStyles.Italic
            });
            var textBuffer = DstEntityManager.AddBuffer<TextData>(entity);
            var content = text.text;
            textBuffer.ResizeUninitialized(content.Length);
            unsafe
            {
                fixed (char* textPtr = content)
                    UnsafeUtility.MemCpy(textBuffer.GetUnsafePtr(), textPtr, content.Length * sizeof(char));
            }
            DstEntityManager.AddBuffer<ControlVertexData>(entity);
            DstEntityManager.AddBuffer<ControlVertexIndex>(entity);
            DstEntityManager.AddComponentData(entity, new RebuildElementMeshFlag() { Rebuild = true });
            ConvertGraphic(text);
        }
    }
}
