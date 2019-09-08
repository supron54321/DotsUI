using DotsUI.Controls;
using Unity.Entities;
using DotsUI.Core;
using Unity.Collections;
using Unity.Transforms;

[DisableAutoCreation]
[UpdateInGroup(typeof(UserInputSystemGroup))]
public class CloseButtonSystem : ComponentSystem
{
    private EntityQuery m_CloseButtonClickGroup;
    protected override void OnCreate()
    {
        m_CloseButtonClickGroup = GetEntityQuery(ComponentType.ReadOnly<ButtonClickedEvent>(),
            ComponentType.ReadOnly<CloseButtonComponent>());
    }

    protected override void OnUpdate()
    {
        using (var closeButtons = m_CloseButtonClickGroup.ToComponentDataArray<CloseButtonComponent>(Allocator.TempJob))
        {
            for (int i = 0; i < closeButtons.Length; i++)
            {
                // Unfortunately, we have to manually set dirty component
                var parent = EntityManager.GetComponentData<Parent>(closeButtons[i].WindowTransform);
                EntityManager.DestroyEntity(closeButtons[i].WindowTransform);
                EntityManager.AddComponent(parent.Value, typeof(DirtyElementFlag));
            }
        }

        
    }
}
