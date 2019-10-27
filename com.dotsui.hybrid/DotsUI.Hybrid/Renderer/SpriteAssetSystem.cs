using DotsUI.Core;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace DotsUI.Hybrid
{
    [ExecuteAlways]
    [UpdateInGroup(typeof(AssetUpdateSystemGroup))]
    class SpriteAssetSystem : JobComponentSystem
    {
        struct SpriteAssetInitialized : ISystemStateComponentData
        {

        }
        EntityQuery m_SpriteAssetGroup;
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            ArchetypeChunkSharedComponentType<SpriteAsset> assetType = GetArchetypeChunkSharedComponentType<SpriteAsset>();
            ArchetypeChunkComponentType<SpriteVertexData> spriteDataType = GetArchetypeChunkComponentType<SpriteVertexData>();
            using (var chunkArray = m_SpriteAssetGroup.CreateArchetypeChunkArray(Unity.Collections.Allocator.TempJob))
            {
                foreach (var chunk in chunkArray)
                {
                    var spriteDataArray = chunk.GetNativeArray(spriteDataType);
                    var asset = chunk.GetSharedComponentData(assetType, EntityManager);
                    var spriteData = SpriteUtility.GetSpriteVertexData(asset.Value);
                    spriteData.NativeMaterialId = chunk.GetSharedComponentIndex(assetType);
                    for (int i = 0; i < chunk.Count; i++)
                        spriteDataArray[i] = spriteData;
                    //chunk.SetChunkComponentData(spriteDataType, spriteData);
                }
            }
            EntityManager.AddComponent(m_SpriteAssetGroup, typeof(SpriteAssetInitialized));
            return inputDeps;
        }

        protected override void OnCreate()
        {
            m_SpriteAssetGroup = GetEntityQuery(new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<SpriteAsset>(),
                    ComponentType.ReadWrite<SpriteVertexData>(),
                },
                None = new ComponentType[]
                {
                    typeof(SpriteAssetInitialized)
                }
            });
        }
    }
}
