using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

[assembly: InternalsVisibleTo("DotsUI.Hybrid")]

namespace DotsUI.Controls
{
    public struct InputField : IComponentData
    {
        public Entity Target;
        public Entity Placeholder;
    }

    public struct InputFieldReturnEvent : IComponentData
    {

    }

    public struct InputFieldEndEditEvent : IComponentData
    {

    }

    /// <summary>
    /// Added only if entity is selected
    /// </summary>
    internal struct InputFieldCaretState : IComponentData
    {
        public int CaretPosition;
    }

    internal struct InputFieldCaretEntityLink : IComponentData
    {
        public Entity CaretEntity;
    }

    internal struct InputFieldCaret : IComponentData
    {
        public Entity InputFieldEntity;
    }
    internal struct InputFieldCaretShow : IComponentData
    {
    }
}
