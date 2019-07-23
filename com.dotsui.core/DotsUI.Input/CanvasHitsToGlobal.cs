using System;
using DotsUI.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DotsUI.Input
{
    [BurstCompile]
    internal struct CanvasHitsToGlobal : IJob
    {
        [ReadOnly][DeallocateOnJobCompletion] public NativeArray<Entity> Roots;
        [ReadOnly] public ComponentDataFromEntity<CanvasSortLayer> CanvasLayerFromEntity;
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> PerCanvasHits;
        public NativeArray<Entity> GlobalHits;

        public void Execute()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (PerCanvasHits.Length % GlobalHits.Length != 0)
                throw new InvalidOperationException();
#endif
            NativeArray<int> currentLayers = new NativeArray<int>(GlobalHits.Length, Allocator.Temp);
            for (int i = 0; i < currentLayers.Length; i++)
                currentLayers[i] = int.MinValue;

            for (int i = 0; i < Roots.Length; i++)
            {
                var layer = CanvasLayerFromEntity[Roots[i]].Value;
                for (int j = 0; j < GlobalHits.Length; j++)
                {
                    var hit = PerCanvasHits[i * GlobalHits.Length + j];
                    if (hit != default && layer > currentLayers[j])
                    {
                        GlobalHits[j] = hit;
                        currentLayers[j] = layer;
                    }
                }
            }
        }
    }
}