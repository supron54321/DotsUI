using DotsUI.Hybrid;
using Unity.Entities;
using UnityEngine;

public class RectMaskExample : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var canvas = FindObjectOfType<Canvas>();
        RectTransformConversionUtils.ConvertCanvasHierarchy(canvas, World.Active.EntityManager);
        GameObject.Destroy(canvas.gameObject);
    }

}
