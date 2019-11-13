using System.Collections.Generic;
using DotsUI.Core.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DotsUI.Input
{
    internal struct ReworkedSpawnPointerEvents : IJob
    {
        [ReadOnly] public NativeMultiHashMap<Entity, PointerInputBuffer> TargetToEvent;
        [WriteOnly] public NativeList<PointerInputBuffer> FinalEventList;
        public AddComponentDataCommandBuffer<ControlPointerEvent>.ParallelWriter ComponentCommandBuffer;
        public EntityArchetype EventArchetype;

        struct EventComparer : IComparer<PointerInputBuffer>
        {
            public int Compare(PointerInputBuffer x, PointerInputBuffer y) => x.EventId.CompareTo(y.EventId);
        }

        public void Execute()
        {
            var targets = TargetToEvent.GetKeyArray(Allocator.Temp);
            NativeList<PointerInputBuffer> eventList = new NativeList<PointerInputBuffer>(4, Allocator.Temp);
            for (int i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                EventComparer eventComparer = new EventComparer();
                if (TargetToEvent.TryGetFirstValue(target, out var item, out var it))
                {
                    do
                    {
                        eventList.Add(item);
                    } while (TargetToEvent.TryGetNextValue(out item, ref it));
                    eventList.Sort(eventComparer);
                    ComponentCommandBuffer.TryAdd(target, new ControlPointerEvent()
                    {
                        EventIdx = FinalEventList.Length,
                        EventCount = eventList.Length
                    });
                    FinalEventList.AddRange(eventList.AsArray());
                    eventList.Clear();
                }
            }
        }
    }
    internal struct SpawnPointerEvents : IJob
    {
        [ReadOnly] public NativeMultiHashMap<Entity,  PointerInputBuffer> TargetToEvent;
        public EntityCommandBuffer Ecb;
        public EntityArchetype EventArchetype;

        struct EventComparer : IComparer< PointerInputBuffer>
        {
            public int Compare(PointerInputBuffer x, PointerInputBuffer y) => x.EventId.CompareTo(y.EventId);
        }

        public void Execute()
        {
            var targets = TargetToEvent.GetKeyArray(Allocator.Temp);
            NativeList<PointerInputBuffer> eventList = new NativeList<PointerInputBuffer>(4, Allocator.Temp);
            for (int i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                EventComparer eventComparer = new EventComparer();
                if (TargetToEvent.TryGetFirstValue(target, out var item, out var it))
                {
                    var eventEntity = Ecb.CreateEntity(EventArchetype);
                    Ecb.SetComponent(eventEntity, new PointerEvent
                    {
                        Target = target
                    });
                    var buffer = Ecb.SetBuffer<PointerInputBuffer>(eventEntity);
                    do
                    {
                        eventList.Add(item);
                    } while (TargetToEvent.TryGetNextValue(out item, ref it));
                    eventList.Sort(eventComparer);
                    buffer.ResizeUninitialized(eventList.Length);
                    for (int j = 0; j < eventList.Length; j++)
                        buffer[j] = eventList[j];
                    eventList.Clear();
                    eventList.Clear();
                }
            }
        }
    }
}