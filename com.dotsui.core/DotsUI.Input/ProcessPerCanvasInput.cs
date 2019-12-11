using DotsUI.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsUI.Input
{
    [BurstCompile]
    internal struct ProcessPerCanvasInput : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Entity> Roots;
        [ReadOnly] public ComponentDataFromEntity<WorldSpaceRect> WorldSpaceRectFromEntity;
        [ReadOnly] public ComponentDataFromEntity<PointerInputReceiver> PointerInputReceiver;
        [ReadOnly] public BufferFromEntity<Child> ChildrenFromEntity;

        [ReadOnly] public NativeArray<MouseInputFrameData> PointersPosition;

        [WriteOnly] [NativeDisableContainerSafetyRestriction]
        public NativeArray<Entity> Hits;

        public void Execute(int index)
        {
            var pointerData = PointersPosition[0];
            GoDownHierarchyTree(Roots[index], index, pointerData.Position);
        }

        private bool IsInRect(ref WorldSpaceRect rect, float2 position)
        {
            return (position.x >= rect.Min.x && position.x <= rect.Max.x &&
                    position.y >= rect.Min.y && position.y <= rect.Max.y);
        }

        private bool GoDownHierarchyTree(Entity entity, int index, float2 position)
        {
            if (ChildrenFromEntity.Exists(entity))
            {
                var children = ChildrenFromEntity[entity];
                for (int i = children.Length-1; i >= 0; i--)
                {
                    if (GoDownHierarchyTree(children[i].Value, index, position))
                        return true;
                }
            }

            if (PointerInputReceiver.Exists(entity) && WorldSpaceRectFromEntity.Exists(entity))
            {
                var localToWorld = WorldSpaceRectFromEntity[entity];

                if (IsInRect(ref localToWorld, position))
                {
                    Hits[index * PointersPosition.Length] = entity;
                    return true;
                }
            }

            return false;
        }
    }
}