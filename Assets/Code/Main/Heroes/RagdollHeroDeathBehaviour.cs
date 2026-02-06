using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Main.Heroes {
    public class DeathRagdollHeroBehaviour : DeathRagdollBehaviour {
        bool _isRagdollApplied;
        Vector3 _rootRagdollBoneDefaultPosition;
        Quaternion _rootRagdollBoneDefaultRotation;

        protected override IModel TimeOwnerModel => Hero.Current;
        protected override Optional<Vector3> ForceDirectionOverride => new Vector3(0f, 0.5f, 0.5f);

        public void CacheRigidBody(Hero hero) {
            RagdollController = hero.ParentTransform.GetComponentInChildren<RagdollController>();
            RagdollController.rootBone.GetLocalPositionAndRotation(out _rootRagdollBoneDefaultPosition, out _rootRagdollBoneDefaultRotation);
        }

        public void OnDeath(DamageOutcome damageOutcome) {
            if (_isRagdollApplied) {
                return;
            }

            if (!RagdollController.gameObject.activeInHierarchy) {
                Log.Important?.Error($"Trying to enable ragdoll on inactive object: {RagdollController} {RagdollController.gameObject.PathInSceneHierarchy()}, it will cause errors");
                return;
            }

            bool wasRagdollEnabled = _isRagdollApplied;
            _isRagdollApplied = true;
            ToggleComponents(true);

            if (!wasRagdollEnabled) {
                RagdollController.ApplyRagdoll(AdditionalRigidbodySetup);
            }

            var setup = SetupFromDamageOutcome(damageOutcome);

            if (damageOutcome.Damage.DamageDealerPure is NpcElement { HasBeenDiscarded: false } npcElement) {
                setup.forceMagnitude = npcElement.NpcStats.HeroKnockBack;
            }
            if (setup.forceMagnitude > 0) {
                AddForceToRagdoll(setup);
            }
        }

        public void DisableRagdoll() {
            RagdollController.RemoveRagdoll();

            TimeOwnerModel.GetTimeDependent()?.RemoveInvalidComponentsAfterFrame().Forget();
            _isRagdollApplied = false;
            ToggleComponents(false);

            RagdollController.rootBone.SetLocalPositionAndRotation(_rootRagdollBoneDefaultPosition, _rootRagdollBoneDefaultRotation);
        }

        static void ToggleComponents(bool ragdollEnabled) {
            var vHero = Hero.Current.VHeroController;
            vHero.audioAnimator.enabled = !ragdollEnabled;
            vHero.HeroAnimator.enabled = !ragdollEnabled;
        }
    }
}