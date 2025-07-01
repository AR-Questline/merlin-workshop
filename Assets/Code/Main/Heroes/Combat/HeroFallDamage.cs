using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.MovementSystems;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public partial class HeroFallDamage : Element<Hero> {
        const float MinFallHeightToVibrate = 1f;
        
        public sealed override bool IsNotSaved => true;

        float _yOnLiftOff = float.MinValue;
        float? _nullifiedHeight = null;
        float _invulnerabilityToFallDamage;
        bool _wasGrounded;
        bool _fallDamageEnabled = true;
        
        /// <summary>
        /// [0] - falling force
        /// [1] - surface hit type
        /// </summary>
        readonly FMODParameter[] _heroLandedParams = new FMODParameter[2];

        float DamageNullifier => ParentModel.HeroStats.DamageNullifier.ModifiedValue;
        float Multiplier => ParentModel.HeroStats.FallDamageMultiplier.ModifiedValue;
        
        protected override void OnInitialize() {
            _heroLandedParams[1] = SurfaceType.FallNone;
            ParentModel.AfterFullyInitialized(AfterParentFullyInitialized);
            ParentModel.ListenTo(GroundedEvents.TeleportRequested, OnHeroTeleported, this);
            ParentModel.ListenTo(GroundedEvents.BeforeTeleported, OnHeroTeleported, this);
            ParentModel.ListenTo(HeroMovementSystem.Events.MovementSystemChanged(MovementType.Glider), HeroStartedGliding, this);
            ParentModel.ListenTo(Hero.Events.HeroJumped, CheckConditions, this);
        }

        void HeroStartedGliding(HeroMovementSystem obj) {
            _fallDamageEnabled = false;
            obj.ListenTo(Events.BeforeDiscarded, () => {
                _fallDamageEnabled = true;
                _yOnLiftOff = ParentModel.Coords.y;
            }, this);
        }

        void AfterParentFullyInitialized() {
            _wasGrounded = true;
            ParentModel.GetOrCreateTimeDependent().WithUpdate(Update);
        }

        void OnHeroTeleported() {
            IgnoreFallDamageForDuration();
        }

        void Update(float deltaTime) {
            if (_invulnerabilityToFallDamage > 0) {
                _invulnerabilityToFallDamage -= deltaTime;
                if (_invulnerabilityToFallDamage <= 0) {
                    _yOnLiftOff = ParentModel.Coords.y;
                }
                return;
            }

            CheckConditions();
        }

        void CheckConditions() {
            if (!_fallDamageEnabled) {
                return;
            }

            Vector3 heroCoords = ParentModel.Coords;
            bool heroGrounded = ParentModel.Grounded;
            
            if (ParentModel.IsSwimming) {
                _yOnLiftOff = heroCoords.y;
                return;
            }

            // Hero just jumped/fell off a ledge
            if (_wasGrounded && !heroGrounded) {
                _yOnLiftOff = heroCoords.y;
                _wasGrounded = false;
                return;
            }
            
            if (!_wasGrounded) {
                if (ParentModel.HasElement<GravityMarker>()) {
                    _yOnLiftOff = heroCoords.y;
                }
                if (heroGrounded) {
                    HeroLanded();
                }
            }
            _wasGrounded = heroGrounded;
        }

        void HeroLanded() {
            float originalHeightDifference = _yOnLiftOff - ParentModel.Coords.y;
            float heightDifference = originalHeightDifference - DamageNullifier;
            heightDifference = Mathf.Clamp(heightDifference, 0, float.MaxValue);

            float damage = FallDamageUtil.GetFallDamage(heightDifference, Multiplier);
            VibrationStrength vibrationStrength = VibrationStrength.VeryLow;
            VibrationDuration vibrationDuration = VibrationDuration.VeryShort;
            float noiseRange;
            if (damage > 0 && _invulnerabilityToFallDamage <= 0) {
                GetVibrationSettingsFromFallDamage(damage, out vibrationStrength, out vibrationDuration);
                ParentModel.DealFallDamage(damage);
                noiseRange = Mathf.Clamp(damage, 2f, CharacterDealingDamage.SoundRange);
            } else {
                noiseRange = 2f;
            }
            float noiseModifiers = ParentModel.Template.heroControllerData.jumpSoundMultiplier * ParentModel.HeroStats.NoiseMultiplier;
            AINoises.MakeNoise(noiseRange * noiseModifiers, NoiseStrength.Medium, false, ParentModel.Coords, ParentModel);

            if (_nullifiedHeight != null) {
                originalHeightDifference = _nullifiedHeight.Value;
                _nullifiedHeight = null;
            }

            if (originalHeightDifference > MinFallHeightToVibrate) {
                RewiredHelper.VibrateLowFreq(vibrationStrength, vibrationDuration);
            }

            // --- Audio
            _heroLandedParams[0] = new("FallingForce", Mathf.Lerp(0.25f, 1f, originalHeightDifference.Remap(0, 10, 0, 1)));
            FMODManager.PlayOneShot(CommonReferences.Get.AudioConfig.HeroLandedSound, ParentModel.Coords, _heroLandedParams);

            _heroLandedParams[1] = SurfaceType.FallNone;
            
            // --- Animation
            if (ParentModel.TryGetElement(out CameraShakesFSM cameraShakesFSM)) {
                if (originalHeightDifference > 2) {
                    cameraShakesFSM.SetCurrentState(HeroStateType.JumpEndStrong);
                } else {
                    cameraShakesFSM.SetCurrentState(HeroStateType.JumpEndLight);
                }
            }
        }

        public void SetFallDamageEnabled(bool fallDamageEnabled) {
            _fallDamageEnabled = fallDamageEnabled;
        }
        
        public void IgnoreFallDamageForDuration(float duration = 2.5f) {
            _invulnerabilityToFallDamage = duration;
        }
        
        public void FallDamageNullified(SurfaceType surfaceType) {
            IgnoreFallDamageForDuration(0.01f);
            _nullifiedHeight = _yOnLiftOff - ParentModel.Coords.y;
            _heroLandedParams[1] = surfaceType;
        }

        void GetVibrationSettingsFromFallDamage(float damage, out VibrationStrength strength, out VibrationDuration duration) {
            switch (damage) {
                case > 150:
                    duration = VibrationDuration.Long;
                    strength = VibrationStrength.VeryStrong;
                    break;
                case > 75:
                    duration = VibrationDuration.Medium;
                    strength = VibrationStrength.Strong;
                    break;
                case > 25:
                    duration = VibrationDuration.Short;
                    strength = VibrationStrength.Medium;
                    break;
                default:
                    duration = VibrationDuration.VeryShort;
                    strength = VibrationStrength.Low;
                    break;
            }
        }
    }
}
