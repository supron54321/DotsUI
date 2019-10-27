using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotsUI.Input;
using Unity.Entities;

namespace DotsUI.Hybrid
{
    abstract class DotsUIConversionSystem : GameObjectConversionSystem
    {
        protected static TComponent GetOrAddComponent<TComponent>(EntityManager mgr, Entity entity) where TComponent : struct, IComponentData
        {
            if (mgr.HasComponent<TComponent>(entity))
                return mgr.GetComponentData<TComponent>(entity);
            mgr.AddComponent<TComponent>(entity);
            return default;
        }

        protected void RegisterEventHandler(Entity entity, PointerEventType eventType)
        {
            var pointerInputReceiver = GetOrAddComponent<PointerInputReceiver>(DstEntityManager, entity);
            pointerInputReceiver.ListenerTypes |= eventType;
            DstEntityManager.SetComponentData(entity, pointerInputReceiver);
        }
    }
}
