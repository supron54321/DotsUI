using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotsUI.Input;
using Unity.Entities;
using UnityEngine.UI;

namespace DotsUI.Hybrid
{
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    class SliderConversionSystem : SelectableConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach<Slider>(ConvertSlider);
        }

        private void ConvertSlider(Slider slider)
        {
            var entity = GetPrimaryEntity(slider);
            DstEntityManager.AddComponentData(entity, new DotsUI.Controls.Slider()
            {
                FillRect = GetPrimaryEntity(slider.fillRect),
                HandleRect = GetPrimaryEntity(slider.handleRect),
                MaxValue = slider.maxValue,
                MinValue = slider.minValue,
                Value = slider.value,
                SliderDirection = ConvertDirection(slider.direction),
                WholeNumbers = slider.wholeNumbers
            });
            ConvertSelectable(slider);
            RegisterEventHandler(entity, PointerEventType.Drag);
        }

        private Controls.Slider.Direction ConvertDirection(Slider.Direction sliderDirection)
        {
            switch (sliderDirection)
            {
                case Slider.Direction.LeftToRight:
                    return Controls.Slider.Direction.LeftToRight;
                case Slider.Direction.RightToLeft:
                    return Controls.Slider.Direction.RightToLeft;
                case Slider.Direction.TopToBottom:
                    return Controls.Slider.Direction.TopToBottom;
                case Slider.Direction.BottomToTop:
                    return Controls.Slider.Direction.BottomToTop;
            }

            return Controls.Slider.Direction.LeftToRight;
        }
    }
}
