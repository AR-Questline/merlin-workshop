using System.Linq;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Cameras;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Universal;
using Awaken.TG.Utility;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Lockpicking {
    /// There are two moving parts here Lock and Picklock.
    /// Based on how close Picklock is to target rotation the lock is able to rotate closer to the open rotation.
    /// Picklock's zero/start rotation is <see cref="PickStartRotation"/>, but Lock's is 0
    [SpawnsView(typeof(VLockpicking), order = 0)]
    public partial class LockpickingInteraction : Element<Location>, IUIStateSource, IClosable {
        const int MinLockOpenRotation = 15;
        const int MaxLockOpenRotation = 90;
        const float MinPickRotation = 0;
        const float MaxPickRotation = 180;

        const float PickStartRotation = (MinPickRotation + MaxPickRotation) / 2;
        const float PickDamageBarrier = 0.95f; // Area where pick starts to wiggle
        const float SkillToToleranceMultiplier = 4; // So that skill proficiency has 100% effect at lvl 25 and 400% effect at lvl 100
        const float UnlockTriggerDelay = 1300;

        public sealed override bool IsNotSaved => true;

        float _pickAngleSpeed = 80f;
        float _lockAngleSpeed = 160f;
        AnimationCurve _lockMaxOpenAngleRemap;
        
        int _currentLevel;
        
        float _lockCurrentMaxAngle;
        float _currentPicklockRotation;
        float _pickHp = 1;
        bool _noPicklockLeft;
        bool _inSuccessDelay;
        // If true then user need to zero/reset/release lock rotation input in order to rotate lock again
        bool _requiresZeroInput;

        LockpickingAudio AudioEvents => View<VLockpicking>().audioEvents;
        ARFmodEventEmitter Emitter => View<VLockpicking>().audioEmitter;

        public float CurrentPicklockRotation {
            get => _currentPicklockRotation;
            private set {
                _currentPicklockRotation = value;
                UpdateLockCurrentMaxAngle();
            }
        }
        
        public float CurrentLockRotation { get; private set; }
        
        public LimitedStat HeroTheft => Hero.Current.ProficiencyStats.Theft;
        public LimitedStat HeroToleranceMultiplier => Hero.Current.HeroStats.LockpickToleranceMultiplier;
        public Transform AnimationsParent => View<VLockpicking3D>().AnimationsParent;

        public LockAction Properties { get; }

        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown)
                                         .WithPauseTime()
                                         .WithCursorHidden();

        LockpickingAnimations Animations => Element<LockpickingAnimations>();
        public bool IsBlocked => Animations.IsBlocked || _noPicklockLeft || _inSuccessDelay;

        float LockCurrentMaxAngle {
            get => _lockCurrentMaxAngle;
            set {
                if (CurrentLockRotation > value) {
                    CurrentLockRotation = value;
                }
                _lockCurrentMaxAngle = value;
            }
        }

        HeroStats HeroStats => Hero.Current.HeroStats;
        GameConstants GameConstants => World.Services.Get<GameConstants>();

        public new static class Events {
            public static readonly Event<Location, LockAction> Unlocked = new(nameof(Unlocked));
            public static readonly Event<Location, LockAction> PickBroke = new(nameof(PickBroke));
            public static readonly Event<LockpickingInteraction, LockAction> PickDamaged = new(nameof(PickDamaged));
            public static readonly Event<LockpickingInteraction, int> LevelStarted = new(nameof(LevelStarted));
            
            //Should not be triggered directly
            public static readonly Event<Hero, LockAction> HeroLockUnlocked = new(nameof(HeroLockUnlocked));
            public static readonly Event<Hero, LockAction> HeroPickBroke = new(nameof(HeroPickBroke));
        }

        public LockpickingInteraction(LockAction properties) {
            Properties = properties;
            AddElement<LockpickingAnimations>();
        }

        protected override void OnInitialize() {
            World.SpawnView(this, Properties.Get3DViewType);
            Hero.Current.View<VHeroController>().Hide();
        }

        protected override void OnFullyInitialized() {
            _pickAngleSpeed = GameConstants.pickAngleSpeed;
            _lockAngleSpeed = GameConstants.lockAngleSpeed;
            _lockMaxOpenAngleRemap = GameConstants.lockMaxOpenAngleRemap;
            
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelFullyInitialized<HeroDialogueInvolvement>(), this, Close);
            Hero.Current.HealthElement.ListenTo(HealthElement.Events.BeforeDamageTaken, Close, this);
            
            // Setup UI such as complexity displays
            this.ListenTo(VModalBlocker.Events.ModalDismissed, Close, this);
            ParentModel.ListenTo(Events.Unlocked, action => {
                if (action == Properties) {
                    Hero.Current.Trigger(Events.HeroLockUnlocked, Properties);
                }
            }, this);
            ParentModel.ListenTo(Events.PickBroke, action => {
                if (action == Properties) {
                    Hero.Current.Trigger(Events.HeroPickBroke, Properties);
                }
            }, this);
            ResetPicklockState();
            ResetLockState();

            PlayAudioClip(AudioEvents.enterToolsIntoLock);
            Animations.PlayStartAnimation();
            this.Trigger(Events.LevelStarted, _currentLevel);
        }
        
        // === View actions
        public void PlayerTryOpen(float deltaTime, float inputSpeed) {
            // move tension tool
            if (CurrentLockRotation > LockCurrentMaxAngle) {
                PauseRotateAndDamageAudio();
                return;
            }

            if (inputSpeed < M.Epsilon) {
                PauseRotateAndDamageAudio();
                _requiresZeroInput = false;
                return;
            }
            if (_requiresZeroInput) {
                PauseRotateAndDamageAudio();
                return;
            }

            CurrentLockRotation += inputSpeed * _lockAngleSpeed * deltaTime;
            
            if (CurrentLockRotation >= LockCurrentMaxAngle) {
                CurrentLockRotation = LockCurrentMaxAngle;
                
                // if reached target angle and it is correct
                if (CurrentLockRotation >= MaxLockOpenRotation) {
                    PauseRotateAndDamageAudio();
                    CurrentLockRotation = MaxLockOpenRotation;
                    OpenSuccess().Forget();
                    return;
                }
            }

            if (LockCurrentMaxAngle != MaxLockOpenRotation && CurrentLockRotation >= LockCurrentMaxAngle * PickDamageBarrier) {
                PauseAudioClip(AudioEvents.lockRotateOpen);
                HandlePickDamage(deltaTime);
                if (!IsPlaying(AudioEvents.pickDamageTaken)) {
                    PlayAudioClip(AudioEvents.pickDamageTaken, false);
                }
            } else {
                if (!IsPlaying(AudioEvents.lockRotateOpen)) {
                    PlayAudioClip(AudioEvents.lockRotateOpen, false);
                }
            }
        }

        bool IsPlaying(EventReference eventReference) => Emitter.EventReference.Guid == eventReference.Guid && false;//Emitter.IsPlaying();
        
        void PauseRotateAndDamageAudio() {
            PauseAudioClip(AudioEvents.lockRotateOpen);
            PauseAudioClip(AudioEvents.pickDamageTaken);
        }

        public void PlayerRotatePick(float deltaTime, float inputSpeed) {
            CurrentPicklockRotation += inputSpeed * _pickAngleSpeed * deltaTime;
            CurrentPicklockRotation = Mathf.Clamp(CurrentPicklockRotation, MinPickRotation, MaxPickRotation);
            RewiredHelper.VibrateLowFreq(VibrationStrength.VeryLow, VibrationDuration.Continuous);
        }

        public void Close() {
            Discard();
        }

        // === State management
        void ResetPicklockState() {
            CurrentPicklockRotation = PickStartRotation;
            _pickHp = 1;
        }

        public void ResetLockState() {
            CurrentLockRotation = 0;
        }

        // === Helpers
        void UpdateLockCurrentMaxAngle() {
            float tolerance = Properties.Tolerance.angle * HeroTheft.Percentage * SkillToToleranceMultiplier * HeroToleranceMultiplier.ModifiedValue;
            float angleToOpen = Properties.Angles[_currentLevel];
            float distance = Mathf.Abs(angleToOpen - CurrentPicklockRotation)-tolerance;
            distance = Mathf.Max(distance, 0);
            var remapDistance = _lockMaxOpenAngleRemap.Evaluate(distance);
            LockCurrentMaxAngle = Mathf.RoundToInt(Mathf.Lerp(MinLockOpenRotation, MaxLockOpenRotation, remapDistance));
        }
        
        void HandlePickDamage(float deltaTime) {
            ConsumePickHP(deltaTime);
            
            if (_pickHp <= 0) {
                OpenFail().Forget();
            } else {
                RewiredHelper.VibrateHighFreq(VibrationStrength.Medium, VibrationDuration.Continuous);
            }
        }
        
        void ConsumePickHP(float deltaTime) {
            _pickHp -= Properties.Tolerance.toolDamage * deltaTime * HeroStats.LockpickDamageMultiplier;
            Animations.PlayPicklockDamageAnimation();
            this.Trigger(Events.PickDamaged, Properties);
        }
        
        void PlayAudioClip(EventReference eventReference, bool asOneShot = true, params FMODParameter[] eventParams) {
            if (asOneShot) {
                FMODManager.PlayAttachedOneShotWithParameters(eventReference, Emitter.gameObject, Emitter, eventParams);
            } else if (Emitter.EventReference.Guid != eventReference.Guid) {
                //Emitter.UnPause();
                //Emitter.PlayNewEventWithPauseTracking(eventReference, eventParams);
            } else {
                //Emitter.UnPause(true);
            }
        }

        void PauseAudioClip(EventReference eventReference) {
            if (Emitter.EventReference.Guid == eventReference.Guid) {
                //Emitter.Pause();
            }
        }
        
        // === Final actions
        async UniTaskVoid OpenSuccess() {
            _inSuccessDelay = true;
            _requiresZeroInput = true;
            RewiredHelper.VibrateHighFreq(VibrationStrength.VeryStrong, VibrationDuration.VeryShort);
            
            if (Properties.Complexity > _currentLevel) {
                PlayAudioClip(AudioEvents.lockToNextLayer);

                if (!await AsyncUtil.DelayTime(this, GameConstants.lockSuccessDelay/1000, true)) {
                    return;
                }

                ++_currentLevel;
                this.Trigger(Events.LevelStarted, _currentLevel);
                Animations.PlayNextLevelAnimation();
                ResetStateWithDelay().Forget();
                return;
            }
            // Unlocking complete
            PlayAudioClip(AudioEvents.lockOpen);

            if (!await AsyncUtil.DelayTime(this, GameConstants.lockSuccessDelay/1000, true)) {
                return;
            }
            
            Animations.PlayLockOpenedAnimation();
            ResetStateWithDelay().Forget();

            if (!await AsyncUtil.DelayTime(this, UnlockTriggerDelay/1000, true)) {
                return;
            }

            CommitCrime.Lockpicking(ParentModel);
            ParentModel.Trigger(Events.Unlocked, Properties);
        }
        
        async UniTaskVoid ResetStateWithDelay() {
            if (!await AsyncUtil.DelayTime(this, GameConstants.lockResetDelay/1000, true)) {
                return;
            }
            ResetPicklockState();
            ResetLockState();
            _inSuccessDelay = false;
        }

        async UniTaskVoid OpenFail() {
            // Remove pick item
            _requiresZeroInput = true;
            var heroItems = World.Only<HeroItems>();
            var picklock = heroItems.Items.First(i => i.HasElement<Lockpick>());
            picklock.ChangeQuantity(-1);

            RewiredHelper.VibrateHighFreq(VibrationStrength.VeryStrong, VibrationDuration.VeryShort);

            PauseAudioClip(AudioEvents.pickDamageTaken);
            PlayAudioClip(AudioEvents.pickBreak);
            if (picklock.HasBeenDiscarded) {
                Animations.PlayNoPicklockAnimation();
                _noPicklockLeft = true;
            } else {
                Animations.PlayPicklockBrokenAnimation();
            }
            ParentModel.Trigger(Events.PickBroke, Properties);
            
            if (heroItems.Items.All(i => !i.HasElement<Lockpick>())) {
                // Exit current callstack otherwise unity crashes
                if (!await AsyncUtil.DelayTime(this, UnlockTriggerDelay/1000, true)) {
                    return;
                }
                // No more picks left. Close lockpicking
                Close();
                return;
            }
            
            // Revert to initial state
            ResetPicklockState();
            ResetLockState();
        }

        public void OnLockpickStartMoving() {
            PlayAudioClip(AudioEvents.pickRotate, false);
        }

        public void OnLockpickStoppedMoving() {
            PauseAudioClip(AudioEvents.pickRotate);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            PauseAudioClip(AudioEvents.pickRotate);
            Hero.Current.View<VHeroController>().Show();
        }
    }
}