using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace DotsUI.Input
{
    //internal struct UpdateKeyboardEvents : IJob
    //{
    //    [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<(NativeInputEventType, KeyboardInputBuffer)> KeyboardFrameData;
    //    [ReadOnly] public ComponentDataFromEntity<InputSystemState> StateFromEntity;
    //    [ReadOnly] public Entity StateEntity;
    //    public EntityCommandBuffer Manager;
    //    public EntityArchetype EventArchetype;

    //    public void Execute()
    //    {
    //        var state = StateFromEntity[StateEntity];

    //        if (state.SelectedEntity != default)
    //        {
    //            var eventEntity = Manager.CreateEntity(EventArchetype);
    //            Manager.SetComponent(eventEntity, new KeyboardEvent()
    //            {
    //                Target = state.SelectedEntity
    //            });
    //            var buffer = Manager.SetBuffer<KeyboardInputBuffer>(eventEntity);
    //            for (int i = 0; i < KeyboardFrameData.Length; i++)
    //                if (KeyboardFrameData[i].Item1 == NativeInputEventType.KeyDown)
    //                    buffer.Add(KeyboardFrameData[i].Item2);
    //        }
    //    }
    //}
}