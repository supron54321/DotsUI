using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotsUI.Input;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DotsUI.Controls
{
    class ToggleInputSystem : PointerInputComponentSystem<Toggle>
    {
        private EntityQuery m_ToggleQuery;
        protected override void OnCreateInput()
        {
            m_ToggleQuery = GetEntityQuery(typeof(Toggle));
        }

        struct UpdateToggle : IJobChunk
        {
            public ArchetypeChunkEntityType EntityType;
            public ArchetypeChunkComponentType<Toggle> ToggleType;
            public ComponentDataFromEntity<Toggle> ToggleComponent;
            public NativeHashMap<Entity, Entity> TargetToEvent;
            public BufferFromEntity<PointerInputBuffer> PointerBufferFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                NativeArray<Entity> entityArray = chunk.GetNativeArray(EntityType);
                NativeArray<Toggle> toggleArray = chunk.GetNativeArray(ToggleType);
                for (int i = 0; i < entityArray.Length; i++)
                {
                    if (TargetToEvent.TryGetValue(entityArray[i], out var entity))
                    {
                    }
                }
            }
        }

        protected override JobHandle OnUpdateInput(JobHandle inputDeps, NativeHashMap<Entity, Entity> targetToEvent, BufferFromEntity<PointerInputBuffer> pointerBufferFromEntity)
        {
            return inputDeps;
        }
    }
}
