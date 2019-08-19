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

        protected TComponent GetOrAddComponent<TComponent>(EntityManager mgr, Entity entity) where TComponent : struct, IComponentData
        {
            if (mgr.HasComponent<TComponent>(entity))
                return mgr.GetComponentData<TComponent>(entity);
            mgr.AddComponent<TComponent>(entity);
            return default;
        }
        protected void SetOrAddComponentData<TComponent>(EntityManager mgr, Entity entity, TComponent value) where TComponent : struct, IComponentData
        {
            if(mgr.HasComponent<TComponent>(entity))
                mgr.SetComponentData(entity, value);
            else
                mgr.AddComponentData(entity, value);
        }
    }
}