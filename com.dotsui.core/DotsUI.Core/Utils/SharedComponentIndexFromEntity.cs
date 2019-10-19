using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Unity.Entities
{
//    [NativeContainer]
//    public unsafe struct SharedComponentIndexFromEntity<T> where T : struct, ISharedComponentData
//    {
//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//        readonly AtomicSafetyHandle m_Safety;
//#endif
//        [NativeDisableUnsafePtrRestriction]
//        readonly EntityComponentStore* m_EntityComponentStore;
//        readonly int m_TypeIndex;
//        readonly uint m_GlobalSystemVersion;
//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//        readonly bool m_IsZeroSized;          // cache of whether T is zero-sized
//#endif
//        int m_TypeLookupCache;

//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//        internal SharedComponentIndexFromEntity(int typeIndex, EntityComponentStore* entityComponentStoreComponentStore, AtomicSafetyHandle safety)
//        {
//            m_Safety = safety;
//            m_TypeIndex = typeIndex;
//            m_EntityComponentStore = entityComponentStoreComponentStore;
//            m_GlobalSystemVersion = entityComponentStoreComponentStore->GlobalSystemVersion;
//            m_IsZeroSized = ComponentType.FromTypeIndex(typeIndex).IsZeroSized;
//        }
//#else
//        internal SharedComponentIndexFromEntity(int typeIndex, EntityComponentStore* entityComponentStoreComponentStore)
//        {
//            m_TypeIndex = typeIndex;
//            m_EntityComponentStore = entityComponentStoreComponentStore;
//            m_GlobalSystemVersion = entityComponentStoreComponentStore->GlobalSystemVersion;
//        }
//#endif
//        public bool Exists(Entity entity)
//        {
//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
//#endif
//            return m_EntityComponentStore->HasComponent(entity, m_TypeIndex);
//        }
//        public bool HasComponent(Entity entity)
//        {
//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
//#endif
//            return m_EntityComponentStore->HasComponent(entity, m_TypeIndex);
//        }
//        public int this[Entity entity]
//        {
//            get
//            {
//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
//#endif
//                m_EntityComponentStore->AssertEntityHasComponent(entity, m_TypeIndex);
//                return m_EntityComponentStore->GetSharedComponentDataIndex(entity, m_TypeIndex);
//            }
//        }
//    }
}
