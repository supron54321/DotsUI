using System;
using System.Collections.Generic;
using DotsUI.Hybrid;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;


public class FpsCounter : MonoBehaviour, IRectTransformToEntity
{
    public RectTransform TargetText;

    public void ConvertToEntity(Entity entity, RectTransformToEntity rectTransformToEntity, Dictionary<Object, Entity> assetToEntity,
        EntityManager mgr)
    {
        mgr.AddComponentData(entity, new FpsCounterComponent()
        {
            TargetText = rectTransformToEntity[TargetText]
        });
    }
}

[Serializable]
public struct FpsCounterComponent : IComponentData
{
    public Entity TargetText;
}
