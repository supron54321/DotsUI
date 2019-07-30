using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DotsUI.Core;

namespace DotsUI.Controls
{
    [UpdateInGroup(typeof(PostRectTransformSystemGroup))]
    class ScrollRectSystem : JobComponentSystem
    {
        private EntityQuery m_ScrollRectQuery;

        protected override void OnCreate()
        {
            m_ScrollRectQuery = GetEntityQuery(ComponentType.ReadOnly<ScrollRect>(), typeof(ScrollRectData));
        }

        struct UpdateScrollRectData : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkComponentType<ScrollRect> ScrollRectType;
            public ArchetypeChunkComponentType<ScrollRectData> ScrollRectData;

            public ComponentDataFromEntity<RectTransform> RectTransformFromEntity;
            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                NativeArray<ScrollRect> scrollRect = chunk.GetNativeArray(ScrollRectType);

                for (int i = 0; i < scrollRect.Length; i++)
                {
                    var vertical = scrollRect[i].VerticalBar;
                    var contentRect = RectTransformFromEntity[scrollRect[i].Content];
                    var viewportRect = RectTransformFromEntity[scrollRect[i].Viewport];


                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return inputDeps;
        }
    }
}
