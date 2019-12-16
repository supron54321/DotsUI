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
    public class PointerEventQuery
    {
        private EntityManager m_Manager;
        private ComponentType m_Type;
        private ControlsInputSystem m_ControlSystem;

        private PointerEventQuery(EntityManager manager, ComponentType type)
        {
            m_Manager = manager;
            m_Type = type;
            m_ControlSystem = manager.World.GetOrCreateSystem<ControlsInputSystem>();
        }

        public static PointerEventQuery Create<T>(EntityManager manager) where T : IComponentData
        {
            return new PointerEventQuery(manager, ComponentType.ReadOnly<T>());
        }

        public InputEventReader<PointerInputBuffer> CreatePointerEventReader(Allocator allocator)
        {
            var targetList = m_ControlSystem.QueryPointerEvents(m_Type, allocator);
            return new InputEventReader<PointerInputBuffer>(targetList, m_ControlSystem.GetPointerEventMap());
        }
    }
    public class KeyboardEventQuery
    {
        private EntityManager m_Manager;
        private ComponentType m_Type;
        private ControlsInputSystem m_ControlSystem;

        private KeyboardEventQuery(EntityManager manager, ComponentType type)
        {
            m_Manager = manager;
            m_Type = type;
            m_ControlSystem = manager.World.GetOrCreateSystem<ControlsInputSystem>();
        }

        public static KeyboardEventQuery Create<T>(EntityManager manager) where T : IComponentData
        {
            return new KeyboardEventQuery(manager, ComponentType.ReadOnly<T>());
        }

        public InputEventReader<KeyboardInputBuffer> CreateKeyboardEventReader(Allocator allocator)
        {
            var targetList = m_ControlSystem.QueryKeyboardEvents(m_Type, allocator);
            return new InputEventReader<KeyboardInputBuffer>(targetList, m_ControlSystem.GetKeyboardEventMap());
        }
    }

    public struct InputEventReader<T> : IDisposable where T : struct, IInputEvent
    {
        [ReadOnly]
        private NativeMultiHashMap<Entity, T> m_EventMap;
        [ReadOnly]
        private NativeList<Entity> m_Entities;
        public int EntityCount => m_Entities.Length;
        public Entity this[int idx] => m_Entities[idx];

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entities"> allocated in PointerEventQuery</param>
        /// <param name="eventMap"> persistent and shared between multiple queries</param>
        internal InputEventReader(NativeList<Entity> entities,
            NativeMultiHashMap<Entity, T> eventMap)
        {
            m_Entities = entities;
            m_EventMap = eventMap;
        }

        public NativeArray<Entity> GetParallelEntityReader()
        {
            return m_Entities.AsParallelReader();
        }
        public NativeMultiHashMap<Entity, T>.Enumerator GetEventsForTargetEntity(Entity targetEntity)
        {
            return m_EventMap.GetValuesForKey(targetEntity);
        }

        public void GetFirstEvent(Entity entity, out T item, out NativeMultiHashMapIterator<Entity> it)
        {
            m_EventMap.TryGetFirstValue(entity, out item, out it);
        }
        public bool TryGetNextEvent(out T item, ref NativeMultiHashMapIterator<Entity> it)
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
