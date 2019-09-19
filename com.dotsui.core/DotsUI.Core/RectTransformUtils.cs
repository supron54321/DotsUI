using Unity.Mathematics;

namespace DotsUI.Core
{
    public class RectTransformUtils
    {

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
