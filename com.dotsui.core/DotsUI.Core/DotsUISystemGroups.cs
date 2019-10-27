using DotsUI.Core.Utils;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace DotsUI.Core
{
    // Simulation group
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class InputSystemGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InputSystemGroup))]
    public class InputHandleBarrier : MicroCommandBufferSystem
    {

    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InputHandleBarrier))]
    public class UserInputSystemGroup : ComponentSystemGroup
    {

    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UserInputSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [ExecuteAlways]
    public class BeforeRectTransformUpdateGroup : ComponentSystemGroup
    {

    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BeforeRectTransformUpdateGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    [ExecuteAlways]
    public class RectTransformSystemGroup : ComponentSystemGroup
    {

    }

    /// <summary>
    /// This group is intended for complex transform dependencies like ScrollRect.
    /// It's executed after hierarchy update and allows you to override some of the transforms without rebuilding entire transforms.
    /// </summary>
    [ExecuteAlways]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RectTransformSystemGroup))]
    public class PostRectTransformSystemGroup : ComponentSystemGroup
    {

    }

    [ExecuteAlways]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PostRectTransformSystemGroup))]
    public class AssetUpdateSystemGroup : ComponentSystemGroup
    {

    }

    [ExecuteAlways]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AssetUpdateSystemGroup))]
    public class ElementMeshUpdateSystemGroup : ComponentSystemGroup
    {

    }

    // Presentation group
    [ExecuteAlways]
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
