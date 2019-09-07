using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using DotsUI.Core;
using UnityEngine.UI;

namespace DotsUI.Hybrid{
    internal class ImageConverter : TypedConverter<UnityEngine.UI.Image>
    {
        Sprite m_DefaultSprite;

        public ImageConverter(Sprite defaultSprite) => m_DefaultSprite = defaultSprite;
        protected override void ConvertComponent(Image unityComponent, Entity entity, RectTransformToEntity rectTransformToEntity, Dictionary<UnityEngine.Object, Entity> assetToEntity, EntityManager commandBuffer)
        {
            var sprite = unityComponent.sprite ?? m_DefaultSprite;
            if(!assetToEntity.TryGetValue(sprite, out var assetEntity))
            {
                assetEntity = commandBuffer.CreateEntity(typeof(SpriteAsset), typeof(SpriteVertexData));
                commandBuffer.SetSharedComponentData(assetEntity, new SpriteAsset { Value = sprite });
                assetToEntity.Add(sprite, assetEntity);
            }
            SpriteImage image = new SpriteImage{
                Asset = assetEntity
            };
            commandBuffer.AddComponentData(entity, image);
            commandBuffer.AddBuffer<ControlVertexData>(entity);
            commandBuffer.AddBuffer<ControlVertexIndex>(entity);
            commandBuffer.AddComponent(entity, typeof(ElementVertexPointerInMesh));
            commandBuffer.AddComponent(entity, typeof(RebuildElementMeshFlag));
        }
    }
}