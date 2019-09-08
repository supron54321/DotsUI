using DotsUI.Controls;
using DotsUI.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[DisableAutoCreation]
[UpdateInGroup(typeof(UserInputSystemGroup))]
public class InstantiationSystem : ComponentSystem
{
    private EntityQuery m_InstantiateButtonQuery;
    private Entity m_WindowPrefab;

    protected override void OnCreate()
    {
        m_InstantiateButtonQuery = GetEntityQuery(ComponentType.ReadOnly<ButtonClickedEvent>(),
            ComponentType.ReadOnly<InstantiateButtonComponent>());

        m_WindowPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(Resources.Load<GameObject>("WindowPrefab"), new GameObjectConversionSettings(World, GameObjectConversionUtility.ConversionFlags.AssignName));
    }

    protected override void OnDestroy()
    {
    }

    protected override void OnUpdate()
    {
        int clickCount = m_InstantiateButtonQuery.CalculateEntityCount();
        for (int i = 0; i < clickCount; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                Entity entity = EntityManager.Instantiate(m_WindowPrefab);
                DotsUI.Core.RectTransform rectTransform = EntityManager.GetComponentData<DotsUI.Core.RectTransform>(entity);
                rectTransform.Position = new float2(10.0f + j*10.0f, - 10.0f - j*10.0f);
                EntityManager.SetComponentData(entity, rectTransform);
                EntityManager.AddComponentData(entity, new Parent { Value = GetSingletonEntity<WindowCanvasComponent>() });
                EntityManager.AddComponent<LocalToParent>(entity);
                EntityManager.AddComponent<DirtyElementFlag>(entity);
            }

        }
    }
}
