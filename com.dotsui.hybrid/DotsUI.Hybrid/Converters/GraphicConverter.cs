using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using DotsUI.Core;
using Unity.Mathematics;
using UnityEngine.UI;

namespace DotsUI.Hybrid{
    internal class GraphicConverter : TypedConverter<UnityEngine.UI.Graphic>
    {
        protected override void ConvertComponent(UnityEngine.UI.Graphic unityComponent, Entity entity, RectTransformToEntity rectTransformToEntity, Dictionary<UnityEngine.Object, Entity> assetToEntity, EntityManager commandBuffer)
        {
            commandBuffer.AddComponentData(entity, new VertexColorValue(){
                Value = unityComponent.color.ToFloat4()
            });
            commandBuffer.AddComponentData(entity, new VertexColorMultiplier()
            {
                Value = new float4(1.0f, 1.0f, 1.0f, 1.0f)
            });
            commandBuffer.AddComponent(entity, typeof(ElementVertexPointerInMesh));
        }
    }
}