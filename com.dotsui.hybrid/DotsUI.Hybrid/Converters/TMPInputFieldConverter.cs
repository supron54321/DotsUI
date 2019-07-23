using System;
using System.Collections.Generic;
using DotsUI.Controls;
using UnityEngine;
using Unity.Entities;
using DotsUI.Core;
using UnityEngine.UI;
using TMPro;
using Unity.Mathematics;
using RectTransform = DotsUI.Core.RectTransform;

namespace DotsUI.Hybrid
{
    internal class TMPInputFieldConverter : TypedConverter<TMP_InputField>
    {
        protected override void ConvertComponent(TMP_InputField unityComponent, Entity entity, RectTransformToEntity rectTransformToEntity, Dictionary<UnityEngine.Object, Entity> assetToEntity, EntityManager mgr)
        {
            mgr.AddComponentData(entity, new DotsUI.Input.KeyboardInputReceiver());
            mgr.AddBuffer<DotsUI.Input.KeyboardInputBuffer>(entity);
            Entity target = default;
            if (!rectTransformToEntity.TryGetValue(unityComponent.textComponent?.rectTransform, out target))
                target = entity;
            Entity placeholder = default;
            rectTransformToEntity.TryGetValue(unityComponent.placeholder.rectTransform, out placeholder);
            mgr.AddComponentData(entity, new DotsUI.Controls.InputField()
            {
                Target = target,
                Placeholder = placeholder
            });
            mgr.AddComponentData(entity, new DotsUI.Controls.InputFieldCaretState(){
                CaretPosition = 0,
            });
        }
    }
}