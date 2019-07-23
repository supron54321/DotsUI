using System.Collections;
using System.Collections.Generic;
using DotsUI.Hybrid;
using Unity.Entities;
using UnityEngine;

public class InstantiateButton : MonoBehaviour, IRectTransformToEntity
{
    public void ConvertToEntity(Entity entity, RectTransformToEntity rectTransformToEntity, Dictionary<Object, Entity> assetToEntity,
        EntityManager mgr)
    {
        mgr.AddComponent(entity, typeof(InstantiateButtonComponent));
    }
}


public struct InstantiateButtonComponent : IComponentData
{

}