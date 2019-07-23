using DotsUI.Core;
using DotsUI.Input;
using NUnit.Framework;
using Unity.Entities;
using Unity.Entities.Tests;
using Unity.Mathematics;

namespace DotsUI.Controls.Tests
{
    
    [TestFixture]
    public class SelectableTests : ECSTestsFixture
    {
//        [Test]
//        public void TestButtonDown()
//        {
//            var entity = CreateDummyEntity();
//            var selectableColors = SetTestColors();
//            m_Manager.SetComponentData(entity, selectableColors);
//            var buffer = m_Manager.GetBuffer<PointerInputBuffer>(entity);
//            buffer.Add(new PointerInputBuffer()
//            {
//                EventType = PointerEventType.Down,
//                TouchData = default
//            });
//            World.GetOrCreateSystem<SelectableSystem>().Update();
//            var colorValue = m_Manager.GetComponentData<VertexColorValue>(entity);
//            Assert.That(colorValue.Value, Is.EqualTo(selectableColors.Selected));
//        }
//
//        [Test]
//        public void TestButtonUp()
//        {
//            var entity = CreateDummyEntity();
//            var selectableColors = SetTestColors();
//            m_Manager.SetComponentData(entity, selectableColors);
//            var buffer = m_Manager.GetBuffer<PointerInputBuffer>(entity);
//            buffer.Add(new PointerInputBuffer()
//            {
//                EventType = PointerEventType.Up,
//                TouchData = default
//            });
//            World.GetOrCreateSystem<SelectableSystem>().Update();
//            var colorValue = m_Manager.GetComponentData<VertexColorValue>(entity);
//            Assert.That(colorValue.Value, Is.EqualTo(selectableColors.Normal));
//        }

        private static SelectableColor SetTestColors()
        {
            var selectableColors = new SelectableColor()
            {
                Normal = new float4(0.5f, 0.5f, 0.5f, 0.5f),
                Hover = new float4(1.0f, 0.0f, 0.0f, 0.0f),
                Selected = new float4(0.0f, 1.0f, 0.0f, 0.0f)
            };
            return selectableColors;
        }

        private Entity CreateDummyEntity()
        {
            var entity = m_Manager.CreateEntity(typeof(SelectableColor), typeof(VertexColorValue), typeof(PointerInputBuffer));
            m_Manager.SetComponentData(entity, new VertexColorValue()
            {
                Value = new float4(1.0f, 1.0f, 1.0f, 1.0f)
            });
            return entity;
        }
    }
}