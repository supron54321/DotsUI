using DotsUI.Input;
using DotsUI.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using KeyCode = UnityEngine.KeyCode;    // Avoid whole UnityEngine namespace

namespace DotsUI.Controls
{
    [UpdateInGroup(typeof(InputSystemGroup))]
    [UpdateAfter(typeof(ControlsInputSystem))]
    public class InputFieldSystem : JobComponentSystem
    {
        private EntityQuery m_KeyboardEventGroup;
        private EntityQuery m_PointerEventGroup;
        private EntityQuery m_InputFieldGroup;
        private EntityQuery m_InputOnFocusQuery;
        private EntityQuery m_InputOnLostFocusQuery;

        private EntityArchetype m_CaretArchetype;
        private Entity m_CaretEntity;

        private NativeHashMap<Entity, Entity> m_TargetToKeyboardEvent;
        private NativeHashMap<Entity, Entity> m_TargetToPointerEvent;
        private InputHandleBarrier m_InputSystemBarrier;

        protected override void OnCreate()
        {
            m_CaretArchetype = EntityManager.CreateArchetype(typeof(RectTransform), typeof(Parent), typeof(WorldSpaceRect), typeof(SpriteImage), 
                typeof(ControlVertexData), typeof(ControlVertexIndex), typeof(VertexColorValue), typeof(VertexColorMultiplier),
                typeof(InputFieldCaret));

            m_TargetToKeyboardEvent = new NativeHashMap<Entity, Entity>(4, Allocator.Persistent);
            m_TargetToPointerEvent = new NativeHashMap<Entity, Entity>(4, Allocator.Persistent);

            m_InputSystemBarrier = World.GetOrCreateSystem<InputHandleBarrier>();

            m_KeyboardEventGroup = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<KeyboardEvent>(),
                    ComponentType.ReadOnly<KeyboardInputBuffer>(),
                }
            });
            m_PointerEventGroup = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<PointerEvent>(),
                    ComponentType.ReadOnly<PointerInputBuffer>(),
                }
            });
            m_InputFieldGroup = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<InputField>(),
                    ComponentType.ReadWrite<InputFieldCaretState>()
                }
            });
            base.OnCreateManager();
        }

        protected override void OnDestroy()
        {
            m_TargetToKeyboardEvent.Dispose();
            m_TargetToPointerEvent.Dispose();
        }
        private struct EventProcessor : IJobChunk
        {
            [ReadOnly] public BufferFromEntity<KeyboardInputBuffer> KeyboardInputBufferFromEntity;
            [ReadOnly] public BufferFromEntity<PointerInputBuffer> PointerInputBufferFromEntity;
            [ReadOnly] public ComponentDataFromEntity<InputFieldCaretEntityLink> InputFieldCaretLinkFromEntity;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            public ArchetypeChunkComponentType<InputFieldCaretState> CaretStateType;
            [ReadOnly] public ArchetypeChunkComponentType<InputField> InputFieldType;
            [ReadOnly] public NativeHashMap<Entity, Entity> TargetToKeyboardEvent;
            [ReadOnly] public NativeHashMap<Entity, Entity> TargetToPointerEvent;
            [WriteOnly] [NativeDisableParallelForRestriction] public BufferFromEntity<TextData> TextDataFromEntity;
            public EntityCommandBuffer.Concurrent CommandBuff;

            public EntityArchetype CaretArchetype;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);
                var caretStateAccessor = chunk.GetNativeArray(CaretStateType);
                var inputFieldArray = chunk.GetNativeArray(InputFieldType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    var inputFieldEntity = entityArray[i];
                    var thisInputField = inputFieldArray[i];

                    var isPointerInputPending = TargetToPointerEvent.TryGetValue(inputFieldEntity, out var pointerEventEntity);
                    var isKeyboardInputPending =
                        TargetToKeyboardEvent.TryGetValue(inputFieldEntity, out var eventEntity);

                    if (isPointerInputPending || isKeyboardInputPending)
                    {
                        DynamicBuffer<PointerInputBuffer> pointerInputBuffer = default;
                        if (isPointerInputPending)
                            pointerInputBuffer = PointerInputBufferFromEntity[pointerEventEntity];

                        var textData = TextDataFromEntity[thisInputField.Target];
                        int oldTextLen = textData.Length;

                        // Handle selection and click events
                        if (isPointerInputPending)
                        {
                            caretStateAccessor[i] = ProcessPointerInput(inputFieldEntity, pointerInputBuffer, textData, caretStateAccessor[i], chunkIndex);
                        }
                        if (isKeyboardInputPending)
                        {
                            CommandBuff.AddComponent(chunkIndex, thisInputField.Target, new DirtyElementFlag());
                            caretStateAccessor[i] = ProcessInput(inputFieldEntity, KeyboardInputBufferFromEntity[eventEntity], textData, caretStateAccessor[i], chunkIndex);
                            if (thisInputField.Placeholder != default)
                            {
                                if (textData.Length > 0)
                                    CommandBuff.AddComponent(chunkIndex, thisInputField.Placeholder, new Disabled());
                                else if (oldTextLen > 0 && textData.Length == 0)
                                    CommandBuff.RemoveComponent<Disabled>(chunkIndex, thisInputField.Placeholder);
                            }
                        }

                    }

                }
            }

            private InputFieldCaretState ProcessPointerInput(Entity inputFieldEntity, DynamicBuffer<PointerInputBuffer> pointerInputBuffer, DynamicBuffer<TextData> textData, InputFieldCaretState caretState, int chunkIdx)
            {
                for (int i = 0; i < pointerInputBuffer.Length; i++)
                {
                    var pointerEvent = pointerInputBuffer[i];
                    if (pointerEvent.EventType == PointerEventType.Selected)
                    {
                        caretState.CaretPosition = textData.Length;
                        CommandBuff.AddComponent(chunkIdx, inputFieldEntity, new InputFieldCaretEntityLink()
                        {
                            CaretEntity = CreateCaret(inputFieldEntity, chunkIdx)
                        });
                        //CommandBuff.RemoveComponent(chunkIdx, caretState.CaretEntity, typeof(Disabled));
                    }

                    if (pointerEvent.EventType == PointerEventType.Click)
                    {
                    }

                    if (pointerEvent.EventType == PointerEventType.Deselected)
                    {
                        CommandBuff.AddComponent(chunkIdx, inputFieldEntity, new InputFieldEndEditEvent());
                        if (InputFieldCaretLinkFromEntity.Exists(inputFieldEntity))
                        {
                            CommandBuff.DestroyEntity(chunkIdx, InputFieldCaretLinkFromEntity[inputFieldEntity].CaretEntity);
                            CommandBuff.RemoveComponent(chunkIdx, inputFieldEntity, typeof(InputFieldCaretEntityLink));
                        }
                        CommandBuff.AddComponent(chunkIdx, inputFieldEntity, new DirtyElementFlag());
                    }
                }

                return caretState;
            }


            Entity CreateCaret(Entity inputFieldEntity, int chunkIdx)
            {
                var caret = CommandBuff.CreateEntity(chunkIdx, CaretArchetype);
#if UNITY_EDITOR
                //CommandBuff.SetName(caret, "INPUT_FIELD_CARET");
#endif
                CommandBuff.SetComponent(chunkIdx, caret, new InputFieldCaret
                {
                    InputFieldEntity = inputFieldEntity
                });
                CommandBuff.SetComponent(chunkIdx, caret, new Parent
                {
                    Value = inputFieldEntity
                });
                CommandBuff.SetComponent(chunkIdx, caret, new RectTransform()
                {
                    AnchorMin = new float2(0.0f, 0.0f),
                    AnchorMax = new float2(0.0f, 0.0f),
                    Pivot = new float2(0.0f, 0.0f),
                    Position = new float2(0.0f, 0.0f),
                    SizeDelta = new float2(2.0f, 10.0f)
                });
                CommandBuff.SetComponent(chunkIdx, caret, new VertexColorValue()
                {
                    Value = new float4(1.0f, 0.0f, 0.0f, 1.0f),
                });
                CommandBuff.SetComponent(chunkIdx, caret, new VertexColorMultiplier()
                {
                    Value = new float4(1.0f, 1.0f, 1.0f, 1.0f),
                });

                return caret;
            }

            private InputFieldCaretState ProcessInput(Entity inputFieldEntity, DynamicBuffer<KeyboardInputBuffer> inputBuffer,
                DynamicBuffer<TextData> textData, InputFieldCaretState caretState, int chunkIndex)
            {
                for (int i = 0; i < inputBuffer.Length; i++)
                {
                    var key = inputBuffer[i];
                    if (key.EventType == KeyboardEventType.Key)
                    {
                        switch ((KeyCode)key.KeyCode)
                        {
                            case KeyCode.Backspace:
                                if (caretState.CaretPosition > 0)
                                {
                                    textData.RemoveAt(caretState.CaretPosition - 1);
                                    caretState.CaretPosition--;
                                }
                                break;
                            case KeyCode.LeftArrow:
                                caretState.CaretPosition = math.max(0, caretState.CaretPosition - 1);
                                break;
                            case KeyCode.RightArrow:
                                caretState.CaretPosition = math.min(textData.Length, caretState.CaretPosition + 1);
                                break;
                            case KeyCode.Home:
                                caretState.CaretPosition = 0;
                                break;
                            case KeyCode.End:
                                caretState.CaretPosition = textData.Length;
                                break;
                            case KeyCode.Return:
                            case KeyCode.KeypadEnter:
                                CommandBuff.AddComponent(chunkIndex, inputFieldEntity, new InputFieldReturnEvent());
                                return caretState;
                        }
                    }
                    else
                    {
                        textData.Insert(caretState.CaretPosition, new TextData() { Value = key.Character });
                        caretState.CaretPosition++;
                    }
                }
                return caretState;
            }

        }


        [BurstCompile]
        struct CreateTargetToKeyboardEvent : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkComponentType<KeyboardEvent> KbdEventType;
            [WriteOnly] public NativeHashMap<Entity, Entity>.ParallelWriter TargetToEvent;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);
                var eventArray = chunk.GetNativeArray(KbdEventType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    TargetToEvent.TryAdd(eventArray[i].Target, entityArray[i]);
                }
            }
        }
        [BurstCompile]
        struct CreateTargetToPointerEvent : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkComponentType<PointerEvent> PointerEventType;
            [WriteOnly] public NativeHashMap<Entity, Entity>.ParallelWriter TargetToEvent;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);
                var eventArray = chunk.GetNativeArray(PointerEventType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    TargetToEvent.TryAdd(eventArray[i].Target, entityArray[i]);
                }
            }
        }


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (m_KeyboardEventGroup.CalculateEntityCount() > 0 || m_PointerEventGroup.CalculateEntityCount() > 0)
            {
                m_TargetToKeyboardEvent.Clear();
                m_TargetToPointerEvent.Clear();
                var entityType = GetArchetypeChunkEntityType();
                using(new Profiling.ProfilerSample("ScheduleJobs"))
                {
                    CreateTargetToKeyboardEvent createTargetToKeyboardEvent = new CreateTargetToKeyboardEvent()
                    {
                        EntityType = entityType,
                        KbdEventType = GetArchetypeChunkComponentType<KeyboardEvent>(true),
                        TargetToEvent = m_TargetToKeyboardEvent.AsParallelWriter()
                    };
                    inputDeps = createTargetToKeyboardEvent.Schedule(m_KeyboardEventGroup, inputDeps);
                    CreateTargetToPointerEvent createTargetToPointerEvent = new CreateTargetToPointerEvent()
                    {
                        EntityType = entityType,
                        PointerEventType = GetArchetypeChunkComponentType<PointerEvent>(true),
                        TargetToEvent = m_TargetToPointerEvent.AsParallelWriter()
                    };
                    inputDeps = createTargetToPointerEvent.Schedule(m_PointerEventGroup, inputDeps);
                    EventProcessor inputEventProcessor = new EventProcessor()
                    {
                        KeyboardInputBufferFromEntity = GetBufferFromEntity<KeyboardInputBuffer>(true),
                        PointerInputBufferFromEntity = GetBufferFromEntity<PointerInputBuffer>(true),
                        InputFieldCaretLinkFromEntity = GetComponentDataFromEntity<InputFieldCaretEntityLink>(true),
                        EntityType = entityType,
                        CaretStateType = GetArchetypeChunkComponentType<InputFieldCaretState>(),
                        InputFieldType = GetArchetypeChunkComponentType<InputField>(),
                        TextDataFromEntity = GetBufferFromEntity<TextData>(),
                        TargetToKeyboardEvent = m_TargetToKeyboardEvent,
                        TargetToPointerEvent = m_TargetToPointerEvent,
                        CommandBuff = m_InputSystemBarrier.CreateCommandBuffer().ToConcurrent(),
                        CaretArchetype = m_CaretArchetype
                    };
                    inputDeps = inputEventProcessor.Schedule(m_InputFieldGroup, inputDeps);
                    m_InputSystemBarrier.AddJobHandleForProducer(inputDeps);
                }
            }

            return inputDeps;
        }

        private void CreateCaretState(Entity focusedEntity)
        {
            EntityManager.AddComponentData(focusedEntity, new InputFieldCaretState()
            {
                CaretPosition = EntityManager.GetBuffer<TextData>(focusedEntity).Length,
            });
            m_CaretEntity = EntityManager.CreateEntity(m_CaretArchetype);
            EntityManager.SetComponentData(m_CaretEntity, new Parent()
            {
                Value = focusedEntity
            });
        }
    }
}
