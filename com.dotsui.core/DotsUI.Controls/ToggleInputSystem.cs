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
    class ToggleInputSystem : JobComponentSystem
    {
        private EntityQuery m_ToggleQuery;
        private PointerEventQuery m_EventQuery;
        protected override void OnCreate()
        {
            m_ToggleQuery = GetEntityQuery(typeof(Toggle));
            m_EventQuery = PointerEventQuery.Create<Toggle>(EntityManager);
        }

        struct UpdateToggle : IJobChunk
        {
            public ComponentDataFromEntity<Toggle> ToggleComponentFromEntity;
            public NativeHashMap<Entity, Entity> TargetToEvent;
            public InputEventReader<PointerInputBuffer> EventReader;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return inputDeps;
        }
    }
}
