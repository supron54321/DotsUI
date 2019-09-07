using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DotsUI.Hybrid
{
    [RequireComponent(typeof(Camera))]
    public class CameraImageRenderProxy : MonoBehaviour
    {
        public List<CommandBuffer> InjectedCommandBuffers = new List<CommandBuffer>();
        private Camera m_Camera;

        public Camera UnityCamera
        {
            get { return m_Camera; }
        }

        void Awake()
        {
            m_Camera = GetComponent<Camera>();
        }
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(source, destination);
            Graphics.SetRenderTarget(destination);
            for (int i = 0; i < InjectedCommandBuffers.Count; i++)
            {
                Graphics.ExecuteCommandBuffer(InjectedCommandBuffers[i]);
            }
        }
    }
}
