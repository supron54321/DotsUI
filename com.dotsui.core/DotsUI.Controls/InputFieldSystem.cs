using System.Diagnostics;
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
    [UpdateAfter(typeof(ControlsInputSystemGroup))]
    public class InputFieldSystem : JobComponentSystem
    {
        private EntityQuery m_InputFieldGroup;
        private EntityQuery m_InputOnFocusQuery;
        private EntityQuery m_InputOnLostFocusQuery;

        private PointerEventQuery m_PointerEventQuery;
        private KeyboardEventQuery m_KeyboardEventQuery;

        private EntityArchetype m_CaretArchetype;
        private Entity m_CaretEntity;
        private InputHandleBarrier m_InputSystemBarrier;

        protected override void OnCreate()
        {
            m_CaretArchetype = EntityManager.CreateArchetype(typeof(RectTransform), typeof(Parent), typeof(WorldSpaceRect), typeof(SpriteImage), 
                typeof(ControlVertexData), typeof(ControlVertexIndex), typeof(VertexColorValue), typeof(VertexColorMultiplier),
                typeof(InputFieldCaret));

            m_InputSystemBarrier = World.GetOrCreateSystem<InputHandleBarrier>();

            m_InputFieldGroup = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<InputField>(),
                    ComponentType.ReadWrite<InputFieldCaretState>()
                }
            });
            m_PointerEventQuery = PointerEventQuery.Create<InputField>(EntityManager);
            m_KeyboardEventQuery = KeyboardEventQuery.Create<InputField>(EntityManager);
        }
        
        private struct EventProcessor : IJob
        {
            [ReadOnly] public InputEventReader<KeyboardInputBuffer> KeyboardEventReader;
            [ReadOnly] public InputEventReader<PointerInputBuffer> PointerEventReader;
            [ReadOnly] public ComponentDataFromEntity<InputFieldCaretEntityLink> InputFieldCaretLinkFromEntity;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<InputFieldCaretState> CaretStateFromEntity;
            [ReadOnly] public ComponentDataFromEntity<InputField> InputFieldFromEntity;
            [WriteOnly] [NativeDisableParallelForRestriction] public BufferFromEntity<TextData> TextDataFromEntity;
            public EntityCommandBuffer.Concurrent CommandBuff;

            public EntityArchetype CaretArchetype;

            public void Execute()
            {
                for (int i = 0; i < PointerEventReader.EntityCount; i++)
                {
                    var inputFieldEntity = PointerEventReader[i];
                    var thisInputField = InputFieldFromEntity[inputFieldEntity];

                    var textData = TextDataFromEntity[thisInputField.Target];
                    CaretStateFromEntity[inputFieldEntity] = ProcessPointerInput(inputFieldEntity, textData, CaretStateFromEntity[inputFieldEntity], 0);
                }
                for (int i = 0; i < KeyboardEventReader.EntityCount; i++)
                {
                    var inputFieldEntity = KeyboardEventReader[i];
                    var thisInputField = InputFieldFromEntity[inputFieldEntity];

                    var textData = TextDataFromEntity[thisInputField.Target];
                    var oldTextLen = textData.Length;

                    CommandBuff.AddComponent(0, thisInputField.Target, new DirtyElementFlag());
                    CaretStateFromEntity[inputFieldEntity] = ProcessInput(inputFieldEntity, textData, CaretStateFromEntity[inputFieldEntity], 0);
                    if (thisInputField.Placeholder != default)
                    {
                        if (textData.Length > 0)
                            CommandBuff.AddComponent(0, thisInputField.Placeholder, new Disabled());
                        else if (oldTextLen > 0 && textData.Length == 0)
                            CommandBuff.RemoveComponent<Disabled>(0, thisInputField.Placeholder);
                    }
                }
            }

            private InputFieldCaretState ProcessPointerInput(Entity inputFieldEntity, DynamicBuffer<TextData> textData, InputFieldCaretState caretState, int chunkIdx)
            {
                PointerEventReader.GetFirstEvent(inputFieldEntity, out var pointerEvent, out var it);
                do
                {
                    if (pointerEvent.EventType == PointerEventType.Selected)
                    {
                        caretState.CaretPosition = textData.Length;
                        CommandBuff.AddComponent(chunkIdx, inputFieldEntity, new InputFieldCaretEntityLink()
                        {
                            CaretEntity = CreateCaret(inputFieldEntity, chunkIdx)
                        });
                    }

                    if (pointerEvent.EventType == PointerEventType.Click)
                    {
                    }

                    if (pointerEvent.EventType == PointerEventType.Deselected)
                    {
                        CommandBuff.AddComponent(chunkIdx, inputFieldEntity, new InputFieldEndEditEvent());
                        if (InputFieldCaretLinkFromEntity.Exists(inputFieldEntity))
                        {
                            CommandBuff.DestroyEntity(chunkIdx,
                                InputFieldCaretLinkFromEntity[inputFieldEntity].CaretEntity);
                            CommandBuff.RemoveComponent(chunkIdx, inputFieldEntity, typeof(InputFieldCaretEntityLink));
                        }

                        CommandBuff.AddComponent(chunkIdx, inputFieldEntity, new DirtyElementFlag());
                    }
                } while (PointerEventReader.TryGetNextEvent(out pointerEvent, ref it));

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

            private InputFieldCaretState ProcessInput(Entity inputFieldEntity,
                DynamicBuffer<TextData> textData, InputFieldCaretState caretState, int chunkIndex)
            {
                KeyboardEventReader.GetFirstEvent(inputFieldEntity, out var keyboardEvent, out var it);
                do
                {
                    if (keyboardEvent.EventType == KeyboardEventType.Key)
                    {
                        switch ((KeyCode) keyboardEvent.KeyCode)
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
                        textData.Insert(caretState.CaretPosition, new TextData() {Value = keyboardEvent.Character});
                        caretState.CaretPosition++;
                    }
                } while (KeyboardEventReader.TryGetNextEvent(out keyboardEvent, ref it));
                return caretState;
            }

        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var keyboardEventReader = m_KeyboardEventQuery.CreateKeyboardEventReader(Allocator.TempJob);
            var pointerEventReader = m_PointerEventQuery.CreatePointerEventReader(Allocator.TempJob);
            if (keyboardEventReader.EntityCount > 0 || pointerEventReader.EntityCount > 0)
            {
                using(new Profiling.ProfilerSample("ScheduleJobs"))
                {
                    EventProcessor inputEventProcessor = new EventProcessor()
                    {
                        PointerEventReader = pointerEventReader,
                        KeyboardEventReader = keyboardEventReader,
                        InputFieldCaretLinkFromEntity = GetComponentDataFromEntity<InputFieldCaretEntityLink>(true),
                        CaretStateFromEntity = GetComponentDataFromEntity<InputFieldCaretState>(),
                        InputFieldFromEntity = GetComponentDataFromEntity<InputField>(),
                        TextDataFromEntity = GetBufferFromEntity<TextData>(),
                        CommandBuff = m_InputSystemBarrier.CreateCommandBuffer().ToConcurrent(),
                        CaretArchetype = m_CaretArchetype
                    };
                    inputDeps = inputEventProcessor.Schedule(inputDeps);
                    m_InputSystemBarrier.AddJobHandleForProducer(inputDeps);
                }
            }

            inputDeps = keyboardEventReader.Dispose(inputDeps);
            inputDeps = pointerEventReader.Dispose(inputDeps);
            return inputDeps;
        }
    }
}
