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
        private NativeList<PointerInputBuffer> m_PointerBuffer;

        private EntityQuery m_RootGroup;
        private float2 m_LastFrameMousePos;


        private EntityArchetype m_PointerEventArchetype;
        private EntityArchetype m_KeyboardEventArchetype;

        private NativeArray<MouseButtonState> m_ButtonStates;

        private NativeMultiHashMap<Entity, PointerInputBuffer> m_PointerTargetToEvent;
        private NativeList<Entity> m_PointerEventList;

        public float DragThreshold { get; set; }

        protected override void OnCreate()
        {
            m_PointerTargetToEvent = new NativeMultiHashMap<Entity, PointerInputBuffer>(16, Allocator.Persistent);
            m_PointerEventList = new NativeList<Entity>(16, Allocator.Persistent);


            DragThreshold = 3.0f;
            m_ButtonStates = new NativeArray<MouseButtonState>(3, Allocator.Persistent);

            m_PointerEventArchetype = EntityManager.CreateArchetype(typeof(PointerEvent), typeof(PointerInputBuffer));
            m_KeyboardEventArchetype =
                EntityManager.CreateArchetype(typeof(KeyboardEvent), typeof(KeyboardInputBuffer));

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
            m_PointerTargetToEvent.Dispose();
            m_PointerEventList.Dispose();
        }

        public NativeSlice<PointerInputBuffer> GetEntityEvents(ControlPointerEvent evt)
        {
            return m_PointerBuffer.AsArray().Slice(evt.EventIdx, evt.EventCount);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            GatherEvents(Allocator.TempJob, out var pointerEvents, out var keyboardEvents);
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
            LowLevelUtils.MemSet(perCanvasHits, default);
            LowLevelUtils.MemSet(globalHits, default);

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
            var canvasLayerFromEntity = GetComponentDataFromEntity<CanvasSortLayer>();
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
                PointerEvents = pointerEvents,
                ParentFromEntity = parentFromEntity,
                PointerFrameData = pointerFrameData,
                DragThreshold = DragThreshold,
                TargetToEvent = m_PointerTargetToEvent,
                PointerEventList = m_PointerEventList,
            };
            inputDeps = updatePointerJob.Schedule(inputDeps);
            keyboardEvents.Dispose();
            inputDeps.Complete();

            return inputDeps;
        }

        internal NativeList<Entity> QueryPointerEvents(ComponentType requiredType, Allocator allocator)
        {
            NativeList<Entity> ret = new NativeList<Entity>(allocator);
            for (int i = 0; i < m_PointerEventList.Length; i++)
            {
                if(EntityManager.HasComponent(m_PointerEventList[i], requiredType))
                    ret.Add(m_PointerEventList[i]);
            }

            return ret;
        }

        private void GatherEvents(Allocator allocator, out NativeArray<NativePointerButtonEvent> pointerEventsArray, out NativeArray<NativeKeyboardInputEvent> keyboardEventArray)
        {
            using (new ProfilerSample("GatherEvents"))
            {
                var pointerContainerEntity = GetSingletonEntity<NativePointerInputContainer>();
                var keyboardContainerEntity = GetSingletonEntity<NativeKeyboardInputContainer>();

                var pointerBuffer = EntityManager.GetBuffer<NativePointerButtonEvent>(pointerContainerEntity);
                var keyboardBuffer = EntityManager.GetBuffer<NativeKeyboardInputEvent>(keyboardContainerEntity);

                pointerEventsArray = new NativeArray<NativePointerButtonEvent>(pointerBuffer.Length, allocator);
                keyboardEventArray = new NativeArray<NativeKeyboardInputEvent>(keyboardBuffer.Length, allocator);

                for (int i = 0; i < pointerBuffer.Length; i++)
                    pointerEventsArray[i] = pointerBuffer[i];

                for (int i = 0; i < keyboardBuffer.Length; i++)
                    keyboardEventArray[i] = keyboardBuffer[i];

                pointerBuffer.Clear();
                keyboardBuffer.Clear();
            }
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

        internal NativeMultiHashMap<Entity, PointerInputBuffer> GetEventMap()
        {
            return m_PointerTargetToEvent;
        }
    }
}
