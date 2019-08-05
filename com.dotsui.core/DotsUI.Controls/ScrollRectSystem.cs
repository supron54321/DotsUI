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
using Unity.Transforms;
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
            [NativeDisableParallelForRestriction] public BufferFromEntity<Child> ChildrenFromEntity;
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
                    var contentRect = WorldSpaceRectFromEntity[scrollRect[i].Content];
                    var viewportRect = WorldSpaceRectFromEntity[scrollRect[i].Viewport];

                    var contentToViewportRatio = viewportRect.Size / contentRect.Size;
                    var centerDiff = contentRect.Center - viewportRect.Center;

                    float2 moveRange = (viewportRect.Size - contentRect.Size);

                    float2 value = math.saturate((centerDiff + moveRange*0.5f) / moveRange);


                    var verticalEntity = scrollRect[i].VerticalBar;
                    if(ScrollBarFromEntity.Exists(verticalEntity))
                        UpdateScrollBar(verticalEntity, contentToViewportRatio, value, ScrollBarAxis.Vertical);

                    var horizontalEntity = scrollRect[i].HorizontalBar;
                    if (ScrollBarFromEntity.Exists(horizontalEntity))
                        UpdateScrollBar(horizontalEntity, contentToViewportRatio, value, ScrollBarAxis.Horizontal);

                    RectTransformUtils.UpdateTransformRecursive(ref scrollRectWorldSpace, WorldSpaceMaskFromEntity[scrollEntity[i]], scrollEntity[i], ElementScaleFromEntity[scrollEntity[i]].Value, ref rebuildContext);
                }
            }

            private void UpdateScrollBar(Entity scrollBarEntity, float2 contentToViewportRatio, float2 value, ScrollBarAxis axis)
            {
                var scrollBar = ScrollBarFromEntity[scrollBarEntity];
                var handleTransform = RectTransformFromEntity[scrollBar.ScrollHandle];

                var scrollBarSize = math.saturate(contentToViewportRatio);

                if(axis == ScrollBarAxis.Vertical)
                {
                    handleTransform.AnchorMin = new float2(0.0f, value.y * (1.0f - scrollBarSize.y));
                    handleTransform.AnchorMax =
                        new float2(1.0f, handleTransform.AnchorMin.y + scrollBarSize.y);
                }
                else
                {
                    handleTransform.AnchorMin = new float2(value.x * (1.0f - scrollBarSize.x), 0.0f);
                    handleTransform.AnchorMax =
                        new float2(handleTransform.AnchorMin.x + scrollBarSize.x, 1.0f);
                }

                RectTransformFromEntity[scrollBar.ScrollHandle] = handleTransform;
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
                ChildrenFromEntity = GetBufferFromEntity<Child>(),
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
