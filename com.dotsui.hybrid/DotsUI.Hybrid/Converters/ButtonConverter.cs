using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using DotsUI.Core;
using UnityEngine.UI;

namespace DotsUI.Hybrid
{
    internal class ButtonConverter : TypedConverter<UnityEngine.UI.Button>
    {
        protected override void ConvertComponent(UnityEngine.UI.Button unityComponent, Entity entity, RectTransformToEntity rectTransformToEntity, Dictionary<UnityEngine.Object, Entity> assetToEntity, EntityManager commandBuffer)
        {
            commandBuffer.AddComponentData(entity, new DotsUI.Controls.Button());
        }
    }
}