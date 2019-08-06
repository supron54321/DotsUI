using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using DotsUI.Core;
using UnityEngine.UI;

namespace DotsUI.Hybrid
{
    internal class RectMaskConverter : TypedConverter<RectMask2D>
    {
        protected override void ConvertComponent(RectMask2D unityComponent, Entity entity, RectTransformToEntity rectTransformToEntity, Dictionary<UnityEngine.Object, Entity> assetToEntity, EntityManager commandBuffer)
        {
            commandBuffer.AddComponent(entity, typeof(RectMask));
        }
    }
    internal class MaskConverter : TypedConverter<Mask>
    {
        protected override void ConvertComponent(Mask unityComponent, Entity entity, RectTransformToEntity rectTransformToEntity, Dictionary<UnityEngine.Object, Entity> assetToEntity, EntityManager commandBuffer)
        {
            commandBuffer.AddComponent(entity, typeof(RectMask));
        }
    }
}