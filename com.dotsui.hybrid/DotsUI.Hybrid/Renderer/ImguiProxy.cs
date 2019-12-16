using System;
using UnityEngine;

namespace DotsUI.Hybrid.Renderer
{
    [ExecuteAlways]
    class ImguiProxy : MonoBehaviour
    {
        public Action OnRenderGui;

        void OnGUI()
        {
            if (Event.current.type == EventType.Repaint)
                OnRenderGui?.Invoke();
        }


        public static ImguiProxy Create()
        {
            return new GameObject("DotsUI_Imgui-proxy").AddComponent<ImguiProxy>();
        }
    }
}