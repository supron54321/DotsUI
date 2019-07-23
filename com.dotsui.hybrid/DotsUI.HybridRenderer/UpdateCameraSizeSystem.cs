using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotsUI.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace DotsUI.UnityEngineBackend
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
                    ComponentType.ReadOnly<RebuildCanvasHierarchyFlag>(),
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
                foreach (var chunk in chunkArray)
                {
                    var camera = chunk.GetSharedComponentData(cameraType, EntityManager);
                    var canvasSize = new CanvasScreenSize()
                    {
                        Value = new float2(camera.Target.UnityCamera.pixelWidth, camera.Target.UnityCamera.pixelHeight)
                    };
                    chunk.SetChunkComponentData(sizeType, canvasSize);
                }
            }
        }
    }
}
