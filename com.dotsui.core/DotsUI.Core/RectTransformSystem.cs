using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using System;
using Unity.Transforms;
using UnityEngine;

namespace DotsUI.Core
{
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

            /*
            [ReadOnly] public BufferFromEntity<UIChild> ChildrenFromEntity;
            [NativeDisableContainerSafetyRestriction]
            public ComponentDataFromEntity<WorldSpaceRect> WorldSpaceRectFromEntity;
            [NativeDisableContainerSafetyRestriction]
            public ComponentDataFromEntity<RectTransform> RectTransformFromEntity;
            [NativeDisableContainerSafetyRestriction] public ComponentDataFromEntity<RebuildElementMeshFlag> RebuildFlagFromEntity;
            [NativeDisableContainerSafetyRestriction] public ComponentDataFromEntity<ElementScale> ElementScaleFromEntity;
            [NativeDisableContainerSafetyRestriction] public ComponentDataFromEntity<WorldSpaceMask> WorldSpaceMaskFromEntity;
            [ReadOnly] public ComponentDataFromEntity<RectMask> RectMaskFromEntity;
            */
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

                //HierarchyRebuildContext rebuildContext = new HierarchyRebuildContext()
                //{
                //    ChildrenFromEntity = RebuildContext.ChildrenFromEntity,
                //    ElementScaleFromEntity = RebuildContext.ElementScaleFromEntity,
                //    RebuildFlagFromEntity = RebuildFlagFromEntity,
                //    RectMaskFromEntity = RectMaskFromEntity,
                //    RectTransformFromEntity = RectTransformFromEntity,
                //    WorldSpaceMaskFromEntity = WorldSpaceMaskFromEntity,
                //    WorldSpaceRectFromEntity = WorldSpaceRectFromEntity
                //};

                for (int i = 0; i < chunk.Count; i++)
                {
                    float2 scale = new float2(1.0f, 1.0f);
                    if (useConstantPhysicalSize)
                        scale = Dpi * physicalSizeArray[i].Factor;
                    var canvasRect = new WorldSpaceRect()
                    {
                        Min = chunkRectTransform[i].Position,
                        Max = (chunkRectTransform[i].Position + canvasSizeArray[i].Value)
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
                        RectTransformUtils.UpdateTransformRecursive(ref parentLocalToWorld, canvasMask, children[j].Value, scale, ref RebuildContext);
                        //UpdateTransformRecursive(ref parentLocalToWorld, canvasMask, children[j].Value, scale);
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
                },
                Options = EntityQueryOptions.FilterWriteGroup
            });
        }
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var entityType = GetArchetypeChunkEntityType();
            var childType = GetArchetypeChunkBufferType<Child>(true);
            var childFromEntity = GetBufferFromEntity<Child>(true);
            var worldSpaceRectFromEntity = GetComponentDataFromEntity<WorldSpaceRect>();
            var rectTransformFromEntity = GetComponentDataFromEntity<RectTransform>(true);
            var rebuildFlagType = GetComponentDataFromEntity<RebuildElementMeshFlag>();

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
                RebuildContext = new HierarchyRebuildContext()
                {
                    ChildrenFromEntity = childFromEntity,
                    WorldSpaceRectFromEntity = worldSpaceRectFromEntity,
                    RectTransformFromEntity = rectTransformFromEntity,
                    RebuildFlagFromEntity = rebuildFlagType,
                    ElementScaleFromEntity = GetComponentDataFromEntity<ElementScale>(),
                    WorldSpaceMaskFromEntity = GetComponentDataFromEntity<WorldSpaceMask>(),
                    RectMaskFromEntity = GetComponentDataFromEntity<RectMask>(true)
                }

            };
            var updateHierarchyJobHandle = updateHierarchyJob.Schedule(m_Group, inputDeps);
            return updateHierarchyJobHandle;
        }
    }
}
