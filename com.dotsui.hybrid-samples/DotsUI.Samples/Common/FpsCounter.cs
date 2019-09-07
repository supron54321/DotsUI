using System;
using Unity.Entities;
using UnityEngine;


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
