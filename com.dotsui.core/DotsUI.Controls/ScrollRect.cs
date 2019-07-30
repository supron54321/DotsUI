using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace DotsUI.Controls
{
    public struct ScrollRect : IComponentData
    {
        public Entity Content;
        public Entity Viewport;
        public Entity VerticalBar;
        public Entity HorizontalBar;
    }

    public struct ScrollRectData : IComponentData
    {

    }

    public struct ScrollBar : IComponentData
    {
        public Entity ScrollHandle;
        public Entity ParentScrollRect;
    }

    public struct ScrollBarHandle : IComponentData
    {

    }
}
