using System;
using DotsUI.Profiling;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DotsUI.Core.Utils
{
    public struct AddFlagComponentCommandBuffer : IDisposable
    {
        [ReadOnly]
        private NativeQueue<Entity> m_EntityList;
        private ComponentType m_ComponentType;


        public AddFlagComponentCommandBuffer(ComponentType componentType, Allocator allocator)
        {
            m_EntityList = new NativeQueue<Entity>(allocator);
            m_ComponentType = componentType;
        }

        public ParallelWriter AsParallelWriter()
        {
            return new ParallelWriter(m_EntityList.AsParallelWriter());
        }

        public void Playback(EntityManager manager)
        {
            using (new ProfilerSample("AddFlagCommandBuffer.Playback"))
            {
                var asArray = new NativeArray<Entity>(m_EntityList.Count, Allocator.TempJob);
                for (int i = 0; i < asArray.Length; i++)
                    asArray[i] = m_EntityList.Dequeue();
                manager.AddComponent(asArray, m_ComponentType);
                asArray.Dispose();
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
            private NativeQueue<Entity>.ParallelWriter m_Writer;

            public ParallelWriter(NativeQueue<Entity>.ParallelWriter writer)
            {
                m_Writer = writer;
            }

            public void TryAdd(Entity entity)
            {
                m_Writer.Enqueue(entity);
            }
        }
    }
}
