using System;
using System.Collections.Generic;
using System.Threading;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    public class BallistaArrow : Arrow {
        [TemplateType(typeof(LocationTemplate))]
        public TemplateReference destructibleReplacementArrowLocation;
        [SerializeField]
        List<DifficultyParameters> difficultyParameters = new();
        
        int _currentDifficultyIndex = -1;

        protected override void OnFullyConfigured() {
            base.OnFullyConfigured();
            
            var parameters = ParametersForCurrentDifficulty();
            AddMultiplierToBaseDamage(parameters.damageMultiplier);
            float baseDamage = parameters.overrideDamage
                ? parameters.baseDamageOverride
                : RawDamageData.UncalculatedValue;
            OverrideBaseDamage(baseDamage + parameters.ngPlusGain * Hero.Current.NewGamePlusLevel);
            SetVelocityAndForward(_rb.linearVelocity.normalized * parameters.projectileVelocity);
        }

        protected override void OnTargetHit(HitResult hitResult, bool environmentHit, IAlive aliveHit) {
            var colliderHit = hitResult.Collider;
            var cancellationTokenSource = new CancellationTokenSource();
            CustomTrailHolderBasedDestroy(hitResult.Point, cancellationTokenSource, discardOnHit).Forget();
            
            if (discardOnHit) {
                _rb.isKinematic = true;
                ReleaseSelf();
            } else {
                Target.RemoveElementsOfType<Skill>();
                _originalParent = _transform.parent;
                
                if (aliveHit != null && !aliveHit.HasBeenDiscarded) {
                    if (aliveHit is NpcElement {NpcType: NpcType.Boss}) {
                        _transform.SetParent(null);
                        ReflectArrowFromHitbox(hitResult);
                        ReleaseSelf(LifeTime).Forget();
                        return;
                    }
                }
                _rb.isKinematic = true;
                _transform.SetParent(environmentHit ? null : colliderHit.transform);
                foreach (Collider childCollider in _transform.GetComponentsInChildren<Collider>()) {
                    if (childCollider.isTrigger == false && childCollider.enabled == false) {
                        childCollider.enabled = true;
                    }
                }
                if (!destructibleReplacementArrowLocation.IsSet) {
                    ReleaseSelf(LifeTime).Forget();
                    return;
                }
                ReplaceProjectileWithDestructibleArrow();
                OnEnvironmentHit(hitResult.Point, hitResult.Normal);
            }
        }
        
        void ReplaceProjectileWithDestructibleArrow() {
            var location = destructibleReplacementArrowLocation.Get<LocationTemplate>();
            if (location == null) return;
            var destructibleArrow = location.SpawnLocation(overridenLocationName: "Destructible Ballista Arrow");
            destructibleArrow.MoveAndRotateTo(_transform.position, _transform.rotation);
            destructibleArrow.OnVisualLoaded(t => SetupDestructibleAndCleanupSelf(t, destructibleArrow));
        }
        
        void SetupDestructibleAndCleanupSelf(Transform destructibleArrowTransform, Location destructibleArrow) {
            ReleaseSelf();
        }
        
        void OnEnvironmentHit(Vector3 position, Vector3 hitResultNormal) {
            TrySpawnEnviroVfx(position, hitResultNormal);
            ParametersForCurrentDifficulty().explosionDamageParameters.ApplySphereDamage(position);
        }
        
        protected override void OnEnvironmentHit(EnvironmentHitData hitData, float bowDrawStrength) {}
        
        DifficultyParameters ParametersForCurrentDifficulty() {
            if (difficultyParameters == null || difficultyParameters.Count == 0) {
                return new DifficultyParameters {
                    projectileVelocity = 100f,
                    damageMultiplier = 1f
                };
            }
            
            if (_currentDifficultyIndex >= 0) {
                return difficultyParameters[_currentDifficultyIndex];
            }

            var difficulty = World.Only<DifficultySetting>().Difficulty;

            // Find exact match
            for (int i = 0; i < difficultyParameters.Count; i++) {
                if (difficultyParameters[i].rawDifficulty == difficulty) {
                    _currentDifficultyIndex = i;
                    return difficultyParameters[i];
                }
                if (difficultyParameters[i].rawDifficulty == null) {
                    _currentDifficultyIndex = i;
                }
            }
            
            // Fallback to first entry with null difficulty (default)
            if (_currentDifficultyIndex >= 0) {
                return difficultyParameters[_currentDifficultyIndex];
            }
            
            // Last resort: return first entry
            return difficultyParameters[0];
        }
        
        [Serializable]
        public struct DifficultyParameters {
            [RichEnumExtends(typeof(Difficulty)), LabelText("Player Difficulty")]
            public RichEnumReference rawDifficulty;

            public float projectileVelocity;
            public float damageMultiplier;
            public bool overrideDamage;
            [Indent, ShowIf(nameof(overrideDamage))] 
            public float baseDamageOverride;
            public float ngPlusGain;
            
            [Space, InfoBox("VFX is not used for explosion damage in Ballista Arrow.")]
            public SphereDamageSerializableParameters explosionDamageParameters;
        }
    }
}