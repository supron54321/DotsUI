using DotsUI.Core;
using System;
using TMPro;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;
using RectTransform = UnityEngine.RectTransform;

namespace DotsUI.Hybrid
{
	[UpdateInGroup(typeof(GameObjectConversionGroup))]
    class RectTransformConversionSystem : DotsUIConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((RectTransform transform) => { Convert(transform); });
        }


        private void Convert(RectTransform transform)
        {
            var entity = GetPrimaryEntity(transform);

            DstEntityManager.AddComponentData(entity, new DotsUI.Core.RectTransform()
            {
                AnchorMin = transform.anchorMin,
                AnchorMax = transform.anchorMax,
                Pivot = transform.pivot,
                Position = transform.anchoredPosition,
                SizeDelta = transform.sizeDelta,
            });

            DstEntityManager.AddComponent(entity, typeof(WorldSpaceRect));
            DstEntityManager.AddComponent(entity, typeof(WorldSpaceMask));

            DstEntityManager.RemoveComponent(entity, typeof(Translation));
            DstEntityManager.RemoveComponent(entity, typeof(Rotation));
            DstEntityManager.RemoveComponent(entity, typeof(NonUniformScale));

            if (transform.childCount != 0)
            {
                var childBuffer = DstEntityManager.AddBuffer<Child>(entity);
                for (int i = 0; i < transform.childCount; i++)
                {
                    var child = transform.GetChild(i);
                    if (child is RectTransform rectChild)
                    {
                        childBuffer.Add(new Child
                        {
                            Value = GetPrimaryEntity(rectChild)
                        });
                    }
                }
            }

            if (HasPrimaryEntity(transform.parent))
            {
                DstEntityManager.AddComponentData(entity, new PreviousParent()
                {
                    Value = GetPrimaryEntity(transform.parent)
                });
            }
        }
    }
}
