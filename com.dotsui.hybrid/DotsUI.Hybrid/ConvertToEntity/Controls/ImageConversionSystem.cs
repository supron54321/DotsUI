using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotsUI.Core;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace DotsUI.Hybrid
{
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    class ImageConversionSystem : GraphicConversionSystem
    {
        public static Sprite DefaultSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0.0f, 0.0f, 0.0f, 0.0f), Vector2.zero);
        protected override void OnUpdate()
        {
            Entities.ForEach<Image>(ConvertImage);
        }

        void ConvertImage(Image image)
        {
            var entity = GetPrimaryEntity(image);
            Sprite sprite = null;
            if (image.sprite != null)
                sprite = image.sprite;
            else
                sprite = DefaultSprite;
            var assetEntity = GetPrimaryEntity(sprite);

            //Debug.Log($"image.sprite: {image.sprite} DefaultSprite: {DefaultSprite} sprite: {sprite} assetEntity: {assetEntity}");

            SpriteImage spriteImage = new SpriteImage
            {
                Asset = assetEntity
            };
            DstEntityManager.AddComponentData(entity, spriteImage);
            DstEntityManager.AddBuffer<ElementVertexData>(entity);
            DstEntityManager.AddBuffer<ElementVertexIndex>(entity);
            DstEntityManager.AddComponent(entity, typeof(ElementVertexPointerInMesh));
            DstEntityManager.AddComponentData(entity, new RebuildElementMeshFlag() { Rebuild = true });
            ConvertGraphic(image);
        }
    }
}
