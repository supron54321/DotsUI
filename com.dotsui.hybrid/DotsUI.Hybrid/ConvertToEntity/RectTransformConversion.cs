using DotsUI.Core;
using System;
using TMPro;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;
using RectTransform = UnityEngine.RectTransform;

namespace DotsUI.Hybrid
{
	[UpdateInGroup(typeof(GameObjectConversionGroup))]
    class RectTransformConversion : GameObjectConversionSystem
    {
        public static Sprite DefaultSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0.0f, 0.0f, 0.0f, 0.0f), Vector2.zero);

		protected override void OnCreate()
        {
            base.OnCreate();
            InitEntityQueryCache(20);
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((RectTransform transform) => { Convert(transform); });
            Entities.ForEach((Canvas canvas) => { ConvertCanvas(canvas); });
            Entities.ForEach((CanvasScaler scaler) => { ConvertScaler(scaler); });
            Entities.ForEach((TMP_InputField selectable) => { ConvertInputField(selectable); ConvertSelectable(selectable); });
            Entities.ForEach((ScrollRect scrollRect) => { ConvertScrollRect(scrollRect); });
            Entities.ForEach((Scrollbar scrollBar) => { ConvertScrollBar(scrollBar); ConvertSelectable(scrollBar); });
            Entities.ForEach((Mask mask) => { ConvertMask(mask); });
            Entities.ForEach((RectMask2D mask) => { ConvertMask(mask); });
        }

        private void ConvertMask(Mask mask)
        {
            var entity = GetPrimaryEntity(mask);
            DstEntityManager.AddComponent<RectMask>(entity);
        }
        private void ConvertMask(RectMask2D mask)
        {
            var entity = GetPrimaryEntity(mask);
            DstEntityManager.AddComponent<RectMask>(entity);
        }

        private void ConvertScrollBar(Scrollbar scrollBar)
        {
            var entity = GetPrimaryEntity(scrollBar);
            var scrollHandle = GetPrimaryEntity(scrollBar.handleRect);
            DstEntityManager.AddComponentData(entity, new DotsUI.Controls.ScrollBar()
            {
                ScrollHandle = scrollHandle,
                Value = scrollBar.value,
                ParentScrollRect = GetPrimaryEntity(scrollBar.GetComponentsInParent<ScrollRect>(true)[0])  // temporary workaround for inactive transforms
            });
            DstEntityManager.AddComponentData(entity, new Controls.ScrollBarHandle()
            {

            });
            var pointerInputReceiver = GetOrAddComponent<Input.PointerInputReceiver>(DstEntityManager, entity);
            pointerInputReceiver.ListenerTypes |= Input.PointerEventType.BeginDrag | Input.PointerEventType.Drag |
                                                  Input.PointerEventType.EndDrag;
            DstEntityManager.SetComponentData(entity, pointerInputReceiver);
        }

        private void ConvertScrollRect(ScrollRect scrollRect)
        {
            var entity = GetPrimaryEntity(scrollRect);
            DstEntityManager.AddComponentData(entity, new DotsUI.Controls.ScrollRect()
            {
                Content = GetPrimaryEntity(scrollRect.content),
                Viewport = GetPrimaryEntity(scrollRect.viewport),
                HorizontalBar = GetPrimaryEntity(scrollRect.horizontalScrollbar),
                VerticalBar = GetPrimaryEntity(scrollRect.verticalScrollbar),
                HorizontalBarSpacing = scrollRect.horizontalScrollbarSpacing,
                VerticalBarSpacing = scrollRect.verticalScrollbarSpacing
            });
            var pointerInputReceiver = GetOrAddComponent<Input.PointerInputReceiver>(DstEntityManager, entity);
            pointerInputReceiver.ListenerTypes |= Input.PointerEventType.BeginDrag | Input.PointerEventType.Drag |
                                                  Input.PointerEventType.EndDrag;
            DstEntityManager.SetComponentData(entity, pointerInputReceiver);
        }

        private void ConvertInputField(TMP_InputField inputField)
        {
            var entity = GetPrimaryEntity(inputField);
            DstEntityManager.AddComponentData(entity, new Input.KeyboardInputReceiver());
            DstEntityManager.AddBuffer<Input.KeyboardInputBuffer>(entity);
            Entity target = TryGetPrimaryEntity(inputField.textComponent);
            Entity placeholder = TryGetPrimaryEntity(inputField.placeholder);
            DstEntityManager.AddComponentData(entity, new Controls.InputField()
            {
                Target = target,
                Placeholder = placeholder
            });
            DstEntityManager.AddComponentData(entity, new Controls.InputFieldCaretState()
            {
                CaretPosition = 0
            });
        }

        private void ConvertSelectable(Selectable selectable)
        {
            var entity = GetPrimaryEntity(selectable);
            DstEntityManager.AddComponent(entity, typeof(DotsUI.Input.Selectable));
            var colors = selectable.colors;
            Entity target = TryGetPrimaryEntity(selectable.targetGraphic);
            DstEntityManager.AddComponentData(entity, new DotsUI.Input.SelectableColor()
            {
                Normal = colors.normalColor.ToFloat4(),
                Hover = colors.highlightedColor.ToFloat4(),
                Pressed = colors.pressedColor.ToFloat4(),
                Selected = colors.selectedColor.ToFloat4(),
                Disabled = colors.disabledColor.ToFloat4(),
                TransitionTime = colors.fadeDuration,
                Target = target
            });

            var pointerInputReceiver = GetOrAddComponent<Input.PointerInputReceiver>(DstEntityManager, entity);
            pointerInputReceiver.ListenerTypes |= Input.PointerEventType.SelectableGroup;
            DstEntityManager.SetComponentData(entity, pointerInputReceiver);
        }

        private void ConvertGraphic(Graphic graphic)
        {
            var entity = GetPrimaryEntity(graphic);
            DstEntityManager.AddComponentData(entity, new VertexColorValue()
            {
                Value = graphic.color.ToFloat4()
            });
            DstEntityManager.AddComponentData(entity, new VertexColorMultiplier()
            {
                Value = new float4(1.0f, 1.0f, 1.0f, 1.0f)
            });
            DstEntityManager.AddComponent(entity, typeof(ElementVertexPointerInMesh));
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

            if (transform.childCount != 0)
            {
                var childBuffer = DstEntityManager.AddBuffer<Child>(entity);
                for (int i = 0; i < transform.childCount; i++)
                {
                    var child = transform.GetChild(i);
                    if (child is RectTransform rectChild)
                    {
                        childBuffer.Add(new Child
                        {
                            Value = GetPrimaryEntity(rectChild)
                        });
                    }
                }
            }

            if (HasPrimaryEntity(transform.parent))
            {
                DstEntityManager.AddComponentData(entity, new PreviousParent()
                {
                    Value = GetPrimaryEntity(transform.parent)
                });
            }
        }
        static TComponent GetOrAddComponent<TComponent>(EntityManager mgr, Entity entity) where TComponent : struct, IComponentData
        {
            if (mgr.HasComponent<TComponent>(entity))
                return mgr.GetComponentData<TComponent>(entity);
            mgr.AddComponent<TComponent>(entity);
            return default;
        }
    }
}
