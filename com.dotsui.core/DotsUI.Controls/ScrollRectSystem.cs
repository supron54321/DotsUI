using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DotsUI.Core;
using Unity.Mathematics;
using UnityEngine;
using RectTransform = DotsUI.Core.RectTransform;

namespace DotsUI.Controls
{
    [UpdateInGroup(typeof(PostRectTransformSystemGroup))]
    class ScrollRectSystem : JobComponentSystem
    {
        private EntityQuery m_ScrollRectQuery;

        protected override void OnCreate()
        {
            m_ScrollRectQuery = GetEntityQuery(ComponentType.ReadOnly<ScrollRect>(), ComponentType.ReadWrite<WorldSpaceRect>());
        }
        struct UpdateScrollRectData : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkComponentType<ScrollRect> ScrollRectType;
            //public ArchetypeChunkComponentType<ScrollRectData> ScrollRectData;

            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<WorldSpaceRect> WorldSpaceRectFromEntity;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<RectTransform> RectTransformFromEntity;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<ScrollBar> ScrollBarFromEntity;
            [NativeDisableParallelForRestriction] public BufferFromEntity<UIChild> ChildrenFromEntity;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<RebuildElementMeshFlag> RebuildFlagFromEntity;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<ElementScale> ElementScaleFromEntity;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<WorldSpaceMask> WorldSpaceMaskFromEntity;
            [ReadOnly] public ComponentDataFromEntity<RectMask> RectMaskFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                NativeArray<ScrollRect> scrollRect = chunk.GetNativeArray(ScrollRectType);
                NativeArray<Entity> scrollEntity = chunk.GetNativeArray(EntityType);
                var rebuildContext = new HierarchyRebuildContext()
                {
                    ChildrenFromEntity = ChildrenFromEntity,
                    WorldSpaceRectFromEntity = WorldSpaceRectFromEntity,
                    RectTransformFromEntity = RectTransformFromEntity,
                    RebuildFlagFromEntity = RebuildFlagFromEntity,
                    ElementScaleFromEntity = ElementScaleFromEntity,
                    WorldSpaceMaskFromEntity = WorldSpaceMaskFromEntity,
                    RectMaskFromEntity = RectMaskFromEntity
                };
                for (int i = 0; i < scrollRect.Length; i++)
                {
                    var scrollRectWorldSpace = WorldSpaceRectFromEntity[scrollEntity[i]];
                    var verticalEntity = scrollRect[i].VerticalBar;
                    var contentRect = WorldSpaceRectFromEntity[scrollRect[i].Content];
                    var viewportRect = WorldSpaceRectFromEntity[scrollRect[i].Viewport];

                    var contentToViewportRatio = viewportRect.Size / contentRect.Size;
                    var centerDiff = contentRect.Center - viewportRect.Center;

                    float2 moveRange = (viewportRect.Size - contentRect.Size);

                    float2 value = (centerDiff + moveRange*0.5f) / moveRange;
                    Debug.Log(centerDiff + " " + moveRange + " " + value);

                    // clampContent

                    var verticalBar = ScrollBarFromEntity[verticalEntity];
                    var verticalHandleTransform = RectTransformFromEntity[verticalBar.ScrollHandle];
                    var verticalHandleAreaRect = WorldSpaceRectFromEntity[verticalEntity];
                    //RectTransformUtils.UpdateTransformRecursive()

                    var scrollBarSize = math.saturate(contentToViewportRatio);

                    verticalHandleTransform.AnchorMin = new float2(0.0f, value.y * (1.0f - scrollBarSize.y));
                    verticalHandleTransform.AnchorMax = new float2(1.0f, verticalHandleTransform.AnchorMin.y + scrollBarSize.y);

                    RectTransformFromEntity[verticalBar.ScrollHandle] = verticalHandleTransform;
                    RectTransformUtils.UpdateTransformRecursive(ref scrollRectWorldSpace, WorldSpaceMaskFromEntity[scrollEntity[i]], scrollEntity[i], ElementScaleFromEntity[scrollEntity[i]].Value, ref rebuildContext);
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            UpdateScrollRectData updateJob = new UpdateScrollRectData()
            {
                EntityType = GetArchetypeChunkEntityType(),
                ScrollRectType = GetArchetypeChunkComponentType<ScrollRect>(),
                //ScrollRectData = GetArchetypeChunkComponentType<ScrollRectData>(),

                WorldSpaceRectFromEntity = GetComponentDataFromEntity<WorldSpaceRect>(),
                RectTransformFromEntity = GetComponentDataFromEntity<RectTransform>(),
                ScrollBarFromEntity = GetComponentDataFromEntity<ScrollBar>(),
                ChildrenFromEntity = GetBufferFromEntity<UIChild>(),
                RebuildFlagFromEntity = GetComponentDataFromEntity<RebuildElementMeshFlag>(),
                ElementScaleFromEntity = GetComponentDataFromEntity<ElementScale>(),
                WorldSpaceMaskFromEntity = GetComponentDataFromEntity<WorldSpaceMask>(),
                RectMaskFromEntity = GetComponentDataFromEntity<RectMask>(),
            };
            inputDeps = updateJob.Schedule(m_ScrollRectQuery, inputDeps);
            return inputDeps;
        }
    }
}
