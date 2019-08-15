using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Entities;
using DotsUI.Core;
using DotsUI.Profiling;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace DotsUI.Hybrid
{
    [UpdateInGroup(typeof(RenderSystemGroup))]
    class HybridRenderSystem : ComponentSystem
    {
        private EntityQuery m_UpdateMeshAndCommandBufferGroup;
        private EntityQuery m_UpdateVerticesOnlyGroup;
        private EntityQuery m_AnyTarget;
        private Material m_DefaultMaterial;
        private MaterialPropertyBlock m_TemporaryBlock = new MaterialPropertyBlock();
        private VertexAttributeDescriptor[] m_MeshDescriptors;


        private struct CanvasLayer : IComparable<CanvasLayer>
        {
            public int SortId;
            public Entity CanvasEntity;
            public int CompareTo(CanvasLayer other)
            {
                return SortId.CompareTo(other.SortId);
            }
        }


        protected override void OnCreate()
        {
            m_UpdateMeshAndCommandBufferGroup = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<MeshVertex>(),
                    ComponentType.ReadOnly<MeshVertexIndex>(),
                    ComponentType.ReadOnly<SubMeshInfo>(),
                    ComponentType.ReadOnly<CanvasCommandBufferContainer>(),
                    ComponentType.ReadOnly<CanvasMeshContainer>(),
                    ComponentType.ReadOnly<CanvasSortLayer>(),
                    ComponentType.ReadOnly<RebuildCanvasHierarchyFlag>(),
                },
                Any = new ComponentType[]
                {
                    ComponentType.ReadOnly<CanvasTargetCamera>(),
                    ComponentType.ReadOnly<CanvasTargetRenderTexture>(),
                }
            });
            m_UpdateVerticesOnlyGroup = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<MeshVertex>(),
                    ComponentType.ReadOnly<MeshVertexIndex>(),
                    ComponentType.ReadOnly<SubMeshInfo>(),
                    ComponentType.ReadOnly<CanvasCommandBufferContainer>(),
                    ComponentType.ReadOnly<CanvasMeshContainer>(),
                    ComponentType.ReadOnly<CanvasSortLayer>(),
                    ComponentType.ReadOnly<UpdateCanvasVerticesFlag>(),
                },
                Any = new ComponentType[]
                {
                    ComponentType.ReadOnly<CanvasTargetCamera>(),
                    ComponentType.ReadOnly<CanvasTargetRenderTexture>(),
                },
                None = new ComponentType[]
                {
                    ComponentType.ReadOnly<RebuildCanvasHierarchyFlag>(),
                }
            });
            m_AnyTarget = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<CanvasCommandBufferContainer>(),
                },
                Any = new ComponentType[]
                {
                    ComponentType.ReadOnly<CanvasTargetCamera>(),
                    ComponentType.ReadOnly<CanvasTargetRenderTexture>(),
                }
            });
            m_DefaultMaterial = Material.Instantiate(Resources.Load<Material>("DotsUIDefaultMaterial"));
            m_MeshDescriptors = new VertexAttributeDescriptor[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
                new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 0),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4, 0),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 0),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 2, 0),
            };
        }

        protected override void OnUpdate()
        {
            if (m_UpdateMeshAndCommandBufferGroup.CalculateEntityCount() < 1 && m_UpdateVerticesOnlyGroup.CalculateEntityCount() < 1)
                return;

            if (m_UpdateMeshAndCommandBufferGroup.CalculateEntityCount() > 0)
            {
                RebuildMeshAndCommandBuffers();

                // Now sort layers:
                NativeArray<CanvasLayer> layerEntity;
                var canvasLayerIdFromEntity = GetComponentDataFromEntity<CanvasSortLayer>(true);
                using (var roots = m_AnyTarget.ToEntityArray(Allocator.TempJob))
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
                NativeMultiHashMap<int, int> cameraIdToLayerIdx = new NativeMultiHashMap<int, int>(layerEntity.Length, Allocator.Temp);
                for (int i = 0; i < layerEntity.Length; i++)
                {
                    if (EntityManager.HasComponent(layerEntity[i].CanvasEntity,
                        typeof(CanvasTargetCamera)))
                    {
                        var camera = EntityManager.GetSharedComponentData<CanvasTargetCamera>(layerEntity[i].CanvasEntity);
                        camera.Target.InjectedCommandBuffers.Clear();
                        cameraIdToLayerIdx.Add(camera.Target.GetInstanceID(), i);
                    }
                }

                for (int i = 0; i < layerEntity.Length; i++)
                {
                    var entity = layerEntity[i].CanvasEntity;
                    if (EntityManager.HasComponent(entity, typeof(CanvasTargetCamera)))
                    {
                        var camera = EntityManager.GetSharedComponentData<CanvasTargetCamera>(entity);
                        camera.Target.InjectedCommandBuffers.Add(EntityManager.GetSharedComponentData<CanvasCommandBufferContainer>(entity).Value);
                    }
                    else if (EntityManager.HasComponent(entity, typeof(CanvasTargetRenderTexture)))
                    {

                    }
                }
                layerEntity.Dispose();
                EntityManager.RemoveComponent<RebuildCanvasHierarchyFlag>(m_UpdateMeshAndCommandBufferGroup);
            }

            if (m_UpdateVerticesOnlyGroup.CalculateEntityCount() > 0)
            {
                UpdateVerticesOnly();
                EntityManager.RemoveComponent<UpdateCanvasVerticesFlag>(m_UpdateVerticesOnlyGroup);
            }
        }

        private void RebuildMeshAndCommandBuffers()
        {
            using (var chunkArray = m_UpdateMeshAndCommandBufferGroup.CreateArchetypeChunkArray(Allocator.TempJob))
            {
                var commandBufferType = GetArchetypeChunkSharedComponentType<CanvasCommandBufferContainer>();
                var meshType = GetArchetypeChunkSharedComponentType<CanvasMeshContainer>();

                var vertexBufferType = GetArchetypeChunkBufferType<MeshVertex>();
                var vertexIndexBufferType = GetArchetypeChunkBufferType<MeshVertexIndex>();
                var subMeshBufferType = GetArchetypeChunkBufferType<SubMeshInfo>();

                var entityType = GetArchetypeChunkEntityType();


                for (int i = 0; i < chunkArray.Length; i++)
                {
                    var chunk = chunkArray[i];

                    if (chunk.Count > 1)
                    {
                        Debug.LogError($"One archetype can contain only one canvas.");
                        continue;
                    }

                    var entity = chunk.GetNativeArray(entityType)[0];
                    var commandBuffer = chunk.GetSharedComponentData(commandBufferType, EntityManager);
                    var mesh = chunk.GetSharedComponentData(meshType, EntityManager);
                    var vertices = chunk.GetBufferAccessor(vertexBufferType)[0];
                    var indices = chunk.GetBufferAccessor(vertexIndexBufferType)[0];
                    var subMeshes = chunk.GetBufferAccessor(subMeshBufferType)[0];

                    BuildCommandBuffer(vertices, indices, subMeshes, mesh.UnityMesh, commandBuffer.Value);
                }
            }
        }

        private void UpdateVerticesOnly()
        {
            using (var chunkArray = m_UpdateVerticesOnlyGroup.CreateArchetypeChunkArray(Allocator.TempJob))
            {
                var meshType = GetArchetypeChunkSharedComponentType<CanvasMeshContainer>();

                var vertexBufferType = GetArchetypeChunkBufferType<MeshVertex>();


                for (int i = 0; i < chunkArray.Length; i++)
                {
                    var chunk = chunkArray[i];

                    if (chunk.Count > 1)
                    {
                        Debug.LogError($"One archetype can contain only one canvas.");
                        continue;
                    }

                    var mesh = chunk.GetSharedComponentData(meshType, EntityManager);
                    var vertices = chunk.GetBufferAccessor(vertexBufferType)[0];

                    //mesh.UnityMesh.SetVertexBufferParams(vertices.Length, m_MeshDescriptors[0], m_MeshDescriptors[1], m_MeshDescriptors[2], m_MeshDescriptors[3], m_MeshDescriptors[4]);
                    mesh.UnityMesh.SetVertexBufferData(vertices.AsNativeArray(), 0, 0, vertices.Length, 0);
                    mesh.UnityMesh.UploadMeshData(false);
                }
            }
        }

        private void BuildCommandBuffer(DynamicBuffer<MeshVertex> vertexArray, DynamicBuffer<MeshVertexIndex> indexArray, DynamicBuffer<SubMeshInfo> subMeshArray, Mesh unityMesh, CommandBuffer canvasCommandBuffer)
        {
            using (new ProfilerSample("RenderSystem.SetVertexBuffer"))
            {
                unityMesh.Clear(true);
                unityMesh.SetVertexBufferParams(vertexArray.Length, m_MeshDescriptors[0], m_MeshDescriptors[1], m_MeshDescriptors[2], m_MeshDescriptors[3], m_MeshDescriptors[4]);
            }
            using (new ProfilerSample("UploadMesh"))
            {
                unityMesh.SetVertexBufferData(vertexArray.AsNativeArray(), 0, 0, vertexArray.Length, 0);
                unityMesh.SetIndexBufferParams(indexArray.Length, IndexFormat.UInt32);
                unityMesh.SetIndexBufferData(indexArray.AsNativeArray(), 0, 0, indexArray.Length);
                unityMesh.subMeshCount = subMeshArray.Length;
                for (int i = 0; i < subMeshArray.Length; i++)
                {
                    var subMesh = subMeshArray[i];
                    var descr = new SubMeshDescriptor()
                    {
                        baseVertex = 0,
                        bounds = default,
                        firstVertex = 0,
                        indexCount = i < subMeshArray.Length - 1
                            ? subMeshArray[i + 1].Offset - subMesh.Offset
                            : indexArray.Length - subMesh.Offset,
                        indexStart = subMesh.Offset,
                        topology = MeshTopology.Triangles,
                        vertexCount = vertexArray.Length
                    };
                    unityMesh.SetSubMesh(i, descr);
                }
                unityMesh.UploadMeshData(false);
            }

            using (new ProfilerSample("BuildCommangBuffer"))
            {
                canvasCommandBuffer.Clear();
                //canvasCommandBuffer.ClearRenderTarget(true, true, UnityEngine.Color.black);
                canvasCommandBuffer.SetProjectionMatrix(Matrix4x4.Ortho(0.0f, Screen.width, 0.0f, Screen.height, -100.0f, 100.0f));
                canvasCommandBuffer.SetViewMatrix(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one));
                for (int i = 0; i < unityMesh.subMeshCount; i++)
                {
                    var subMesh = subMeshArray[i];
                    var renderMaterial = SetMaterial(ref subMesh);
                    canvasCommandBuffer.DrawMesh(unityMesh, float4x4.identity, renderMaterial, i, -1, m_TemporaryBlock);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Material SetMaterial(ref SubMeshInfo subMesh)
        {
            using (new ProfilerSample("SetMaterial"))
            {
                if (subMesh.MaterialType == SubMeshType.SpriteImage)
                {
                    Texture2D spriteTex = Texture2D.whiteTexture;
                    if (subMesh.MaterialId != -1)
                    {
                        var sprite = EntityManager.GetSharedComponentData<SpriteAsset>(subMesh.MaterialId).Value;
                        if (sprite != null)
                            spriteTex = sprite.texture;
                    }

                    m_TemporaryBlock.SetTexture("_MainTex", spriteTex);
                    return m_DefaultMaterial;
                }
                else if (subMesh.MaterialType == SubMeshType.Text)
                {
                    var fontMaterial = EntityManager.GetSharedComponentData<LegacyTextFontMaterial>(subMesh.MaterialId).FontMaterial;
                    m_TemporaryBlock.SetTexture("_MainTex", fontMaterial.mainTexture);
                    return fontMaterial;
                }
                else
                {
                    return m_DefaultMaterial;
                }
            }
        }
    }
}
