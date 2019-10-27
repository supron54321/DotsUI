using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotsUI.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace DotsUI.Hybrid
{
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    class CanvasConversionSystem : DotsUIConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach<Canvas>(ConvertCanvas);
            Entities.ForEach<CanvasScaler>(ConvertScaler);
        }
        private void ConvertScaler(CanvasScaler scaler)
        {
            var entity = GetPrimaryEntity(scaler);
            if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPhysicalSize)
            {
                var scaleFactor = 1.0f;
                switch (scaler.physicalUnit)
                {
                    case CanvasScaler.Unit.Centimeters: scaleFactor = 1.0f / 2.54f; break;
                    case CanvasScaler.Unit.Millimeters: scaleFactor = 1.0f / 25.4f; break;
                    case CanvasScaler.Unit.Inches: scaleFactor = 1.0f / 1; break;
                    case CanvasScaler.Unit.Points: scaleFactor = 1.0f / 72; break;
                    case CanvasScaler.Unit.Picas: scaleFactor = 1.0f / 6; break;
                }
                DstEntityManager.AddComponentData(entity, new CanvasConstantPhysicalSizeScaler()
                {
                    Factor = scaleFactor
                });
            }
            else if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize)
            {
                DstEntityManager.AddComponentData(entity, new CanvasConstantPixelSizeScaler());
            }
            else
            {
                throw new NotSupportedException($"Canvas scaler mode {scaler.uiScaleMode} is not supported yet");
            }
        }

        private void ConvertCanvas(Canvas canvas)
        {
            var entity = GetPrimaryEntity(canvas);
            CanvasSortLayer image = new CanvasSortLayer
            {
                Value = canvas.sortingOrder
            };
            DstEntityManager.AddComponentData(entity, image);
            DstEntityManager.AddComponent(entity, typeof(RebuildCanvasHierarchyFlag));
            DstEntityManager.AddBuffer<MeshVertex>(entity);
            DstEntityManager.AddBuffer<MeshVertexIndex>(entity);
            DstEntityManager.AddBuffer<SubMeshInfo>(entity);
            if (canvas.renderMode == RenderMode.WorldSpace)
                throw new InvalidOperationException($"Canvas ({canvas}) render mode ({canvas.renderMode}) is not supported yet");

            if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
                SetUpScreenSpaceCamera(canvas, entity);
            else if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                SetUpScreenSpaceOverlay(canvas, entity);
        }

        private void SetUpScreenSpaceOverlay(Canvas canvas, Entity entity)
        {
            DstEntityManager.AddComponentData(entity, new CanvasScreenSpaceOverlay());
            DstEntityManager.AddComponentData(entity, new CanvasScreenSize
            {
                Value = new int2(Screen.width, Screen.height)
            });
        }


        private void SetUpScreenSpaceCamera(Canvas canvas, Entity entity)
        {
            if (canvas.worldCamera == null)
                throw new InvalidOperationException($"Target camera is null or destroyed. Canvas {canvas}");
            var proxy = canvas.worldCamera.GetComponent<CameraImageRenderProxy>();
            if (proxy == null)
                proxy = canvas.worldCamera.gameObject.AddComponent<CameraImageRenderProxy>();
            DstEntityManager.AddSharedComponentData(entity, new CanvasTargetCamera()
            {
                Target = proxy
            });
            DstEntityManager.AddComponentData(entity, new CanvasScreenSize
            {
                Value = new int2(canvas.worldCamera.pixelWidth, canvas.worldCamera.pixelHeight)
            });
        }
    }
}
