using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Heroes {
    public class DeathRagdollHeroBehaviour : DeathRagdollBehaviour {
        
        protected override bool IsForceDirectionOverriden => true;
        protected override Transform RootRagdollBone => _rootRagdollBone;
        protected override Transform RootGameObject => _rootGameObject;
        protected override IModel TimeOwnerModel => Hero.Current;
        protected override Vector3 ForceDirectionOverride { get; } = new(0f, 0.5f, 0.5f);

        Vector3 _rootRagdollBoneDefaultPosition;
        Quaternion _rootRagdollBoneDefaultRotation;
        Transform _rootRagdollBone;
        Transform _rootGameObject;

        public DeathRagdollHeroBehaviour() : base(true, true) { }

        public void CacheRigidBody(Hero hero) {
            _rootGameObject = hero.ParentTransform;

            var bonesCount = 0;
            foreach (var _ in RagdollBones(RootGameObject)) {
                ++bonesCount;
            }

            CreateDataDictionary(bonesCount);

            foreach (var bone in RagdollBones(RootGameObject)) {
                if (RootRagdollBone == null) {
                    _rootRagdollBone = bone;
                    _rootRagdollBoneDefaultPosition = RootRagdollBone.localPosition;
                    _rootRagdollBoneDefaultRotation = RootRagdollBone.localRotation;
                }

                CacheBone(bone);
            }
        }
        
        public override void EnableRagdoll(Vector3 force, Transform parent = null, Collider hitCollider = null, Vector3? hitPosition = null, float radius = 0) {
            RootRagdollBone.gameObject.SetActive(true);
            base.EnableRagdoll(force, parent, hitCollider, hitPosition, radius);
        }

        public override void DisableRagdoll() {
            base.DisableRagdoll();
            RootRagdollBone.SetLocalPositionAndRotation(_rootRagdollBoneDefaultPosition, _rootRagdollBoneDefaultRotation);
        }

        protected override void ToggleComponents(bool ragdollEnabled) {
            foreach (var animator in Hero.Current.VHeroController.HeroAnimators) {
                animator.enabled = !ragdollEnabled;
            }
        }

        protected override void OnDeathInternal(DamageOutcome damageOutcome) {
            Vector3 force = damageOutcome.RagdollForce;
            if (damageOutcome.Damage.DamageDealer is NpcElement { HasBeenDiscarded: false } npcElement)  {
                force = damageOutcome.RagdollForce.normalized * npcElement.NpcStats.HeroKnockBack;
            }
            TryToEnableRagdoll(force, RootGameObject, damageOutcome.HitCollider, damageOutcome.Position, damageOutcome.Radius);
        }
    }
}