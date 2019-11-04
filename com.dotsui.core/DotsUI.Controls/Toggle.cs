using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace DotsUI.Controls
{
    public struct Toggle : IComponentData
    {
        public bool IsOn;
        public Entity TargetGraphic;
        public Entity Group;
    }

    public struct ToggleGroup : IComponentData
    {

    }

    public struct ToggleValueChangedEvent : IComponentData
    {

    }
}
