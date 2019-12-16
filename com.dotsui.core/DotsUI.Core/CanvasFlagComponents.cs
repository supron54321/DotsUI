using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace DotsUI.Core
{
    /// <summary>
    /// This flag indicated structural change in the canvas hierarchy.
    /// Structural change means updated parent/child relation.
    /// </summary>
    struct CanvasStructuralChange : IComponentData
    {
    }

    /// <summary>
    /// This flag indicates layout change of the canvas.
    /// It's attached to the canvas when any of the canvas children requires update
    /// </summary>
    struct CanvasLayoutChange : IComponentData
    {

    }

    /// <summary>
    /// Added to the canvas when any child needs vertex data update (in example color update)
    /// </summary>
    struct CanvasVertexDataChange : IComponentData
    {

    }
}
