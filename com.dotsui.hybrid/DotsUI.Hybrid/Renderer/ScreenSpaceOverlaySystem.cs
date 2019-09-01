using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotsUI.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace DotsUI.Hybrid.Renderer
{
    class GuiProxy : MonoBehaviour
    {
        public Action onRenderGui;

        void OnGUI()
        {
            if(Event.current.type == EventType.Repaint)
                onRenderGui?.Invoke();
        }
    }
    [UpdateInGroup(typeof(RenderSystemGroup))]
    [UpdateAfter(typeof(HybridRenderSystem))]
    class ScreenSpaceOverlaySystem : JobComponentSystem
    {
        private List<CommandBuffer> m_Buffers = new List<CommandBuffer>();
        private EntityQuery m_OverlayCanvasQuery;

        private GuiProxy m_Proxy;
        protected override void OnCreate()
        {
            //Canvas.willRenderCanvases += OnAfterFrameRender;
            m_OverlayCanvasQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<CanvasCommandBufferContainer>(),
                    ComponentType.ReadOnly<CanvasMeshContainer>(),
                    ComponentType.ReadOnly<CanvasSortLayer>(),
                    ComponentType.ReadOnly<RebuildCanvasHierarchyFlag>(),
                    ComponentType.ReadOnly<CanvasScreenSpaceOverlay>(),
                }
            });
            m_Proxy = new UnityEngine.GameObject("ScreenSpaceGUIProxy_DO_NOT_DESTROY").AddComponent<GuiProxy>();
            m_Proxy.onRenderGui = OnAfterFrameRender;
        }

        protected override void OnDestroy()
        {
            m_Buffers.Clear();
            //GameObject.Destroy(m_Proxy.gameObject);
        }

        private void OnAfterFrameRender()
        {
            for(int i = 0; i < m_Buffers.Count; i++)
            {
                //RenderTexture.active = null;
                Graphics.ExecuteCommandBuffer(m_Buffers[i]);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            m_Buffers.Clear();
            var chunkArray = m_OverlayCanvasQuery.CreateArchetypeChunkArray(Allocator.TempJob);
            var containerType = GetArchetypeChunkSharedComponentType<CanvasCommandBufferContainer>();
            for (int i = 0; i < chunkArray.Length; i++)
            {
                var commandBuffer = chunkArray[i].GetSharedComponentData(containerType, EntityManager);
                m_Buffers.Add(commandBuffer.Value);
            }
            chunkArray.Dispose();
            return inputDeps;
        }
    }
}
