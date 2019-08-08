using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DotsUI.Core;
using Unity.Burst;
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

        [BurstCompile]
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
                    var contentTransform = RectTransformFromEntity[scrollRect[i].Content];
                    var scrollRectWorldSpace = WorldSpaceRectFromEntity[scrollEntity[i]];
                    var contentRect = WorldSpaceRectFromEntity[scrollRect[i].Content];
                    var viewportRect = WorldSpaceRectFromEntity[scrollRect[i].Viewport];

                    var contentToViewportRatio = viewportRect.Size / contentRect.Size;

                    var verticalEntity = scrollRect[i].VerticalBar;
                    if(ScrollBarFromEntity.Exists(verticalEntity))
                    {
                        contentRect = UpdateScrollBar(verticalEntity, contentToViewportRatio, viewportRect, contentRect, ScrollBarAxis.Vertical);
                    }

                    var horizontalEntity = scrollRect[i].HorizontalBar;
                    if (ScrollBarFromEntity.Exists(horizontalEntity))
                    {
                        contentRect = UpdateScrollBar(horizontalEntity, contentToViewportRatio, viewportRect, contentRect, ScrollBarAxis.Horizontal);
                    }

                    var newContentTransform = RectTransformUtils.CalculateInverseTransformWithAnchors(contentRect,
                        viewportRect, contentTransform, ElementScaleFromEntity[scrollRect[i].Content].Value);
                    RectTransformFromEntity[scrollRect[i].Content] = newContentTransform;
                    rebuildContext = UpdateScrollRectTransform(scrollEntity[i], scrollRectWorldSpace, rebuildContext);
                }
            }

            private HierarchyRebuildContext UpdateScrollRectTransform(Entity scrollEntity,
                WorldSpaceRect scrollRectWorldSpace, HierarchyRebuildContext rebuildContext)
            {
                var children = ChildrenFromEntity[scrollEntity];
                var scrollMask = WorldSpaceMaskFromEntity[scrollEntity];
                var scale = ElementScaleFromEntity[scrollEntity].Value;
                for (int j = 0; j < children.Length; j++)
                    RectTransformUtils.UpdateTransformRecursive(ref scrollRectWorldSpace, scrollMask, children[j].Value, scale,
                        ref rebuildContext);
                return rebuildContext;
            }

            private WorldSpaceRect UpdateScrollBar(Entity scrollBarEntity, float2 contentToViewportRatio,
                WorldSpaceRect viewportRect, WorldSpaceRect contentRect, ScrollBarAxis axis)
            {
                var scrollBar = ScrollBarFromEntity[scrollBarEntity];
                var areaRect = WorldSpaceRectFromEntity[scrollBarEntity];
                var handleTransform = RectTransformFromEntity[scrollBar.ScrollHandle];

                var scrollBarSize = math.saturate(contentToViewportRatio);
                float2 moveRange = (viewportRect.Size - contentRect.Size);
                float2 contentSize = contentRect.Size;

                if (axis == ScrollBarAxis.Vertical)
                {
                    handleTransform.AnchorMin = new float2(0.0f, scrollBar.Value * (1.0f - scrollBarSize.y));
                    handleTransform.AnchorMax =
                        new float2(1.0f, handleTransform.AnchorMin.y + scrollBarSize.y);
                    WorldSpaceRect handleRect = RectTransformUtils.CalculateWorldSpaceRect(areaRect, ElementScaleFromEntity[scrollBarEntity].Value,
                        handleTransform);
                    scrollBar.DragSensitivity = 1.0f / (areaRect.Size.y - handleRect.Size.y);
                    contentRect.Min.y = viewportRect.Min.y + scrollBar.Value * moveRange.y;
                    contentRect.Max.y = contentRect.Min.y + contentSize.y;
                }
                else
                {
                    handleTransform.AnchorMin = new float2(scrollBar.Value * (1.0f - scrollBarSize.x), 0.0f);
                    handleTransform.AnchorMax =
                        new float2(handleTransform.AnchorMin.x + scrollBarSize.x, 1.0f);
                    WorldSpaceRect handleRect = RectTransformUtils.CalculateWorldSpaceRect(areaRect, ElementScaleFromEntity[scrollBarEntity].Value,
                        handleTransform);
                    scrollBar.DragSensitivity = 1.0f / (areaRect.Size.x - handleRect.Size.x);
                    contentRect.Min.x = viewportRect.Min.x + scrollBar.Value * moveRange.x;
                    contentRect.Max.x = contentRect.Min.x + contentSize.x;
                }

                ScrollBarFromEntity[scrollBarEntity] = scrollBar;
                RectTransformFromEntity[scrollBar.ScrollHandle] = handleTransform;

                return contentRect;
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
