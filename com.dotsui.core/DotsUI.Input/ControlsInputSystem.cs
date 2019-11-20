using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotsUI.Core;
using DotsUI.Core.Utils;
using DotsUI.Profiling;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsUI.Input
{
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

    public struct ControlPointerEvent : IComponentData
    {
        public int EventIdx;
        public int EventCount;
    }

    [UpdateInGroup(typeof(InputSystemGroup))]
    public class ControlsInputSystemGroup : ComponentSystemGroup
    {

    }

    [UpdateInGroup(typeof(ControlsInputSystemGroup))]
    public class ControlsInputSystem : JobComponentSystem
    {
        private EntityQuery m_RootGroup;
        private float2 m_LastFrameMousePos;

        private NativeArray<MouseButtonState> m_ButtonStates;

        private NativeMultiHashMap<Entity, PointerInputBuffer> m_PointerTargetToEvent;
        private NativeList<Entity> m_PointerEventList;
        private NativeMultiHashMap<Entity, KeyboardInputBuffer> m_KeyboardTargetToEvent;
        private NativeList<Entity> m_KeyboardEventList;

        public float DragThreshold = 3.0f;
        private NativeList<NativePointerButtonEvent> m_NativePointerEvents;
        private NativeList<NativeKeyboardInputEvent> m_NativeKeyboardEvents;

        protected override void OnCreate()
        {
            m_NativePointerEvents = new NativeList<NativePointerButtonEvent>(14, Allocator.Persistent);
            m_NativeKeyboardEvents = new NativeList<NativeKeyboardInputEvent>(14, Allocator.Persistent);

            m_PointerTargetToEvent = new NativeMultiHashMap<Entity, PointerInputBuffer>(16, Allocator.Persistent);
            m_PointerEventList = new NativeList<Entity>(16, Allocator.Persistent);

            m_KeyboardTargetToEvent = new NativeMultiHashMap<Entity, KeyboardInputBuffer>(16, Allocator.Persistent);
            m_KeyboardEventList = new NativeList<Entity>(16, Allocator.Persistent);

            m_ButtonStates = new NativeArray<MouseButtonState>(3, Allocator.Persistent);

            m_RootGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<RectTransform>(),
                    ComponentType.ReadOnly<Child>(),
                    ComponentType.ReadOnly<WorldSpaceRect>(),
                },
                None = new ComponentType[]
                {
                    typeof(Parent)
                },
                Options = EntityQueryOptions.FilterWriteGroup
            });

            EntityManager.CreateEntity(typeof(InputSystemState));
            EntityManager.CreateEntity(typeof(NativePointerInputContainer), typeof(NativePointerButtonEvent), typeof(NativePointerState));
            EntityManager.CreateEntity(typeof(NativeKeyboardInputContainer), typeof(NativeKeyboardInputEvent));
        }

        protected override void OnDestroy()
        {
            m_NativePointerEvents.Dispose();
            m_NativeKeyboardEvents.Dispose();

            m_PointerTargetToEvent.Dispose();
            m_PointerEventList.Dispose();

            m_KeyboardTargetToEvent.Dispose();
            m_KeyboardEventList.Dispose();

            m_ButtonStates.Dispose();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var pointerFrameData = GatherPointerFrameData(Allocator.TempJob);
            NativeArray<Entity> roots = m_RootGroup.ToEntityArray(Allocator.TempJob, out var rootsDeps);
            inputDeps = JobHandle.CombineDependencies(inputDeps, rootsDeps);
            var childrenFromEntity = GetBufferFromEntity<Child>(true);
            var worldSpaceRectFromEntity = GetComponentDataFromEntity<WorldSpaceRect>(true);
            var parentFromEntity = GetComponentDataFromEntity<Parent>(true);
            var pointerReceiverFromEntity = GetComponentDataFromEntity<PointerInputReceiver>(true);

            var stateComponentFromEntity = GetComponentDataFromEntity<InputSystemState>();
            var stateEntity = GetSingletonEntity<InputSystemState>();

            NativeArray<Entity> perCanvasHits =
                new NativeArray<Entity>(pointerFrameData.Length * roots.Length, Allocator.TempJob);
            NativeArray<Entity> globalHits = new NativeArray<Entity>(pointerFrameData.Length, Allocator.TempJob);
            var canvasLayerFromEntity = GetComponentDataFromEntity<CanvasSortLayer>();

            m_PointerTargetToEvent.Clear();
            m_PointerEventList.Clear();
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
            CanvasHitsToGlobal canvasHits = new CanvasHitsToGlobal()
            {
                Roots = roots,
                CanvasLayerFromEntity = canvasLayerFromEntity,
                GlobalHits = globalHits,
                PerCanvasHits = perCanvasHits
            };
            inputDeps = canvasHits.Schedule(inputDeps);
            UpdatePointerEvents updatePointerJob = new UpdatePointerEvents()
            {
                ButtonStates = m_ButtonStates,
                StateEntity = stateEntity,
                Hits = globalHits,
                ReceiverFromEntity = pointerReceiverFromEntity,
                StateFromEntity = stateComponentFromEntity,
                PointerEvents = m_NativePointerEvents.AsParallelReader(),
                ParentFromEntity = parentFromEntity,
                PointerFrameData = pointerFrameData,
                DragThreshold = DragThreshold,
                TargetToEvent = m_PointerTargetToEvent,
                PointerEventList = m_PointerEventList,
            };
            inputDeps = updatePointerJob.Schedule(inputDeps);
            UpdateKeyboardEvents updateKeyboardJob = new UpdateKeyboardEvents()
            {
                KeyboardEvents = m_NativeKeyboardEvents.AsParallelReader(),
                KeyboardTargetToEvent = m_KeyboardTargetToEvent,
                KeyboardTargetList = m_KeyboardEventList,
                StateEntity = stateEntity,
                StateFromEntity = stateComponentFromEntity
            };
            inputDeps = updateKeyboardJob.Schedule(inputDeps);
            ClearNativeBuffers clearBuffers = new ClearNativeBuffers()
            {
                PointerEventBuffer = m_NativePointerEvents,
                KeyboardEventBuffer = m_NativeKeyboardEvents,
            };
            inputDeps = clearBuffers.Schedule(inputDeps);
            inputDeps.Complete();

            return inputDeps;
        }

        internal NativeList<Entity> QueryPointerEvents(ComponentType requiredType, Allocator allocator)
        {
            return QueryListByComponent(m_PointerEventList, requiredType, allocator);
        }

        internal NativeList<Entity> QueryKeyboardEvents(ComponentType requiredType, Allocator allocator)
        {
            return QueryListByComponent(m_KeyboardEventList, requiredType, allocator);
        }

        NativeList<Entity> QueryListByComponent(NativeList<Entity> entityList, ComponentType requiredType, Allocator allocator)
        {
            NativeList<Entity> ret = new NativeList<Entity>(allocator);
            for (int i = 0; i < entityList.Length; i++)
            {
                if (EntityManager.HasComponent(entityList[i], requiredType))
                    ret.Add(entityList[i]);
            }

            if (ret.Length > 1)
            {
                for(int i = 0; i < ret.Length; i++)
                    UnityEngine.Debug.Log($"registered: {ret[i]}");
            }

            return ret;
        }

        public NativeEventWriter GetEventWriter()
        {
            return new NativeEventWriter(m_NativePointerEvents, m_NativeKeyboardEvents);
        }

        /// <summary>
        /// TODO: This function has to be refactored and moved to the Hybrid package.
        /// </summary>
        /// <param name="allocator"></param>
        /// <returns></returns>
        private NativeArray<MouseInputFrameData> GatherPointerFrameData(Allocator allocator)
        {
            using (new ProfilerSample("GatherPointerFrameData"))
            {
                NativeArray<MouseInputFrameData> ret =
                    new NativeArray<MouseInputFrameData>(1 + UnityEngine.Input.touchCount, allocator);
                float2 mousePos = ((float3)UnityEngine.Input.mousePosition).xy;
                ret[0] = new MouseInputFrameData()
                {
                    Position = mousePos,
                    Delta = mousePos - m_LastFrameMousePos
                };
                for (int i = 0; i < UnityEngine.Input.touchCount; i++)
                {
                    UnityEngine.Touch touch = UnityEngine.Input.GetTouch(i);
                    ret[i + 1] = new MouseInputFrameData()
                    {
                        Position = touch.position,
                        Delta = touch.deltaPosition
                    };
                }

                return ret;
            }
        }

        internal NativeMultiHashMap<Entity, PointerInputBuffer> GetPointerEventMap()
        {
            return m_PointerTargetToEvent;
        }

        internal NativeMultiHashMap<Entity, KeyboardInputBuffer> GetKeyboardEventMap()
        {
            return m_KeyboardTargetToEvent;
        }
    }
}
