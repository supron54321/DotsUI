using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace DotsUI.Core
{
    [UpdateInGroup(typeof(ElementMeshUpdateSystemGroup))]
    class UpdateElementColorSystem : JobComponentSystem
    {
        private EntityQuery m_ColorUpdateQuery;
        protected override void OnCreate()
        {
            m_ColorUpdateQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<UpdateElementColor>(),
                    ComponentType.ReadOnly<VertexColorValue>(),
                    ComponentType.ReadOnly<VertexColorMultiplier>(),
                    ComponentType.ReadWrite<ControlVertexData>(),
                    ComponentType.ReadOnly<RebuildElementMeshFlag>(), 
                }
            });
        }

        [BurstCompile]
        struct UpdateColorVertices : IJobChunk
        {
            [NativeDisableContainerSafetyRestriction] public BufferFromEntity<MeshVertex> VertexFromCanvasEntity;
            [ReadOnly] public ComponentDataFromEntity<Parent> ParentFromEntity;
            [NativeDisableContainerSafetyRestriction]public ArchetypeChunkBufferType<ControlVertexData> VertexDataType;
            [ReadOnly] public ArchetypeChunkComponentType<ElementVertexPointerInMesh> VertexPointerInCanvasMeshType;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkComponentType<VertexColorValue> VertexColorType;
            [ReadOnly] public ArchetypeChunkComponentType<VertexColorMultiplier> VertexColorMultiplierType;
            [ReadOnly] public ArchetypeChunkComponentType<RebuildElementMeshFlag> RebuildElementMeshFlagType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var colorArray = chunk.GetNativeArray(VertexColorType);
                var colorMultiplierArray = chunk.GetNativeArray(VertexColorMultiplierType);
                var entityArray = chunk.GetNativeArray(EntityType);
                var vertexDataAccessor = chunk.GetBufferAccessor(VertexDataType);
                var pointerArray = chunk.GetNativeArray(VertexPointerInCanvasMeshType);
                var rebuildMeshFlagArray = chunk.GetNativeArray(RebuildElementMeshFlagType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    if(!rebuildMeshFlagArray[i].Rebuild)
                    {
                        var controlVertexBuffer = vertexDataAccessor[i];
                        int bufferLen = controlVertexBuffer.Length;
                        var color = colorArray[i].Value* colorMultiplierArray[i].Value;

                        Entity root = GetRootRecursive(entityArray[i]);
                        var canvasVertexBuffer = VertexFromCanvasEntity[root];
                        int pointerToCanvasVertex = pointerArray[i].VertexPointer;

                        for (int j = 0; j < bufferLen; j++)
                        {
                            var controlVertex = controlVertexBuffer[j];
                            controlVertex.Color = color;
                            controlVertexBuffer[j] = controlVertex;
                            canvasVertexBuffer[j + pointerToCanvasVertex] = controlVertex;
                        }
                    }
                }
            }

            public Entity GetRootRecursive(Entity entity)
            {
                if (ParentFromEntity.Exists(entity))
                    return GetRootRecursive(ParentFromEntity[entity].Value);
                return entity;
            }
        }


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            UpdateColorVertices updateJob = new UpdateColorVertices()
            {
                EntityType = GetArchetypeChunkEntityType(),
                ParentFromEntity = GetComponentDataFromEntity<Parent>(true),
                VertexColorType = GetArchetypeChunkComponentType<VertexColorValue>(true),
                VertexColorMultiplierType = GetArchetypeChunkComponentType<VertexColorMultiplier>(true),
                VertexDataType = GetArchetypeChunkBufferType<ControlVertexData>(),
                VertexFromCanvasEntity = GetBufferFromEntity<MeshVertex>(),
                VertexPointerInCanvasMeshType = GetArchetypeChunkComponentType<ElementVertexPointerInMesh>(true),
                RebuildElementMeshFlagType = GetArchetypeChunkComponentType<RebuildElementMeshFlag>(true)
            };
            inputDeps = updateJob.Schedule(m_ColorUpdateQuery, inputDeps);
            return inputDeps;
        }
    }
}
