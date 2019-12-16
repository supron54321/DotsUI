using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotsUI.Core;
using Unity.Entities;
using UnityEngine.UI;

namespace DotsUI.Hybrid
{

    abstract class SelectableConversionSystem<T> : DotsUIConversionSystem where T : Selectable
    {
        protected sealed override void OnUpdate()
        {
            Entities.ForEach<T>(ConvertSelectable);
        }
        private void ConvertSelectable(T selectable)
        {
            var entity = GetPrimaryEntity(selectable);
            DstEntityManager.AddComponent(entity, typeof(Input.Selectable));
            var colors = selectable.colors;
            Entity target = TryGetPrimaryEntity(selectable.targetGraphic);
            DstEntityManager.AddComponentData(entity, new Input.SelectableColor()
            {
                Normal = colors.normalColor.ToFloat4(),
                Hover = colors.highlightedColor.ToFloat4(),
                Pressed = colors.pressedColor.ToFloat4(),
                Selected = colors.selectedColor.ToFloat4(),
                Disabled = colors.disabledColor.ToFloat4(),
                TransitionTime = colors.fadeDuration,
                Target = target
            });

            RegisterEventHandler(entity, Input.PointerEventType.SelectableGroup);

            ConvertUnityComponent(selectable);
        }

        protected abstract void ConvertUnityComponent(T component);
    }
}
