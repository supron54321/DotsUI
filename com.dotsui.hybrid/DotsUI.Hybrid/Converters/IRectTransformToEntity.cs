using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotsUI.Hybrid;
using Unity.Entities;

namespace DotsUI.Hybrid
{
    public interface IRectTransformToEntity
    {
        void ConvertToEntity(Entity entity, RectTransformToEntity rectTransformToEntity,
            Dictionary<UnityEngine.Object, Entity> assetToEntity, EntityManager mgr);
    }
}
