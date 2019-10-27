using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsUI.Core
{
    public struct HierarchyRebuildContext
    {
        [ReadOnly] public BufferFromEntity<Child> ChildrenFromEntity;
        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<WorldSpaceRect> WorldSpaceRectFromEntity;
        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<RectTransform> RectTransformFromEntity;
        [NativeDisableContainerSafetyRestriction] public ComponentDataFromEntity<RebuildElementMeshFlag> RebuildFlagFromEntity;
        [NativeDisableContainerSafetyRestriction] public ComponentDataFromEntity<ElementScale> ElementScaleFromEntity;
        [NativeDisableContainerSafetyRestriction] public ComponentDataFromEntity<WorldSpaceMask> WorldSpaceMaskFromEntity;
        [ReadOnly] public ComponentDataFromEntity<RectMask> RectMaskFromEntity;
        [ReadOnly] public ComponentDataFromEntity<Disabled> DisabledFromEntity;
        [NativeDisableContainerSafetyRestriction] [WriteOnly] public ComponentDataFromEntity<ElementHierarchyIndex> HierarchyIndexFromEntity;


        public static HierarchyRebuildContext Create(JobComponentSystem system)
        {
            return new HierarchyRebuildContext()
            {
                ChildrenFromEntity = system.GetBufferFromEntity<Child>(true),
                WorldSpaceRectFromEntity = system.GetComponentDataFromEntity<WorldSpaceRect>(),
                RectTransformFromEntity = system.GetComponentDataFromEntity<RectTransform>(true),
                RebuildFlagFromEntity = system.GetComponentDataFromEntity<RebuildElementMeshFlag>(),
                ElementScaleFromEntity = system.GetComponentDataFromEntity<ElementScale>(),
                WorldSpaceMaskFromEntity = system.GetComponentDataFromEntity<WorldSpaceMask>(),
                RectMaskFromEntity = system.GetComponentDataFromEntity<RectMask>(true),
                DisabledFromEntity = system.GetComponentDataFromEntity<Disabled>(true),
                HierarchyIndexFromEntity = system.GetComponentDataFromEntity<ElementHierarchyIndex>(),
            };
        }

        public void UpdateTransformRecursive(Entity parent, Entity entity)
        {
            var thisRect = WorldSpaceRectFromEntity[parent];
            var thisMask = WorldSpaceMaskFromEntity[parent];
            var thisScale = ElementScaleFromEntity[parent].Value;
            UpdateTransformRecursive(ref thisRect,thisMask, entity, thisScale);
        }

        public void UpdateTransformRecursive(ref WorldSpaceRect parentLocalToWorldSpaceRect, WorldSpaceMask currentMask, Entity entity, float2 scale)
        {
            if (DisabledFromEntity.Exists(entity))
                return;
            var childTransform = RectTransformFromEntity[entity];
            var childLocalToWorld = RectTransformUtils.CalculateWorldSpaceRect(parentLocalToWorldSpaceRect, scale, childTransform);
            WorldSpaceRectFromEntity[entity] = childLocalToWorld;
            ElementScaleFromEntity[entity] = new ElementScale() { Value = scale };
            UpdateRectMask(entity, childLocalToWorld, ref currentMask);
            WorldSpaceMaskFromEntity[entity] = currentMask;

            if (RebuildFlagFromEntity.Exists(entity))
                RebuildFlagFromEntity[entity] = new RebuildElementMeshFlag()
                {
                    Rebuild = true
                };
            if (ChildrenFromEntity.Exists(entity))
            {
                var children = ChildrenFromEntity[entity];
                for (int i = 0; i < children.Length; i++)
                {
                    UpdateTransformRecursive(ref childLocalToWorld, currentMask, children[i].Value, scale);
                }
            }
        }
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
    }
}