using DotsUI.Core;
using TMPro;
using Unity.Entities;
using UnityEngine;

namespace DotsUI.Hybrid
{
	[UpdateInGroup(typeof(GameObjectConversionGroup))]
	public class TMPFontAssetConversionSystem : GameObjectConversionSystem
	{
		protected override void OnUpdate()
		{
			Entities.ForEach<TMP_FontAsset>(this.ConvertTMPFontAsset);
		}

		private void ConvertTMPFontAsset(TMP_FontAsset tmpFontAsset)
		{
			var assetEntity = GetPrimaryEntity(tmpFontAsset);

			this.DstEntityManager.AddSharedComponentData(assetEntity, new LegacyTextFontAsset());
			this.DstEntityManager.AddComponent<TextFontAsset>(assetEntity);
			this.DstEntityManager.AddBuffer<FontGlyphData>(assetEntity);

			TextUtils.SetupFontAssetFromTmp(this.DstEntityManager, assetEntity, tmpFontAsset);
		}
	}
}