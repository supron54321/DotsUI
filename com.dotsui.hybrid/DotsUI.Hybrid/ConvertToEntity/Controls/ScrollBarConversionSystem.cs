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
    class ScrollBarConversionSystem : SelectableConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach<Scrollbar>(ConvertScrollBar);
        }

        private void ConvertScrollBar(Scrollbar scrollBar)
        {
            var entity = GetPrimaryEntity(scrollBar);
            var scrollHandle = GetPrimaryEntity(scrollBar.handleRect);
            DstEntityManager.AddComponentData(entity, new DotsUI.Controls.ScrollBar()
            {
                ScrollHandle = scrollHandle,
                Value = scrollBar.value,
                ParentScrollRect = GetPrimaryEntity(scrollBar.GetComponentsInParent<ScrollRect>(true)[0])  // temporary workaround for inactive transforms
            });
            DstEntityManager.AddComponentData(entity, new Controls.ScrollBarHandle()
            {

            });
            RegisterEventHandler(entity, Input.PointerEventType.BeginDrag | Input.PointerEventType.Drag |
                                         Input.PointerEventType.EndDrag);
            ConvertSelectable(scrollBar);
        }
    }
}
