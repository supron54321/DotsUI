using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCommandBuff : MonoBehaviour
{
    public Action<RenderTexture, RenderTexture> onRenderImg;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (onRenderImg != null)
            onRenderImg(source, destination);
        else
            Graphics.Blit(source, destination);
    }
}
