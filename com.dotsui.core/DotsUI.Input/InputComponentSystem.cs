using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DotsUI.Core;

namespace DotsUI.Input
{
    public abstract class PointerInputComponentSystem<T> : JobComponentSystem where T : struct, IComponentData
    {
        private EntityQuery m_PointerEventQuery;
        private NativeHashMap<Entity, Entity> m_TargetToEvent;
        protected sealed override void OnCreate()
        {
            m_PointerEventQuery = GetEntityQuery(ComponentType.ReadOnly<PointerEvent>(), ComponentType.ReadOnly<PointerInputBuffer>());
            m_TargetToEvent = new NativeHashMap<Entity, Entity>(10, Allocator.Persistent);
            RequireForUpdate(m_PointerEventQuery);
            OnCreateInput();
        }

        protected sealed override void OnDestroy()
        {
            m_TargetToEvent.Dispose();
            OnDestroyInput();
        }

        [BurstCompile]
        struct CreateTargetToEventMap : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkComponentType<PointerEvent> EventType;
            [ReadOnly] public ComponentDataFromEntity<T> TargetType;
            [WriteOnly] public NativeHashMap<Entity, Entity>.ParallelWriter TargetToEvent;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);
                var eventArray = chunk.GetNativeArray(EventType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    if (TargetType.Exists(eventArray[i].Target))
                        TargetToEvent.TryAdd(eventArray[i].Target, entityArray[i]);
                }
            }
        }
        protected sealed override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (m_PointerEventQuery.CalculateEntityCount() > 0)
            {
                ClearHashMap<Entity, Entity> clearContainer = new ClearHashMap<Entity, Entity>()
                {
                    Container = m_TargetToEvent
                };
                inputDeps = clearContainer.Schedule(inputDeps);
                CreateTargetToEventMap createMap = new CreateTargetToEventMap()
                {
                    EntityType = GetArchetypeChunkEntityType(),
                    EventType = GetArchetypeChunkComponentType<PointerEvent>(true),
                    TargetToEvent = m_TargetToEvent.AsParallelWriter(),
                    TargetType = GetComponentDataFromEntity<T>(true)
                };
                inputDeps = createMap.Schedule(m_PointerEventQuery, inputDeps);
                inputDeps = OnUpdateInput(inputDeps, m_TargetToEvent, GetBufferFromEntity<PointerInputBuffer>(true));
            }
            return inputDeps;
        }


        protected abstract JobHandle OnUpdateInput(JobHandle inputDeps, NativeHashMap<Entity, Entity> targetToEvent,
            BufferFromEntity<PointerInputBuffer> pointerBufferFromEntity);

        protected virtual void OnCreateInput()
        {
        }

        protected virtual void OnDestroyInput()
        {
        }
    }
}
