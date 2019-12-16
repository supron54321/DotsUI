using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DotsUI.Input
{
    [BurstCompile]
    internal struct UpdateKeyboardEvents : IJob
    {
        [ReadOnly] public NativeArray<NativeKeyboardInputEvent> KeyboardEvents;
        [ReadOnly] public ComponentDataFromEntity<InputSystemState> StateFromEntity;
        [ReadOnly] public Entity StateEntity;
        public NativeMultiHashMap<Entity, KeyboardInputBuffer> KeyboardTargetToEvent;
        public NativeList<Entity> KeyboardTargetList;

        public void Execute()
        {
            var state = StateFromEntity[StateEntity];
            KeyboardTargetToEvent.Clear();
            KeyboardTargetList.Clear();
            // Same as we did in pointer events
            var uniqueTargets = new NativeHashMap<Entity, int>(KeyboardEvents.Length, Allocator.Temp);
            if (KeyboardEvents.Length > 0)
            {
                var selected = state.SelectedEntity;
                if (selected != default)
                {
                    for (int i = 0; i < KeyboardEvents.Length; i++)
                    {
                        if (KeyboardEvents[i].EventType == NativeInputEventType.KeyDown)
                            KeyboardTargetToEvent.Add(selected, new KeyboardInputBuffer()
                            {
                                Character = KeyboardEvents[i].Character,
                                EventType = KeyboardEvents[i].KbdEvent,
                                KeyCode = KeyboardEvents[i].KeyCode
                            });
                        uniqueTargets.TryAdd(selected, 0);
                    }
                }
            }
            KeyboardTargetList.AddRange(uniqueTargets.GetKeyArray(Allocator.Temp));
            uniqueTargets.Dispose();
        }
    }
}