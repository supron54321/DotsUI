using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotsUI.Core;
using Unity.Entities;

namespace DotsUI.Editor
{
#if UNITY_EDITOR
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(InputSystemGroup))]
    public class DbgBeforeInputSystemGroup : ComponentSystemGroup
    {
    }
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InputSystemGroup))]
    public class DbgAfterInputSystemGroup : ComponentSystemGroup
    {
    }


    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(RectTransformSystemGroup))]
    public class DbgBeforeRectTransformSystemGroup : ComponentSystemGroup
    {
    }
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RectTransformSystemGroup))]
    public class DbgAfterRectTransformSystemGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(RenderSystemGroup))]
    public class DbgBeforeRenderSystemGroup : ComponentSystemGroup
    {
    }
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(RenderSystemGroup))]
    public class DbgAfterRenderSystemGroup : ComponentSystemGroup
    {
    }
#endif
}
