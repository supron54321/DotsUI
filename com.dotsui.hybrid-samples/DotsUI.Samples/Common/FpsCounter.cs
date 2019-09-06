using System;
using System.Collections.Generic;
using DotsUI.Hybrid;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;


public class FpsCounter : MonoBehaviour, IConvertGameObjectToEntity
{
    public RectTransform TargetText;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new FpsCounterComponent()
        {
            TargetText = conversionSystem.GetPrimaryEntity(TargetText)
        });
    }
}

[Serializable]
public struct FpsCounterComponent : IComponentData
{
    public Entity TargetText;
}
