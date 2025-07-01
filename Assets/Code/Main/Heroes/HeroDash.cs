using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using FMODUnity;

namespace Awaken.TG.Main.Heroes {
    public partial class HeroDash : Element<Hero> {
        const float DashDefaultRegenDuration = 3f;
        const int MaxExhaustDashStacks = 1;
        const string DashFMODParameterName = "Dash";
        const string AttackFMODParameterName = "Attack";

        public sealed override bool IsNotSaved => true;
        
        static StatusTemplate PersistentOptimalTemplate => CommonReferences.Get.PersistentDashStatus;
        static StatusTemplate OptimalTemplate => CommonReferences.Get.DashStatus(true);
        static StatusTemplate ExhaustTemplate => CommonReferences.Get.DashStatus(false);
        
        Status _persistentStatusDashOptimal;
        Status _statusDashOptimal;
        Status _statusDashExhaust;
        IEventListener _statusListener;
        VHeroController _controller;
        StatTweak _dashSpeedTweak;
        
        Stat DashMaxOptimalCounter => ParentModel.HeroStats.MaxDashOptimalCounter;
        float DashRegenDuration => DashDefaultRegenDuration * ParentModel.HeroStats.DashRegenDurationMultiplier;
        
        protected override void OnInitialize() {
            ParentModel.ListenTo(Hero.Events.HeroDashed, OnHeroDashed, this);
            ParentModel.ListenTo(Stat.Events.StatChanged(DashMaxOptimalCounter.Type), OnHeroDashLimitChanged, this);
            _dashSpeedTweak = StatTweak.Multi(ParentModel.HeroStats.DashSpeed, 1, parentModel: this);
            ApplyPersistentOptimalStatus();
            _controller = ParentModel.VHeroController;
        }
        
        void OnHeroDashed(bool lungeAttack) {
            PlayDashAudio(lungeAttack);

            if (!lungeAttack) {
                UpdateDashStatus();
            }
        }

        void UpdateDashStatus() {
            float dashMultiplier = 1;
            if (_persistentStatusDashOptimal != null) {
                int stacks = _persistentStatusDashOptimal.StackLevel - 1;
                if (stacks > 0) {
                    ApplyOptimalStatus(stacks);
                } else {
                    ApplyExhaustStatus();
                }
            } else if (_statusDashOptimal != null) {
                _statusDashOptimal.ConsumeStack();
                _statusDashOptimal.TryGetElement<IDuration>()?.ResetDuration();
                if (_statusDashOptimal.StackLevel <= 0) {
                    ApplyExhaustStatus();
                }
            } else {
                dashMultiplier = _statusDashExhaust.StackLevel.RemapInt(0, MaxExhaustDashStacks, 1, 0.5f);
                _statusDashExhaust.TryGetElement<IDuration>()?.ResetDuration();
                if (_statusDashExhaust.StackLevel < MaxExhaustDashStacks) {
                    _statusDashExhaust.IncreaseStack();
                }
            }
            // Apply Dash Speed Multiplier
            _dashSpeedTweak.SetModifier(dashMultiplier);
        }
        
        void PlayDashAudio(bool attack) {
            bool isExhaustedDash = _statusDashExhaust != null;
            FMODParameter[] parameters = {
                new(DashFMODParameterName, isExhaustedDash),
                new(AttackFMODParameterName, attack)
            };
            _controller.PlayAudioClip(AliveAudioType.Dash, asOneShot: true, eventParams: parameters);
        }

        void OnHeroDashLimitChanged(Stat stat) {
            if (_persistentStatusDashOptimal != null) {
                _persistentStatusDashOptimal.SetStacksTo(stat.ModifiedInt);
            }
            if (_statusDashOptimal != null) {
                if (_statusDashOptimal.StackLevel > stat.ModifiedInt) {
                    _statusDashOptimal.SetStacksTo(stat.ModifiedInt);
                }
            }
        }
        
        // === Helpers
        public void ApplyPersistentOptimalStatus() {
            World.EventSystem.TryDisposeListener(ref _statusListener);
            RemoveStatus(ref _statusDashOptimal);
            RemoveStatus(ref _statusDashExhaust);
            
            _persistentStatusDashOptimal = ApplyStatus(PersistentOptimalTemplate, false);
            _persistentStatusDashOptimal.SetStacksTo(DashMaxOptimalCounter.ModifiedInt);
        }
        
        void ApplyOptimalStatus(int stacks) {
            World.EventSystem.TryDisposeListener(ref _statusListener);
            RemoveStatus(ref _persistentStatusDashOptimal);
            RemoveStatus(ref _statusDashExhaust);
            
            _statusDashOptimal = ApplyStatus(OptimalTemplate, true);
            _statusDashOptimal.IncreaseStacks(stacks);
            _statusListener = _statusDashOptimal.ListenTo(Events.AfterDiscarded, ApplyPersistentOptimalStatus, this);
        }
        
        void ApplyExhaustStatus() {
            World.EventSystem.TryDisposeListener(ref _statusListener);
            RemoveStatus(ref _persistentStatusDashOptimal);
            RemoveStatus(ref _statusDashOptimal);
            
            _statusDashExhaust = ApplyStatus(ExhaustTemplate, true);
            _statusDashExhaust.SetStacksTo(1);
            _statusListener = _statusDashExhaust.ListenTo(Events.AfterDiscarded, ApplyPersistentOptimalStatus, this);
        }

        void RemoveStatus(ref Status status) {
            if (status == null) {
                return;
            }
            
            ParentModel.Statuses.RemoveStatus(status);
            status = null;
        }
        
        Status ApplyStatus(StatusTemplate template, bool applyDuration) {
            var newStatus = ParentModel.Statuses.AddStatus(template, StatusSourceInfo.FromStatus(template).WithCharacter(ParentModel)).newStatus;
            newStatus.MarkedNotSaved = true;
            if (applyDuration) {
                newStatus.AttachDuration(new TimeDuration(DashRegenDuration));
            }
            return newStatus;
        }
    }
}