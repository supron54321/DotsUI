using System.Collections.Generic;
using System.Text;
using DotsUI.Core;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
#if UNITY_2019_3_OR_NEWER
using UnityEngine.PlayerLoop;
#else
using UnityEngine.Experimental.PlayerLoop;
#endif
using UnityEngineInternal.Input;
using Unity.Entities;
using RectTransform = DotsUI.Core.RectTransform;

namespace DotsUI.Input
{
    [UpdateInGroup(typeof(InputSystemGroup))]
    public class ControlsInputSystem : ComponentSystemGroup
    {

    }
    [UpdateInGroup(typeof(ControlsInputSystem))]
    [UpdateAfter(typeof(ControlsInputSystemHandler))]
    public class ControlsInputBufferSystem : EntityCommandBufferSystem { }

    public enum PointerButton
    {
        Invalid = -1,
        /// <summary>
        /// Left button
        /// </summary>
        Left = 0,

        /// <summary>
        /// Right button.
        /// </summary>
        Right = 1,

        /// <summary>
        /// Middle button
        /// </summary>
        Middle = 2
    }
    /// <summary>
    /// This structure is similar to UnityEngine.EventSystems
    /// </summary>
    public struct PointerEventData
    {

        public int PointerId;
        public float2 Position;
        public float2 Delta;
        public float2 PressPosition;
        public float ClickTime;
        public int ClickCount;
        public float2 ScrollDelta;
        public bool UseDragThreshold;
        public bool IsDragging;
        public PointerButton Button;
        public Entity PressEntity { get; set; }
    }

    internal struct MouseButtonState
    {
        public bool Pressed;
        public bool IsDragging;
        public float2 PressPosition;
        public Entity PressEntity;
    }

    internal struct MouseInputFrameData
    {
        public float2 Position;
        public float2 Delta;
    }

    internal struct InputSystemState : IComponentData
    {
        public Entity HoveredEntity;
        public Entity SelectedEntity;
    }

    [UpdateInGroup(typeof(ControlsInputSystem))]
    public class ControlsInputSystemHandler : JobComponentSystem
    {
        private EntityQuery m_PointerGroup;

        private NativeHashMap<int, Touch> m_Touches;
        private EntityQuery m_RootGroup;
        private float2 m_LastFrameMousePos;


        private EntityArchetype m_PointerEventArchetype;
        private EntityArchetype m_KeyboardEventArchetype;

        private NativeArray<MouseButtonState> m_ButtonStates;

        private ControlsInputBufferSystem m_CommandBufferSystem;

        public float DragThreshold { get; set; }

        protected override void OnCreateManager()
        {
            DragThreshold = 3.0f;
            m_ButtonStates = new NativeArray<MouseButtonState>(3, Allocator.Persistent);
            m_CommandBufferSystem = World.GetOrCreateSystem<ControlsInputBufferSystem>();

            m_PointerEventArchetype = EntityManager.CreateArchetype(typeof(PointerEvent), typeof(PointerInputBuffer));
            m_KeyboardEventArchetype =
                EntityManager.CreateArchetype(typeof(KeyboardEvent), typeof(KeyboardInputBuffer));
            m_PointerGroup = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[]
                {
                    ComponentType.ReadOnly<PointerInputReceiver>(),
                    ComponentType.ReadOnly<RectTransform>()
                },
                Any = new[]
                {
                    ComponentType.ReadOnly<WorldSpaceRect>(),
                }
            });

