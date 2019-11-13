using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DotsUI.Input
{
    public class InputEventQuery
    {
        private EntityManager m_Manager;
        private ComponentType m_Type;
        private ControlsInputSystem m_ControlSystem;

        private InputEventQuery(EntityManager manager, ComponentType type)
        {
            m_Manager = manager;
            m_Type = type;
            m_ControlSystem = manager.World.GetOrCreateSystem<ControlsInputSystem>();
        }

        public static InputEventQuery Create<T>(EntityManager manager) where T : IComponentData
        {
            return new InputEventQuery(manager, ComponentType.ReadOnly<T>());
        }

        public PointerInputEventReader CreateEventReader(Allocator allocator)
        {
            var targetList = m_ControlSystem.QueryPointerEvents(m_Type, allocator);
            return new PointerInputEventReader(targetList, m_ControlSystem.GetEventMap());
        }
    }

    public struct PointerInputEventReader : IDisposable
    {
        [ReadOnly]
        private NativeMultiHashMap<Entity, PointerInputBuffer> m_EventMap;
        [ReadOnly]
        private NativeList<Entity> m_Entities;
        public int EntityCount => m_Entities.Length;
        public Entity this[int idx] => m_Entities[idx];

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entities"> allocated in InputEventQuery</param>
        /// <param name="eventMap"> persistent and shared between multiple queries</param>
        internal PointerInputEventReader(NativeList<Entity> entities,
            NativeMultiHashMap<Entity, PointerInputBuffer> eventMap)
        {
            m_Entities = entities;
            m_EventMap = eventMap;
        }

        public NativeArray<Entity> GetParallelEntityReader()
        {
            return m_Entities.AsParallelReader();
        }
        public NativeMultiHashMap<Entity, PointerInputBuffer>.Enumerator GetEventsForTargetEntity(Entity targetEntity)
        {
            return m_EventMap.GetValuesForKey(targetEntity);
        }

        public void GetFirstEvent(Entity entity, out PointerInputBuffer item, out NativeMultiHashMapIterator<Entity> it)
        {
            m_EventMap.TryGetFirstValue(entity, out item, out it);
        }
        public bool TryGetNextEvent(out PointerInputBuffer item, ref NativeMultiHashMapIterator<Entity> it)
        {
            return m_EventMap.TryGetNextValue(out item, ref it);
        }

        public void Dispose()
        {
            m_Entities.Dispose();
        }
        public JobHandle Dispose(JobHandle inputDeps)
        {
            return m_Entities.Dispose(inputDeps);
        }
    }
}
