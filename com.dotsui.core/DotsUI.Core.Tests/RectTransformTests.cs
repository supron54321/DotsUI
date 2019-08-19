using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Tests;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsUI.Core.Tests
{
    [TestFixture]
    public class RectTransformTests : ECSTestsFixture
    {
        private EntityArchetype m_ChildArchetype;
        private EntityArchetype m_RootArchetype;

        public override void Setup()
        {
            base.Setup();

            m_ChildArchetype = m_Manager.CreateArchetype(ComponentType.ReadWrite<RectTransform>(),
                ComponentType.ReadWrite<Parent>(), typeof(LocalToWorld), typeof(LocalToParent));
            m_RootArchetype = m_Manager.CreateArchetype(ComponentType.ReadWrite<RectTransform>(),
                ComponentType.ReadWrite<WorldSpaceRect>(), typeof(LocalToWorld));
        }
        /*
         * Test hierarchy:
         *
         * root
         * |_l1c1        [0]
         * |_l1c2        [1]
         * | |_l2c1      [2]
         * | |_l2c2      [3]
         * | |_l2c3      [4]
         * |   |_l3c1    [5]
         *       |_l4c1  [6]
         * |_l1c3        [7]
         */
        private (Entity, NativeArray<Entity>) CreateInitialHierarchy()
        {
            var root = m_Manager.CreateEntity(m_RootArchetype);
            var childArchetype = m_Manager.CreateArchetype(ComponentType.ReadWrite<RectTransform>(),
                ComponentType.ReadWrite<LocalToParent>(),
                ComponentType.ReadWrite<LocalToWorld>(),
                ComponentType.ReadWrite<WorldSpaceRect>(),
                ComponentType.ReadWrite<Parent>());
            NativeArray<Entity> children = new NativeArray<Entity>(8, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            m_Manager.CreateEntity(childArchetype, children);
            m_Manager.SetComponentData(children[0], new Parent() { Value = root });
            m_Manager.SetComponentData(children[1], new Parent() { Value = root });
            m_Manager.SetComponentData(children[2], new Parent() { Value = children[1] });
            m_Manager.SetComponentData(children[3], new Parent() { Value = children[1] });
            m_Manager.SetComponentData(children[4], new Parent() { Value = children[1] });
            m_Manager.SetComponentData(children[5], new Parent() { Value = children[4] });
            m_Manager.SetComponentData(children[6], new Parent() { Value = children[5] });
            m_Manager.SetComponentData(children[7], new Parent() { Value = root });
            World.GetOrCreateSystem<EndFrameParentSystem>().Update();
            return (root, children);
        }

        [Test]
        public void TestHierarchy()
        {
            var (root, children) = CreateInitialHierarchy();

            Assert.AreEqual(3, m_Manager.GetBuffer<Child>(root).Length);
            Assert.Throws<ArgumentException>(() => m_Manager.GetBuffer<Child>(children[0]));
            Assert.AreEqual(3, m_Manager.GetBuffer<Child>(children[1]).Length);
            Assert.Throws<ArgumentException>(() => m_Manager.GetBuffer<Child>(children[2]));
            Assert.Throws<ArgumentException>(() => m_Manager.GetBuffer<Child>(children[3]));
            Assert.AreEqual(1, m_Manager.GetBuffer<Child>(children[4]).Length);
            Assert.AreEqual(1, m_Manager.GetBuffer<Child>(children[5]).Length);
            Assert.Throws<ArgumentException>(() => m_Manager.GetBuffer<Child>(children[6]));
            Assert.Throws<ArgumentException>(() => m_Manager.GetBuffer<Child>(children[7]));
        }
        [Test]
        public void TestParentAdd()
        {
            var (root, children) = CreateInitialHierarchy();

            var child = m_Manager.CreateEntity(m_ChildArchetype);
            m_Manager.SetComponentData(child, new Parent() { Value = children[7] });
            World.GetOrCreateSystem<EndFrameParentSystem>().Update();

            Assert.AreEqual(child, m_Manager.GetBuffer<Child>(children[7])[0].Value);
        }

        [Test]
        public void TestParentRemove()
        {
            var (root, children) = CreateInitialHierarchy();

            m_Manager.RemoveComponent<Parent>(children[5]);
            World.GetOrCreateSystem<EndFrameParentSystem>().Update();
            Assert.Throws<ArgumentException>(() => m_Manager.GetBuffer<Child>(children[4]));
        }

        [Test]
        public void TestParentChange()
        {
            var (root, children) = CreateInitialHierarchy();

            m_Manager.SetComponentData(children[6], new Parent() { Value = children[4] });
            World.GetOrCreateSystem<EndFrameParentSystem>().Update();
            Assert.Throws<ArgumentException>(() => m_Manager.GetBuffer<Child>(children[5]));
            Assert.AreEqual(2, m_Manager.GetBuffer<Child>(children[4]).Length);
        }

        [Test]
        public void TestNonScaledCanvas()
        {

        }

        [Test]
        public void TestPhysicalScaledCanvas()
        {
        }

        [Test]
        public void InverseRectTransform()
        {
            NativeArray<WorldSpaceRect> parentRects = new NativeArray<WorldSpaceRect>(4, Allocator.Temp);
            parentRects[0] = new WorldSpaceRect() { Min = new float2(0.0f, 0.0f), Max = new float2(100.0f, 100.0f) };
            parentRects[1] = new WorldSpaceRect() { Min = new float2(10.0f, 10.0f), Max = new float2(100.0f, 100.0f) };
            parentRects[2] = new WorldSpaceRect() { Min = new float2(-10.0f, -10.0f), Max = new float2(100.0f, 100.0f) };
            parentRects[3] = new WorldSpaceRect() { Min = new float2(-100.0f, -100.0f), Max = new float2(-10.0f, -10.0f) };
            NativeArray<RectTransform> rectTransforms = new NativeArray<RectTransform>(4, Allocator.Temp);
            rectTransforms[0] = new RectTransform()
            {
                AnchorMin = new float2(0.0f, 0.0f),
                AnchorMax = new float2(1.0f, 1.0f),
                Pivot = new float2(0.5f, 0.5f),
                Position = new float2(5.0f, 5.0f),
                SizeDelta = new float2(20.0f, 20.0f)
            };
            rectTransforms[1] = new RectTransform()
            {
                AnchorMin = new float2(0.0f, 0.0f),
                AnchorMax = new float2(0.0f, 1.0f),
                Pivot = new float2(1.0f, 1.0f),
                Position = new float2(5.0f, 5.0f),
                SizeDelta = new float2(20.0f, 20.0f)
            };
            rectTransforms[2] = new RectTransform()
            {
                AnchorMin = new float2(0.5f, 0.5f),
                AnchorMax = new float2(0.5f, 0.5f),
                Pivot = new float2(0.5f, 0.5f),
                Position = new float2(-5.0f, -5.0f),
                SizeDelta = new float2(20.0f, 20.0f)
            };
            rectTransforms[3] = new RectTransform()
            {
                AnchorMin = new float2(1.0f, 1.0f),
                AnchorMax = new float2(1.0f, 1.0f),
                Pivot = new float2(0.0f, 0.0f),
                Position = new float2(-5.0f, -5.0f),
                SizeDelta = new float2(20.0f, 20.0f)
            };
            for (int i = 0; i < parentRects.Length; i++)
            {
                for (int j = 0; j < rectTransforms.Length; j++)
                {
                    var worldSpace = RectTransformUtils.CalculateWorldSpaceRect(parentRects[i], new float2(1.0f, 1.0f), rectTransforms[j]);
                    var emptyTransform = new RectTransform()
                    {
                        AnchorMin = rectTransforms[j].AnchorMin,
                        AnchorMax = rectTransforms[j].AnchorMax,
                        Pivot = rectTransforms[j].Pivot
                    };
                    var calculatedTransform = RectTransformUtils.CalculateInverseTransformWithAnchors(worldSpace,
                        parentRects[i], emptyTransform, new float2(1.0f, 1.0f));

                    Assert.IsTrue(RectTransformEquals(rectTransforms[j], calculatedTransform));
                }
            }
        }

        bool RectTransformEquals(RectTransform item1, RectTransform item2)
        {
            return Float2Equals(item1.AnchorMin, item2.AnchorMin) && Float2Equals(item1.AnchorMax, item2.AnchorMax) &&
                   Float2Equals(item1.Pivot, item2.Pivot) && Float2Equals(item1.Position, item2.Position) &&
                   Float2Equals(item1.SizeDelta, item2.SizeDelta);
        }

        bool Float2Equals(float2 item1, float2 item2)
        {
            return math.abs(item1.x - item2.x) < 0.001f && math.abs(item1.y - item2.y) < 0.001f;
        }
    }
}
