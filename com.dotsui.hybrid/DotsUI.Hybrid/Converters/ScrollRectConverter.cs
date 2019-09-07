using System.Collections.Generic;
using Unity.Entities;

namespace DotsUI.Hybrid
{
    internal class ScrollRectConverter : TypedConverter<UnityEngine.UI.ScrollRect>
    {
        protected override void ConvertComponent(UnityEngine.UI.ScrollRect unityComponent, Entity entity, RectTransformToEntity rectTransformToEntity, Dictionary<UnityEngine.Object, Entity> assetToEntity, EntityManager mgr)
        {
            mgr.AddComponentData(entity, new DotsUI.Controls.ScrollRect()
            {
                Content = rectTransformToEntity[unityComponent.content],
                Viewport = rectTransformToEntity[unityComponent.viewport],
                HorizontalBar = rectTransformToEntity[unityComponent.horizontalScrollbar.transform as UnityEngine.RectTransform],
                VerticalBar = rectTransformToEntity[unityComponent.verticalScrollbar.transform as UnityEngine.RectTransform],
                HorizontalBarSpacing = unityComponent.horizontalScrollbarSpacing,
                VerticalBarSpacing = unityComponent.verticalScrollbarSpacing
            });
            var pointerInputReceiver = GetOrAddComponent<Input.PointerInputReceiver>(mgr, entity);
            pointerInputReceiver.ListenerTypes |= Input.PointerEventType.BeginDrag | Input.PointerEventType.Drag |
                                                  Input.PointerEventType.EndDrag;
            mgr.SetComponentData(entity, pointerInputReceiver);
        }
    }
}