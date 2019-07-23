using System;
using Unity.Collections;
using Unity.Entities;

namespace DotsUI.Hybrid
{
    public struct RectTransformToEntity : IDisposable
    {
        NativeHashMap<int, Entity> m_InstanceIdToEntity;
        NativeHashMap<Entity, int> m_EntityToInstanceId;
        public RectTransformToEntity(int capacity, Allocator allocator)
        {
            m_InstanceIdToEntity = new NativeHashMap<int, Entity>(capacity, allocator);
            m_EntityToInstanceId = new NativeHashMap<Entity, int>(capacity, allocator);
        }

        public Entity this[UnityEngine.RectTransform transform]
        {
            get
            {
                int instanceId = transform.GetInstanceID();
                return m_InstanceIdToEntity[instanceId];
            }
        }
        public int this[Entity entity]
        {
            get
            {
                return m_EntityToInstanceId[entity];
            }
        }

        public void Add(UnityEngine.RectTransform transform, Entity entity)
        {
            int instanceId = transform.GetInstanceID();
            m_InstanceIdToEntity.TryAdd(instanceId, entity);
            m_EntityToInstanceId.TryAdd(entity, instanceId);
        }
        public void Add(int transformInstanceId, Entity entity)
        {
            m_InstanceIdToEntity.TryAdd(transformInstanceId, entity);
            m_EntityToInstanceId.TryAdd(entity, transformInstanceId);
        }

        internal void AddNew(Entity caret)
        {
            int dummyId = -m_InstanceIdToEntity.Length;
            m_InstanceIdToEntity.TryAdd(dummyId, caret);
            m_EntityToInstanceId.TryAdd(caret, dummyId);
        }

        public bool TryGetValue(UnityEngine.RectTransform transform, out Entity entity)
        {
            entity = default;
            if (ReferenceEquals(transform, null))
                return false;
            return m_InstanceIdToEntity.TryGetValue(transform.GetInstanceID(), out entity);
        }
        public bool TryGetValue(Entity entity, out int rectTransformInstanceId)
        {
            return m_EntityToInstanceId.TryGetValue(entity, out rectTransformInstanceId);
        }

        public void Clear()
        {
            m_InstanceIdToEntity.Clear();
            m_EntityToInstanceId.Clear();
        }

        public void Dispose()
        {
            m_InstanceIdToEntity.Dispose();
            m_EntityToInstanceId.Dispose();
        }

        public NativeArray<int> GetKeyArray(Allocator allocator = Allocator.Temp)
        {
            return m_InstanceIdToEntity.GetKeyArray(allocator);
        }
    }
}