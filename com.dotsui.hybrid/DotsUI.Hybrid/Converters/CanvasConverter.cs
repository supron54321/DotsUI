using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using DotsUI.Core;
using DotsUI.Hybrid;
using Unity.Mathematics;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace DotsUI.Hybrid{
    internal class CanvasConverter : TypedConverter<Canvas>
    {
        public CanvasConverter(){

        }
        protected override void ConvertComponent(Canvas unityComponent, Entity entity, RectTransformToEntity rectTransformToEntity, Dictionary<UnityEngine.Object, Entity> assetToEntity, EntityManager commandBuffer)
        {
            CanvasSortLayer image = new CanvasSortLayer{
                Value = unityComponent.sortingOrder
            };
            commandBuffer.AddComponentData(entity, image);
            commandBuffer.AddComponent(entity, typeof(RebuildCanvasHierarchyFlag));
            commandBuffer.AddBuffer<MeshVertex>(entity);
            commandBuffer.AddBuffer<MeshVertexIndex>(entity);
            commandBuffer.AddBuffer<SubMeshInfo>(entity);
            if(unityComponent.renderMode != RenderMode.ScreenSpaceCamera)
                throw new InvalidOperationException($"Canvas ({unityComponent}) render mode ({unityComponent.renderMode}) is not supported yet");
            if(unityComponent.worldCamera == null)
                throw new InvalidOperationException($"Target camera is null or destroyed. Canvas {unityComponent}");
            var camera = unityComponent.worldCamera;
            var proxy = camera.GetComponent<CameraImageRenderProxy>();
            if (proxy == null)
                proxy = camera.gameObject.AddComponent<CameraImageRenderProxy>();
            commandBuffer.AddSharedComponentData(entity, new CanvasTargetCamera()
            {
                Target = proxy
            });
            commandBuffer.AddComponentData(entity, new CanvasScreenSize
            {
                Value = new int2(camera.pixelWidth, camera.pixelHeight)
            });
        }
    }

    internal class CanvasScalerConverter : TypedConverter<UnityEngine.UI.CanvasScaler>
    {
        public CanvasScalerConverter(){

        }
        protected override void ConvertComponent(UnityEngine.UI.CanvasScaler unityComponent, Entity entity, RectTransformToEntity rectTransformToEntity, Dictionary<UnityEngine.Object, Entity> assetToEntity, EntityManager commandBuffer)
        {
            if(unityComponent.uiScaleMode == UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPhysicalSize)
            {
                var scaleFactor = 1.0f;
                switch (unityComponent.physicalUnit)
                {
                    case CanvasScaler.Unit.Centimeters: scaleFactor = 1.0f/2.54f; break;
                    case CanvasScaler.Unit.Millimeters: scaleFactor = 1.0f / 25.4f; break;
                    case CanvasScaler.Unit.Inches: scaleFactor = 1.0f / 1; break;
                    case CanvasScaler.Unit.Points: scaleFactor = 1.0f / 72; break;
                    case CanvasScaler.Unit.Picas: scaleFactor = 1.0f / 6; break;
                }
                commandBuffer.AddComponentData(entity, new CanvasConstantPhysicalSizeScaler(){
                    Factor = scaleFactor
                });
            }
            else if(unityComponent.uiScaleMode == UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize)
            {
                commandBuffer.AddComponentData(entity, new CanvasConstantPixelSizeScaler());
            }
        }
    }
}