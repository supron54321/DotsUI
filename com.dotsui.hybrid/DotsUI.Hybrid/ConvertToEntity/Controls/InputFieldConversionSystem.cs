using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.Entities;
using UnityEngine.UI;

namespace DotsUI.Hybrid
{
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    class InputFieldConversionSystem : SelectableConversionSystem<TMP_InputField>
    {
        protected override void ConvertUnityComponent(TMP_InputField inputField)
        {
            var entity = GetPrimaryEntity(inputField);
            DstEntityManager.AddComponentData(entity, new Input.KeyboardInputReceiver());
            Entity target = TryGetPrimaryEntity(inputField.textComponent);
            Entity placeholder = TryGetPrimaryEntity(inputField.placeholder);
            DstEntityManager.AddComponentData(entity, new Controls.InputField()
            {
                Target = target,
                Placeholder = placeholder
            });
            DstEntityManager.AddComponentData(entity, new Controls.InputFieldCaretState()
            {
                CaretPosition = 0
            });
        }
    }
}
