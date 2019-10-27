using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotsUI.Core;
using Unity.Entities;
using UnityEngine.UI;

namespace DotsUI.Hybrid
{
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    class MasksConversionSystem : DotsUIConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach<RectMask2D>(ConvertMask);
            Entities.ForEach<Mask>(ConvertMask);
        }

        private void ConvertMask(Mask mask)
        {
            var entity = GetPrimaryEntity(mask);
            DstEntityManager.AddComponent<RectMask>(entity);
        }
        private void ConvertMask(RectMask2D mask)
        {
            var entity = GetPrimaryEntity(mask);
            DstEntityManager.AddComponent<RectMask>(entity);
        }
    }
}
