using System;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Attachments.Customs;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Behaviours.CustomBehaviours {
    [Serializable]
    public partial class ExplodeBehaviour : AttackBehaviour<EnemyBaseClass> {
        const float DefaultExplosionHeightPosition = 1.5f;
        
        // === Serialized Fields
        [SerializeField] NpcStateType animatorStateTye = NpcStateType.SpecialAttack;
        [SerializeField] NpcDamageData damageData = NpcDamageData.DefaultMagicAttackData;
        [SerializeField] float explosionForceDamage = 5;
        [SerializeField] float explosionDuration = 0.5f;
        [SerializeField] float explosionRange = 9;
      	[SerializeField] float ragdollForce = 5;
  		[SerializeField, InfoBox("If set to nothing will use NpcHitMask from Template")] LayerMask explosionHitMask;
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        ShareableARAssetReference explosionVFX;
        
        public override bool CanBeUsed => true;
        public override bool CanBeInterrupted => false;
        protected override NpcStateType StateType => animatorStateTye;
        protected override MovementState OverrideMovementState => new NoMove();
        protected override float? OverrideCrossFadeTime => 0f;
        Location Location => ParentModel.ParentModel;
        bool _exploded;

        protected override bool OnStart() {
            return true;
        }

        public override void OnStop() { }
        
        public override void TriggerAnimationEvent(ARAnimationEvent animationEvent) {
            if (animationEvent.actionType == ARAnimationEvent.ActionType.SpecialAttackTrigger) {
                Explode();
            }
        }

        protected override void OnAnimatorExitDesiredState() {
            // --- For some reason explode trigger wasn't invoked so explode from here
            Explode();
        }

        public void Explode() {
            if (_exploded) return;
            _exploded = true;
            
            ParentModel.Trigger(Exploder.Events.ExplosionExploded, true);
            Vector3 explosionPosition = ExplosionPosition;

            var parameters = DamageParameters.Default;
            parameters.ForceDamage = explosionForceDamage;
            parameters.RagdollForce = ragdollForce;
            parameters.Inevitable = true;
            parameters.DamageTypeData = damageData.GetDamageTypeData(Npc).GetRuntimeData();
            
            LayerMask mask = explosionHitMask == 0 ? ParentModel.NpcElement.HitLayerMask : explosionHitMask;
            var explosionParams = new SphereDamageParameters {
                rawDamageData = damageData.GetRawDamageData(Npc),
                duration = explosionDuration,
                endRadius = explosionRange,
                hitMask = mask,
                item = ParentModel.StatsItem,
                baseDamageParameters = parameters
            };

            DealDamageInSphereOverTime dealDamageInSphereOverTime = new(explosionParams, explosionPosition, Npc);
            // --- Assign Explosion to Location so that when Exploder dies the explosion is not discarded with it's death
            Location.AddElement(dealDamageInSphereOverTime);
            PrefabPool.InstantiateAndReturn(explosionVFX, explosionPosition, Quaternion.identity).Forget();
            
            // --- Kill Self
            Location.Kill();
        }
        
        // === Helpers
        Vector3 ExplosionPosition {
            get {
                Transform t = Location.MainView.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.gameObject.CompareTag("ExplosionParent"));
                if (t != null) {
                    return t.position;
                }
                return ParentModel.Coords + Vector3.up * DefaultExplosionHeightPosition;
            }
        }
    }
}
