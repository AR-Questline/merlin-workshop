using System;
using System.Collections.Generic;
using System.Threading;
using Awaken.Kandra;
using Awaken.Kandra.VFXs;
using Awaken.TG.Assets;
using Awaken.TG.Code.Utility;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.AI.Combat.Attachments.Bosses;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.Attachments;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Utils;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.AI.Combat.Attachments.Customs {
    [Serializable]
    public partial class FurDad : BaseBossCombat, IStatusConsumptioner {
        public override ushort TypeForSerialization => SavedModels.FurDad;

        [SerializeField, FoldoutGroup("Elemental Consuming")]
        float statusConsumeInterval = 30;
        [SerializeField, FoldoutGroup("Elemental Consuming")]
        float chanceForAoePhaseTransition = 0.5f;
        [SerializeField, FoldoutGroup("Elemental Consuming")] 
        float buildupStrength = 50;
        [SerializeField, FoldoutGroup("Elemental Consuming"), TemplateType(typeof(StatusTemplate))] 
        TemplateReference initialConsumedElemental;
        [SerializeField, FoldoutGroup("Elemental Consuming")]
        List<StatusConsumeData> statusToVFXMap = new();
        [SerializeField, FoldoutGroup("Phase Transition Explosion Settings")] NpcDamageData damageData = NpcDamageData.DefaultAttackData;
        [SerializeField, FoldoutGroup("Phase Transition Explosion Settings")] float explosionForceDamage = 5;
        [SerializeField, FoldoutGroup("Phase Transition Explosion Settings")] float explosionDuration = 0.01f;
        [SerializeField, FoldoutGroup("Phase Transition Explosion Settings")] float explosionRange = 9;
        [SerializeField, FoldoutGroup("Phase Transition Explosion Settings")] float ragdollForce = 5;
        
        WeakModelRef<StatusConsumption> _consumption;
        IPooledInstance _consumedVFXInstance, _handVFXInstance;
        CancellationTokenSource _cancelVFXSpawn;
        float _lastStatusConsumeTime = float.MinValue;
        bool _aoePhaseTransition;
        ShareableARAssetReference _explosionVfx;
        
        public CharacterStatuses StatusesOwner => NpcElement.Statuses;
        
        public override void InitFromAttachment(BossCombatAttachment spec, bool isRestored) {
            FurDad copyFrom = (FurDad)spec.BossBaseClass;
            buildupStrength = copyFrom.buildupStrength;
            initialConsumedElemental = new TemplateReference(copyFrom.initialConsumedElemental.GUID);
            statusToVFXMap = new List<StatusConsumeData>(copyFrom.statusToVFXMap);
            base.InitFromAttachment(spec, isRestored);
        }
        
        protected override void OnFullyInitialized() {
            NpcElement.ListenTo(ICharacter.Events.OnEffectInvokedAnimationEvent, OnEffectInvoked, this);
            NpcElement.OnCompletelyInitialized(_ => {
                var consumption = AddElement(new StatusConsumption(buildupStrength));
                consumption.ListenTo(StatusConsumption.Events.ConsumedStatus, s => OnConsumedStatus(s).Forget(), this);
                _consumption = consumption;
                ConsumeInitialStatus(consumption);
            });
            base.OnFullyInitialized();
        }

        void ConsumeInitialStatus(StatusConsumption statusConsumption) {
            if (initialConsumedElemental.TryGet(out StatusTemplate statusTemplate)) {
                statusConsumption.ConsumeStatus(statusTemplate);
                _lastStatusConsumeTime = float.MinValue;
            }
        }

        public void OnStatusDiscarded() {
            if (_consumedVFXInstance != null && _consumedVFXInstance.Instance.TryGetComponent(out VisualEffect vfx)) {
                vfx.SendEvent("StatusConsumed");
            }
        }
        
        async UniTaskVoid OnConsumedStatus(StatusTemplate consumedStatus) {
            _cancelVFXSpawn?.Cancel();
            _cancelVFXSpawn = new CancellationTokenSource();
            
            if (_consumedVFXInstance != null) {
                VFXUtils.StopVfxAndReturn(_consumedVFXInstance, 0.5f);
                _consumedVFXInstance = null;
            }

            if (_handVFXInstance != null) {
                VFXUtils.StopVfxAndReturn(_handVFXInstance, 0.5f);
                _handVFXInstance = null;
            }


            var statusConsumeData = TryGetFurDadConsumedStatusVFX(consumedStatus);
            if (statusConsumeData == null) {
                Log.Minor?.Error("FurDad failed to consume status!", consumedStatus);
                return;
            }

            StatusConsumeData data = statusConsumeData.Value;
            ShareableARAssetReference consumedVFX = data.vfx;
            ShareableARAssetReference handVfx = data.handVfx;
            _explosionVfx = data.explosionVfx;
            StatusConsumeType consumeType = data.consumeType;
            
            SetPhaseWithTransition((int)consumeType, RandomUtil.WithProbability(chanceForAoePhaseTransition));
            
            Transform consumedVfxParent = GetAdditionalHand(AdditionalHand.Hand1);
            _consumedVFXInstance = await PrefabPool.Instantiate(consumedVFX, Vector3.zero, Quaternion.identity, consumedVfxParent, cancellationToken: _cancelVFXSpawn.Token);
            Transform handVfxParent = GetAdditionalHand(AdditionalHand.Hand2);
            _handVFXInstance = await PrefabPool.Instantiate(handVfx, Vector3.zero, Quaternion.identity, handVfxParent, cancellationToken: _cancelVFXSpawn.Token);
            _handVFXInstance.Instance.GetComponent<VFXKandraRendererBinder>().kandraRenderer = handVfxParent.GetComponent<KandraRenderer>();
        }

        void OnEffectInvoked() {
            if (InPhaseTransition && _explosionVfx.IsSet) {
                PrefabPool.InstantiateAndReturn(_explosionVfx, Coords, Quaternion.identity).Forget();

                var parameters = DamageParameters.Default;
                parameters.ForceDamage = explosionForceDamage;
                parameters.RagdollForce = ragdollForce;
                parameters.DamageTypeData = damageData.GetDamageTypeData(NpcElement).GetRuntimeData();
                parameters.Inevitable = true;
                
                SphereDamageParameters sphereDamageParameters = new() {
                    rawDamageData = damageData.GetRawDamageData(NpcElement),
                    duration = explosionDuration,
                    endRadius = explosionRange,
                    hitMask = NpcElement.HitLayerMask,
                    item = StatsItem,
                    baseDamageParameters = parameters,
                    onHitStatusTemplate = null,
                    onHitStatusBuildup = 0,
                    disableFriendlyFire = true
                };
            
                Transform hand = GetAdditionalHand(AdditionalHand.Hand1);
                DealDamageInSphereOverTime sphere = new(sphereDamageParameters, hand.position, NpcElement);
                ParentModel.AddElement(sphere);
            }
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            if (_consumption.TryGet(out StatusConsumption consumption)) {
                consumption.Discard();
                _consumption = null;
            }

            _cancelVFXSpawn?.Cancel();
            _cancelVFXSpawn = null;
            
            if (_consumedVFXInstance != null) {
                VFXUtils.StopVfxAndReturn(_consumedVFXInstance, 0.5f);
                _consumedVFXInstance = null;
            }
            
            base.OnDiscard(fromDomainDrop);
        }
        
        // === Helpers
        public bool CanConsume(StatusTemplate status) {
            if (Time.time - statusConsumeInterval < _lastStatusConsumeTime) {
                return false;
            }
            
            bool statusConsumable = TryGetFurDadConsumedStatusVFX(status) != null;
            if (!statusConsumable) {
                return false;
            }
            
            _lastStatusConsumeTime = Time.time;
            return true;
        }
        
        StatusConsumeData? TryGetFurDadConsumedStatusVFX(StatusTemplate statusTemplate) {
            var buildupAttachment = statusTemplate.GetComponent<BuildupAttachment>();
            if (buildupAttachment == null) {
                return null;
            }
            
            var buildupStatusType = buildupAttachment.BuildupStatusType;
            foreach (var map in statusToVFXMap) {
                if (map.StatusType == buildupStatusType) {
                    return map;
                }
            }

            return null;
        }
    }
}
