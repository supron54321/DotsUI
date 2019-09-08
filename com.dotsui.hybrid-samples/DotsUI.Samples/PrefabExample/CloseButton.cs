using System.Collections.Generic;
using DotsUI.Hybrid;
using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class CloseButton : MonoBehaviour, IConvertGameObjectToEntity
{
    public RectTransform WindowTransform;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new CloseButtonComponent()
        {
            WindowTransform = conversionSystem.GetPrimaryEntity(WindowTransform)
        });
    }
}

struct CloseButtonComponent : IComponentData
{
    public Entity WindowTransform;
}