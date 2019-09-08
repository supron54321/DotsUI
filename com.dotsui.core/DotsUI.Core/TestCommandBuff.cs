using System;
using UnityEngine;

public class TestCommandBuff : MonoBehaviour
{
    public Action<RenderTexture, RenderTexture> onRenderImg;
    // Start is called before the first frame update
    private void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {
        
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (onRenderImg != null)
            onRenderImg(source, destination);
        else
            Graphics.Blit(source, destination);
    }
}
