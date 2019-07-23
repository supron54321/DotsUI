using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace DotsUI.Core
{
    // Simulation group
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class InputSystemGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InputSystemGroup))]
    public class InputHandleBarrier : EntityCommandBufferSystem
    {

    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InputHandleBarrier))]
    public class UserInputSystemGroup : ComponentSystemGroup
    {

    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UserInputSystemGroup))]
    public class BeforeRectTransformUpdateGroup : ComponentSystemGroup
    {

    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BeforeRectTransformUpdateGroup))]
    public class RectTransformSystemGroup : ComponentSystemGroup
    {

    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RectTransformSystemGroup))]
    public class AssetUpdateSystemGroup : ComponentSystemGroup
    {

    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AssetUpdateSystemGroup))]
    public class ElementMeshUpdateSystemGroup : ComponentSystemGroup
    {

    }

    // Presentation group

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class RenderSystemGroup : ComponentSystemGroup
    {

    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(RenderSystemGroup))]
    public class CleanupSystemGroup : ComponentSystemGroup
    {

    }
}
