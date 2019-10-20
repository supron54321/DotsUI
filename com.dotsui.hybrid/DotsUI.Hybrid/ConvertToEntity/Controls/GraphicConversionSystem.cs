using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotsUI.Core;
using Unity.Mathematics;
using UnityEngine.UI;

namespace DotsUI.Hybrid
{
    abstract class GraphicConversionSystem : DotsUIConversionSystem
    {
        protected void ConvertGraphic(Graphic graphic)
        {
            var entity = GetPrimaryEntity(graphic);
            DstEntityManager.AddComponentData(entity, new VertexColorValue()
            {
                Value = graphic.color.ToFloat4()
            });
            DstEntityManager.AddComponentData(entity, new VertexColorMultiplier()
            {
                Value = new float4(1.0f, 1.0f, 1.0f, 1.0f)
            });
            DstEntityManager.AddComponent(entity, typeof(ElementVertexPointerInMesh));
        }
    }
}
