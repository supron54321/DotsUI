using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotsUI.Core;
using Unity.Entities;
using Unity.Jobs;

namespace DotsUI.UnityEngineBackend
{
    [UpdateInGroup(typeof(AssetUpdateSystemGroup))]
    class FontAssetSystem : JobComponentSystem
    {
        struct FontAssetInitialized : ISystemStateComponentData
        {

        }
        EntityQuery m_FontAssetGroup;
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            ArchetypeChunkSharedComponentType<LegacyTextFontMaterial> materialType = GetArchetypeChunkSharedComponentType<LegacyTextFontMaterial>();
            ArchetypeChunkComponentType<TextFontAsset> fontType = GetArchetypeChunkComponentType<TextFontAsset>();
            using (var chunkArray = m_FontAssetGroup.CreateArchetypeChunkArray(Unity.Collections.Allocator.TempJob))
            {
                foreach (var chunk in chunkArray)
                {
                    var fontAssetArray = chunk.GetNativeArray(fontType);
                    var nativeId = chunk.GetSharedComponentIndex(materialType);
                    for (int i = 0; i < chunk.Count; i++)
                    {
                        var fontData = fontAssetArray[i];
                        fontData.NativeMaterialId = nativeId;
                        fontAssetArray[i] = fontData;
                    }
                    //chunk.SetChunkComponentData(spriteDataType, spriteData);
                }
            }
            EntityManager.AddComponent(m_FontAssetGroup, typeof(FontAssetInitialized));
            return inputDeps;
        }

        protected override void OnCreate()
        {
            m_FontAssetGroup = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<LegacyTextFontMaterial>(),
                    ComponentType.ReadWrite<TextFontAsset>(),
                },
                None = new ComponentType[]
                {
                    typeof(FontAssetInitialized)
                }
            });
        }
    }
}