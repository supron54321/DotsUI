using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine.UI;

namespace DotsUI.Hybrid
{
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    class ScrollRectConversionSystem: DotsUIConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach<ScrollRect>(ConvertScrollRect);
        }

        private void ConvertScrollRect(ScrollRect scrollRect)
        {
            var entity = GetPrimaryEntity(scrollRect);
            DstEntityManager.AddComponentData(entity, new DotsUI.Controls.ScrollRect()
            {
                Content = GetPrimaryEntity(scrollRect.content),
                Viewport = GetPrimaryEntity(scrollRect.viewport),
                HorizontalBar = GetPrimaryEntity(scrollRect.horizontalScrollbar),
                VerticalBar = GetPrimaryEntity(scrollRect.verticalScrollbar),
                HorizontalBarSpacing = scrollRect.horizontalScrollbarSpacing,
                VerticalBarSpacing = scrollRect.verticalScrollbarSpacing
            });
            var pointerInputReceiver = GetOrAddComponent<Input.PointerInputReceiver>(DstEntityManager, entity);
            pointerInputReceiver.ListenerTypes |= Input.PointerEventType.BeginDrag | Input.PointerEventType.Drag |
                                                  Input.PointerEventType.EndDrag;
            DstEntityManager.SetComponentData(entity, pointerInputReceiver);
        }
    }
}
