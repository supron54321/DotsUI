using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotsUI.Profiling;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace DotsUI.Core.Utils
{
    public struct AddComponentDataCommandBuffer<T> : IDisposable where T : struct, IComponentData
    {
        [ReadOnly]
        private NativeQueue<(Entity, T)> m_EntityList;

        public AddComponentDataCommandBuffer(Allocator allocator)
        {
            m_EntityList = new NativeQueue<(Entity, T)>(allocator);
        }

        public ParallelWriter AsParallelWriter()
        {
            return new ParallelWriter(m_EntityList.AsParallelWriter());
        }

        public bool IsCreated => m_EntityList.IsCreated;

        public void Playback(EntityManager manager)
        {
            using (new ProfilerSample("AddFlagCommandBuffer.Playback"))
            {
                while (m_EntityList.TryDequeue(out var entityData))
                {
                    manager.AddComponentData(entityData.Item1, entityData.Item2);
                }
            }
        }

        public void Dispose()
        {
            m_EntityList.Dispose();
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            return m_EntityList.Dispose(inputDeps);
        }

        public struct ParallelWriter
        {
            private NativeQueue<(Entity, T)>.ParallelWriter m_Writer;

            public ParallelWriter(NativeQueue<(Entity, T)>.ParallelWriter writer)
            {
                m_Writer = writer;
            }

            public void TryAdd(Entity entity, T data)
            {
                m_Writer.Enqueue((entity, data));
            }
        }
    }
}
