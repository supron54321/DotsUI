using System.Collections.Generic;
using Unity.Entities;
using UnityEngine.UI;

namespace DotsUI.Hybrid
{
    internal class ScrollBarConverter : TypedConverter<UnityEngine.UI.Scrollbar>
    {
        protected override void ConvertComponent(UnityEngine.UI.Scrollbar unityComponent, Entity entity, RectTransformToEntity rectTransformToEntity, Dictionary<UnityEngine.Object, Entity> assetToEntity, EntityManager mgr)
        {
            var scrollHandle = rectTransformToEntity[unityComponent.handleRect];
            mgr.AddComponentData(entity, new DotsUI.Controls.ScrollBar(){
                ScrollHandle = scrollHandle,
                Value = unityComponent.value,
                ParentScrollRect = rectTransformToEntity[unityComponent.GetComponentsInParent<ScrollRect>(true)[0].transform as UnityEngine.RectTransform]  // temporary workaround for inactive transforms
            });
            mgr.AddComponentData(entity, new Controls.ScrollBarHandle()
            {

            });
            var pointerInputReceiver = GetOrAddComponent<Input.PointerInputReceiver>(mgr, entity);
            pointerInputReceiver.ListenerTypes |= Input.PointerEventType.BeginDrag | Input.PointerEventType.Drag |
                                                  Input.PointerEventType.EndDrag;
            mgr.SetComponentData(entity, pointerInputReceiver);
        }
    }
}