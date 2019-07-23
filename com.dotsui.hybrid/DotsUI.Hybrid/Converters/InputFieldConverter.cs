using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using DotsUI.Core;
using UnityEngine.UI;

namespace DotsUI.Hybrid
{
    internal class InputFieldConverter : TypedConverter<UnityEngine.UI.InputField>
    {
        protected override void ConvertComponent(UnityEngine.UI.InputField unityComponent, Entity entity, RectTransformToEntity rectTransformToEntity, Dictionary<UnityEngine.Object, Entity> assetToEntity, EntityManager commandBuffer)
        {
            commandBuffer.AddComponentData(entity, new DotsUI.Input.KeyboardInputReceiver());
            commandBuffer.AddBuffer<DotsUI.Input.KeyboardInputBuffer>(entity);
            Entity target = default;
            if (!rectTransformToEntity.TryGetValue(unityComponent.targetGraphic?.rectTransform, out target))
                target = entity;
            commandBuffer.AddComponentData(entity, new DotsUI.Controls.InputField()
            {
                Target = target
            });
            commandBuffer.AddComponentData(entity, new DotsUI.Controls.InputFieldCaretState(){
                CaretPosition = 0
            });
        }
    }
}