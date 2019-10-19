//#define ENABLE_RECT_MASK
#define TEST
using DotsUI.Profiling;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace DotsUI.Core
{
    [UnityEngine.ExecuteAlways]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ElementMeshUpdateSystemGroup))]
    public class MeshBatchSystem : JobComponentSystem
    {
        private struct MaterialInfo
        {
            public SubMeshType Type;
            public int Id;
        }

        [BurstCompile(FloatMode = FloatMode.Fast)]
        private struct MeshBatching : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<ElementVertexPointerInMesh> VertexPointerFromEntity;
            [ReadOnly] public ComponentDataFromEntity<Disabled> DisabledFromEntity;
            [ReadOnly] public BufferFromEntity<Child> ChildFromEntity;
            [ReadOnly] public NativeHashMap<Entity, MaterialInfo> EntityToMaterial;


            [ReadOnly] public BufferFromEntity<ControlVertexData> VerticesFromEntity;
            [ReadOnly] public BufferFromEntity<ControlVertexIndex> TrianglesFromEntity;

            [NativeDisableContainerSafetyRestriction]
            [WriteOnly] public ArchetypeChunkBufferType<MeshVertex> VertexType;
            [NativeDisableContainerSafetyRestriction]
            [WriteOnly] public ArchetypeChunkBufferType<MeshVertexIndex> VertexIndexType;
            [NativeDisableContainerSafetyRestriction] [WriteOnly] public ArchetypeChunkBufferType<SubMeshInfo> SubMeshList;


            private int m_CurrentMaterialId;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entities = chunk.GetNativeArray(EntityType);
                var vertexAccessor = chunk.GetBufferAccessor(VertexType);
                var indexAccessor = chunk.GetBufferAccessor(VertexIndexType);
                var subMeshAccessor = chunk.GetBufferAccessor(SubMeshList);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var vertices = vertexAccessor[i];
                    var triangles = indexAccessor[i];
                    var subMeshes = subMeshAccessor[i];
                    vertices.Clear();
                    triangles.Clear();
                    subMeshes.Clear();
                    GoDownRoot(entities[i], ref vertices, ref triangles, ref subMeshes);
                }
            }
            private void GoDownRoot(Entity parent, ref DynamicBuffer<MeshVertex> vertices, ref DynamicBuffer<MeshVertexIndex> triangles, ref DynamicBuffer<SubMeshInfo> subMeshes)
            {
                if (ChildFromEntity.Exists(parent))
                {
                    var childern = ChildFromEntity[parent];
                    for (int i = 0; i < childern.Length; i++)
                    {
                        var child = childern[i].Value;
                        if (DisabledFromEntity.Exists(child))
                            continue;
                        if (VerticesFromEntity.Exists(child) && TrianglesFromEntity.Exists(child))
                        {
                            PopulateMesh(child, ref vertices, ref triangles, ref subMeshes);
                        }
                        GoDownRoot(child, ref vertices, ref triangles, ref subMeshes);
                    }
                }
            }

            private void PopulateMesh(Entity entity, ref DynamicBuffer<MeshVertex> vertices, ref DynamicBuffer<MeshVertexIndex> triangles, ref DynamicBuffer<SubMeshInfo> subMeshes)
            {
                bool materialAssigned = EntityToMaterial.TryGetValue(entity, out MaterialInfo material);
                if (!materialAssigned)
                {
                    material.Type = SubMeshType.SpriteImage;
                    material.Id = -1;
                }

                if (m_CurrentMaterialId != material.Id)
                {
                    subMeshes.Add(new SubMeshInfo()
                    {
                        Offset = triangles.Length,
                        MaterialId = material.Id,
                        MaterialType = material.Type
                    });
                    m_CurrentMaterialId = material.Id;
                }


                int startIndex = vertices.Length;
                if(VertexPointerFromEntity.Exists(entity))
                    VertexPointerFromEntity[entity] = new ElementVertexPointerInMesh(){VertexPointer = startIndex};
                Populate(entity, ref vertices, ref triangles, startIndex);
            }

            private void Populate(Entity entity, ref DynamicBuffer<MeshVertex> vertices, ref DynamicBuffer<MeshVertexIndex> triangles, int startIndex)
            {
                var textVertices = VerticesFromEntity[entity];
                var textIndices = TrianglesFromEntity[entity];

                for (int i = 0; i < textIndices.Length; i++)
                    triangles.Add(new MeshVertexIndex()
                    {
                        Value = textIndices[i].Value + startIndex
                    });
                vertices.AddRange(textVertices.Reinterpret<MeshVertex>().AsNativeArray());
            }
        }

        private EntityQuery m_RootGroup;
        private EntityQuery m_RendererGroup;

        private NativeHashMap<Entity, MaterialInfo> m_EntityToMaterialID;

        protected override void OnDestroy()
        {
            m_EntityToMaterialID.Dispose();
        }

        protected override void OnCreate()
        {
            m_EntityToMaterialID = new NativeHashMap<Entity, MaterialInfo>(10000, Allocator.Persistent);

            m_RootGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<RectTransform>(),
                    ComponentType.ReadOnly<Child>(),
                    ComponentType.ReadOnly<WorldSpaceRect>(),
                    ComponentType.ReadWrite<MeshVertex>(),
                    ComponentType.ReadWrite<MeshVertexIndex>(),
                    ComponentType.ReadOnly<RebuildCanvasHierarchyFlag>(),
                },
                None = new ComponentType[]
                {
                    typeof(Parent)
                },
                Options = EntityQueryOptions.FilterWriteGroup
            });
            m_RendererGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<WorldSpaceRect>(),
                },
                Any = new[]
                {
                    ComponentType.ReadOnly<TextRenderer>(),
                    ComponentType.ReadOnly<SpriteImage>(),
                },
                Options = EntityQueryOptions.FilterWriteGroup
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (m_RootGroup.CalculateEntityCount() < 1)
                return inputDeps;

            inputDeps = UpdateBatchIndices(inputDeps);

            var childFromEntity = GetBufferFromEntity<Child>(true);
            var verticesFromEntity = GetBufferFromEntity<ControlVertexData>(true);
            var trianglesFromEntity = GetBufferFromEntity<ControlVertexIndex>(true);

            using (new ProfilerSample("RenderSystem.BuildingChunks"))
            {
                MeshBatching chunkJob = new MeshBatching()
                {
                    EntityType = GetArchetypeChunkEntityType(),
                    ChildFromEntity = childFromEntity,
                    SubMeshList = GetArchetypeChunkBufferType<SubMeshInfo>(),
                    EntityToMaterial = m_EntityToMaterialID,
                    VerticesFromEntity = verticesFromEntity,
                    TrianglesFromEntity = trianglesFromEntity,
                    VertexPointerFromEntity = GetComponentDataFromEntity<ElementVertexPointerInMesh>(),
                    DisabledFromEntity = GetComponentDataFromEntity<Disabled>(true),

                    VertexType = GetArchetypeChunkBufferType<MeshVertex>(),
                    VertexIndexType = GetArchetypeChunkBufferType<MeshVertexIndex>(),
                };

                inputDeps = chunkJob.Schedule(m_RootGroup, inputDeps);
            }

            return inputDeps;
        }

        [BurstCompile]
        private struct EntityToMaterialJob : IJob
        {
            [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<ArchetypeChunk> Chunks;
            [ReadOnly] public ComponentDataFromEntity<SpriteVertexData> SpriteDataFromEntity;
            [ReadOnly] public ComponentDataFromEntity<TextFontAsset> FontAssetFromEntity;
            [ReadOnly] public ArchetypeChunkEntityType Entities;
            [ReadOnly] public ArchetypeChunkComponentType<SpriteImage> SpriteImageType;
            [ReadOnly] public ArchetypeChunkComponentType<TextRenderer> TextRendererType;
            public NativeHashMap<Entity, MaterialInfo> NativeToSprite;

            public void Execute()
            {
                for (int i = 0; i < Chunks.Length; i++)
                {
                    var chunk = Chunks[i];
                    var spriteArray = chunk.GetNativeArray(SpriteImageType);
                    var textArray = chunk.GetNativeArray(TextRendererType);
                    var entityArray = chunk.GetNativeArray(Entities);
                    int materialId = -1;
                    SubMeshType type = SubMeshType.SpriteImage;
                    if (chunk.Has(SpriteImageType))
                    {
                        type = SubMeshType.SpriteImage;

                        for (int j = 0; j < entityArray.Length; j++)
                        {
                            if (SpriteDataFromEntity.Exists(spriteArray[j].Asset))
                                materialId = SpriteDataFromEntity[spriteArray[j].Asset].NativeMaterialId;
                            else
                                materialId = -1;
                            NativeToSprite.TryAdd(entityArray[j], new MaterialInfo()
                            {
                                Id = materialId,
                                Type = type
                            });
                        }
                    }
                    else if (chunk.Has(TextRendererType))
                    {
                        type = SubMeshType.Text;

                        for (int j = 0; j < entityArray.Length; j++)
                        {
                            if (FontAssetFromEntity.Exists(textArray[j].Font))
                                materialId = FontAssetFromEntity[textArray[j].Font].NativeMaterialId;
                            else
                                materialId = -1;
                            NativeToSprite.TryAdd(entityArray[j], new MaterialInfo()
                            {
                                Id = materialId,
                                Type = type
                            });
                        }
                    }
                }
            }
        }
        private JobHandle UpdateBatchIndices(JobHandle inputDeps)
        {
            using (new ProfilerSample("UpdateBatchIndices"))
            {
                m_EntityToMaterialID.Clear();

                var chunkArray = m_RendererGroup.CreateArchetypeChunkArray(Allocator.TempJob);
                var entityType = GetArchetypeChunkEntityType();
                var spriteType = GetArchetypeChunkComponentType<SpriteImage>();
                var textType = GetArchetypeChunkComponentType<TextRenderer>();
                var spriteDataFromEntity = GetComponentDataFromEntity<SpriteVertexData>();
                //var textType = GetArchetypeChunkComponentType<TextRenderer>();

                EntityToMaterialJob materialBatchJob = new EntityToMaterialJob()
                {
                    Chunks = chunkArray,
                    Entities = entityType,
                    SpriteImageType = spriteType,
                    TextRendererType = textType,
                    FontAssetFromEntity = GetComponentDataFromEntity<TextFontAsset>(),
                    SpriteDataFromEntity = spriteDataFromEntity,
                    NativeToSprite = m_EntityToMaterialID
                };
                return materialBatchJob.Schedule(inputDeps);
            }
        }
    }
}