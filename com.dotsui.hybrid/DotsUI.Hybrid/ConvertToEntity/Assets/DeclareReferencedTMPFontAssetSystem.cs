using TMPro;
using Unity.Entities;
using UnityEngine;

namespace DotsUI.Hybrid
{
	[UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
	class DeclareReferencedTMPFontAssetSystem : GameObjectConversionSystem
	{
		protected override void OnUpdate()
		{
			this.Entities.ForEach<TextMeshProUGUI>(this.ConvertTextMeshProUGUI);
		}

		private void ConvertTextMeshProUGUI(TextMeshProUGUI tmp)
		{
			if (tmp.font != null)
				DeclareReferencedAsset(tmp.font);
			else
				Debug.LogError($"DeclareReferencedTMPFontAssetSystem - font asset cannot be null reference. Object: {tmp}", tmp);
		}
	}
}