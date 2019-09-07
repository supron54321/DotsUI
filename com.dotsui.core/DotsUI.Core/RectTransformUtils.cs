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
            var start =
                math.lerp(parentRect.Min, parentRect.Max,
                    math.lerp(childTransform.AnchorMin, childTransform.AnchorMax, childTransform.Pivot)) +
                childTransform.Position * scale;
            var anchorDiff = childTransform.AnchorMax - childTransform.AnchorMin;
            var size = (parentRect.Max - parentRect.Min) * anchorDiff +
                          childTransform.SizeDelta * scale;

            var min = start - size * childTransform.Pivot;
            var max = start + size * (new float2(1.0f, 1.0f) - childTransform.Pivot);

            var childRect = new WorldSpaceRect()
            {
                Min = min,
                Max = max,
            };
            return childRect;
        }
        public static RectTransform CalculateInverseTransformWithAnchors(WorldSpaceRect desiredRect, WorldSpaceRect parent, RectTransform currentTransform, float2 scale)
        {
            var anchorDiff = currentTransform.AnchorMax - currentTransform.AnchorMin;
            var parentSizeRelation = parent.Size * anchorDiff;
            currentTransform.SizeDelta = (desiredRect.Size - parentSizeRelation) / scale;
            var start = desiredRect.Min + desiredRect.Size * currentTransform.Pivot;
            currentTransform.Position = (start - math.lerp(parent.Min, parent.Max, math.lerp(currentTransform.AnchorMin, currentTransform.AnchorMax, currentTransform.Pivot))) / scale;
            return currentTransform;
        }
    }
}
