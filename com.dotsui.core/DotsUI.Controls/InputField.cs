using System.Runtime.CompilerServices;
using Unity.Entities;

[assembly: InternalsVisibleTo("DotsUI.Hybrid")]
[assembly: InternalsVisibleTo("DotsUI.Hybrid.Tests")]
[assembly: InternalsVisibleTo("DotsUI.Controls.Tests")]

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
