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
    class ButtonConversionSystem : SelectableConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach<Button>(ConvertButton);
        }

        private void ConvertButton(Button button)
        {
            var entity = GetPrimaryEntity(button);
            DstEntityManager.AddComponent(entity, typeof(Controls.Button));
            ConvertSelectable(button);
        }
    }
}
