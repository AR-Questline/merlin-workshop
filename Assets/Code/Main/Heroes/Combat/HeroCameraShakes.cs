using System.Threading;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Modifiers;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public partial class HeroCameraShakes : Element<Hero> {
        const float StealthKillPositionForwardOffset = -2.5f;
        const float StealthKillPositionRightOffset = 0.75f;
        const float StealthKillDurationMultiplier = 1.75f;
        const float MinTimeBetweenSlowedTime = 5f;

        public sealed override bool IsNotSaved => true;
        
        bool _shakeOverriden;
        bool _allowCameraKillEffect;
        float _lastStealthKillCamera;
        CancellationTokenSource _overridenToken;
        CameraKillEffectSetting _cameraKillEffectSetting;
        VHeroController _heroController;

        static GameCamera GameCamera => World.Only<GameCamera>();
        protected override void OnInitialize() {
            ParentModel.HealthElement.ListenTo(HealthElement.Events.OnDamageBlocked, OnDamageBlockedShake, this);
            ParentModel.HealthElement.ListenTo(HealthElement.Events.OnDamageParried, OnDamageParriedShake, this);
            ParentModel.HealthElement.ListenTo(HealthElement.Events.OnDamageTaken, OnDamageTakenShake, this);
            ParentModel.ListenTo(HealthElement.Events.OnDamageDealt, OnDamageDealtShake, this);
            ParentModel.ListenTo(BowFSM.Events.OnBowRelease, OnBowReleaseShake, this);
            _cameraKillEffectSetting = World.Only<CameraKillEffectSetting>();
            _allowCameraKillEffect = _cameraKillEffectSetting.Enabled;
            _cameraKillEffectSetting.ListenTo(Setting.Events.SettingChanged, OnCameraKillEffectSettingChanged, this);
            _heroController = ParentModel.VHeroController;
        }

        public async UniTaskVoid CustomShake(bool isProactiveAction, float amplitude = 0.5f, float frequency = 0.15f, float time = 0.5f, float pick = 0.1f) {
            _overridenToken?.Cancel();
            _overridenToken = new CancellationTokenSource();
            
            _shakeOverriden = true;
            GameCamera.Shake(isProactiveAction, amplitude, frequency, time, pick).Forget();
            bool success = await AsyncUtil.DelayTime(ParentModel, time, source: _overridenToken);
            if (!success) {
                return;
            }
            _shakeOverriden = false;
            _overridenToken = null;
        }

        void OnDamageBlockedShake(Damage damage) {
            if (_shakeOverriden || damage.IsParried) {
                return;
            }

            _heroController.DirectionalCameraShakeFromBeingHit(damage.DealerPosition, damage.Amount);
        }

        void OnDamageParriedShake(Damage damage) {
            if (_shakeOverriden || !Hero.TppActive) {
                return;
            }
            
            _heroController.DirectionalCameraShakeFromBeingHit(damage.DealerPosition, damage.Amount);
        }
        
        void OnDamageTakenShake(DamageOutcome outcome) {
            if (_shakeOverriden || outcome.Damage.IsDamageOverTime || outcome.Damage.IsBlocked) {
                return;
            }

            _heroController.DirectionalCameraShakeFromBeingHit(outcome.Damage.DealerPosition, outcome.FinalAmount);
        }

        void OnDamageDealtShake(DamageOutcome outcome) {
            bool isFinisher = outcome.DamageModifiersInfo.IsFinisher;
            bool fromSneak = outcome.DamageModifiersInfo.IsSneak && _cameraKillEffectSetting.StealthEnabled;
            bool fromWeakSpot = outcome.DamageModifiersInfo.IsWeakSpot && _cameraKillEffectSetting.WeakspotEnabled;
            bool fromCritical = outcome.DamageModifiersInfo.IsCritical && _cameraKillEffectSetting.CriticalEnabled;
            bool anySpecialEffectAllowed = fromCritical || fromWeakSpot || fromSneak;
            bool enoughTimeSinceLast = _lastStealthKillCamera + MinTimeBetweenSlowedTime < Time.time;
            
            if (_shakeOverriden || isFinisher || !_allowCameraKillEffect || outcome.Damage.IsDamageOverTime) {
                return;
            }
            
            bool isProjectile = outcome.Damage.Projectile != null && outcome.Damage.Item is { IsRanged: true } or { IsMagic: true };
            
            if (!anySpecialEffectAllowed) {
                if (Hero.TppActive && !isProjectile) {
                    GameCamera.Shake(true, 0.75f, 5, 0.2f, 0.2f).Forget();
                }
                return;
            }

            if (!enoughTimeSinceLast) {
                return;
            }

            bool isDying = outcome.Target?.IsDying ?? false;
            
            if (!isProjectile) {
                GameCamera.Shake(true, 2.25f, 5, 0.4f, 0.2f).Forget();
                // --- Apply slow down time for melee hits only when critical
                if (outcome.DamageModifiersInfo.IsCritical) {
                    MeleeSlowDownTime(isDying);
                }
                return;
            }
            
            if (!fromSneak || !isDying) {
                // --- With projectiles only Sneak Kills should slow down time
                return;
            }
            
            if (!fromWeakSpot && !fromCritical) {
                // --- Normal stealth kills should only trigger camera very rarely
                if (RandomUtil.UniformFloat(0, 1.0f) > 0.1f) {
                    return;
                }
            }
            
            _lastStealthKillCamera = Time.time;
            RangedSlowDownTime(outcome);
        }
        
        void RangedSlowDownTime(DamageOutcome outcome) {
            Transform projectileTransform = outcome.Damage.Projectile.transform;
            if (projectileTransform == null) {
                return;
            }

            Vector3 projectileForward = projectileTransform.forward;
            Vector3 position = outcome.Position 
                               + projectileForward * StealthKillPositionForwardOffset 
                               + projectileTransform.right * StealthKillPositionRightOffset;
            Transform transformToFollow = outcome.Target.ParentTransform;
            if (outcome.Target is ICharacter character) {
                position = new Vector3(position.x, character.Coords.y + character.Height, position.z);
                transformToFollow = character.Head;
            }
            Quaternion rotation = Quaternion.LookRotation(transformToFollow.position - position, Vector3.up);
            
            GameConstants gc = GameConstants.Get;
            float duration = gc.chonkyCriticalSlowMoDuration * StealthKillDurationMultiplier;
            AddElement(new StealthKillCamera(transformToFollow, position, rotation, duration));
            SlowDownTime.SlowTime(new TimeDuration(duration, true), gc.chonkyCriticalSlowMoCurve, gc.chonkyCriticalSlowMoFovMultiplier);
        }

        void MeleeSlowDownTime(bool isDying) {
            GameConstants gc = GameConstants.Get;
            if (isDying) {
                SlowDownTime.SlowTime(new TimeDuration(gc.chonkyCriticalSlowMoDuration, true), gc.chonkyCriticalSlowMoCurve, gc.chonkyCriticalSlowMoFovMultiplier);
            } else {
                SlowDownTime.SlowTime(new TimeDuration(gc.criticalSlowMoDuration, true), gc.criticalSlowMoCurve, gc.criticalSlowMoFovMultiplier);
            }
        }
        
        void OnBowReleaseShake(bool _) {
            if (_shakeOverriden) {
                return;
            }

            GameCamera.Shake(true, 0.75f, 5, 0.2f, 0.2f).Forget();
        }

        void OnCameraKillEffectSettingChanged(Model model) {
            if (model is not CameraKillEffectSetting stealthKillCamera) {
                return;
            }
            _allowCameraKillEffect = stealthKillCamera.Enabled;
        }
    }
}