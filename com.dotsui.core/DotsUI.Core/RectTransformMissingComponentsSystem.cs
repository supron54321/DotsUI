using Unity.Entities;

namespace DotsUI.Core
{
    [UnityEngine.ExecuteAlways]
    [UpdateInGroup(typeof(BeforeRectTransformUpdateGroup))]
    class RectTransformMissingComponentsSystem : ComponentSystem
    {
        private EntityQuery m_MissingScale;
        private EntityQuery m_MissingWorldRect;
        private EntityQuery m_MissingWorldMask;
        private EntityQuery m_MissingCanvasReference;
        private EntityQuery m_MissingHierarchyIndex;

        protected override void OnCreate()
        {
            m_MissingScale = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    typeof(RectTransform)
                },
                None = new ComponentType[]
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
            m_MissingCanvasReference = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    typeof(RectTransform)
                },
                None = new ComponentType[]
                {
                    typeof(ElementCanvasReference),
                    typeof(CanvasScreenSize)    // should be enough to exclude canvases
                }
            });
            m_MissingHierarchyIndex = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    typeof(RectTransform),
                },
                None = new ComponentType[]
                {
                    typeof(ElementHierarchyIndex),
                }
            });
        }

        protected override void OnUpdate()
        {
            EntityManager.AddComponent(m_MissingScale, typeof(ElementScale));
            EntityManager.AddComponent(m_MissingWorldRect, typeof(WorldSpaceRect));
            EntityManager.AddComponent(m_MissingWorldMask, typeof(WorldSpaceMask));
            EntityManager.AddComponent(m_MissingCanvasReference, typeof(ElementCanvasReference));
            EntityManager.AddComponent(m_MissingHierarchyIndex, typeof(ElementHierarchyIndex));
        }
    }
}
