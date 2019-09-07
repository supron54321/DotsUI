using Unity.Entities;
using UnityEngine;

public class WindowCanvas : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponent(entity, typeof(WindowCanvasComponent));
    }
}

public struct WindowCanvasComponent : IComponentData
{

}