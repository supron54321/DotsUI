using System.Collections;
using System.Collections.Generic;
using DotsUI;
using DotsUI.Controls;
using DotsUI.Core;
using DotsUI.Hybrid;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using RectTransform = UnityEngine.RectTransform;

[DisableAutoCreation]
[UpdateInGroup(typeof(UserInputSystemGroup))]
public class InstantiationSystem : ComponentSystem
{
    private EntityQuery m_InstantiateButtonQuery;
    private DotsUIPrefab m_WindowPrefab;

    protected override void OnCreate()
    {
        m_InstantiateButtonQuery = GetEntityQuery(ComponentType.ReadOnly<ButtonClickedEvent>(),
            ComponentType.ReadOnly<InstantiateButtonComponent>());

        m_WindowPrefab = new DotsUIPrefab(Resources.Load<RectTransform>("WindowPrefab"), EntityManager, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        m_WindowPrefab.Dispose();
    }

    protected override void OnUpdate()
    {
        int clickCount = m_InstantiateButtonQuery.CalculateLength();
        for (int i = 0; i < clickCount; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                Entity entity = m_WindowPrefab.Instantiate();
                DotsUI.Core.RectTransform rectTransform = EntityManager.GetComponentData<DotsUI.Core.RectTransform>(entity);
                rectTransform.Position = new float2(10.0f + j*10.0f, - 10.0f - j*10.0f);
                EntityManager.SetComponentData(entity, rectTransform);
                EntityManager.SetComponentData(entity, new UIParent { Value = GetSingletonEntity<WindowCanvasComponent>() });
            }

        }
    }
}
