using Unity.Entities;

namespace DotsUI.Core
{
    [UpdateInGroup(typeof(BeforeRectTransformUpdateGroup))]
    class RectTransformMissingComponentsSystem : ComponentSystem
    {
        private EntityQuery m_MissingScale;
        private EntityQuery m_MissingWorldRect;
        private EntityQuery m_MissingWorldMask;

        protected override void OnCreate()
        {
            m_MissingScale = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    typeof(RectTransform)
                },
                None =  new ComponentType[]
                {
                    typeof(ElementScale)
                }
            });
            m_MissingWorldRect = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    typeof(RectTransform)
                },
                None = new ComponentType[]
                {
                    typeof(WorldSpaceRect)
                }
            });
            m_MissingWorldMask = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    typeof(RectTransform)
                },
                None = new ComponentType[]
                {
                    typeof(WorldSpaceMask)
                }
            });
        }

        protected override void OnUpdate()
        {
            EntityManager.AddComponent(m_MissingScale, typeof(ElementScale));
            EntityManager.AddComponent(m_MissingWorldRect, typeof(WorldSpaceRect));
            EntityManager.AddComponent(m_MissingWorldMask, typeof(WorldSpaceMask));
        }
    }
}
