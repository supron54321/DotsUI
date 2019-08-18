using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotsUI.Core;
using DotsUI.Input;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DotsUI.Controls
{
    class ScrollRectDragSystem : PointerInputComponentSystem<ScrollRect>
    {
        private EntityQuery m_ScrollRectQuery;
        private InputHandleBarrier m_Barrier;

        protected override void OnCreateInput()
        {
            m_ScrollRectQuery = GetEntityQuery(ComponentType.ReadOnly<ScrollBar>());
            m_Barrier = World.GetOrCreateSystem<InputHandleBarrier>();
        }
        protected override JobHandle OnUpdateInput(JobHandle inputDeps, NativeHashMap<Entity, Entity> targetToEvent, BufferFromEntity<PointerInputBuffer> pointerBufferFromEntity)
        {

            return inputDeps;
        }

        protected override void OnDestroyInput()
        {
        }
    }
}
