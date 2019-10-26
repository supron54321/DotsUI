using DotsUI.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsUI.Controls
{

    [UpdateInGroup(typeof(PostRectTransformSystemGroup))]
    class SliderTransformSystem : RectTransformRebuildSystem
    {
        private EntityQuery m_SliderQuery;

        protected override void OnCreate()
        {
            m_SliderQuery = GetEntityQuery(ComponentType.ReadOnly<Slider>(), ComponentType.ReadWrite<WorldSpaceRect>());
            m_SliderQuery.SetFilterChanged(typeof(Slider));
            RequireForUpdate(m_SliderQuery);
        }

        [BurstCompile]
        struct UpdateSliderTransform : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkComponentType<Slider> SliderType;
            [ReadOnly] public ComponentDataFromEntity<Parent> ParentFromEntity;

            public HierarchyRebuildContext RebuildContext;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                NativeArray<Slider> sliderArray = chunk.GetNativeArray(SliderType);
                NativeArray<Entity> entityArray = chunk.GetNativeArray(EntityType);
                for (int i = 0; i < entityArray.Length; i++)
                {
                    UpdateTransforms(entityArray[i], sliderArray[i]);
                }
            }

            private void UpdateTransforms(Entity entity, Slider slider)
            {
                if (slider.HandleRect != Entity.Null)
                    UpdateHandle(slider);
                if (slider.FillRect != Entity.Null)
                    UpdateFill(slider);
                RebuildContext.UpdateTransformRecursive(ParentFromEntity[entity].Value, entity);
            }

            private void UpdateFill(Slider slider)
            {
                if (!ParentFromEntity.Exists(slider.FillRect))
                    return;
                var fillParent = ParentFromEntity[slider.FillRect].Value; 
                if (fillParent == Entity.Null)  // Is this check necessary?
                    return;

                int axis = slider.GetAxis();

                float2 anchorMin = new float2(0.0f);
                float2 anchorMax = new float2(1.0f);

                if (slider.SliderDirection == Slider.Direction.RightToLeft || slider.SliderDirection == Slider.Direction.TopToBottom)
                    anchorMin[axis] = 1 - slider.NormalizedValue;
                else
                    anchorMax[axis] = slider.NormalizedValue;

                var fillRect = RebuildContext.RectTransformFromEntity[slider.FillRect];
                fillRect.AnchorMin = anchorMin;
                fillRect.AnchorMax = anchorMax;
                RebuildContext.RectTransformFromEntity[slider.FillRect] = fillRect;
            }

            private void UpdateHandle(Slider slider)
            {
                if (!ParentFromEntity.Exists(slider.HandleRect))
                    return;
                var handleParent = ParentFromEntity[slider.HandleRect].Value;
                if (handleParent == Entity.Null)
                    return;

                int axis = slider.GetAxis();

                float2 anchorMin = new float2(0.0f);
                float2 anchorMax = new float2(1.0f);

                float anchorValue;
                if (slider.Reversed)
                    anchorValue = 1 - slider.NormalizedValue;
                else
                    anchorValue = slider.NormalizedValue;
                anchorMin[axis] = anchorValue;
                anchorMax[axis] = anchorValue;

                var handleTransform = RebuildContext.RectTransformFromEntity[slider.HandleRect];
                handleTransform.AnchorMin = anchorMin;
                handleTransform.AnchorMax = anchorMax;
                RebuildContext.RectTransformFromEntity[slider.HandleRect] = handleTransform;
            }
        }

        protected override JobHandle OnUpdateTransform(JobHandle inputDeps, HierarchyRebuildContext rebuildContext)
        {
            UpdateSliderTransform updateJob = new UpdateSliderTransform()
            {
                EntityType = GetArchetypeChunkEntityType(),
                RebuildContext = rebuildContext,
                ParentFromEntity = GetComponentDataFromEntity<Parent>(true),
                SliderType = GetArchetypeChunkComponentType<Slider>()
            };
            inputDeps = updateJob.Schedule(m_SliderQuery, inputDeps);
            return inputDeps;
        }
    }
}
