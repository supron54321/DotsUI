using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace DotsUI.Core
{
    public struct HierarchyRebuildContext
    {
        [ReadOnly] public BufferFromEntity<UIChild> ChildrenFromEntity;
        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<WorldSpaceRect> WorldSpaceRectFromEntity;
        [NativeDisableContainerSafetyRestriction]
        public ComponentDataFromEntity<RectTransform> RectTransformFromEntity;
        [NativeDisableContainerSafetyRestriction] public ComponentDataFromEntity<RebuildElementMeshFlag> RebuildFlagFromEntity;
        [NativeDisableContainerSafetyRestriction] public ComponentDataFromEntity<ElementScale> ElementScaleFromEntity;
        [NativeDisableContainerSafetyRestriction] public ComponentDataFromEntity<WorldSpaceMask> WorldSpaceMaskFromEntity;
        [ReadOnly] public ComponentDataFromEntity<RectMask> RectMaskFromEntity;
    }
    public class RectTransformUtils
    {
        public static void UpdateTransformRecursive(ref WorldSpaceRect parentLocalToWorldSpaceRect, WorldSpaceMask currentMask, Entity entity, float2 scale, ref HierarchyRebuildContext rebuildContext)
        {
            var childTransform = rebuildContext.RectTransformFromEntity[entity];
            var childLocalToWorld = CalculateWorldSpaceRect(parentLocalToWorldSpaceRect, scale, childTransform);
            rebuildContext.WorldSpaceRectFromEntity[entity] = childLocalToWorld;
            rebuildContext.ElementScaleFromEntity[entity] = new ElementScale() { Value = scale };
            UpdateRectMask(entity, childLocalToWorld, ref currentMask, ref rebuildContext);
            rebuildContext.WorldSpaceMaskFromEntity[entity] = currentMask;

            if (rebuildContext.RebuildFlagFromEntity.Exists(entity))
                rebuildContext.RebuildFlagFromEntity[entity] = new RebuildElementMeshFlag()
                {
                    Rebuild = true
                };
            if (rebuildContext.ChildrenFromEntity.Exists(entity))
            {
                var children = rebuildContext.ChildrenFromEntity[entity];
                for (int i = 0; i < children.Length; i++)
                {
                    UpdateTransformRecursive(ref childLocalToWorld, currentMask, children[i].Value, scale, ref rebuildContext);
                }
            }
        }
        private static void UpdateRectMask(Entity entity, WorldSpaceRect elementRect, ref WorldSpaceMask mask, ref HierarchyRebuildContext rebuildContext)
        {
            if (rebuildContext.RectMaskFromEntity.Exists(entity))
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

        public static WorldSpaceRect CalculateWorldSpaceRect(WorldSpaceRect parentRect, float2 scale,
            RectTransform childTransform)
        {
            float2 start =
                math.lerp(parentRect.Min, parentRect.Max,
                    math.lerp(childTransform.AnchorMin, childTransform.AnchorMax, childTransform.Pivot)) +
                childTransform.Position * scale;
            float2 anchorDiff = childTransform.AnchorMax - childTransform.AnchorMin;
            float2 size = (parentRect.Max - parentRect.Min) * anchorDiff +
                          childTransform.SizeDelta * scale;

            float2 min = start - size * childTransform.Pivot;
            float2 max = start + size * (new float2(1.0f, 1.0f) - childTransform.Pivot);

            var childRect = new WorldSpaceRect()
            {
                Min = min,
                Max = max,
            };
            return childRect;
        }
    }
}
