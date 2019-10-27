using DotsUI.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DotsUI.Hybrid
{

    [UnityEngine.ExecuteAlways]
    [UpdateInGroup(typeof(BeforeRectTransformUpdateGroup))]
    class UpdateScreenSpaceOverlaySize : ComponentSystem
    {
        private EntityQuery m_UpdateSizeGroup;
        private int m_OldWidth;
        private int m_OldHeight;

        protected override void OnCreate()
        {
            m_OldHeight = Screen.width;
            m_OldHeight = Screen.height;

            m_UpdateSizeGroup = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[]
                {
                    ComponentType.ReadOnly<CanvasScreenSpaceOverlay>(),
                    ComponentType.ReadWrite<CanvasScreenSize>(),
                }
            });
        }

        protected override void OnUpdate()
        {
            int newWidth = Screen.width;
            int newHeight = Screen.height;
            if (m_OldWidth != newWidth || m_OldHeight != newHeight)
            {
                using (var chunkArray = m_UpdateSizeGroup.CreateArchetypeChunkArray(Allocator.TempJob))
                {
                    var sizeType = GetArchetypeChunkComponentType<CanvasScreenSize>();
                    var entityType = GetArchetypeChunkEntityType();
                    NativeQueue<Entity> commandBuffer = new NativeQueue<Entity>(Allocator.Temp);
                    foreach (var chunk in chunkArray)
                    {
                        var canvasSize = new CanvasScreenSize()
                        {
                            Value = new int2(newWidth, newHeight)
                        };

                        var sizeArray = chunk.GetNativeArray(sizeType);
                        var entityArray = chunk.GetNativeArray(entityType);
                        for (int i = 0; i < sizeArray.Length; i++)
                        {
                            if (!sizeArray[i].Value.Equals(canvasSize.Value))
                            {
                                commandBuffer.Enqueue(entityArray[i]);
                                sizeArray[i] = canvasSize;
                            }
                        }
                    }
                    while (commandBuffer.TryDequeue(out var entity))
                        EntityManager.AddComponent<RebuildCanvasHierarchyFlag>(entity);
                }

                m_OldWidth = newWidth;
                m_OldHeight = newHeight;
            }
            
        }
    }
}
