using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotsUI.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;
using RectTransform = UnityEngine.RectTransform;

namespace DotsUI.Hybrid
{
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    class RectTransformConversion : GameObjectConversionSystem
    {
        Sprite m_DefaultSprite;
        protected override void OnCreate()
        {
            base.OnCreate();

            m_DefaultSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0.0f, 0.0f, 0.0f, 0.0f), Vector2.zero);
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((RectTransform transform) => { Convert(transform); });
            Entities.ForEach((Canvas canvas) => { ConvertCanvas(canvas); });
            Entities.ForEach((CanvasScaler scaler) => { ConvertScaler(scaler); });
            Entities.ForEach((Image image) => { ConvertImage(image); });
        }

        private void ConvertImage(Image image)
        {
            var assetQuery = DstEntityManager.CreateEntityQuery(ComponentType.ReadOnly<SpriteAsset>());
            var entity = GetPrimaryEntity(image);
            var sprite = image.sprite ?? m_DefaultSprite;
            var asset = new SpriteAsset()
            {
                Value = sprite,
            };
            assetQuery.SetFilter(asset);
            Entity assetEntity;
            if (assetQuery.CalculateEntityCount() == 0)
            {
                assetEntity = DstEntityManager.CreateEntity(typeof(SpriteAsset), typeof(SpriteVertexData));
                DstEntityManager.SetSharedComponentData(assetEntity, new SpriteAsset { Value = sprite });
            }
            else
            {
                using (var assetEntityArray = assetQuery.ToEntityArray(Allocator.TempJob))
                    assetEntity = assetEntityArray[0];
            }
            SpriteImage spriteImage = new SpriteImage
            {
                Asset = assetEntity
            };
            DstEntityManager.AddComponentData(entity, spriteImage);
            DstEntityManager.AddBuffer<ControlVertexData>(entity);
            DstEntityManager.AddBuffer<ControlVertexIndex>(entity);
            DstEntityManager.AddComponent(entity, typeof(ElementVertexPointerInMesh));
            DstEntityManager.AddComponent(entity, typeof(RebuildElementMeshFlag));
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
            if (canvas.renderMode != RenderMode.ScreenSpaceCamera)
                throw new InvalidOperationException($"Canvas ({canvas}) render mode ({canvas.renderMode}) is not supported yet");
            if (canvas.worldCamera == null)
                throw new InvalidOperationException($"Target camera is null or destroyed. Canvas {canvas}");
            var proxy = canvas.worldCamera.GetComponent<CameraImageRenderProxy>();
            if (proxy == null)
                proxy = canvas.worldCamera.gameObject.AddComponent<CameraImageRenderProxy>();
            DstEntityManager.AddSharedComponentData(entity, new CanvasTargetCamera()
            {
                Target = proxy
            });
        }

        private void Convert(RectTransform transform)
        {
            var entity = GetPrimaryEntity(transform);

            DstEntityManager.AddComponentData(entity, new DotsUI.Core.RectTransform()
            {
                AnchorMin = transform.anchorMin,
                AnchorMax = transform.anchorMax,
                Pivot = transform.pivot,
                Position = transform.anchoredPosition,
                SizeDelta = transform.sizeDelta,
            });

            DstEntityManager.AddComponent(entity, typeof(WorldSpaceRect));
            DstEntityManager.AddComponent(entity, typeof(WorldSpaceMask));

            DstEntityManager.RemoveComponent(entity, typeof(Translation));
            DstEntityManager.RemoveComponent(entity, typeof(Rotation));
            DstEntityManager.RemoveComponent(entity, typeof(NonUniformScale));
        }
    }
}
