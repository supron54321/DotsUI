using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.Entities;

namespace DotsUI.Hybrid
{
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    class InputFieldConversionSystem : SelectableConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach<TMP_InputField>(ConvertInputField);
        }

        private void ConvertInputField(TMP_InputField inputField)
        {
            var entity = GetPrimaryEntity(inputField);
            DstEntityManager.AddComponentData(entity, new Input.KeyboardInputReceiver());
            DstEntityManager.AddBuffer<Input.KeyboardInputBuffer>(entity);
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
            ConvertSelectable(inputField);
        }
    }
}
