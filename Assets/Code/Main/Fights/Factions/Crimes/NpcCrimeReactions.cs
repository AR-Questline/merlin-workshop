using System.Threading;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.Barks;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.AI.States.Flee;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.Main.Utility.StateMachines;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Cysharp.Threading.Tasks;
using Awaken.Utility.Maths;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Factions.Crimes {
    public partial class NpcCrimeReactions : Element<NpcElement> {
        const float ObservingTime = 4;
        const float LoseHeroSightDelay = 10f;
        const float InstantLoseHeroSightDistanceSqr = 7f * 7f;
        const float LockpickForgiveness = 2;

        public sealed override bool IsNotSaved => true;
        
        bool _isSeeingHero;
        float? _seeingHeroLoseSightTime;
        float _observingTime;
        float _pickpocketAlert;
        float _timeToLockpickCrime;
        float _timeToNextGuardCall;
        
        float _minProficiencyAlertMultiplier;
        float _maxProficiencyAlertMultiplier;
        float _pickpocketAlertLose;

        CancellationTokenSource _pickpocketingEndedDelay;

        bool _isBeingPickpocketed;
        bool _wasPickpocketed;
        
        public bool IsGuard => CrimeReactionUtils.IsGuard(ParentModel);
        bool IsDefender => CrimeReactionUtils.IsDefender(ParentModel);
        bool IsVigilante => CrimeReactionUtils.IsVigilante(ParentModel);
        
        public bool HasBeenPickpocketed => _wasPickpocketed || _isBeingPickpocketed;
        public bool IsSeeingHero => _isSeeingHero;
        public bool IsLosingHeroSight => _isSeeingHero && _seeingHeroLoseSightTime.HasValue;
        public bool IsObservingHero => _observingTime > 0;
        public float ObserveTime => _observingTime;
        public float PickpocketAlert => _pickpocketAlert;
        public float TimeToLockpickCrime => _timeToLockpickCrime;
        public float SeeingHeroLoseFactor => _seeingHeroLoseSightTime.HasValue ? (_seeingHeroLoseSightTime.Value - Time.time) / LoseHeroSightDelay : 0f;

        public new static class Events {
            public static readonly Event<Location, bool> ObservingStateChanged = new(nameof(ObservingStateChanged));
            public static readonly Event<Location, float> PickpocketAlertChange = new(nameof(PickpocketAlertChange));
        }
        protected override void OnInitialize() {
            ParentModel.GetOrCreateTimeDependent().WithUpdate(OnUpdate);
            GameConstants gameConstants = World.Services.Get<GameConstants>();
            _minProficiencyAlertMultiplier = gameConstants.minProficiencyAlertMultiplier;
            _maxProficiencyAlertMultiplier = gameConstants.maxProficiencyAlertMultiplier;
            _pickpocketAlertLose = gameConstants.pickpocketAlertLose;
            
            ParentModel.ListenTo(NpcAI.Events.NpcStateChanged, OnStateChanged, this);
        }

        void OnStateChanged(Change<IState> change) {
            if (change is (StateAIWorking, _)) {
                SetSeeingHero(false, true);
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            SetSeeingHero(false, true);
            ParentModel.GetTimeDependent()?.WithoutUpdate(OnUpdate);
        }

        void OnUpdate(float deltaTime) {
            if (ParentModel.NpcAI == null) {
                return;
            }
            
            if (_observingTime > 0) {
                UpdateObserving();
                _observingTime -= deltaTime;
                if (_observingTime <= 0) {
                    StopObserving();
                }
            }

            if (_pickpocketAlert > 0f) {
                _pickpocketAlert -= _pickpocketAlertLose * deltaTime;
                if (_pickpocketAlert < 0f) {
                    _pickpocketAlert = 0f;
                }
                ParentModel.ParentModel.Trigger(Events.PickpocketAlertChange, _pickpocketAlert);
            }
            
            if (_timeToLockpickCrime > 0f) {
                _timeToLockpickCrime -= deltaTime;
                if (_timeToLockpickCrime < 0f) {
                    _timeToLockpickCrime = 0f;
                    if (_isSeeingHero) {
                        CommitCrime.Lockpicking(ParentModel.ParentModel);
                    }
                }
            }
            
            if (ParentModel.NpcAI.InCombat) {
                if (_timeToNextGuardCall > 0f) {
                    _timeToNextGuardCall -= deltaTime;
                    if (_timeToNextGuardCall <= 0f) {
                        _timeToNextGuardCall = 0f;
                        CrimeArchetype crimeArchetype = CrimeArchetype.Combat(ParentModel.CrimeValue);
                        CrimeReactionUtils.CallGuardsToHero(ParentModel.GetCurrentCrimeOwnersFor(crimeArchetype).PrimaryOwner);
                    }
                }
            } else {
                _timeToNextGuardCall = 0f;
            }
        }
        
        public void SetSeeingHero(bool heroVisible, bool instant = false) {
            if (_isSeeingHero && !heroVisible) {
                if (!instant && ParentModel.Coords.SquaredDistanceTo(Hero.Current.Coords) < InstantLoseHeroSightDistanceSqr) {
                    if (_seeingHeroLoseSightTime.HasValue) {
                        if (_seeingHeroLoseSightTime.Value > Time.time) {
                            return;
                        }
                    } else {
                        _seeingHeroLoseSightTime = Time.time + LoseHeroSightDelay;
                        return;
                    }
                }
                _isSeeingHero = false;
                _seeingHeroLoseSightTime = null;
                Hero.Current.Element<IllegalActionTracker>().RemoveWatchingNpc(this);
                return;
            }

            if (heroVisible) {
                _seeingHeroLoseSightTime = null;
                if (!_isSeeingHero) {
                    _isSeeingHero = true;
                    // Hostile Factions use Alert System not Crime System.
                    // There's a need to use Faction to Faction check to ignore temporary antagonism.
                    if (!ParentModel.Faction.IsHostileTo(Hero.Current.Faction)) {
                        if (ParentModel.GetCurrentCrimeOwnersFor(CrimeArchetype.Theft(CrimeItemValue.High)) is {IsEmpty: false} 
                            || ParentModel.GetCurrentCrimeOwnersFor(CrimeArchetype.Pickpocketing(CrimeItemValue.High, ParentModel.CrimeValue)) is {IsEmpty: false} 
                            || ParentModel.GetCurrentCrimeOwnersFor(CrimeArchetype.Trespassing) is {IsEmpty: false}) {
                            Hero.Current.Element<IllegalActionTracker>().AddWatchingNpc(this);
                        }
                    }
                }
            }
        }
        
        public void ObserveSuspiciousActivity() {
            if (_observingTime <= 0) {
                StartObserving();
            }
            _observingTime = ObservingTime;
        }
        
        public void StartedLockpicking() {
            _timeToLockpickCrime = LockpickForgiveness;
        }
        
        public void StoppedLockpicking() {
            _timeToLockpickCrime = 0f;
        }

        public void ReactToNoise(Vector3 noisePosition, float noiseRange, float noiseStrength) {
            ObserveSuspiciousActivity();
        }

        void StartObserving() {
            ParentModel.TryGetElement<BarkElement>()?.OnNPCNoticedCrimeAttempt();
            ParentModel.ParentModel.Trigger(Events.ObservingStateChanged, true);
        }

        void UpdateObserving() { }

        void StopObserving() {
            ParentModel.ParentModel.Trigger(Events.ObservingStateChanged, false);
        }

        public void ReactToCrime(in CrimeArchetype archetype) {
            if (HasBeenDiscarded || ParentModel is not { HasBeenDiscarded: false, NpcAI: { AlertStack: not null } }) {
                // Multiple enemies killed in a single frame can cause this
                return;
            }
            
            Log.Debug?.Info("[Thievery] " + LogUtils.GetDebugName(ParentModel) + ": reactsToCrime '" + archetype.SimpleCrimeType.ToStringFast() + "'");
            
            ParentModel.Trigger(IllegalActionTracker.Events.NPCNoticedCrime, archetype);
            
            if (IsGuard) {
                ParentModel.TryGetElement<BarkElement>()?.OnGuardNoticedCrime(archetype);
                // will enter combat due to bounty/temp bounty
            } else {
                AntagonismMarker.TryApplySingleton(
                    new CharacterAntagonism(AntagonismLayer.Default, AntagonismType.Mutual, ParentModel, Antagonism.Hostile),
                    new UntilIdle(ParentModel),
                    Hero.Current
                );
                if (IsDefender) {
                    ParentModel.TryGetElement<BarkElement>()?.OnDefenderNoticedCrime(archetype);
                } else if (IsVigilante) {
                    ParentModel.TryGetElement<BarkElement>()?.OnVigilanteNoticedCrime(archetype);
                } else {
                    ParentModel.TryGetElement<BarkElement>()?.OnPeasantNoticedCrime(archetype);
                    ParentModel.DangerTracker?.OnPeasantNoticedCrime(archetype);
                }
                
                ParentModel.NpcAI.EnterCombatWith(Hero.Current, true);
            }

            ParentModel.NpcAI.AlertStack.NewPoi((int) AlertStack.AlertStrength.Max * 2, Hero.Current);
            CrimeReactionUtils.CallGuardsToHero(ParentModel.GetCurrentCrimeOwnersFor(archetype).PrimaryOwner);
            _timeToNextGuardCall = ObservingTime;
        }
        
        public float Pickpocketing(float deltaTime, Item item) {
            _pickpocketingEndedDelay?.Cancel();
            _isBeingPickpocketed = true;
            var crime = Crime.Pickpocket(item, ParentModel);
            
            var pickpocketAlert = IncreasePickpocketAlert(crime, deltaTime);
            if (pickpocketAlert >= 1f) {
                if (ShouldResetPickpocketAlert()) {
                    pickpocketAlert = ResetPickpocketAlert();
                } else {
                    CommitCrime.Pickpocket(item, ParentModel);
                }
            }
            return pickpocketAlert;
        }

        public async UniTaskVoid PickpocketingEnded() {
            _isBeingPickpocketed = false;
            _wasPickpocketed = true;
            
            _pickpocketingEndedDelay?.Cancel();
            _pickpocketingEndedDelay = new CancellationTokenSource();
            
            // delay required for Ai state update
            if (await AsyncUtil.DelayTime(ParentModel, 2, _pickpocketingEndedDelay.Token)) {
                _wasPickpocketed = false;
                _pickpocketingEndedDelay = null;
            }
        }

        public bool ShouldReactToBeingPickpocketed(in Crime pickpocket) {
            var template = ParentModel.GetCurrentCrimeOwnersFor(pickpocket.Archetype).PrimaryOwner;
            if (!CrimeUtils.IsCrimeFor(pickpocket, template)) {
                return false;
            }
            return _pickpocketAlert >= 1f;
        }

        float IncreasePickpocketAlert(in Crime pickpocket, in float deltaTime) {
            var template = ParentModel.GetCurrentCrimeOwnersFor(pickpocket.Archetype).PrimaryOwner;
            if (!CrimeUtils.IsCrimeFor(pickpocket, template)) {
                return _pickpocketAlert;
            }
            var archetype = pickpocket.Archetype;
            var proficiencyBoost = template.NpcBounty(archetype.NpcValue).pickpocketProficiencyBoost;
            float proficiencyMultiplier = (Hero.Current.ProficiencyStats.Theft.ModifiedValue + proficiencyBoost).Remap(0, 100, _minProficiencyAlertMultiplier, _maxProficiencyAlertMultiplier, true);
            _pickpocketAlert += deltaTime * proficiencyMultiplier;
            var alert = _pickpocketAlert;
            
            float loseNullifierBonus = deltaTime * _pickpocketAlertLose;
            _pickpocketAlert += loseNullifierBonus;
            
            return alert;
        }

        bool ShouldResetPickpocketAlert() {
            return RandomUtil.WithProbability(Hero.Current.Stat(HeroStatType.PickpocketRecoveryChance));
        }

        float ResetPickpocketAlert() {
            _pickpocketAlert = 0f;
            return _pickpocketAlert;
        }
    }
}