using System.Threading;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items.Weapons;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Controls;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Heroes {
    public partial class HeroFoV : Element<Hero> {
        public const float DefaultFoVChangeLength = 0.5f;
        const float FastFoVChangeLength = 0.33f;
        const float SprintStartLength = 1.25f;
        const float SprintEndLength = 0.75f;
        const float SprintFovMult = 1.055f;
        const float SlideFovMult = 1.1f;
        const float DashFovMult = 1.05f;

        public sealed override bool IsNotSaved => true;

        float DefaultFoV { get;  set; }
        float FoVChangeMultiplier { get;  set; }
        
        bool _sprintActive;
        bool _slideActive;
        bool _dashActive;
        float _weaponFovMultiplier = 1f;
        float _bowShotFovMultiplier = 1f;
        float _toolFovMultiplier = 1f;
        float _slowTimeFovMultiplier = 1f;
        float _customLocomotionFovMultiplier = 1f;
        CancellationTokenSource _sprintDelayCancellationToken;
        bool _fovChangesAllowed;

        Hero Hero => ParentModel;
        float FoV => DefaultFoV * _toolFovMultiplier * (_fovChangesAllowed ? GetMovementFoVMultiplier() * _weaponFovMultiplier * _bowShotFovMultiplier * _slowTimeFovMultiplier : 1);

        public new static class Events {
            public static readonly Event<Hero, FoVChangeData> FoVUpdated = new(nameof(FoVUpdated));
        }
        
        protected override void OnInitialize() {
            var fovSettings = World.Only<FOVSetting>();
            var fovChangeSettings = World.Only<FOVChanges>();
            fovSettings.ListenTo(Setting.Events.SettingChanged, setting => RefreshFoV(setting as FOVSetting), this);
            fovChangeSettings.ListenTo(Setting.Events.SettingChanged, setting => RefreshFoVMultiplier(setting as FOVChanges), this);
            fovChangeSettings.ListenTo(Setting.Events.SettingRefresh, UpdateFOVChangesAllowed, this);
            Hero.ListenTo(Hero.Events.HeroSprintingStateChanged, UpdateHeroSprintFoV, this);
            DefaultFoV = fovSettings.FOV;
            FoVChangeMultiplier = fovChangeSettings.FOVChangeMultiplier;
            _fovChangesAllowed = fovChangeSettings.AreFOVChangesAllowed;
        }

        void UpdateFOVChangesAllowed(Setting setting) {
            if (setting is FOVChanges fovChanges) {
                _fovChangesAllowed = fovChanges.AreFOVChangesAllowed;
            }
        }

        void RefreshFoV(FOVSetting fovSetting) {
            DefaultFoV = fovSetting.FOV;
            UpdateFoV();
        }

        void RefreshFoVMultiplier(FOVChanges fovChanges) {
            FoVChangeMultiplier = fovChanges.FOVChangeMultiplier;
            UpdateFoV();
        }
        
        public void UpdateFoV(float changeLength = DefaultFoVChangeLength) {
            var data = new FoVChangeData(FoV, changeLength);
            Hero.Trigger(Events.FoVUpdated, data);
        }
        
        // --- Movement
        
        public void UpdateHeroSprintFoV(bool enabled) {
            if (_sprintDelayCancellationToken != null) {
                _sprintDelayCancellationToken.Cancel();
                _sprintDelayCancellationToken = null;
            }
            
            if (!ParentModel.Grounded) {
                DelayHeroSprintUpdateTillGrounded(enabled).Forget();
                return;
            }
            
            _sprintActive = enabled;
            UpdateFoV(enabled ? SprintStartLength : SprintEndLength);
        }
        
        async UniTaskVoid DelayHeroSprintUpdateTillGrounded(bool enabled) {
            _sprintDelayCancellationToken = new CancellationTokenSource();
            var vHeroController = ParentModel.VHeroController;
            if (!await AsyncUtil.WaitWhile(ParentModel, () => !vHeroController.Grounded, _sprintDelayCancellationToken)) {
                return;
            }
            _sprintActive = enabled;
            UpdateFoV(enabled ? SprintStartLength : SprintEndLength);
        }
        
        public void UpdateHeroDashedFoV(bool enabled) {
            _dashActive = enabled;
            UpdateFoV(FastFoVChangeLength);
        }
        
        public void UpdateHeroSlidedFoV(bool enabled) {
            _slideActive = enabled;
            UpdateFoV(enabled ? DefaultFoVChangeLength : FastFoVChangeLength);
        }
        
        public void ApplySpyglassZoom(bool enabled) {
            _toolFovMultiplier = enabled ? GameConstants.Get.SpyglassFovMultiplier : 1;
            UpdateFoV();
        }
        
        public void UpdateCustomLocomotionFoVMultiplier(float multiplier, float duration = DefaultFoVChangeLength) {
            if (!Mathf.Approximately(multiplier, _customLocomotionFovMultiplier)) {
                _customLocomotionFovMultiplier = multiplier;
                UpdateFoV(duration);
            }
        }
        
        float GetMovementFoVMultiplier() {
            float multiplier = 1f;
            
            if (_sprintActive) {
                multiplier *= SprintFovMult;
            }
            if (_slideActive) {
                multiplier *= SlideFovMult;
            }
            if (_dashActive) {
                multiplier *= DashFovMult;
            }
            
            multiplier *= _customLocomotionFovMultiplier;
            
            return Mathf.LerpUnclamped(1f, multiplier, FoVChangeMultiplier);
        }
        
        // --- Weapons
        
        public void ApplyBowZoomFoV() {
            ItemStats itemItemStats = Hero.MainHandWeapon?.Item?.ItemStats;
            if (itemItemStats == null) return;

            _weaponFovMultiplier = itemItemStats.RangedZoomModifier;
            UpdateFoV();
            Hero.Trigger(ICharacter.Events.OnBowZoomStart, Hero);
        }

        public void EndBowZoomFoV() {
            _weaponFovMultiplier = 1f;
            UpdateFoV();
            Hero.Trigger(ICharacter.Events.OnBowZoomEnd, Hero);
        }

        public void ApplyBowShootZoom(float value) {
            _bowShotFovMultiplier = value;
            UpdateFoV();
        }

        public void EndBowShootZoom() {
            _bowShotFovMultiplier = 1f;
            UpdateFoV();
        }
        
        // --- Other

        public void ApplySlowTimeZoom(float value) {
            _slowTimeFovMultiplier = value;
            UpdateFoV();
        }
        
        public void EndBowSlowTimeZoom() {
            _slowTimeFovMultiplier = 1f;
            UpdateFoV();
        }

        public struct FoVChangeData {
            public float newFoV;
            public float changeLength;

            public FoVChangeData(float newFoV, float changeLength) {
                this.newFoV = newFoV;
                this.changeLength = changeLength;
            }
        }
    }
}
