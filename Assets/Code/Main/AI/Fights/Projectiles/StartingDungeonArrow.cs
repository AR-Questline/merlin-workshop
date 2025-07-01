using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.Utility.Collections;
using FMODUnity;
using System;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    /// <summary>
    /// This is debug arrow for instant killing AI from story in starting dungeon.
    /// </summary>
    public class StartingDungeonArrow : Arrow {
        [InfoBox("If set: arrow will target the head transform of the closest NPC with this template.")]
        [SerializeField] LocationReference locReference;
        [SerializeField] public EventReference onEnableAudio, onHitAudio;
        [SerializeField] public EventReference[] additionalOnHitAudio = Array.Empty<EventReference>();

        Transform _target;
        
        void OnEnable() {
            if (locReference.IsSet) {
                var location = locReference.MatchingLocations(null).FirstOrFallback(null);

                var targetNpc = location?.TryGetElement<NpcElement>();
                if (targetNpc != null) {
                    targetNpc.OnVisualLoaded((_, _) => {
                        _target = targetNpc.Head;
                        Setup();
                    });
                    return;
                }

                Log.Important?.Error($"StartingDungeonArrow: No NPC found with reference {locReference}");
            }

            Setup();
        }

        void Setup() {
            Setup(ProjectileLogicData.DefaultArrow, GetComponentInChildren<ProjectileVisualData>(), null, null, new ProjectileData());
            OverrideBaseDamage(9999);
            SetVelocityAndForward(_transform.forward * 20f);
            if (!onEnableAudio.IsNull) {
                //RuntimeManager.PlayOneShotAttached(onEnableAudio, gameObject, this);
            }
            FinalizeConfiguration();
        }

        protected override void ProcessUpdate(float deltaTime) {
            base.ProcessUpdate(deltaTime);
            
            if (!_initialized || !_isSetup || _destroyed || _target == null) {
                return;
            }
            Vector3 directionTowardsTarget = _target.position - _rb.position;
            SetVelocityAndForward(directionTowardsTarget.normalized * 20f);
        }

        protected override void OnContact(HitResult hitResult) {
            LocationSpec spec = GetComponentInParent<LocationSpec>();
            Location location = null;
            if (spec != null) {
                location = VGUtils.TryGetModel<Location>(spec.gameObject);
            }

            base.OnContact(hitResult);
            if (!onHitAudio.IsNull) {
                //RuntimeManager.PlayOneShot(onHitAudio, _rb.position);
            }

            if (additionalOnHitAudio.Length > 0) {
                //additionalOnHitAudio.ForEach(e => RuntimeManager.PlayOneShot(e, _rb.position));
            }

            location?.Discard();
        }

        protected override void LogMissingItemTemplateError() {}
        protected override void OnEnvironmentHit(EnvironmentHitData hitData, float bowDrawStrength) {}
    }
}