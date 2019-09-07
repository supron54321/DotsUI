using System.Collections.Generic;
using Unity.Entities;
using DotsUI.Core;

namespace DotsUI.Hybrid{
    internal class SelectableConverter : TypedConverter<UnityEngine.UI.Selectable>
    {
        protected override void ConvertComponent(UnityEngine.UI.Selectable unityComponent, Entity entity, RectTransformToEntity rectTransformToEntity, Dictionary<UnityEngine.Object, Entity> assetToEntity, EntityManager mgr)
        {
            mgr.AddComponent(entity, typeof(DotsUI.Input.Selectable));
            var colors = unityComponent.colors;
            Entity target;
            if(!rectTransformToEntity.TryGetValue(unityComponent.targetGraphic?.rectTransform, out target))
                target = entity;
            mgr.AddComponentData(entity, new DotsUI.Input.SelectableColor(){
                Normal = colors.normalColor.ToFloat4(),
                Hover = colors.highlightedColor.ToFloat4(),
                Pressed = colors.pressedColor.ToFloat4(),
                Selected = colors.selectedColor.ToFloat4(),
                Disabled = colors.disabledColor.ToFloat4(),
                TransitionTime = colors.fadeDuration,
                Target = target
            });

            var pointerInputReceiver = GetOrAddComponent<Input.PointerInputReceiver>(mgr, entity);
            pointerInputReceiver.ListenerTypes |= Input.PointerEventType.SelectableGroup;
            mgr.SetComponentData(entity, pointerInputReceiver);
        }
    }
}