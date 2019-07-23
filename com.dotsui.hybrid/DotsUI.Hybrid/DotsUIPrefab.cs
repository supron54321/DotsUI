using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using DotsUI.Core;
using DotsUI.Profiling;

namespace DotsUI.Hybrid
{
    public struct DotsUIPrefab : IDisposable
    {
        RectTransformToEntity m_RectTransformToPrefabEntity;
        EntityManager m_Manager;
        Entity m_Root;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unityPrefab"></param>
        /// <param name="prefabWorld">This world will keep your converter prefab for further instantiation
        public DotsUIPrefab(UnityEngine.RectTransform unityPrefab, EntityManager manager, Allocator allocator)
        {
            m_RectTransformToPrefabEntity = new RectTransformToEntity(16, allocator);
            m_Manager = manager;
            m_Root = RectTransformConversionUtils.ConvertPrefabHierarchy(unityPrefab, manager, m_RectTransformToPrefabEntity);
            m_Manager.SetEnabled(m_Root, false);
        }

        public void Dispose()
        {
            m_RectTransformToPrefabEntity.Dispose();
            m_Manager.DestroyEntity(m_Root);
        }

        /// <summary>
        /// Simply instantiate prefab
        /// </summary>
        /// <returns></returns>
        public Entity Instantiate()
        {
            using(new ProfilerSample("DotsUIPrefab.Instantiate"))
            {
                var ret = m_Manager.Instantiate(m_Root);
                m_Manager.SetEnabled(ret, true);
                m_Manager.AddComponent(ret, typeof(DirtyElementFlag));
                return ret;
            }
        }

        /// <summary>
        /// Instantiate prefab and fill RectTransform to Entity map
        /// </summary>
        /// <param name="rectTransformToEntity"></param>
        /// <returns></returns>
        public Entity Instantiate(RectTransformToEntity rectTransformToEntity)
        {
            Entity instance = Instantiate();
            var prefabGroup = m_Manager.GetBuffer<LinkedEntityGroup>(m_Root);
            var instanceGroup = m_Manager.GetBuffer<LinkedEntityGroup>(instance);
            for(int i = 0; i < prefabGroup.Length; i++)
            {
                rectTransformToEntity.Add(m_RectTransformToPrefabEntity[prefabGroup[i].Value], instanceGroup[i].Value);
            }

            return instance;
        }
    }
}
