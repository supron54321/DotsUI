using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotsUI.Core;
using DotsUI.Input;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

namespace DotsUI.Controls
{
    [UpdateInGroup(typeof(InputSystemGroup))]
    [UpdateAfter(typeof(ControlsInputSystem))]
    public class ButtonSystem : JobComponentSystem
    {
        private EntityQuery m_EventGroup;

        protected override void OnCreate()
        {
            m_EventGroup = GetEntityQuery(new EntityQueryDesc()
            {
                All = new[]
                {
                    ComponentType.ReadWrite<PointerInputBuffer>(),
                    ComponentType.ReadWrite<PointerEvent>()
                }
            });
        }

        protected override void OnDestroy()
        {
        }
        [BurstCompile]
        private struct ProcessClicks : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkComponentType<PointerEvent> EventType;
            [ReadOnly] public ArchetypeChunkBufferType<PointerInputBuffer> BufferType;

            [WriteOnly] public NativeQueue<Entity>.Concurrent ClickedButtons;
            [ReadOnly] public ComponentDataFromEntity<Button> ButtonTargetType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var eventArray = chunk.GetNativeArray(EventType);
                var bufferAccessor = chunk.GetBufferAccessor(BufferType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    if (ButtonTargetType.Exists(eventArray[i].Target))
                    {
                        var buff = bufferAccessor[i];
                        for (int j = 0; j < buff.Length; j++)
                        {
                            if (buff[j].EventType == PointerEventType.Click)
                            {
                                ClickedButtons.Enqueue(eventArray[i].Target);
                            }
                        }
                    }

                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            NativeQueue<Entity> clickedButtons = new NativeQueue<Entity>(Allocator.TempJob);
            {
                ProcessClicks clicksJob = new ProcessClicks
                {
                    EntityType = GetArchetypeChunkEntityType(),
                    BufferType = GetArchetypeChunkBufferType<PointerInputBuffer>(),
                    EventType = GetArchetypeChunkComponentType<PointerEvent>(),
                    ButtonTargetType = GetComponentDataFromEntity<Button>(),
                    ClickedButtons = clickedButtons.ToConcurrent()
                };
                inputDeps = clicksJob.Schedule(m_EventGroup, inputDeps);
                inputDeps.Complete();

                while (clickedButtons.TryDequeue(out Entity entity))
                {
                    EntityManager.AddComponent(entity, typeof(ButtonClickedEvent));
                }
            }
            clickedButtons.Dispose();

            return inputDeps;
        }
    }
    // Old delegate-based system
    //
    //[UpdateInGroup(typeof(InputSystemGroup))]
    //[UpdateAfter(typeof(ControlsInputBufferSystem))]
    //public class ButtonSystem : JobComponentSystem
    //{
    //    private EntityQuery m_EventGroup;
    //    private List<Action<Entity>> m_OnClickList;
    //    private NativeHashMap<Entity, int> m_EntityToActionID;
    //    private NativeList<Entity> m_RegisteredEntities;
    //    private NativeList<int> m_FreeSlots;

    //    protected override void OnCreateManager()
    //    {
    //        m_OnClickList = new List<Action<Entity>>(100);
    //        m_EntityToActionID = new NativeHashMap<Entity, int>(100, Allocator.Persistent);
    //        m_RegisteredEntities = new NativeList<Entity>(100, Allocator.Persistent);
    //        m_FreeSlots = new NativeList<int>(100, Allocator.Persistent);
    //        m_EventGroup = GetEntityQuery(new EntityQueryDesc()
    //        {
    //            All = new[]
    //            {
    //                ComponentType.ReadWrite<PointerInputBuffer>(),
    //                ComponentType.ReadWrite<PointerEvent>()
    //            }
    //        });
    //    }

    //    protected override void OnDestroyManager()
    //    {
    //        m_OnClickList = null;
    //        m_EntityToActionID.Dispose();
    //        m_RegisteredEntities.Dispose();
    //        m_FreeSlots.Dispose();
    //    }
    //    [BurstCompile]
    //    private struct ProcessClicks : IJobChunk
    //    {
    //        [ReadOnly] public ArchetypeChunkEntityType EntityType;
    //        [ReadOnly] public ArchetypeChunkComponentType<PointerEvent> EventType;
    //        [ReadOnly] public ArchetypeChunkBufferType<PointerInputBuffer> BufferType;

    //        [WriteOnly] public NativeQueue<Entity>.Concurrent ClickedEntities;
    //        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
    //        {
    //            var eventArray = chunk.GetNativeArray(EventType);
    //            var bufferAccessor = chunk.GetBufferAccessor(BufferType);
    //            for (int i = 0; i < chunk.Count; i++)
    //            {
    //                var buff = bufferAccessor[i];
    //                for (int j = 0; j < buff.Length; j++)
    //                {
    //                    if (buff[j].EventType == PointerEventType.Click)
    //                    {
    //                        ClickedEntities.Enqueue(eventArray[i].Target);
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    protected override JobHandle OnUpdate(JobHandle inputDeps)
    //    {
    //        NativeQueue<Entity> clickedButtons = new NativeQueue<Entity>(Allocator.TempJob);
    //        {
    //            ProcessClicks clicksJob = new ProcessClicks
    //            {
    //                EntityType = GetArchetypeChunkEntityType(),
    //                BufferType = GetArchetypeChunkBufferType<PointerInputBuffer>(),
    //                EventType = GetArchetypeChunkComponentType<PointerEvent>(),
    //                ClickedEntities = clickedButtons.ToConcurrent()
    //            };
    //            inputDeps = clicksJob.Schedule(m_EventGroup, inputDeps);
    //            inputDeps.Complete();

    //            while(clickedButtons.TryDequeue(out Entity entity))
    //            {
    //                if(m_EntityToActionID.TryGetValue(entity, out int eventID))
    //                    m_OnClickList[eventID].Invoke(entity);
    //            }
    //        }
    //        clickedButtons.Dispose();



    //        return inputDeps;
    //    }

    //    public void SetOnClickEvent(Entity entity, Action<Entity> onClick)
    //    {
    //        if (m_EntityToActionID.TryGetValue(entity, out var previousEvent))
    //        {
    //            m_OnClickList[m_EntityToActionID[entity]] = onClick;
    //        }
    //        else
    //        {
    //            int eventID;
    //            if (m_FreeSlots.Length > 0)
    //            {
    //                eventID = m_FreeSlots[m_FreeSlots.Length];
    //                m_FreeSlots.ResizeUninitialized(m_FreeSlots.Length - 1);
    //                m_OnClickList[eventID] = onClick;
    //            }
    //            else
    //            {
    //                eventID = m_OnClickList.Count;
    //                m_OnClickList.Add(onClick);
    //            }

    //            m_EntityToActionID.TryAdd(entity, eventID);
    //            m_RegisteredEntities.Add(entity);
    //        }
    //    }
    //}
}
