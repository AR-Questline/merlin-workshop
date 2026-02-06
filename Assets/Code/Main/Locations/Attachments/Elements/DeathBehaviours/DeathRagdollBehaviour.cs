using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Timing.ARTime.TimeComponents;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours {
    public abstract class DeathRagdollBehaviour {
        public RagdollController RagdollController { get; protected set; }
        protected virtual Optional<Vector3> ForceDirectionOverride => Optional<Vector3>.None;
        protected abstract IModel TimeOwnerModel { get; }

        protected void AddForceToRagdoll(in EnableSetup setup) {
            var rootBone = RagdollController.rootBone;

            var rb = rootBone.GetComponentInChildren<Rigidbody>();
            if (rb == null) {
                Log.Important?.Error($"Ragdoll root has no rigidbody!", rootBone);
                return;
            }
            
            var hitPosition = setup.hitPosition.GetValueOrDefault(rootBone.position);
            float timeScale = TimeOwnerModel.GetTimeScale();
            
            ApplyForces(setup, rb, timeScale, hitPosition, ForceDirectionOverride);
        }

        public static void ApplyForces(EnableSetup setup, Rigidbody rb, float timeScale, Vector3 hitPosition, Optional<Vector3> forceDirectionOverride) {
            if (setup.radius > 0) {
                rb.AddExplosionForce(setup.forceMagnitude * timeScale, hitPosition, setup.radius, 1, ForceMode.Impulse);
            } else {
                var direction = setup.forceMagnitude * timeScale * forceDirectionOverride.GetValueOrDefault(setup.forceDirection);
                rb.AddForceAtPosition(direction, hitPosition, ForceMode.Impulse);
            }
        }

        protected void AdditionalRigidbodySetup(Rigidbody rigidbody) {
            TimeOwnerModel?.GetTimeDependent()?.WithTimeComponent(new TimeRigidbody(rigidbody));
        }

        public static EnableSetup SetupFromDamageOutcome(DamageOutcome damageOutcome) {
            var forceMagnitude = damageOutcome.RagdollForce.magnitude;
            var forceDirection = forceMagnitude > 0 ? damageOutcome.RagdollForce / forceMagnitude : damageOutcome.RagdollForce;

            return new EnableSetup {
                forceDirection = forceDirection,
                forceMagnitude = forceMagnitude,
                hitPosition = damageOutcome.Position,
                radius = damageOutcome.Radius
            };
        }

        public struct EnableSetup {
            public Vector3 forceDirection;
            public float forceMagnitude;
            public Optional<Vector3> hitPosition;
            public float radius;
        }
    }
}