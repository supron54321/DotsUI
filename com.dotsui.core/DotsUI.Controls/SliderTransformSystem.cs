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
    class SliderTransformSystem : ControlRectTransformRebuildSystem
    {
        private EntityQuery m_SliderQuery;

        protected override void OnCreate()
        {
            m_SliderQuery = GetEntityQuery(ComponentType.ReadOnly<Slider>(), ComponentType.ReadWrite<WorldSpaceRect>());
            m_SliderQuery.SetChangedVersionFilter(typeof(Slider));
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
                if (!IsValidRect(slider.FillRect)) return;

                int axis = slider.GetAxis();

                float2 anchorMin = new float2(0.0f);
                float2 anchorMax = new float2(1.0f);

                if (slider.Reversed)
                    anchorMin[axis] = 1 - slider.NormalizedValue;
                else
                    anchorMax[axis] = slider.NormalizedValue;

                SetAnchors(slider.FillRect, anchorMin, anchorMax);
            }

            private void UpdateHandle(Slider slider)
            {
                if (!IsValidRect(slider.HandleRect)) return;

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

                SetAnchors(slider.HandleRect, anchorMin, anchorMax);
            }

            private bool IsValidRect(Entity rectEntity)
            {
                if (!ParentFromEntity.Exists(rectEntity))
                    return false;
                var fillParent = ParentFromEntity[rectEntity].Value;
                if (fillParent == Entity.Null) // Is this check necessary?
                    return false;
                return true;
            }

            private void SetAnchors(Entity rectEntity, float2 anchorMin, float2 anchorMax)
            {
                var rectTransform = RebuildContext.RectTransformFromEntity[rectEntity];
                rectTransform.AnchorMin = anchorMin;
                rectTransform.AnchorMax = anchorMax;
                RebuildContext.RectTransformFromEntity[rectEntity] = rectTransform;
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
