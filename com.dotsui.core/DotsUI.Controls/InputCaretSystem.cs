using DotsUI.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace DotsUI.Controls
{
    [UpdateInGroup(typeof(UserInputSystemGroup))]
    internal class InputCaretSystem : JobComponentSystem
    {
        private EntityQuery m_CaretGroup;
        private float m_DeltaTime = 0.0f;
        private bool m_Show = false;

        protected override void OnCreate()
        {
            m_CaretGroup = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<InputFieldCaret>(),
                    ComponentType.ReadWrite<ControlVertexData>(),
                    ComponentType.ReadWrite<ControlVertexIndex>(),
                }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            m_DeltaTime += UnityEngine.Time.deltaTime;
            
            if (m_DeltaTime > 0.5f)
            {
                m_DeltaTime = 0.0f;
                m_Show = !m_Show;
                using (var entityArray = m_CaretGroup.ToEntityArray(Allocator.TempJob))
                {
                    for (int i = 0; i < entityArray.Length; i++)
                    {
                        var entity = entityArray[i];
                        var caret = EntityManager.GetComponentData<InputFieldCaret>(entity);

                        var inputFieldEntity = caret.InputFieldEntity;
                        var inputField = EntityManager.GetComponentData<InputField>(inputFieldEntity);
                        var caretState = EntityManager.GetComponentData<InputFieldCaretState>(inputFieldEntity);

                        var controlVertexData = EntityManager.GetBuffer<ControlVertexData>(entity);
                        var controlVertexIndex = EntityManager.GetBuffer<ControlVertexIndex>(entity);

                        var rect = TextUtils.GetCaretRect(inputField.Target, EntityManager, caretState.CaretPosition);

                        controlVertexData.Clear();
                        controlVertexIndex.Clear();
                        var color = m_Show
                            ? new float4(0.0f, 0.0f, 0.0f, 1.0f)
                            : new float4(0.0f, 0.0f, 0.0f, 0.0f);

                        controlVertexData.Add(new ControlVertexData()
                        {
                            Color = color,
                            Normal = new float3(0.0f, 0.0f, -1.0f),
                            Position = new float3(rect.Min, 0.0f),
                            TexCoord0 = new float2(0.0f)
                        });
                        controlVertexData.Add(new ControlVertexData()
                        {
                            Color = color,
                            Normal = new float3(0.0f, 0.0f, -1.0f),
                            Position = new float3(rect.Max.x, rect.Min.y, 0.0f),
                            TexCoord0 = new float2(0.0f)
                        });
                        controlVertexData.Add(new ControlVertexData()
                        {
                            Color = color,
                            Normal = new float3(0.0f, 0.0f, -1.0f),
                            Position = new float3(rect.Max.x, rect.Max.y, 0.0f),
                            TexCoord0 = new float2(0.0f)
                        });
                        controlVertexData.Add(new ControlVertexData()
                        {
                            Color = color,
                            Normal = new float3(0.0f, 0.0f, -1.0f),
                            Position = new float3(rect.Min.x, rect.Max.y, 0.0f),
                            TexCoord0 = new float2(0.0f)
                        });

                        controlVertexIndex.Add(0);
                        controlVertexIndex.Add(3);
                        controlVertexIndex.Add(2);
                        controlVertexIndex.Add(2);
                        controlVertexIndex.Add(1);
                        controlVertexIndex.Add(0);

                        EntityManager.AddComponent(inputFieldEntity, typeof(DirtyElementFlag));
                    }
                }
            }
            return inputDeps;
        }
    }
}
