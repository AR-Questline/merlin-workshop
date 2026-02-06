using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Combat.Behaviours.MagicBehaviours;
using Awaken.TG.Main.AI.Idle;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.BossBehaviours.Tristan {
    public class TristanWaterWave : SpellCastingBehaviourBase {
        [SerializeField] NpcDamageData damageData = NpcDamageData.DefaultMagicAttackData;
        [SerializeField] SphereDamageParameters sphereDamageParameters;
        [SerializeField] float explosionForceDamage = 100;
        [SerializeField] float ragdollForce = 100;
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)] ShareableARAssetReference waveVFX;
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)] ShareableARAssetReference waveHittingStalagmiteVFX;
        [SerializeField] float waveVFXLifetime = 10f;
        [SerializeField] int waveMaskTextureHalfSizeInMeters = 16;
        [SerializeField] NpcStateType exitType = NpcStateType.Spawn;

        RenderTexture _maskTexture;
        ARAsyncOperationHandle<ComputeShader> _computeShaderHandle;
        
        public override int Weight => (int) (base.Weight * TristanBoss.StalagmitesPriorityMultiplier);
        public override bool CanBeInterrupted => false;
        public override bool UseConditionsEnsured() => TristanBoss.AnyStalagmites;
        protected override bool IsInValidState => base.IsInValidState || NpcGeneralFSM.CurrentAnimatorState.Type == exitType;
        Attachments.Customs.Tristan TristanBoss => (Attachments.Customs.Tristan) ParentModel;

        protected override bool StartBehaviour() {
            if (!_computeShaderHandle.IsValid()) {
                _computeShaderHandle = WaterWave.ComputeShaderAddressable.Get().LoadAsset<ComputeShader>();
            }
            return base.StartBehaviour();
        }

        protected override UniTask CastSpell(bool returnFireballInHandAfterSpawned = true) {
            if (NpcGeneralFSM.CurrentAnimatorState.Type != exitType) {
                Npc.Controller.TeleportTo(TristanBoss.GetWaterWaveTeleportDestination(), TeleportContext.FromCombat);
                ParentModel.SetAnimatorState(exitType);
                return UniTask.CompletedTask;
            }
            
            var parameters = DamageParameters.Default;
            parameters.KnockdownType = KnockdownType;
            parameters.KnockdownStrength = KnockdownStrength;
            parameters.ForceDamage = explosionForceDamage;
            parameters.RagdollForce = ragdollForce;
            parameters.DamageTypeData = damageData.GetDamageTypeData(Npc).GetRuntimeData();
            parameters.Inevitable = true;
            
            var newSphereDamageParameters = sphereDamageParameters;
            newSphereDamageParameters.baseDamageParameters = parameters;
            newSphereDamageParameters.rawDamageData = damageData.GetRawDamageData(Npc);

            var stalagmitePositions = TristanBoss.GetAllStalagmitesPositions();
            var stalagmiteRadius = TristanBoss.StalagmiteRadius;
            var maxDistance = TristanBoss.MaxBlockingDistanceBehind;
            WaterWave.ComputeMaskTexture(Npc.Coords, stalagmitePositions, stalagmiteRadius, maxDistance, waveMaskTextureHalfSizeInMeters, _computeShaderHandle.Result, ref _maskTexture);
            WaterWave.TriggerWaterWave(Npc, Npc, newSphereDamageParameters, stalagmitePositions, stalagmiteRadius, maxDistance, waveVFX, waveVFXLifetime, ref _maskTexture);
            TristanBoss.SpawnVFXAndDestroyStalagmites(waveHittingStalagmiteVFX, newSphereDamageParameters.endRadius / newSphereDamageParameters.duration);
            
            if (returnFireballInHandAfterSpawned) {
                ReturnInstantiatedPrefabs();
            }
            
            return UniTask.CompletedTask;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            base.OnDiscard(fromDomainDrop);
            if (_maskTexture != null) {
                _maskTexture.Release();
                Object.Destroy(_maskTexture);
                _maskTexture = null;
            }
            if (_computeShaderHandle.IsValid()) {
                _computeShaderHandle.Release();
                _computeShaderHandle = default;
            }
        }
    }
}