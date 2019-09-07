using System.Collections.Generic;
using DotsUI.Hybrid;
using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class CloseButton : MonoBehaviour, IRectTransformToEntity
{
    public RectTransform WindowTransform;

    public void ConvertToEntity(Entity entity, RectTransformToEntity rectTransformToEntity, Dictionary<Object, Entity> assetToEntity,
        EntityManager mgr)
    {
        mgr.AddComponentData(entity, new CloseButtonComponent()
        {
            WindowTransform = rectTransformToEntity[WindowTransform]
        });
    }
}

struct CloseButtonComponent : IComponentData
{
    public Entity WindowTransform;
}