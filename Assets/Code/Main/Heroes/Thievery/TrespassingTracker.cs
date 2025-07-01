using Awaken.TG.Main.AI.Barks;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Times;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Thievery {
    [SpawnsView(typeof(VTrespassingTracker))]
    public partial class TrespassingTracker : Element<Hero>, ICrimeSource {
        public const float TimeToCrime = 5f;
        public const float TimeToWarningExpiration = 60f;

        public sealed override bool IsNotSaved => true;

        GameRealTime _gameRealTime;
        GameRealTime GameRealTime => _gameRealTime?.HasBeenDiscarded ?? true ? _gameRealTime = World.Only<GameRealTime>() : _gameRealTime;
        
        public bool IsTrespassing { get; private set; }
        public bool IsWarned { get; private set; }
        public bool IsCrime { get; private set; }
        public bool IsTimerStarted => IsWarned && CrimeTimer > 0;
        public bool BountyApplied { get; private set; }

        public float InitialCrimeTimer { get; private set; } = TimeToCrime;
        public float CrimeTimer {
            get => _crimeTimer;
            private set {
                _crimeTimer = value;
                this.Trigger(Events.TimeToCrimeChanged, value);
            }
        }

        float _timeToWarningExpiration;
        ARDateTime _warningExpirationDate;
        float _crimeTimer;

        Vector3 ICrimeSource.Position => ParentModel.Coords;

        public CrimeOwnerTemplate DefaultOwner => null;
        public Faction Faction => null;

        public bool IsNoCrime(in CrimeArchetype archetype) => false;
        public ref readonly CrimeArchetype OverrideArchetype(in CrimeArchetype archetype) => ref archetype;
        public float GetBountyMultiplierFor(in CrimeArchetype archetype) => 1;

        EventReference _audioTrespassingStart = CommonReferences.Get.AudioConfig.TrespassingWarning;
        EventReference _audioTrespassingDetection = CommonReferences.Get.AudioConfig.TrespassingDetection;

        public new static class Events {
            public static readonly Event<TrespassingTracker, bool> TrespassingVolumeChanged = new(nameof(TrespassingVolumeChanged));
            public static readonly Event<TrespassingTracker, bool> TrespassingStateChanged = new(nameof(TrespassingStateChanged));
            public static readonly Event<TrespassingTracker, float> TimeToCrimeChanged = new(nameof(TimeToCrimeChanged));
            public static readonly Event<TrespassingTracker, bool> CrimeStateChanged = new(nameof(CrimeStateChanged));
        }

        protected override void OnInitialize() {
            _timeToWarningExpiration = TimeToWarningExpiration * GameRealTime.WeatherSecondsPerRealSecond;
        }

        protected override void OnFullyInitialized() {
            ParentModel.GetOrCreateTimeDependent().WithUpdate(OnUpdate);
            this.ListenTo(Events.TrespassingVolumeChanged, OnTrespassingVolumeChanged, this);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel.GetTimeDependent()?.WithoutUpdate(OnUpdate);
        }

        void OnTrespassingVolumeChanged(bool state) {
            IsTrespassing = state;
            if (state) {
                if (IsCrime || !IsWarned || !(CrimeTimer <= 0)) {
                    FMODManager.PlayOneShot(_audioTrespassingStart);
                }

                ParentModel.Element<IllegalActionTracker>().StartTrespassing(this);
            } else {
                IsCrime = false;
            }
            this.Trigger(Events.TrespassingStateChanged, state);
        }

        void OnUpdate(float deltaTime) {
            if (IsTrespassing) {
                if (IsWarned && CrimeTimer > 0) {
                    CrimeTimer -= deltaTime;
                }

                if (!IsCrime && IsWarned && CrimeTimer <= 0) {
                    if (!BountyApplied) {
                        BountyApplied = true;
                        CommitCrime.Trespassing(this);
                    }
                    IsCrime = true;
                    this.Trigger(Events.CrimeStateChanged, true);
                    FMODManager.PlayOneShot(_audioTrespassingDetection);
                }
            } else {
                if (IsWarned && GameRealTime.WeatherTime >= _warningExpirationDate) {
                    IsWarned = false;
                    BountyApplied = false;
                    CrimeTimer = TimeToCrime;
                    this.Trigger(Events.CrimeStateChanged, true);
                }
            }
        }

        public void Warn(NpcElement npc) {
            npc.TryGetElement<BarkElement>()?.OnTrespasserSpotted();
            _warningExpirationDate = GameRealTime.WeatherTime.IncrementSeconds(_timeToWarningExpiration);
            if (!IsWarned) {
                IsWarned = true;
            }
            this.Trigger(Events.CrimeStateChanged, true);
        }
        
        public void ResetWarning() {
            IsWarned = false;
            BountyApplied = false;
            CrimeTimer = TimeToCrime;
            this.Trigger(Events.CrimeStateChanged, true);
        }

        public void ResetTimeToCrime(float? timeToCrime) {
            IsCrime = false;
            this.Trigger(Events.CrimeStateChanged, true);
            BountyApplied = false;
            InitialCrimeTimer = timeToCrime ?? TimeToCrime;
            CrimeTimer = InitialCrimeTimer;
        }

        public void IgnoreTimeToCrime() {
            IsWarned = true;
            CrimeTimer = 0;
        }
    }
}