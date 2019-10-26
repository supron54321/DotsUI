using DotsUI.Core;
using Unity.Entities;
using Unity.Jobs;

namespace DotsUI.Controls
{
    public abstract class ControlRectTransformRebuildSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return OnUpdateTransform(inputDeps, HierarchyRebuildContext.Create(this));
        }

        protected abstract JobHandle OnUpdateTransform(JobHandle inputDeps, HierarchyRebuildContext rebuildContext);
    }
}
