using System.Collections.Generic;
using DotsUI.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace DotsUI.Hybrid.Renderer
{
    [ExecuteAlways]
    [UpdateInGroup(typeof(RenderSystemGroup))]
    [UpdateAfter(typeof(HybridRenderSystem))]
    class ScreenSpaceOverlaySystem : JobComponentSystem
    {
        private List<CommandBuffer> m_Buffers = new List<CommandBuffer>();

        private ImguiProxy m_Proxy;
        private EntityQuery m_UninitializedCanvasQuery;
        private EntityQuery m_ScreenSpaceOverlayQuery;
        private EntityQuery m_DestroyedCanvasQuery;

        struct OverlayCanvasInitialized : ISystemStateComponentData
        {

        }
        protected override void OnCreate()
        {
            m_UninitializedCanvasQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<CanvasCommandBufferContainer>(),
                    ComponentType.ReadOnly<CanvasSortLayer>(),
                    ComponentType.ReadOnly<CanvasScreenSpaceOverlay>(),
                },
                None = new ComponentType[]
                {
                    typeof(OverlayCanvasInitialized)
                }
            });
            m_DestroyedCanvasQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    typeof(OverlayCanvasInitialized),
                },
                None = new ComponentType[]
                {
                    typeof(CanvasScreenSpaceOverlay),
                }
            });

            m_ScreenSpaceOverlayQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<CanvasCommandBufferContainer>(),
                    ComponentType.ReadOnly<CanvasSortLayer>(),
                    ComponentType.ReadOnly<CanvasScreenSpaceOverlay>(),
                }
            });

            RequireForUpdate(m_UninitializedCanvasQuery);
            InitializeRendererCallbacks();
        }

        private void InitializeRendererCallbacks()
        {
            if (Application.isPlaying)  // We cannot create IMGUI proxy in edit mode
            {
                if (GameObject.FindObjectOfType<ImguiProxy>() == null)
                {
                    m_Proxy = ImguiProxy.Create();
                    m_Proxy.OnRenderGui = OnRenderGui;
                }
            }
            RenderPipelineManager.endFrameRendering += OnSrpFinishedRendering;
        }

        private void OnSrpFinishedRendering(ScriptableRenderContext srpCtx, Camera[] cameras)
        {
            if(m_Proxy != null)
            {
                GameObject.Destroy(m_Proxy.gameObject);
                m_Proxy = null;
            }
            OnRenderGui();
        }

        protected override void OnDestroy()
        {
            m_Buffers.Clear();
            RenderPipelineManager.endFrameRendering -= OnSrpFinishedRendering;
            if(m_Proxy != null)
                GameObject.Destroy(m_Proxy.gameObject);
        }

        private void OnRenderGui()
        {
            for (int i = 0; i < m_Buffers.Count; i++)
            {
                Graphics.ExecuteCommandBuffer(m_Buffers[i]);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            m_Buffers.Clear();
            NativeArray<CanvasLayer> layerEntity;
            var canvasLayerIdFromEntity = GetComponentDataFromEntity<CanvasSortLayer>(true);
            using (var roots = m_ScreenSpaceOverlayQuery.ToEntityArray(Allocator.TempJob))
            {
                layerEntity = new NativeArray<CanvasLayer>(roots.Length, Allocator.TempJob);
                for (int i = 0; i < layerEntity.Length; i++)
                    layerEntity[i] = new CanvasLayer()
                    {
                        CanvasEntity = roots[i],
                        SortId = canvasLayerIdFromEntity[roots[i]].Value
                    };
                layerEntity.Sort();
            }
            for (int i = 0; i < layerEntity.Length; i++)
            {
                if (EntityManager.HasComponent(layerEntity[i].CanvasEntity,
                    typeof(CanvasCommandBufferContainer)))
                {
                    var commandBuffer = EntityManager.GetSharedComponentData<CanvasCommandBufferContainer>(layerEntity[i].CanvasEntity);
                    m_Buffers.Add(commandBuffer.Value);
                }
            }
            layerEntity.Dispose();
            EntityManager.AddComponent(m_UninitializedCanvasQuery, typeof(OverlayCanvasInitialized));
            EntityManager.RemoveComponent(m_DestroyedCanvasQuery, typeof(OverlayCanvasInitialized));
            return inputDeps;
        }
    }
}
