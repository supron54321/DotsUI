using System.Collections.Generic;
using Unity.Entities;

namespace DotsUI.Hybrid
{
    public interface IRectTransformToEntity
    {
        void ConvertToEntity(Entity entity, RectTransformToEntity rectTransformToEntity,
            Dictionary<UnityEngine.Object, Entity> assetToEntity, EntityManager mgr);
    }
}
