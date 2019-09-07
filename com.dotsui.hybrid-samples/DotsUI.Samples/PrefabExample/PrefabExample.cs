using UnityEngine;
using DotsUI.Core;
using Unity.Entities;

public class PrefabExample : MonoBehaviour
{

    [SerializeField] Canvas m_TopCanvas;
    [SerializeField] Canvas m_RightCanvas;
    [SerializeField] Canvas m_PrefabDestination;

    // Start is called before the first frame update
    void Start()
    {
        if (World.Active == null)
            DefaultWorldInitialization.Initialize("UI World", false);
        //var entityManager = World.Active.EntityManager;

        World.Active.GetOrCreateSystem<UserInputSystemGroup>().AddSystemToUpdateList(World.Active.GetOrCreateSystem<InstantiationSystem>());
        World.Active.GetOrCreateSystem<UserInputSystemGroup>().AddSystemToUpdateList(World.Active.GetOrCreateSystem<CloseButtonSystem>());

        //RectTransformToEntity transformToEntity = new RectTransformToEntity(100, Allocator.Temp);
        //RectTransformConversionUtils.ConvertCanvasHierarchy(m_TopCanvas, World.Active.EntityManager, transformToEntity);
        //RectTransformConversionUtils.ConvertCanvasHierarchy(m_RightCanvas, World.Active.EntityManager, transformToEntity);
        //RectTransformConversionUtils.ConvertCanvasHierarchy(m_PrefabDestination, World.Active.EntityManager, transformToEntity);

        //GameObject.Destroy(m_TopCanvas.gameObject);
        //GameObject.Destroy(m_RightCanvas.gameObject);
        //GameObject.Destroy(m_PrefabDestination.gameObject);
        //transformToEntity.Dispose();
    }
}
