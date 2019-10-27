using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DotsUI.Core;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using RectTransform = DotsUI.Core.RectTransform;

namespace DotsUI.Controls
{
    [UpdateInGroup(typeof(PostRectTransformSystemGroup))]
    class ScrollRectTransformSystem : JobComponentSystem
    {
        private EntityQuery m_ScrollRectQuery;

        protected override void OnCreate()
        {
            m_ScrollRectQuery = GetEntityQuery(ComponentType.ReadOnly<ScrollRect>(), ComponentType.ReadWrite<WorldSpaceRect>());
        }

        struct ScrollViewTransforms
        {
            public RectTransform ContentTransform;
            public RectTransform ViewportTransform;
            public WorldSpaceRect ScrollViewRect;
            public WorldSpaceRect ContentRect;
            public WorldSpaceRect ViewportRect;

            public RectTransform VerticalAreaTransform;
            public RectTransform VerticalHandleTransform;
            public RectTransform HorizontalAreaTransform;
            public RectTransform HorizontalHandleTransform;
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
            [ReadOnly] public BufferFromEntity<Child> ChildrenFromEntity;
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
                    ScrollRect scrollView = scrollRect[i];
                    ScrollViewTransforms transforms = GatherTransforms(scrollEntity[i], scrollView);
                    var oldContentRect = transforms.ContentRect;


                    transforms.ViewportTransform.AnchorMin = new float2(0.0f, 0.0f);
                    transforms.ViewportTransform.AnchorMax = new float2(1.0f, 1.0f);
                    transforms.ViewportTransform.SizeDelta = new float2(-transforms.VerticalAreaTransform.SizeDelta.x-scrollView.VerticalBarSpacing, -transforms.HorizontalAreaTransform.SizeDelta.y-scrollView.HorizontalBarSpacing);

                    transforms.ViewportRect = RectTransformUtils.CalculateWorldSpaceRect(transforms.ScrollViewRect,
                        ElementScaleFromEntity[scrollEntity[i]].Value, transforms.ViewportTransform);

                    RectTransformFromEntity[scrollView.Viewport] = transforms.ViewportTransform;

                    var contentToViewportRatio = transforms.ViewportRect.Size / transforms.ContentRect.Size;

                    var verticalEntity = scrollView.VerticalBar;
                    if(ScrollBarFromEntity.Exists(verticalEntity))
                    {
                        UpdateScrollBar(scrollView, verticalEntity, contentToViewportRatio, ref transforms, ScrollBarAxis.Vertical);
                    }

                    var horizontalEntity = scrollView.HorizontalBar;
                    if (ScrollBarFromEntity.Exists(horizontalEntity))
                    {
                        UpdateScrollBar(scrollView, horizontalEntity, contentToViewportRatio, ref transforms, ScrollBarAxis.Horizontal);
                    }

