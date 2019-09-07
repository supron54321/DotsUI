using Unity.Entities;
using DotsUI.Core;

namespace DotsUISamples
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    //[UpdateAfter()]
    public class FpsSystem : ComponentSystem
    {
        float m_Frames;
        float m_Time;
        float m_FPS;

        protected override void OnCreate()
        {
            m_Time = 0.0f;
            base.OnCreate();
        }


        protected override void OnUpdate()
        {
            m_Frames++;
            m_Time += UnityEngine.Time.deltaTime;

            if (m_Time >= 1.0f && HasSingleton<FpsCounterComponent>()) 
            {
                var fpsCounter = GetSingleton<FpsCounterComponent>();
                var textBuffer = EntityManager.GetBuffer<TextData>(fpsCounter.TargetText);
                TextData.Set(textBuffer, $"FPS: {m_FPS}");
                m_FPS = m_Frames;
                m_Time = 0.0f;
                m_Frames = 0;
                EntityManager.AddComponent(fpsCounter.TargetText, typeof(DirtyElementFlag));
            }
        }
    }
}
