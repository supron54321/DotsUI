using System;
using DotsUI.Profiling;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DotsUI.Core.Utils
{
    public struct FlagComponentCommandBuffer : IDisposable
    {
        private NativeQueue<Entity> m_AddList;
        private NativeQueue<Entity> m_RemoveList;
        private ComponentType m_ComponentType;

        public FlagComponentCommandBuffer(ComponentType componentType, Allocator allocator)
        {
            m_AddList = new NativeQueue<Entity>(allocator);
            m_RemoveList = new NativeQueue<Entity>(allocator);
            m_ComponentType = componentType;
        }

        public ParallelWriter AsParallelWriter()
        {
            return new ParallelWriter(m_AddList.AsParallelWriter(), m_RemoveList.AsParallelWriter());
        }

        public void Playback(EntityManager manager)
        {
            using (new ProfilerSample("AddFlagCommandBuffer.Playback"))
            {
                if(m_AddList.Count > 0)
                {
                    var asArray = new NativeArray<Entity>(m_AddList.Count, Allocator.TempJob);
                    for (int i = 0; i < asArray.Length; i++)
                        asArray[i] = m_AddList.Dequeue();
                    manager.AddComponent(asArray, m_ComponentType);
                    asArray.Dispose();
                }

                if(m_RemoveList.Count > 0)
                {
                    var asArray = new NativeArray<Entity>(m_RemoveList.Count, Allocator.TempJob);
                    for (int i = 0; i < asArray.Length; i++)
                        asArray[i] = m_RemoveList.Dequeue();
                    manager.RemoveComponent(asArray, m_ComponentType);
                    asArray.Dispose();
                }
            }
        }

        public void Dispose()
        {
            m_AddList.Dispose();
            m_RemoveList.Dispose();
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            return m_AddList.Dispose(inputDeps);
        }

        public struct ParallelWriter
        {
            private NativeQueue<Entity>.ParallelWriter m_AddWriter;
            private NativeQueue<Entity>.ParallelWriter m_RemoveWriter;

            internal ParallelWriter(NativeQueue<Entity>.ParallelWriter addWriter, NativeQueue<Entity>.ParallelWriter removeWriter)
            {
                m_AddWriter = addWriter;
                m_RemoveWriter = removeWriter;
            }

            public void TryAdd(Entity entity)
            {
                m_AddWriter.Enqueue(entity);
            }
            public void TryRemove(Entity entity)
            {
                m_RemoveWriter.Enqueue(entity);
            }
        }
    }
}
