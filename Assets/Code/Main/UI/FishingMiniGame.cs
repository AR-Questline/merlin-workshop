using System;
using System.Collections.Generic;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Code.Utility;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Fishing;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Cysharp.Threading.Tasks;
using FMODUnity;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.UI {
    [SpawnsView(typeof(VFishingMiniGame))]
    public partial class FishingMiniGame : Model, IUIPlayerInput {
        const float MinPosition = 0;
        const float MaxPosition = 100;
        const string FmodRodIntensity = "RodIntensity";
        const string SplashWaterIntensity = "Intensity";
        const float OneThird = 0.33f;
        const float TwoThirds = 0.66f;
        const float VfxReturnDelay = 2f;
        
        CharacterFishingRod _rod;
        float _rodDamage;
        float _pullDistance;
        float _rodDownfallSpeedPerSecond;
        
        FishData.FightingFish _fish;
        float _fishDestination;
        
        Vector3 _fishingBobberPosition;
        bool _playingRodAudio;
        IPooledInstance _currentVfxPooledInstance;
        VisualEffect _currentVfx;
        ShareableARAssetReference _vfx;

        Hero _hero;
        GameRealTime _gameRealTime;
        CommonReferences _commonReferences;
        double _lastMiniGameTickTimestamp;
        bool _isMiniGameStarted;
        bool _isPlayerMovingUpwards;
        CancellationTokenSource _vfxCancellationToken;
        
        public float RodPosition { get; private set; }
        public float MaxRodHealth { get; private set; }
        public float RodHealth { get; private set; }
        
        public float FishPosition { get; private set; }
        public float MaxFishHealth { get; private set; }
        public float FishHealth { get; private set; }
        public float FishRange { get; private set; }
        float FishMinPosition => MinPosition + FishRange;
        float FishMaxPosition => MaxPosition - FishRange;
        
        FishingFSM FishingFSM => _hero.TryGetElement<FishingFSM>();
        HeroStateType CurrentFishingState => FishingFSM?.CurrentStateType ?? HeroStateType.Empty;
        ref readonly FishingAudio Audio => ref _commonReferences.AudioConfig.FishingAudio;
        public sealed override bool IsNotSaved => true;
        public override Domain DefaultDomain => Domain.Gameplay;
        
        public new static class Events {
            public static readonly Event<FishingMiniGame, bool> SetViewActive = new(nameof(SetViewActive));
            public static readonly Event<FishingMiniGame, FishingMiniGame> OnMiniGameStart = new(nameof(OnMiniGameStart));
            public static readonly Event<FishingMiniGame, FishingMiniGame> OnMiniGameTick = new(nameof(OnMiniGameTick));
        }

        protected override void OnInitialize() {
            _hero = Hero.Current;
            _commonReferences = World.Services.Get<CommonReferences>();
            _gameRealTime = World.Only<GameRealTime>();
            this.GetOrCreateTimeDependent().WithUpdate(OnUpdate);
            _hero.HeroItems.ListenTo(HeroLoadout.Events.LoadoutChanged, () => FishingFSM.SetCurrentState(HeroStateType.Idle), this);
        }

        public UIResult Handle(UIEvent evt) {
            if (evt is UIKeyHeldAction key && key.Name == KeyBindings.Gameplay.Interact) {
                PullFishingLine(_hero.GetDeltaTime());
                return UIResult.Accept;
            }

            return UIResult.Ignore;
        }

        public IEnumerable<KeyBindings> PlayerKeyBindings {
            get { yield return KeyBindings.Gameplay.Interact; }
        }
        
        public static FishingMiniGame Show() {
            return ModelUtils.GetSingletonModel<FishingMiniGame>();
        }

        public void StartMiniGame(float rodStartingPosition, float fishStartingPosition, CharacterFishingRod rod, FishData.FightingFish fish, Vector3 fishingBobberPosition) {
            RodPosition = rodStartingPosition;
            FishPosition = fishStartingPosition;
            _fishingBobberPosition = fishingBobberPosition;
            
            _rod = rod;
            _rodDamage = rod.rodDamage;
            _pullDistance = rod.pullDistance;
            _rodDownfallSpeedPerSecond = rod.rodDownfallSpeedPerSecond;
            MaxRodHealth = _hero.Development.IncreasedFishingRodsDurability ? rod.rodHealth * GameConstants.Get.FishingRodIncreasedDurabilityMultiplier : rod.rodHealth;
            RodHealth = MaxRodHealth;
            
            _fish = fish;
            FishHealth = fish.health;
            MaxFishHealth = fish.health;
            FishRange = fish.optimalRange;
            
            _isMiniGameStarted = true;
            DesignateFishDestination();
            World.Only<PlayerInput>().RegisterPlayerInput(this, _hero);
            this.Trigger(Events.OnMiniGameStart, this);
            SpawnVFX().Forget();
        }

        public void StopMiniGame() {
            if (_currentVfxPooledInstance != null) {
                VFXUtils.StopVfxAndReturn(_currentVfxPooledInstance, VfxReturnDelay);
            }

            _vfxCancellationToken?.Cancel();
            _vfxCancellationToken = null;
            _isMiniGameStarted = false;
            World.Only<PlayerInput>().UnregisterPlayerInput(this);
            this.Trigger(Events.SetViewActive, false);
        }
        
        public bool IsWithinRange() {
            bool isAboveRange = RodPosition > FishPosition + _fish.optimalRange;
            bool isBelowRange = RodPosition < FishPosition - _fish.optimalRange;
            return !isAboveRange && !isBelowRange;
        }

        void OnUpdate(float deltaTime) {
            if (CurrentFishingState is HeroStateType.FishingFight && _isMiniGameStarted) {
                CalculateDamage(deltaTime);
                MoveFish(deltaTime);
                HandleRodDownfall(deltaTime);
                UpdateFishingAnimationWeight();

                var totalSeconds = _gameRealTime.PlayRealTime.TotalSeconds;
                if (totalSeconds - _lastMiniGameTickTimestamp > _fish.changeDestinationInterval) {
                    _lastMiniGameTickTimestamp = totalSeconds;
                    MiniGameTick();
                }
            }
            
            _isPlayerMovingUpwards = false;
            this.Trigger(Events.OnMiniGameTick, this);
        }
        
        async UniTaskVoid SpawnVFX() {
            _vfxCancellationToken = new CancellationTokenSource();
            _currentVfxPooledInstance = await PrefabPool.Instantiate(_commonReferences.waterSplashingVfx, _fishingBobberPosition, Quaternion.identity, cancellationToken: _vfxCancellationToken.Token);
        }

        void PullFishingLine(float deltaTime) {
            _isPlayerMovingUpwards = true;
            var positionChange = _pullDistance * deltaTime;
            if (CurrentFishingState is HeroStateType.FishingFight && RodPosition + positionChange < MaxPosition) {
                RodPosition += positionChange;
            }
        }

        void MoveFish(float deltaTime) {
            FishPosition = Mathf.MoveTowards(FishPosition, _fishDestination, _fish.speed * deltaTime);
        }

        void HandleRodDownfall(float deltaTime) {
            if (_isPlayerMovingUpwards) return;
            
            var positionChange = _rodDownfallSpeedPerSecond * deltaTime;
            if (RodPosition - positionChange > MinPosition) {
                RodPosition -= positionChange;
            }
        }

        void DesignateFishDestination() {
            _fishDestination = RandomUtil.UniformFloat(FishMinPosition, FishMaxPosition);
        }

        void UpdateFishingAnimationWeight() {
            var rangeSign = Mathf.Sign(FishPosition - RodPosition);
            var weightSign = rangeSign * -1;
            var rangeInFishDirection = (_fish.optimalRange * rangeSign) / 2;
            var weight = Mathf.InverseLerp(RodPosition, RodPosition + rangeInFishDirection, FishPosition);
            FishingFSM.fishingFightWeight = weight * weightSign;
        }

        void CalculateDamage(float deltaTime) {
            if (IsWithinRange()) {
                _rod.PauseAudioClip();
                _playingRodAudio = false;
                FishHealth -= _rodDamage * deltaTime;
                _currentVfx ??= _currentVfxPooledInstance?.Instance?.GetComponent<VisualEffect>();
                
                if (_currentVfx != null) {
                    _currentVfx.SetFloat(SplashWaterIntensity, FishHealth / MaxFishHealth);
                }

                RewiredHelper.StopVibration();
                FishingFSM.CameraShakesMultiplier = 0.01f;
            } else {
                var rodHealthPercentage = RodHealth / MaxRodHealth;

                if (!_playingRodAudio) {
                    _playingRodAudio = true;
                    _rod.PlayAudioClip(Audio.fishingRodStruggling, false, new FMODParameter(FmodRodIntensity, rodHealthPercentage));
                } else {
                    _rod.SetParameter(FmodRodIntensity, rodHealthPercentage);
                    _rod.UnpauseAudioClip();
                }

                RodHealth -= _fish.damage * deltaTime;
                RewiredHelper.VibrateHighFreq(GetVibrationStrength(rodHealthPercentage), VibrationDuration.Continuous);
                FishingFSM.CameraShakesMultiplier = GetScreenShakeMultiplier(rodHealthPercentage);
            }
        }
        
        float GetScreenShakeMultiplier(float rodHealthPercentage) {
            return rodHealthPercentage switch {
                > TwoThirds => 0.25f,
                > OneThird => 0.5f,
                _ => 0.75f
            };
        }

        VibrationStrength GetVibrationStrength(float rodHealthPercentage) {
            return rodHealthPercentage switch {
                > TwoThirds => VibrationStrength.VeryLow,
                > OneThird => VibrationStrength.Medium,
                _ => VibrationStrength.VeryStrong
            };
        }
        
        void MiniGameTick() {
            if (FishHealth <= 0) {
                this.Trigger(Events.SetViewActive, false);
                
                var catchAudio = _fish.quality switch {
                    FishQuality.Common => Audio.catchCommonFish,
                    FishQuality.Uncommon => Audio.catchUncommonFish,
                    FishQuality.Rare => Audio.catchRareFish,
                    FishQuality.Legendary => Audio.catchLegendaryFish,
                    FishQuality.Garbage => Audio.catchGarbage,
                    _ => throw new ArgumentOutOfRangeException()
                };
                FMODManager.PlayOneShot(catchAudio, _fishingBobberPosition);
                FishingFSM.CameraShakesMultiplier = 1f;
                RewiredHelper.StopVibration();
                FishingFSM.SetCurrentState(HeroStateType.FishingPullOut);
                return;
            }

            if (RodHealth <= 0) {
                this.Trigger(Events.SetViewActive, false);
                FMODManager.PlayOneShot(Audio.lineBreak);
                Hero.Current.Trigger(FishingFSM.Events.Fail, Hero.Current);
                return;
            }

            DesignateFishDestination();
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            this.GetTimeDependent()?.WithoutUpdate(OnUpdate);
        }
    }
}