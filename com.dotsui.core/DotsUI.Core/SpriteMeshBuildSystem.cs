using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DotsUI.Core
{
    [UnityEngine.ExecuteAlways]
    [UpdateInGroup(typeof(ElementMeshUpdateSystemGroup))]
    internal class SpriteMeshBuildSystem : JobComponentSystem
    {
        private EntityQuery m_SpriteGroup;

        protected override void OnCreate()
        {
            m_SpriteGroup = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<SpriteImage>(),
                    ComponentType.ReadWrite<ElementVertexData>(),
                    ComponentType.ReadWrite<ElementVertexIndex>(),
                    ComponentType.ReadOnly<RebuildElementMeshFlag>(),
                    ComponentType.ReadOnly<WorldSpaceRect>(),
                }
            });
            m_SpriteGroup.SetChangedVersionFilter(typeof(WorldSpaceRect));
        }

        [BurstCompile]
        struct SpriteBatchJob : IJobChunk
        {
            //[ReadOnly] public NativeHashMap<int, SpriteVertexData> SpriteData;
            [ReadOnly] public ComponentDataFromEntity<SpriteVertexData> SpriteDataFromEntity;
            public ArchetypeChunkBufferType<ElementVertexData> VertexDataType;
            public ArchetypeChunkBufferType<ElementVertexIndex> VertexIndexType;
            [ReadOnly] public ArchetypeChunkComponentType<SpriteImage> SpriteImageType;
            [ReadOnly] public ArchetypeChunkComponentType<WorldSpaceRect> WorldSpaceRectType;
            [ReadOnly] public ArchetypeChunkComponentType<WorldSpaceMask> WorldSpaceMaskType;
            [ReadOnly] public ArchetypeChunkComponentType<VertexColorValue> ColorValueType;
            [ReadOnly] public ArchetypeChunkComponentType<VertexColorMultiplier> ColorMultiplierType;
            public ArchetypeChunkComponentType<RebuildElementMeshFlag> RebuildElementMeshFlagType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                NativeArray<SpriteImage> spriteImages = chunk.GetNativeArray(SpriteImageType);
                NativeArray<WorldSpaceRect> worldSpaceRects = chunk.GetNativeArray(WorldSpaceRectType);
                NativeArray<WorldSpaceMask> worldSpaceMasks = chunk.GetNativeArray(WorldSpaceMaskType);
                NativeArray<VertexColorValue> colorValues = chunk.GetNativeArray(ColorValueType);
                NativeArray<VertexColorMultiplier> colorMultipliers = chunk.GetNativeArray(ColorMultiplierType);
                NativeArray<RebuildElementMeshFlag> rebuildFlagArray = chunk.GetNativeArray(RebuildElementMeshFlagType);
                BufferAccessor<ElementVertexData> vertexDataAccessor = chunk.GetBufferAccessor(VertexDataType);
                BufferAccessor<ElementVertexIndex> vertexIndexAccessor = chunk.GetBufferAccessor(VertexIndexType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    if (rebuildFlagArray[i].Rebuild)
                    {
                        var assetEntity = spriteImages[i].Asset;
                        var vertices = vertexDataAccessor[i];
                        var triangles = vertexIndexAccessor[i];
                        var worldSpaceMask = worldSpaceMasks[i];

                        vertices.Clear();
                        triangles.Clear();

                        var worldSpaceRect = worldSpaceRects[i];
                        if (SpriteDataFromEntity.Exists(assetEntity))
                        {
                            var spriteData = SpriteDataFromEntity[assetEntity];
                            // TODO: Simple sprite (non sliced)
                            SpriteUtility.PopulateSpriteVertices(ref worldSpaceMask, ref vertices, ref triangles, ref worldSpaceRect, ref spriteData, colorValues[i].Value * colorMultipliers[i].Value);
                        }
                        else
                        {
                            var spriteData = SpriteVertexData.Default;
                            SpriteUtility.PopulateSpriteVertices(ref worldSpaceMask, ref vertices, ref triangles, ref worldSpaceRect, ref spriteData, colorValues[i].Value * colorMultipliers[i].Value);
                        }
                        rebuildFlagArray[i] = new RebuildElementMeshFlag() { Rebuild = false };
                    }
                }
            }
        }
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (m_SpriteGroup.CalculateEntityCount() < 0)
                return inputDeps;
            SpriteBatchJob batchJob = new SpriteBatchJob()
            {
                ColorValueType = GetArchetypeChunkComponentType<VertexColorValue>(true),
                ColorMultiplierType = GetArchetypeChunkComponentType<VertexColorMultiplier>(true),
                SpriteDataFromEntity = GetComponentDataFromEntity<SpriteVertexData>(),
                SpriteImageType = GetArchetypeChunkComponentType<SpriteImage>(true),
                VertexDataType = GetArchetypeChunkBufferType<ElementVertexData>(),
                VertexIndexType = GetArchetypeChunkBufferType<ElementVertexIndex>(),
                WorldSpaceRectType = GetArchetypeChunkComponentType<WorldSpaceRect>(true),
                WorldSpaceMaskType = GetArchetypeChunkComponentType<WorldSpaceMask>(true),
                RebuildElementMeshFlagType = GetArchetypeChunkComponentType<RebuildElementMeshFlag>()
            };
            inputDeps = batchJob.Schedule(m_SpriteGroup, inputDeps);
            return inputDeps;
        }
    }
}
