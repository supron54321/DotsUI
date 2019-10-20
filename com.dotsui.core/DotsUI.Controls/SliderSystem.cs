using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Jobs;

namespace DotsUI.Controls
{
    class SliderSystem : JobComponentSystem
    {
        private EntityQuery m_SliderQuery;
        protected override void OnCreate()
        {
            //m_SliderQuery = GetEntityQuery(new EntityQueryDesc()
            //{
            //    All = new ComponentType[]
            //})
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return inputDeps;
        }
    }
}