                    if (NeedUpdate(oldContentRect, transforms.ContentRect))
                    {
                        var newContentTransform = RectTransformUtils.CalculateInverseTransformWithAnchors(transforms.ContentRect,
                            transforms.ViewportRect, transforms.ContentTransform, ElementScaleFromEntity[scrollView.Content].Value);
                        RectTransformFromEntity[scrollView.Content] = newContentTransform;
                        UpdateScrollRectTransform(scrollEntity[i], transforms.ScrollViewRect, rebuildContext);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ScrollViewTransforms GatherTransforms(Entity scrollRectEntity, ScrollRect scrollRect)
            {
                var verticalBar = ScrollBarFromEntity[scrollRect.VerticalBar];
                var horizontalBar = ScrollBarFromEntity[scrollRect.HorizontalBar];
                return new ScrollViewTransforms
                {
                    ContentTransform = RectTransformFromEntity[scrollRect.Content],
                    ViewportTransform = RectTransformFromEntity[scrollRect.Viewport],
                    ScrollViewRect = WorldSpaceRectFromEntity[scrollRectEntity],
                    ContentRect = WorldSpaceRectFromEntity[scrollRect.Content],
                    ViewportRect = WorldSpaceRectFromEntity[scrollRect.Viewport],

                    VerticalAreaTransform = RectTransformFromEntity[scrollRect.VerticalBar],
                    VerticalHandleTransform = RectTransformFromEntity[verticalBar.ScrollHandle],
                    HorizontalAreaTransform = RectTransformFromEntity[scrollRect.HorizontalBar],
                    HorizontalHandleTransform = RectTransformFromEntity[horizontalBar.ScrollHandle],
                };
            }

            private bool NeedUpdate(WorldSpaceRect oldContentRect, WorldSpaceRect contentRect)
            {
                return math.distance(oldContentRect.Min, contentRect.Min) > 0.5f ||
                       math.distance(oldContentRect.Max, contentRect.Max) > 0.5f;
            }

            private void UpdateScrollRectTransform(Entity scrollEntity,
                WorldSpaceRect scrollRectWorldSpace, HierarchyRebuildContext rebuildContext)
            {
                var children = ChildrenFromEntity[scrollEntity];
                var scrollMask = WorldSpaceMaskFromEntity[scrollEntity];
                var scale = ElementScaleFromEntity[scrollEntity].Value;
                for (int j = 0; j < children.Length; j++)
                    rebuildContext.UpdateTransformRecursive(ref scrollRectWorldSpace, scrollMask, children[j].Value, scale);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void UpdateScrollBar(ScrollRect scrollView, Entity scrollBarEntity, float2 contentToViewportRatio,
                ref ScrollViewTransforms transforms, ScrollBarAxis axis)
            {
                var scrollBar = ScrollBarFromEntity[scrollBarEntity];
                var areaTransform = RectTransformFromEntity[scrollBarEntity];
                var handleTransform = RectTransformFromEntity[scrollBar.ScrollHandle];

                var scrollBarSize = math.saturate(contentToViewportRatio);
                float2 moveRange = (transforms.ViewportRect.Size - transforms.ContentRect.Size);
                float2 contentSize = transforms.ContentRect.Size;

                if (axis == ScrollBarAxis.Vertical)
                {
                    areaTransform.AnchorMin = new float2(areaTransform.AnchorMin.x, 0.0f);
                    areaTransform.AnchorMax = new float2(areaTransform.AnchorMax.x, 1.0f);
                    areaTransform.Pivot = new float2(1.0f, 1.0f);
                    areaTransform.SizeDelta = new float2(areaTransform.SizeDelta.x, -(areaTransform.SizeDelta.x + scrollView.HorizontalBarSpacing));

                    var areaRect = RectTransformUtils.CalculateWorldSpaceRect(transforms.ScrollViewRect,
                        ElementScaleFromEntity[scrollBarEntity].Value, areaTransform);

                    handleTransform.AnchorMin = new float2(0.0f, scrollBar.Value * (1.0f - scrollBarSize.y));
                    handleTransform.AnchorMax =
                        new float2(1.0f, handleTransform.AnchorMin.y + scrollBarSize.y);
                    WorldSpaceRect handleRect = RectTransformUtils.CalculateWorldSpaceRect(areaRect, ElementScaleFromEntity[scrollBarEntity].Value,
                        handleTransform);
                    scrollBar.HandleDragSensitivity = 1.0f / (areaRect.Size.y - handleRect.Size.y);
                    scrollBar.RectDragSensitivity = scrollBar.HandleDragSensitivity / ((transforms.ContentRect.Size.y - transforms.ViewportRect.Size.y) / (areaRect.Size.y - handleRect.Size.y));
                    transforms.ContentRect.Min.y = transforms.ViewportRect.Min.y + scrollBar.Value * moveRange.y;
                    transforms.ContentRect.Max.y = transforms.ContentRect.Min.y + contentSize.y;
                }
                else
                {
                    areaTransform.AnchorMin = new float2(0.0f, areaTransform.AnchorMin.y);
                    areaTransform.AnchorMax = new float2(1.0f, areaTransform.AnchorMax.y);
                    areaTransform.Pivot = new float2(0.0f, 0.0f);
                    areaTransform.SizeDelta = new float2(-(areaTransform.SizeDelta.y + scrollView.VerticalBarSpacing), areaTransform.SizeDelta.y);

                    var areaRect = RectTransformUtils.CalculateWorldSpaceRect(transforms.ScrollViewRect,
                        ElementScaleFromEntity[scrollBarEntity].Value, areaTransform);

                    handleTransform.AnchorMin = new float2(scrollBar.Value * (1.0f - scrollBarSize.x), 0.0f);
                    handleTransform.AnchorMax =
                        new float2(handleTransform.AnchorMin.x + scrollBarSize.x, 1.0f);
                    WorldSpaceRect handleRect = RectTransformUtils.CalculateWorldSpaceRect(areaRect, ElementScaleFromEntity[scrollBarEntity].Value,
                        handleTransform);
                    scrollBar.HandleDragSensitivity = 1.0f / (areaRect.Size.x - handleRect.Size.x);
                    scrollBar.RectDragSensitivity = scrollBar.HandleDragSensitivity / ((transforms.ContentRect.Size.x - transforms.ViewportRect.Size.x) / (areaRect.Size.x - handleRect.Size.x));
                    transforms.ContentRect.Min.x = transforms.ViewportRect.Min.x + scrollBar.Value * moveRange.x;
                    transforms.ContentRect.Max.x = transforms.ContentRect.Min.x + contentSize.x;
                }

                ScrollBarFromEntity[scrollBarEntity] = scrollBar;
                RectTransformFromEntity[scrollBar.ScrollHandle] = handleTransform;
                RectTransformFromEntity[scrollBarEntity] = areaTransform;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            UpdateScrollRectData updateJob = new UpdateScrollRectData()
            {
                EntityType = GetArchetypeChunkEntityType(),
                ScrollRectType = GetArchetypeChunkComponentType<ScrollRect>(),

                WorldSpaceRectFromEntity = GetComponentDataFromEntity<WorldSpaceRect>(),
                RectTransformFromEntity = GetComponentDataFromEntity<RectTransform>(),
                ScrollBarFromEntity = GetComponentDataFromEntity<ScrollBar>(),
                ChildrenFromEntity = GetBufferFromEntity<Child>(),
                RebuildFlagFromEntity = GetComponentDataFromEntity<RebuildElementMeshFlag>(),
                ElementScaleFromEntity = GetComponentDataFromEntity<ElementScale>(),
                WorldSpaceMaskFromEntity = GetComponentDataFromEntity<WorldSpaceMask>(),
                RectMaskFromEntity = GetComponentDataFromEntity<RectMask>(true),
            };
            inputDeps = updateJob.Schedule(m_ScrollRectQuery, inputDeps);
            return inputDeps;
        }
    }
}
