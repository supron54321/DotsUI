using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using System;
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
            [ReadOnly]
            public int ScreenWidth;
            [ReadOnly]
            public int ScreenHeight;
            [ReadOnly] public ArchetypeChunkComponentType<RectTransform> RectTransformType;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkBufferType<UIChild> ChildType;
            [ReadOnly] public ArchetypeChunkComponentType<CanvasConstantPhysicalSizeScaler> ConstantPhysicalScaler;
            [ReadOnly] public ArchetypeChunkComponentType<CanvasConstantPixelSizeScaler> ConstantPixelScaler;
            [ReadOnly] public BufferFromEntity<UIChild> ChildFromEntity;

            [NativeDisableContainerSafetyRestriction]
            public ComponentDataFromEntity<WorldSpaceRect> LocalToWorldFromEntity;
            [NativeDisableContainerSafetyRestriction]
            public ComponentDataFromEntity<RectTransform> RectTransformFromEntity;
            [NativeDisableContainerSafetyRestriction] public ComponentDataFromEntity<RebuildElementMeshFlag> RebuildFlagType;
            [NativeDisableContainerSafetyRestriction] public ComponentDataFromEntity<ElementScale> ElementScaleType;
            [NativeDisableContainerSafetyRestriction] public ComponentDataFromEntity<WorldSpaceMask> WorldSpaceMaskFromEntity;
            [ReadOnly] public ComponentDataFromEntity<RectMask> RectMaskFromEntity;


            private void UpdateRectMask(Entity entity, WorldSpaceRect elementRect, ref WorldSpaceMask mask)
            {
                if (RectMaskFromEntity.Exists(entity))
                {
                    float2 newMin = math.max(mask.Min, elementRect.Min);
                    float2 newMax = math.min(mask.Max, elementRect.Max);
                    mask = new WorldSpaceMask
                    {
                        Min = newMin,
                        Max = newMax
                    };
                }
            }

            private void UpdateTransform(ref WorldSpaceRect parentLocalToWorldSpaceRect, WorldSpaceMask currentMask, Entity entity, float2 scale, int jobIdx)
            {
                var childTransform = RectTransformFromEntity[entity];
                float2 start = math.lerp(parentLocalToWorldSpaceRect.Min, parentLocalToWorldSpaceRect.Max, math.lerp(childTransform.AnchorMin, childTransform.AnchorMax, childTransform.Pivot)) + childTransform.Position * scale;
                float2 anchorDiff = childTransform.AnchorMax - childTransform.AnchorMin;
                float2 size = (parentLocalToWorldSpaceRect.Max - parentLocalToWorldSpaceRect.Min) * anchorDiff + childTransform.SizeDelta * scale;

                float2 min = start - size * childTransform.Pivot;
                float2 max = start + size * (new float2(1.0f, 1.0f) - childTransform.Pivot);

                var childLocalToWorld = new WorldSpaceRect()
                {
                    Min = min,
                    Max = max,
                };
                LocalToWorldFromEntity[entity] = childLocalToWorld;
                ElementScaleType[entity] = new ElementScale() { Value = scale };
                UpdateRectMask(entity, childLocalToWorld, ref currentMask);
                WorldSpaceMaskFromEntity[entity] = currentMask;

                if (RebuildFlagType.Exists(entity))
                    RebuildFlagType[entity] = new RebuildElementMeshFlag()
                    {
                        Rebuild = true
                    };
                if (ChildFromEntity.Exists(entity))
                {
                    var children = ChildFromEntity[entity];
                    for (int i = 0; i < children.Length; i++)
                    {
                        UpdateTransform(ref childLocalToWorld, currentMask, children[i].Value, scale, jobIdx);
                    }
                }
            }

            public void Execute(ArchetypeChunk chunk, int index, int entityOffset)
            {
                var chunkRectTransform = chunk.GetNativeArray(RectTransformType);
                var entities = chunk.GetNativeArray(EntityType);
                var chunkChildren = chunk.GetBufferAccessor(ChildType);

                NativeArray<CanvasConstantPhysicalSizeScaler> physicalSizeArray = default;
                bool useConstantPhysicalSize = chunk.Has(ConstantPhysicalScaler);
                if (useConstantPhysicalSize)
                    physicalSizeArray = chunk.GetNativeArray(ConstantPhysicalScaler);

                for (int i = 0; i < chunk.Count; i++)
                {
                    float2 scale = new float2(1.0f, 1.0f);
                    if (useConstantPhysicalSize)
                        scale = Dpi*physicalSizeArray[i].Factor;
                    var canvasRect = new WorldSpaceRect()
                    {
                        Min = chunkRectTransform[i].Position,
                        Max = (chunkRectTransform[i].Position + new float2(ScreenWidth, ScreenHeight))
                    };
                    LocalToWorldFromEntity[entities[i]] = canvasRect;

                    var children = chunkChildren[i];
                    var parentLocalToWorld = canvasRect;
                    WorldSpaceMask canvasMask = new WorldSpaceMask()
                    {
                        Min = canvasRect.Min,
                        Max = canvasRect.Max
                    };
                    for (int j = 0; j < children.Length; j++)
                    {
                        var childTransform = RectTransformFromEntity[children[j].Value];
                        UpdateTransform(ref parentLocalToWorld, canvasMask, children[j].Value, scale, index);
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
                    ComponentType.ReadOnly<UIChild>(),
                    ComponentType.ReadOnly<RebuildCanvasHierarchyFlag>(),
                    typeof(WorldSpaceRect),
                },
                None = new ComponentType[]
                {
                    typeof(UIParent)
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
            var rectTransformType = GetArchetypeChunkComponentType<RectTransform>(true);
            var entityType = GetArchetypeChunkEntityType();
            var childType = GetArchetypeChunkBufferType<UIChild>(true);
            var childFromEntity = GetBufferFromEntity<UIChild>(true);
            var localToWorldFromEntity = GetComponentDataFromEntity<WorldSpaceRect>();
            var rectTransformFromEntity = GetComponentDataFromEntity<RectTransform>(true);
            var rebuildFlagType = GetComponentDataFromEntity<RebuildElementMeshFlag>();

            var dpi = ScreenUtils.GetScaledDpi();
            var updateHierarchyJob = new UpdateHierarchy
            {
                Dpi = new float2(dpi, dpi),
                ScreenWidth = Screen.width,
                ScreenHeight = Screen.height,
                RectTransformType = rectTransformType,
                EntityType = entityType,
                ChildType = childType,
                ChildFromEntity = childFromEntity,
                LocalToWorldFromEntity = localToWorldFromEntity,
                RectTransformFromEntity = rectTransformFromEntity,
                RebuildFlagType = rebuildFlagType,
                ElementScaleType = GetComponentDataFromEntity<ElementScale>(),
                ConstantPhysicalScaler = GetArchetypeChunkComponentType<CanvasConstantPhysicalSizeScaler>(true),
                ConstantPixelScaler = GetArchetypeChunkComponentType<CanvasConstantPixelSizeScaler>(true),
                WorldSpaceMaskFromEntity = GetComponentDataFromEntity<WorldSpaceMask>(),
                RectMaskFromEntity = GetComponentDataFromEntity<RectMask>(true)
            };
            var updateHierarchyJobHandle = updateHierarchyJob.Schedule(m_Group, inputDeps);
            updateHierarchyJobHandle.Complete();
            return updateHierarchyJobHandle;
        }
    }
}
