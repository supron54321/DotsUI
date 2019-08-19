using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;

namespace DotsUI.Layout
{
    struct LayoutGroup : IComponentData
    {
        public float Left;
        public float Right;
        public float Top;
        public float Bottom;
    }

    struct VerticalLayout : IComponentData
    {
        public float Spacing;
        public bool ControlWidth;
        public bool ControlHeight;
    }

    struct HorizontalLayout : IComponentData
    {
        public float Spacing;
        public bool ControlWidth;
        public bool ControlHeight;
    }

    struct GridLayout : IComponentData
    {
        public float2 Spacing;
    }
}
