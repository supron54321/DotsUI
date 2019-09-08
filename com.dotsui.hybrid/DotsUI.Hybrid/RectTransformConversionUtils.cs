using System;
using System.Collections.Generic;
using DotsUI.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using RectTransform = DotsUI.Core.RectTransform;

namespace DotsUI.Hybrid
{
    public static class DictionaryExtension
    {
        public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
        {
            key = tuple.Key;
            value = tuple.Value;
        }
    }

    public struct DotsUIArchetypes
    {
        public EntityArchetype CanvasArchetype;
        public EntityArchetype GenericChildArchetype;

        public DotsUIArchetypes(EntityManager manager)
        {
            CanvasArchetype = manager.CreateArchetype(typeof(RectTransform), typeof(LocalToWorld), typeof(WorldSpaceRect));
            GenericChildArchetype =
                manager.CreateArchetype(typeof(RectTransform), typeof(LocalToWorld), typeof(LocalToParent), typeof(WorldSpaceRect), typeof(Parent));
        }
    }



    /// <summary>
    /// This class is temporary workaround over ConvertToEntity bug with UnityEngine.RectTransform
    /// Code is ugly and not optimized, but I didnt care that much since it will be replaced anyway
    /// </summary>
    [Obsolete("Use ConvertToEntity workflow instead", false)]
    public static class RectTransformConversionUtils
    {
        static RectTransformConverter m_Converter = new RectTransformConverter();
        /// <summary>
        /// Convert entire hierarchy of canvas
        /// </summary>
        /// <param name="root"></param>
        /// <param name="destinationWorld"></param>
        /// 
        static List<UnityEngine.RectTransform> m_PooledRectTransformList = new List<UnityEngine.RectTransform>(100);


        public static void ConvertCanvasHierarchy(Canvas root, EntityManager manager, RectTransformToEntity rectTransformToEntity)
        {
            Initialize();
            var archetypes = new DotsUIArchetypes(manager);
            var assetToEntity = new Dictionary<UnityEngine.Object, Entity>();
            var entity = manager.CreateEntity(archetypes.CanvasArchetype);
            var linkedGroup = new NativeList<LinkedEntityGroup>(Allocator.Temp);//manager.AddBuffer<LinkedEntityGroup>(entity);
            linkedGroup.Add(entity);
            ConvertRectTransformsRecursive(root.GetComponent<UnityEngine.RectTransform>(), entity,
                rectTransformToEntity,
                manager, ref archetypes, ref linkedGroup, false);
            manager.AddBuffer<LinkedEntityGroup>(entity).AddRange(linkedGroup.AsArray());

            UpdateComponents(rectTransformToEntity, assetToEntity, manager);
        }
        public static void ConvertCanvasHierarchy(Canvas root, EntityManager manager)
        {
            RectTransformToEntity rectTransformToEntity = new RectTransformToEntity(10, Allocator.Temp);
            ConvertCanvasHierarchy(root, manager, rectTransformToEntity);
        }

        private static void Initialize()
        {
            if (m_Converter == null)
                m_Converter = new RectTransformConverter();
            m_PooledRectTransformList.Clear();
        }

        internal static Entity ConvertPrefabHierarchy(UnityEngine.RectTransform root, EntityManager manager, RectTransformToEntity rectTransformToEntity)
        {
            Initialize();
            var archetypes = new DotsUIArchetypes(manager);
            var assetToEntity = new Dictionary<UnityEngine.Object, Entity>();
            var entity = manager.CreateEntity(archetypes.GenericChildArchetype);
            SetRectTransform(manager, root, entity);
            var linkedGroup = new NativeList<LinkedEntityGroup>(Allocator.Temp);
            ConvertRectTransformsRecursive(root.GetComponent<UnityEngine.RectTransform>(), entity,
                rectTransformToEntity,
                manager, ref archetypes, ref linkedGroup, true);
            manager.AddBuffer<LinkedEntityGroup>(entity).AddRange(linkedGroup.AsArray());
            UpdateComponents(rectTransformToEntity, assetToEntity, manager);
            return entity;
        }

        private static void UpdateComponents(RectTransformToEntity hierarchy, Dictionary<UnityEngine.Object, Entity> assetToEntity,
            EntityManager commandBuffer)
        {
            //foreach (var (rectTransform, entity) in hierarchy)
            foreach(var rectTransform in m_PooledRectTransformList)
            {
                m_Converter.ConvertComponents(rectTransform, hierarchy[rectTransform], hierarchy, assetToEntity, commandBuffer);
            }
        }

        public static void ConvertRectTransformsRecursive(UnityEngine.RectTransform parent, Entity parentEntity,
            RectTransformToEntity rectTransformToEntity, EntityManager commandBuffer,
            ref DotsUIArchetypes archetypes, ref NativeList<LinkedEntityGroup> linkedGroup, bool isPrefab)
        {
            rectTransformToEntity.Add(parent, parentEntity);
            linkedGroup.Add(parentEntity);
#if UNITY_EDITOR
            commandBuffer.SetName(parentEntity, parent.name);
#endif
            m_PooledRectTransformList.Add(parent);
            int childCount = parent.childCount;
            for (int i = 0; i < childCount; i++)
            {
                if (parent.GetChild(i) is UnityEngine.RectTransform childTransform)
                {
                    //if(!childTransform.gameObject.activeInHierarchy)
                    //    continue;
                    Entity child = commandBuffer.CreateEntity(archetypes.GenericChildArchetype);
                    SetRectTransform(commandBuffer, childTransform, child);
                    commandBuffer.SetComponentData(child, new Parent() { Value = parentEntity });
                    if (isPrefab)
                        commandBuffer.AddComponent(child, typeof(Prefab));
                    ConvertRectTransformsRecursive(childTransform, child, rectTransformToEntity, commandBuffer,
                        ref archetypes, ref linkedGroup, isPrefab);
                }
            }
        }

        private static void SetRectTransform(EntityManager commandBuffer, UnityEngine.RectTransform childTransform, Entity child)
        {
            commandBuffer.SetComponentData(child, new RectTransform()
            {
                AnchorMin = childTransform.anchorMin,
                AnchorMax = childTransform.anchorMax,
                Pivot = childTransform.pivot,
                Position = childTransform.anchoredPosition,
                SizeDelta = childTransform.sizeDelta
            });
        }
    }
}