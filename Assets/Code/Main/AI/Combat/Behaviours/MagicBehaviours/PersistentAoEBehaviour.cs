using System;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.AI.Fights.Utils;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.TG.VisualScripts.Units.VFX;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.AI.Combat.Behaviours.MagicBehaviours {
    [Serializable]
    public partial class PersistentAoEBehaviour : SpellCastingBehaviourBase {
        // === Serialized Fields
        [SerializeField, Tooltip("Otherwise use NPC position")] bool spawnUsingHandPosition;
        [SerializeField] Vector3 vfxOffset = Vector3.zero;
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.Weapons)]
        ShareableARAssetReference vfxPrefab;
        [SerializeField] float lifeTime = 5f;
        [SerializeField] float vfxStopTime = 1f;
        [SerializeField] bool onlyOnGrounded;
        [SerializeField, FoldoutGroup("Advanced")] bool canApplyToSelf = true;
        [SerializeField, FoldoutGroup("Advanced")] bool discardOnNpcDeath;
        [SerializeField, ShowIf(nameof(UsesTick))] float tickInterval = 0.5f;
        [SerializeField, FoldoutGroup("Status"), TemplateType(typeof(StatusTemplate))] TemplateReference statusTemplateRef;
        [SerializeField, FoldoutGroup("Status"), ShowIf(nameof(IsBuildupAble))] float buildupStrength;
        [SerializeField] bool dealsDamage;
        [SerializeField, FoldoutGroup("Damage"), ShowIf(nameof(dealsDamage))] NpcDamageData damageData = NpcDamageData.DefaultMagicAttackData;
        [SerializeField, FoldoutGroup("Damage"), ShowIf(nameof(dealsDamage))] float poiseDamage;
        [SerializeField, FoldoutGroup("Damage"), ShowIf(nameof(dealsDamage))] float forceDamage;
        [SerializeField, FoldoutGroup("Damage"), ShowIf(nameof(dealsDamage))] float ragdollForce;
        [SerializeField, FoldoutGroup("Damage"), ShowIf(nameof(dealsDamage))] bool inevitable;
        
        protected override UniTask CastSpell(bool returnFireballInHandAfterSpawned = true) {
            CastSpellInternal(returnFireballInHandAfterSpawned).Forget();
            return UniTask.CompletedTask;
        }

        async UniTaskVoid CastSpellInternal(bool returnFireballInHandAfterSpawned) {
            var forward = spawnUsingHandPosition
                ? (castingHand == CastingHand.MainHand ? Npc.MainHand.forward : Npc.OffHand.forward)
                : Npc.Forward();
            
            //TODO: Rework (or revert to) to use Prefab Pool when VFX with VCPersistentAoEChecker are able to be reused
            var assetRef = vfxPrefab.Get();
            var result = await assetRef.LoadAsset<GameObject>();
            if (result == null) {
                assetRef.ReleaseAsset();
                return;
            }
            if (HasBeenDiscarded) {
                assetRef.ReleaseAsset();
                return;
            }

            GameObject vfxInstance = Object.Instantiate(result, GetSpellPosition(), Quaternion.LookRotation(forward.ToHorizontal3()));
            VFXAttachVC.ApplyToVFX(Npc.ParentModel, vfxInstance);
            vfxInstance.AddComponent<OnDestroyReleaseAsset>().Init(assetRef);

            SphereDamageParameters? sphereDamageParameters = null;
            if (dealsDamage) {
                var parameters = DamageParameters.Default;
                parameters.PoiseDamage = poiseDamage;
                parameters.ForceDamage = forceDamage;
                parameters.RagdollForce = ragdollForce;
                parameters.Inevitable = inevitable;
                parameters.DamageTypeData = damageData.GetDamageTypeData(Npc).GetRuntimeData();
                parameters.IsDamageOverTime = true;

                sphereDamageParameters = new SphereDamageParameters {
                    rawDamageData = damageData.GetRawDamageData(Npc, tickInterval),
                    baseDamageParameters = parameters
                };
            }

            float? tick = UsesTick ? tickInterval : null;
            var aoe = PersistentAoE.AddPersistentAoE(Npc.ParentModel, tick, new TimeDuration(lifeTime),
                statusTemplateRef?.Get<StatusTemplate>(), buildupStrength, null,
                sphereDamageParameters, onlyOnGrounded, false, false, canApplyToSelf, false, discardOnNpcDeath);

            if (vfxStopTime > 0f) {
                VFXUtils.StopVfxAndDestroyOnDiscard(vfxInstance, vfxStopTime, aoe);
            } else {
                aoe.ListenTo(Model.Events.BeforeDiscarded, _ => Object.Destroy(vfxInstance), aoe);
            }

            if (returnFireballInHandAfterSpawned) {
                ReturnInstantiatedPrefabs();
            }

            PlaySpecialAttackReleaseAudio();
        }

        protected override Vector3 GetSpellPosition() {
            if (spawnUsingHandPosition) {
                return base.GetSpellPosition() + ParentModel.NpcElement.Rotation * vfxOffset;
            }
            return ParentModel.NpcElement.Coords + ParentModel.NpcElement.Rotation * vfxOffset;
        }
        
        // === Editor
        bool UsesTick => IsBuildupAble || dealsDamage;
        bool IsBuildupAble {
            get {
                if (statusTemplateRef == null || statusTemplateRef.GUID == string.Empty) {
                    return false;
                }
                return statusTemplateRef?.Get<StatusTemplate>()?.IsBuildupAble ?? false;
            }
        }
    }
}