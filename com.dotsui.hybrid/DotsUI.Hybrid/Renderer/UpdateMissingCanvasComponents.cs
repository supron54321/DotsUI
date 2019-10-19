using DotsUI.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;

namespace DotsUI.Hybrid
{
    [UnityEngine.ExecuteAlways]
    [UpdateInGroup(typeof(BeforeRectTransformUpdateGroup))]
    class UpdateMissingCanvasComponents : ComponentSystem
    {
        private EntityQuery m_MissingMesh;
        private EntityQuery m_MissingCommandBuffer;

        protected override void OnCreate()
        {
            m_MissingMesh = GetEntityQuery(new EntityQueryDesc()
            {
                Any = new[]
                {
                    ComponentType.ReadOnly<CanvasTargetCamera>(),
                    ComponentType.ReadOnly<CanvasTargetRenderTexture>(),
                    ComponentType.ReadOnly<CanvasScreenSpaceOverlay>(), 
                },
                None = new[]
                {
                    ComponentType.ReadWrite<CanvasMeshContainer>(),
                },
            });
            m_MissingCommandBuffer = GetEntityQuery(new EntityQueryDesc()
            {
                Any = new[]
                {
                    ComponentType.ReadOnly<CanvasTargetCamera>(),
                    ComponentType.ReadOnly<CanvasTargetRenderTexture>(),
                    ComponentType.ReadOnly<CanvasScreenSpaceOverlay>(),
                },
                None = new[]
                {
                    ComponentType.ReadWrite<CanvasCommandBufferContainer>(),
                },
            });
        }

        protected override void OnUpdate()
        {
            using (EntityCommandBuffer cmdBuff = new EntityCommandBuffer(Allocator.TempJob))
            {
                using (var missingCommandBuff = m_MissingCommandBuffer.ToEntityArray(Allocator.TempJob))
                {
                    for (int i = 0; i < missingCommandBuff.Length; i++)
                        cmdBuff.AddSharedComponent(missingCommandBuff[i], new CanvasCommandBufferContainer()
                        {
                            Value = new CommandBuffer(),
                        });
                }

                using (var missingMesh = m_MissingMesh.ToEntityArray(Allocator.TempJob))
                {
                    for (int i = 0; i < missingMesh.Length; i++)
                    {
                        Mesh newMesh = new Mesh();
                        newMesh.MarkDynamic();
                        newMesh.indexFormat = IndexFormat.UInt32;
                        cmdBuff.AddSharedComponent(missingMesh[i], new CanvasMeshContainer()
                        {
                            UnityMesh = newMesh
                        });
                    }
                }
                cmdBuff.Playback(EntityManager);
            }

        }


    }
}
