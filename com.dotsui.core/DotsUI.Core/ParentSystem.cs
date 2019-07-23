using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using DotsUI.Profiling;

namespace DotsUI.Core
{
    [UpdateInGroup(typeof(BeforeRectTransformUpdateGroup))]
    public class ParentSystem : ComponentSystem
    {
        private EntityQuery m_NewParentsGroup;
        private EntityQuery m_RemovedParentsGroup;
        private EntityQuery m_ExistingParentsGroup;
        private EntityQuery m_DeletedParentsGroup;

        private void AddChildToParent(Entity childEntity, Entity parentEntity)
        {
            EntityManager.SetComponentData(childEntity, new UIPreviousParent { Value = parentEntity });

            if (!EntityManager.HasComponent(parentEntity, typeof(UIChild)))
            {
                var children = EntityManager.AddBuffer<UIChild>(parentEntity);
                children.Add(new UIChild { Value = childEntity });
            }
            else
            {
                var children = EntityManager.GetBuffer<UIChild>(parentEntity);
                children.Add(new UIChild { Value = childEntity });
            }
        }

        private int FindChildIndex(DynamicBuffer<UIChild> children, Entity entity)
        {
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].Value == entity)
                    return i;
            }

            throw new InvalidOperationException("Child entity not in parent");
        }

        private void RemoveChildFromParent(Entity childEntity, Entity parentEntity)
        {
            if (!EntityManager.HasComponent<UIChild>(parentEntity))
                return;

            var children = EntityManager.GetBuffer<UIChild>(parentEntity);
            var childIndex = FindChildIndex(children, childEntity);
            children.RemoveAt(childIndex);
            if (children.Length == 0)
            {
                EntityManager.RemoveComponent(parentEntity, typeof(UIChild));
            }
        }

        private struct ChangedParent
        {
            public Entity ChildEntity;
            public Entity PreviousParentEntity;
            public Entity ParentEntity;
        }
        [BurstCompile]
        private struct FilterChangedParents : IJob
        {
            public NativeList<ChangedParent> ChangedParents;
            [ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
            [ReadOnly] public ArchetypeChunkComponentType<UIPreviousParent> PreviousParentType;
            [ReadOnly] public ArchetypeChunkComponentType<UIParent> ParentType;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;

            public void Execute()
            {
                for (int i = 0; i < Chunks.Length; i++)
                {
                    var chunk = Chunks[i];
                    if (chunk.DidChange(ParentType, chunk.GetComponentVersion(PreviousParentType)))
                    {
                        var chunkPreviousParents = chunk.GetNativeArray(PreviousParentType);
                        var chunkParents = chunk.GetNativeArray(ParentType);
                        var chunkEntities = chunk.GetNativeArray(EntityType);

                        for (int j = 0; j < chunk.Count; j++)
                        {
                            if (chunkParents[j].Value != chunkPreviousParents[j].Value)
                                ChangedParents.Add(new ChangedParent
                                {
                                    ChildEntity = chunkEntities[j],
                                    ParentEntity = chunkParents[j].Value,
                                    PreviousParentEntity = chunkPreviousParents[j].Value
                                });
                        }
                    }
                }
            }
        }

        protected override void OnCreateManager()
        {
            m_NewParentsGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<UIParent>(),
                    ComponentType.ReadOnly<WorldSpaceRect>()
                },
                None = new ComponentType[]
                {
                    typeof(UIPreviousParent)
                },
                Options = EntityQueryOptions.FilterWriteGroup
            });
            m_RemovedParentsGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(UIPreviousParent)
                },
                None = new ComponentType[]
                {
                    typeof(UIParent)
                },
                Options = EntityQueryOptions.FilterWriteGroup
            });
            m_ExistingParentsGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<UIParent>(),
                    ComponentType.ReadOnly<WorldSpaceRect>(),
                    typeof(UIPreviousParent)
                },
                Options = EntityQueryOptions.FilterWriteGroup
            });
            m_DeletedParentsGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(UIChild)
                },
                None = new ComponentType[]
                {
                    typeof(WorldSpaceRect)
                },
                Options = EntityQueryOptions.FilterWriteGroup
            });
        }

        private void UpdateNewParents()
        {
            using (new ProfilerSample("UpdateNewParents"))
            {
                if (m_NewParentsGroup.CalculateLength() < 1)
                    return;
                var childEntities = m_NewParentsGroup.ToEntityArray(Allocator.TempJob);
                var parents = m_NewParentsGroup.ToComponentDataArray<UIParent>(Allocator.TempJob);

                EntityManager.AddComponent(m_NewParentsGroup, typeof(UIPreviousParent));

                for (int i = 0; i < childEntities.Length; i++)
                {
                    var childEntity = childEntities[i];
                    var parentEntity = parents[i].Value;

                    AddChildToParent(childEntity, parentEntity);
                }

                childEntities.Dispose();
                parents.Dispose();
            }
        }

        private void UpdateRemoveParents()
        {
            using (new ProfilerSample("UpdateRemoveParents"))
            {
                if (m_RemovedParentsGroup.CalculateLength() < 1)
                    return;

                var childEntities = m_RemovedParentsGroup.ToEntityArray(Allocator.TempJob);
                var previousParents = m_RemovedParentsGroup.ToComponentDataArray<UIPreviousParent>(Allocator.TempJob);

                for (int i = 0; i < childEntities.Length; i++)
                {
                    var childEntity = childEntities[i];
                    var previousParentEntity = previousParents[i].Value;

                    RemoveChildFromParent(childEntity, previousParentEntity);
                }

                EntityManager.RemoveComponent(m_RemovedParentsGroup, typeof(UIPreviousParent));
                childEntities.Dispose();
                previousParents.Dispose();
            }
        }

        private void UpdateChangeParents()
        {
            using (new ProfilerSample("UpdateChangeParents"))
            {
                if (m_ExistingParentsGroup.CalculateLength() < 1)
                    return;

                var changeParentsChunks = m_ExistingParentsGroup.CreateArchetypeChunkArray(Allocator.TempJob);
                if (changeParentsChunks.Length > 0)
                {
                    var parentType = GetArchetypeChunkComponentType<UIParent>(true);
                    var previousParentType = GetArchetypeChunkComponentType<UIPreviousParent>(true);
                    var entityType = GetArchetypeChunkEntityType();
                    var changedParents = new NativeList<ChangedParent>(Allocator.TempJob);

                    var filterChangedParentsJob = new FilterChangedParents
                    {
                        Chunks = changeParentsChunks,
                        ChangedParents = changedParents,
                        ParentType = parentType,
                        PreviousParentType = previousParentType,
                        EntityType = entityType
                    };
                    var filterChangedParentsJobHandle = filterChangedParentsJob.Schedule();
                    filterChangedParentsJobHandle.Complete();

                    for (int i = 0; i < changedParents.Length; i++)
                    {
                        var childEntity = changedParents[i].ChildEntity;
                        var previousParentEntity = changedParents[i].PreviousParentEntity;
                        var parentEntity = changedParents[i].ParentEntity;

                        RemoveChildFromParent(childEntity, previousParentEntity);
                        AddChildToParent(childEntity, parentEntity);
                    }
                    changedParents.Dispose();
                }
                changeParentsChunks.Dispose();
            }
        }

        private void UpdateDeletedParents()
        {
            using (new ProfilerSample("UpdateDeletedParents"))
            {
                if (m_DeletedParentsGroup.CalculateLength() < 1)
                    return;
                var previousParents = m_DeletedParentsGroup.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < previousParents.Length; i++)
                {
                    var parentEntity = previousParents[i];
                    var childEntitiesSource = EntityManager.GetBuffer<UIChild>(parentEntity).AsNativeArray();
                    var childEntities = new NativeArray<Entity>(childEntitiesSource.Length, Allocator.Temp);
                    for (int j = 0; j < childEntitiesSource.Length; j++)
                        childEntities[j] = childEntitiesSource[j].Value;

                    for (int j = 0; j < childEntities.Length; j++)
                    {
                        var childEntity = childEntities[j];

                        if (!EntityManager.Exists(childEntity))
                            continue;

                        if (EntityManager.HasComponent(childEntity, typeof(UIParent)))
                            EntityManager.RemoveComponent(childEntity, typeof(UIParent));
                        if (EntityManager.HasComponent(childEntity, typeof(UIPreviousParent)))
                            EntityManager.RemoveComponent(childEntity, typeof(UIPreviousParent));
                    }

                    childEntities.Dispose();
                }
                EntityManager.RemoveComponent(m_DeletedParentsGroup, typeof(UIChild));
                previousParents.Dispose();
            }
        }

        protected override void OnUpdate()
        {
            UpdateDeletedParents();
            UpdateRemoveParents();

            UpdateChangeParents();
            UpdateNewParents();
        }

        //protected override JobHandle OnUpdate(JobHandle inputDeps)
        //{
        //    using (new ProfilerSample("ParentSystem.OnUpdate"))
        //    {
        //        UpdateDeletedParents();
        //        UpdateRemoveParents();

        //        UpdateChangeParents();
        //        UpdateNewParents();
        //        return inputDeps;
        //    }
        //}
    }
}
