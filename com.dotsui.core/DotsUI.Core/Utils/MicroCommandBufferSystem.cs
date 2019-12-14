using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace DotsUI.Core.Utils
{
    public class MicroCommandBufferSystem : EntityCommandBufferSystem
    {
        private List<FlagComponentCommandBuffer> m_AddFlagBuffers = new List<FlagComponentCommandBuffer>(32);
        
        public FlagComponentCommandBuffer CreateFlagComponentCommandBuffer<T>() where T : IComponentData
        {
            var flagBuffer = new FlagComponentCommandBuffer(typeof(T), Allocator.TempJob);
            m_AddFlagBuffers.Add(flagBuffer);
            return flagBuffer;
        }
        protected override void OnCreate()
        {
            base.OnCreate();
            //m_AddFlagBuffers = new NativeList<FlagComponentCommandBuffer>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            //m_AddFlagBuffers.Dispose();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            for(int i = 0; i < m_AddFlagBuffers.Count; i++)
            {
                m_AddFlagBuffers[i].Playback(EntityManager);
                m_AddFlagBuffers[i].Dispose();
            }
            m_AddFlagBuffers.Clear();
        }
    }
}
