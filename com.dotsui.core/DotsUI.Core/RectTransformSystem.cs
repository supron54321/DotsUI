using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsUI.Core
{
    [UnityEngine.ExecuteAlways]
    [UpdateInGroup(typeof(RectTransformSystemGroup))]
    public class RectTransformSystem : JobComponentSystem
    {
        private EntityQuery m_Group;

        [BurstCompile(FloatMode = FloatMode.Fast)]
        private struct UpdateHierarchy : IJobChunk
        {
            [ReadOnly]
            public float2 Dpi;
            [ReadOnly] public ArchetypeChunkComponentType<RectTransform> RectTransformType;
            [ReadOnly] public ArchetypeChunkComponentType<CanvasScreenSize> CanvasSizeType;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkBufferType<Child> ChildType;
            [ReadOnly] public ArchetypeChunkComponentType<CanvasConstantPhysicalSizeScaler> ConstantPhysicalScaler;
            [ReadOnly] public ArchetypeChunkComponentType<CanvasConstantPixelSizeScaler> ConstantPixelScaler;
            public HierarchyRebuildContext RebuildContext;


            public void Execute(ArchetypeChunk chunk, int index, int entityOffset)
            {
                var chunkRectTransform = chunk.GetNativeArray(RectTransformType);
                var entities = chunk.GetNativeArray(EntityType);
                var chunkChildren = chunk.GetBufferAccessor(ChildType);
                var canvasSizeArray = chunk.GetNativeArray(CanvasSizeType);

                NativeArray<CanvasConstantPhysicalSizeScaler> physicalSizeArray = default;
                bool useConstantPhysicalSize = chunk.Has(ConstantPhysicalScaler);
                if (useConstantPhysicalSize)
                    physicalSizeArray = chunk.GetNativeArray(ConstantPhysicalScaler);

                for (int i = 0; i < chunk.Count; i++)
                {
                    float2 scale = new float2(1.0f, 1.0f);
                    if (useConstantPhysicalSize)
                        scale = Dpi * physicalSizeArray[i].Factor;
                    var canvasRect = new WorldSpaceRect()
                    {
                        Min = float2.zero,//chunkRectTransform[i].Position,
                        Max = canvasSizeArray[i].Value//(chunkRectTransform[i].Position + canvasSizeArray[i].Value)
                    };
                    RebuildContext.WorldSpaceRectFromEntity[entities[i]] = canvasRect;

                    var children = chunkChildren[i];
                    var parentLocalToWorld = canvasRect;
                    WorldSpaceMask canvasMask = new WorldSpaceMask()
                    {
                        Min = canvasRect.Min,
                        Max = canvasRect.Max
                    };
                    for (int j = 0; j < children.Length; j++)
                    {
                        var childTransform = RebuildContext.RectTransformFromEntity[children[j].Value];
                        RebuildContext.UpdateTransformRecursive(ref parentLocalToWorld, canvasMask, children[j].Value, scale);
                    }
                }
            }
        }

        protected override void OnCreate()
        {
            m_Group = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<RectTransform>(),
                    ComponentType.ReadOnly<CanvasScreenSize>(),
                    ComponentType.ReadOnly<Child>(),
                    ComponentType.ReadOnly<RebuildCanvasHierarchyFlag>(),
                    typeof(WorldSpaceRect),
                },
                None = new ComponentType[]
                {
                    typeof(Parent)
                },
                Any = new ComponentType[]
                {
                    ComponentType.ReadOnly<CanvasConstantPixelSizeScaler>(),
                    ComponentType.ReadOnly<CanvasConstantPhysicalSizeScaler>(),
                }
            });
        }
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var entityType = GetArchetypeChunkEntityType();
            var childType = GetArchetypeChunkBufferType<Child>(true);

            var dpi = ScreenUtils.GetScaledDpi();
            var updateHierarchyJob = new UpdateHierarchy
            {
                Dpi = new float2(dpi, dpi),
                RectTransformType = GetArchetypeChunkComponentType<RectTransform>(true),
                CanvasSizeType = GetArchetypeChunkComponentType<CanvasScreenSize>(true),
                EntityType = entityType,
                ChildType = childType,
                ConstantPhysicalScaler = GetArchetypeChunkComponentType<CanvasConstantPhysicalSizeScaler>(true),
                ConstantPixelScaler = GetArchetypeChunkComponentType<CanvasConstantPixelSizeScaler>(true),
                RebuildContext = HierarchyRebuildContext.Create(this)

            };
            var updateHierarchyJobHandle = updateHierarchyJob.Schedule(m_Group, inputDeps);
            return updateHierarchyJobHandle;
        }
    }
}
