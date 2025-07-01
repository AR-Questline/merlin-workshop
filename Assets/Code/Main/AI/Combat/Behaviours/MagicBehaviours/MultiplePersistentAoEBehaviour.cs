using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Combat.BehavioursHelpers;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Utility.RichEnums;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.AI.Combat.Behaviours.MagicBehaviours {
    [Serializable]
    public partial class MultiplePersistentAoEBehaviour : SpellCastingBehaviourBase {
        const string VfxNextPos = "NextVfxPosition";
        const float AdditionalStepMultiplier = 1.25f;
        const float LeftRightStepMultiplier = 0.333f;
        const float NextLocationSpawnDelay = 0.25f;
        
        [SerializeField] IntRange spawnAmount = new(2, 6);
        [SerializeField] LocationTemplate locationWithPersistentAoE;
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        ShareableARAssetReference inHandSpawnVFX;
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        ShareableARAssetReference locationSpawnVFX;
        [SerializeField, RichEnumExtends(typeof(BehaviourVfxRotation))] RichEnumReference inHandVfxRotation = BehaviourVfxRotation.None;
        
        BehaviourVfxRotation InHandVfxRotation => inHandVfxRotation.EnumAs<BehaviourVfxRotation>();
        
        protected override UniTask CastSpell(bool returnFireballInHandAfterSpawned = true) {
            CastSpellInternal(returnFireballInHandAfterSpawned).Forget();
            return UniTask.CompletedTask;
        }

        protected async UniTaskVoid CastSpellInternal(bool returnFireballInHandAfterSpawned) {
            if (inHandSpawnVFX.IsSet) {
                var hand = GetHand();
                var handPosition = hand.position;
                var vfxRotation = InHandVfxRotation.GetVfxRotation(ParentModel, handPosition);
                PrefabPool.InstantiateAndReturn(inHandSpawnVFX, handPosition, vfxRotation).Forget();
            }

            var coords = ParentModel.Coords;
            var locationsToSpawn = spawnAmount.RandomPick();
            var step = ParentModel.DistanceToTarget / locationsToSpawn * AdditionalStepMultiplier;
            var direction = (ParentModel.NpcElement.GetCurrentTarget().Coords - coords).normalized;
            var left = Vector3.Cross(direction, Vector3.up);
            var leftRight = 1;
            var nextSpawnPosition = default(Vector3?);
            for (int i = 1; i <= locationsToSpawn; i++) {
                var spawnPosition = nextSpawnPosition ?? GetLocationSpawnPosition(coords, i, direction, left * leftRight, step);
                leftRight *= -1;
                if (i < locationsToSpawn) {
                    nextSpawnPosition = GetLocationSpawnPosition(coords, i + 1, direction, left * leftRight, step);
                } else {
                    nextSpawnPosition = null;
                }
                locationWithPersistentAoE.SpawnLocation(spawnPosition, Npc.Rotation);
                SpawnVfxLocation(spawnPosition, Npc.Rotation, nextSpawnPosition).Forget();
                
                if (!await AsyncUtil.DelayTime(this, NextLocationSpawnDelay)) {
                    break;
                }
            }
            
            if (returnFireballInHandAfterSpawned) {
                ReturnInstantiatedPrefabs();
            }

            if (!HasBeenDiscarded) {
                PlaySpecialAttackReleaseAudio();
            }
        }

        async UniTaskVoid SpawnVfxLocation(Vector3 spawnPosition, Quaternion spawnRotation, Vector3? nextSpawnPosition) {
            if (locationSpawnVFX.IsSet) {
                var pooledInstance = await PrefabPool.InstantiateAndReturn(locationSpawnVFX, spawnPosition, spawnRotation);
                var vfxInstance = pooledInstance.Instance;
                
                if (vfxInstance == null) {
                    return;
                }

                Vector3 nextPos = nextSpawnPosition ?? Vector3.zero;
                if (vfxInstance.TryGetComponent(out VisualEffect vfx) && vfx.HasVector3(VfxNextPos)) {
                    vfx.SetVector3(VfxNextPos, nextPos);
                }
            }
        }

        Vector3 GetLocationSpawnPosition(Vector3 coords, int index, Vector3 direction, Vector3 left, float step) {
            var spawnPosition = coords + direction * index * step;
            spawnPosition += left * index * LeftRightStepMultiplier;
            return spawnPosition;
        }
    }
}