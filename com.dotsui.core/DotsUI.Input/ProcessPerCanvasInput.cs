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
        [ReadOnly] public ComponentDataFromEntity<WorldSpaceRect> LocalToWorldFromEntity;
        [ReadOnly] public ComponentDataFromEntity<PointerInputReceiver> PointerInputReceiver;
        [ReadOnly] public BufferFromEntity<Child> ChildrenFromEntity;

        [ReadOnly] public NativeArray<MouseInputFrameData> PointersPosition;

        [WriteOnly] [NativeDisableContainerSafetyRestriction]
        public NativeArray<Entity> Hits;

        public void Execute(int index)
        {
            GoDownHierarchyTree(Roots[index], index);
        }

        private bool IsInRect(ref WorldSpaceRect rect, float2 position)
        {
            return (position.x >= rect.Min.x && position.x <= rect.Max.x &&
                    position.y >= rect.Min.y && position.y <= rect.Max.y);
        }

        private void GoDownHierarchyTree(Entity entity, int index)
        {
            if (PointerInputReceiver.Exists(entity) && LocalToWorldFromEntity.Exists(entity))
            {
                var localToWorld = LocalToWorldFromEntity[entity];

                for (int i = 0; i < PointersPosition.Length; i++)
                {
                    if (IsInRect(ref localToWorld, PointersPosition[i].Position))
                        Hits[index * PointersPosition.Length + i] = entity;
                }
            }

            if (ChildrenFromEntity.Exists(entity))
            {
                var children = ChildrenFromEntity[entity];
                for (int i = 0; i < children.Length; i++)
                {
                    GoDownHierarchyTree(children[i].Value, index);
                }
            }
        }
    }
}