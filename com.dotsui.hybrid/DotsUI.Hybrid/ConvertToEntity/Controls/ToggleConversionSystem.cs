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
    class ToggleConversionSystem : SelectableConversionSystem<Toggle>
    {
        protected override void ConvertUnityComponent(Toggle component)
        {
            var entity = GetPrimaryEntity(component);
            DstEntityManager.AddComponentData(entity,
                new Controls.Toggle()
                {
                    IsOn = component.isOn,
                    TargetGraphic = component.graphic != null ? GetPrimaryEntity(component.graphic) : Entity.Null,
                    Group = component.group != null ? GetPrimaryEntity(component.group) : Entity.Null,
                });
        }
    }
}