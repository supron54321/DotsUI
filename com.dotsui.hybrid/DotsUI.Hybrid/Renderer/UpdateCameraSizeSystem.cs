using DotsUI.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace DotsUI.Hybrid
{

    [UpdateInGroup(typeof(BeforeRectTransformUpdateGroup))]
    class UpdateCameraSizeSystem : ComponentSystem
    {
        private EntityQuery m_UpdateSizeGroup;
        protected override void OnCreate()
        {
            m_UpdateSizeGroup = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[]
                {
                    ComponentType.ReadOnly<CanvasTargetCamera>(),
                    ComponentType.ReadWrite<CanvasScreenSize>(),
                }
            });
        }

        protected override void OnUpdate()
        {
            using (var chunkArray = m_UpdateSizeGroup.CreateArchetypeChunkArray(Allocator.TempJob))
            {
                var cameraType = GetArchetypeChunkSharedComponentType<CanvasTargetCamera>();
                var sizeType = GetArchetypeChunkComponentType<CanvasScreenSize>();
                var entityType = GetArchetypeChunkEntityType();
                NativeQueue<Entity> commandBuffer = new NativeQueue<Entity>(Allocator.Temp);
                foreach (var chunk in chunkArray)
                {
                    var camera = chunk.GetSharedComponentData(cameraType, EntityManager);

                    var canvasSize = new CanvasScreenSize()
                    {
                        Value = new int2(camera.Target.UnityCamera.pixelWidth, camera.Target.UnityCamera.pixelHeight)
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
                while(commandBuffer.TryDequeue(out var entity))
                    EntityManager.AddComponent<RebuildCanvasHierarchyFlag>(entity);
            }
        }
    }
}
