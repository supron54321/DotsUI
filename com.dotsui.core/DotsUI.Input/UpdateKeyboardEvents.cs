using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DotsUI.Input
{
    internal struct UpdateKeyboardEvents : IJob
    {
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<NativeKeyboardInputEvent> KeyboardEvents;
        [ReadOnly] public ComponentDataFromEntity<InputSystemState> StateFromEntity;
        [ReadOnly] public Entity StateEntity;
        public EntityCommandBuffer Manager;
        public EntityArchetype EventArchetype;

        public void Execute()
        {
            var state = StateFromEntity[StateEntity];

            if (KeyboardEvents.Length > 0)
            {
                var selected = state.SelectedEntity;
                if (selected != default)
                {
                    var eventEntity = Manager.CreateEntity(EventArchetype);
                    var buff = Manager.SetBuffer<KeyboardInputBuffer>(eventEntity);
                    Manager.SetComponent(eventEntity, new KeyboardEvent()
                    {
                        Target = selected
                    });
                    for (int i = 0; i < KeyboardEvents.Length; i++)
                    {
                        if (KeyboardEvents[i].EventType == NativeInputEventType.KeyDown)
                            buff.Add(new KeyboardInputBuffer()
                            {
                                Character = KeyboardEvents[i].Character,
                                EventType = KeyboardEvents[i].KbdEvent,
                                KeyCode = KeyboardEvents[i].KeyCode
                            });
                    }
                }
            }
        }
    }
}