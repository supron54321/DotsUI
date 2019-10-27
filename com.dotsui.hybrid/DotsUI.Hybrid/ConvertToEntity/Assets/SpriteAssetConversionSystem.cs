using DotsUI.Core;
using TMPro;
using Unity.Entities;
using UnityEngine;

namespace DotsUI.Hybrid
{
	[UpdateInGroup(typeof(GameObjectConversionGroup))]
	public class SpriteAssetConversionSystem : GameObjectConversionSystem
	{
		protected override void OnUpdate()
		{
			Entities.ForEach((Sprite sprite) => { ConvertSprite(sprite); });
		}

		private void ConvertSprite(Sprite sprite)
		{
			var assetEntity = GetPrimaryEntity(sprite);
			DstEntityManager.AddSharedComponentData(assetEntity, new SpriteAsset { Value = sprite });
			DstEntityManager.AddComponent<SpriteVertexData>(assetEntity);
		}
	}
}