            m_RootGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<RectTransform>(),
                    ComponentType.ReadOnly<UIChild>(),
                    ComponentType.ReadOnly<WorldSpaceRect>(),
                },
                None = new ComponentType[]
                {
                    typeof(UIParent)
                },
                Options = EntityQueryOptions.FilterWriteGroup
            });

            m_Touches = new NativeHashMap<int, Touch>(5, Allocator.Persistent);
            EntityManager.CreateEntity(typeof(InputSystemState));
            EntityManager.CreateEntity(typeof(NativePointerInputContainer), typeof(NativePointerButtonEvent));
            EntityManager.CreateEntity(typeof(NativeKeyboardInputContainer), typeof(NativeKeyboardInputEvent));
            base.OnCreateManager();
        }

        protected override void OnDestroyManager()
        {
            m_Touches.Dispose();
            m_ButtonStates.Dispose();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            GatherEvents(Allocator.TempJob, out var pointerEvents, out var keyboardEvents);
            var pointerFrameData = GatherPointerFrameData(Allocator.TempJob);

            NativeArray<Entity> roots = m_RootGroup.ToEntityArray(Allocator.TempJob);
            var childrenFromEntity = GetBufferFromEntity<UIChild>(true);
            var worldSpaceRectFromEntity = GetComponentDataFromEntity<WorldSpaceRect>(true);
            var parentFromEntity = GetComponentDataFromEntity<UIParent>(true);
            var pointerReceiverFromEntity = GetComponentDataFromEntity<PointerInputReceiver>(true);

            var stateComponentFromEntity = GetComponentDataFromEntity<InputSystemState>();
            var selectableFromEntity = GetComponentDataFromEntity<Selectable>();
            var stateEntity = GetSingletonEntity<InputSystemState>();

            NativeArray<Entity> perCanvasHits =
                new NativeArray<Entity>(pointerFrameData.Length * roots.Length, Allocator.TempJob);
            NativeArray<Entity> globalHits = new NativeArray<Entity>(pointerFrameData.Length, Allocator.TempJob);
            LowLevelUtils.MemSet(perCanvasHits, default);
            LowLevelUtils.MemSet(globalHits, default);
            ProcessPerCanvasInput process = new ProcessPerCanvasInput()
            {
                Roots = roots,
                ChildrenFromEntity = childrenFromEntity,
                Hits = perCanvasHits,
                LocalToWorldFromEntity = worldSpaceRectFromEntity,
                PointerInputReceiver = pointerReceiverFromEntity,
                PointersPosition = pointerFrameData
            };
            inputDeps = process.Schedule(roots.Length, 1, inputDeps);
            var canvasLayerFromEntity = GetComponentDataFromEntity<CanvasSortLayer>();
            LowLevelUtils.MemSet(globalHits, default);
            CanvasHitsToGlobal canvasHits = new CanvasHitsToGlobal()
            {
                Roots = roots,
                CanvasLayerFromEntity = canvasLayerFromEntity,
                GlobalHits = globalHits,
                PerCanvasHits = perCanvasHits
            };
            inputDeps = canvasHits.Schedule(inputDeps);

            EntityCommandBuffer ecb = m_CommandBufferSystem.CreateCommandBuffer();
            UpdatePointerEvents updatePointerJob = new UpdatePointerEvents()
            {
                ButtonStates = m_ButtonStates,
                EventArchetype = m_PointerEventArchetype,
                StateEntity = stateEntity,
                Hits = globalHits,
                ReceiverFromEntity = pointerReceiverFromEntity,
                StateFromEntity = stateComponentFromEntity,
                Manager = ecb,
                PointerEvents = pointerEvents,
                ParentFromEntity = parentFromEntity,
                PointerFrameData = pointerFrameData,
                DragThreshold = DragThreshold,
            };
            inputDeps = updatePointerJob.Schedule(inputDeps);
            inputDeps.Complete();

            if (keyboardEvents.Length > 0)
            {
                var selected = GetSingleton<InputSystemState>().SelectedEntity;
                if (selected != default)
                {
                    var eventEntity = EntityManager.CreateEntity(m_KeyboardEventArchetype);
                    var buff = EntityManager.GetBuffer<KeyboardInputBuffer>(eventEntity);
                    EntityManager.SetComponentData(eventEntity, new KeyboardEvent()
                    {
                        Target = selected
                    });
                    for (int i = 0; i < keyboardEvents.Length; i++)
                    {
                        if (keyboardEvents[i].EventType == NativeInputEventType.KeyDown)
                            buff.Add(new KeyboardInputBuffer()
                            {
                                Character = keyboardEvents[i].Character,
                                EventType = keyboardEvents[i].KbdEvent,
                                KeyCode = keyboardEvents[i].KeyCode
                            });
                    }
                }
            }
            keyboardEvents.Dispose();
            m_LastFrameMousePos = ((float3)UnityEngine.Input.mousePosition).xy;
            return inputDeps;
        }

        private NativeArray<MouseInputFrameData> GatherPointerFrameData(Allocator allocator)
        {
            NativeArray<MouseInputFrameData> ret = new NativeArray<MouseInputFrameData>(1 + UnityEngine.Input.touchCount, allocator);
            float2 mousePos = ((float3)UnityEngine.Input.mousePosition).xy;
            ret[0] = new MouseInputFrameData()
            {
                Position = mousePos,
                Delta = mousePos - m_LastFrameMousePos
            };
            for (int i = 0; i < UnityEngine.Input.touchCount; i++)
            {
                Touch touch = UnityEngine.Input.GetTouch(i);
                ret[i + 1] = new MouseInputFrameData()
                {
                    Position = touch.position,
                    Delta = touch.deltaPosition
                };
            }

            return ret;
        }

        private void GatherEvents(Allocator allocator, out NativeArray<(NativeInputEventType, PointerButton)> pointerEventsArray, out NativeArray<NativeKeyboardInputEvent> keyboardEventArray)
        {
            var pointerContainerEntity = GetSingletonEntity<NativePointerInputContainer>();
            var keyboardContainerEntity = GetSingletonEntity<NativeKeyboardInputContainer>();

            var pointerBuffer = EntityManager.GetBuffer<NativePointerButtonEvent>(pointerContainerEntity);
            var keyboardBuffer = EntityManager.GetBuffer<NativeKeyboardInputEvent>(keyboardContainerEntity);

            pointerEventsArray = new NativeArray<(NativeInputEventType, PointerButton)>(pointerBuffer.Length, allocator);
            keyboardEventArray = new NativeArray<NativeKeyboardInputEvent>(keyboardBuffer.Length, allocator);

            for (int i = 0; i < pointerBuffer.Length; i++)
                pointerEventsArray[i] = (pointerBuffer[i].EventType, pointerBuffer[i].Button);

            for (int i = 0; i < keyboardBuffer.Length; i++)
                keyboardEventArray[i] = keyboardBuffer[i];

            pointerBuffer.Clear();
            keyboardBuffer.Clear();
        }

        private NativeArray<float2> GatherPointersPositions(Allocator allocator)
        {
            var ret = new NativeArray<float2>(UnityEngine.Input.touchCount + 1, allocator);
            ret[0] = ((float3)(UnityEngine.Input.mousePosition)).xy;
            for (int i = 0; i < UnityEngine.Input.touchCount; i++)
            {
                ret[i + 1] = UnityEngine.Input.GetTouch(i).position;
            }

            return ret;
        }


        bool IsValidKeyboardEventReceiver(Entity entity)
        {
            return EntityManager.Exists(entity) &&
                   EntityManager.HasComponent<KeyboardInputReceiver>(entity);
        }
    }
}