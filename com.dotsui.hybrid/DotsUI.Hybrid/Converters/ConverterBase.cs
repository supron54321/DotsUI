using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace DotsUI.Hybrid{
    public abstract class ConverterBase
    {
        public abstract void Convert(Component unityComponent, Entity entity, RectTransformToEntity rectTransformToEntity, Dictionary<UnityEngine.Object, Entity> assetToEntity, EntityManager mgr);
    }

    public abstract class TypedConverter<T> : ConverterBase where T : Component
    {
        public override void Convert(Component unityComponent, Entity entity, RectTransformToEntity rectTransformToEntity, Dictionary<UnityEngine.Object, Entity> assetToEntity, EntityManager mgr)
        {
            ConvertComponent((T)unityComponent, entity, rectTransformToEntity, assetToEntity, mgr);
        }
        abstract protected void ConvertComponent(T unityComponent, Entity entity, RectTransformToEntity rectTransformToEntity, Dictionary<UnityEngine.Object, Entity> assetToEntity, EntityManager mgr);

        protected T GetOrAddComponent<T>(EntityManager mgr, Entity entity) where T : struct, IComponentData
        {
            if (mgr.HasComponent<T>(entity))
                return mgr.GetComponentData<T>(entity);
            mgr.AddComponent<T>(entity);
            return default;
        }
        protected void SetOrAddComponentData<T>(EntityManager mgr, Entity entity, T value) where T : struct, IComponentData
        {
            if(mgr.HasComponent<T>(entity))
                mgr.SetComponentData(entity, value);
            else
                mgr.AddComponentData(entity, value);
        }
    }
}