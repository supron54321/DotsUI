using Unity.Entities;
using UnityEngine.UI;

namespace DotsUI.Hybrid
{
	[UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
	class DeclareReferencedSpriteAssetSystem : GameObjectConversionSystem
	{
		protected override void OnUpdate()
		{
			DeclareReferencedAsset(ImageConversionSystem.DefaultSprite);

			this.Entities.ForEach<Image>(this.ConvertImage);
		}

		private void ConvertImage(Image image)
		{
			if(image.sprite != null)
				DeclareReferencedAsset(image.sprite);
		}
	}
}