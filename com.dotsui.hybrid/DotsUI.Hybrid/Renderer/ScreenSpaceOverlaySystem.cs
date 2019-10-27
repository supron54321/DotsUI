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
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(RenderSystemGroup))]
    [UpdateAfter(typeof(HybridRenderSystem))]
    class ScreenSpaceOverlaySystem : JobComponentSystem
    {
        private List<CommandBuffer> m_Buffers = new List<CommandBuffer>();
        private EntityQuery m_DirtyCanvasQuery;

        private ImguiProxy m_Proxy;
        private EntityQuery m_UnitializedCanvasQuery;
        private EntityQuery m_ScreenSpaceOverlayQuery;
        private EntityQuery m_DestroyedCanvasQuery;

        struct OverlayCanvasInitialized : ISystemStateComponentData
        {

        }
        protected override void OnCreate()
        {
            m_DirtyCanvasQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<CanvasCommandBufferContainer>(),
                    ComponentType.ReadOnly<CanvasSortLayer>(),
                    ComponentType.ReadOnly<RebuildCanvasHierarchyFlag>(),
                    ComponentType.ReadOnly<CanvasScreenSpaceOverlay>(),
                }
            });
            m_UnitializedCanvasQuery = GetEntityQuery(new EntityQueryDesc()
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

            RequireForUpdate(m_UnitializedCanvasQuery);
            m_Proxy = GameObject.FindObjectOfType<ImguiProxy>();
            if (m_Proxy == null)
            {
                m_Proxy = new UnityEngine.GameObject("ScreenSpaceOverlayGUIProxy_DO_NOT_DESTROY").AddComponent<ImguiProxy>();
                GameObject.DontDestroyOnLoad(m_Proxy.gameObject);
                m_Proxy.OnRenderGui = OnRenderGui;
            }
        }

        protected override void OnDestroy()
        {
            m_Buffers.Clear();
            if (m_Proxy != null)
            {
                if(Application.isPlaying)
                    GameObject.Destroy(m_Proxy.gameObject);
                else
                    GameObject.DestroyImmediate(m_Proxy.gameObject);
            }
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
            EntityManager.AddComponent(m_UnitializedCanvasQuery, typeof(OverlayCanvasInitialized));
            EntityManager.RemoveComponent(m_DestroyedCanvasQuery, typeof(OverlayCanvasInitialized));
            return inputDeps;
        }
    }
}
