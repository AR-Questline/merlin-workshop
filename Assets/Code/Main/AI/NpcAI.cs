using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.AI.Graphs;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Maps.Compasses;
using Awaken.TG.Main.Maps.Markers;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Unity.IL2CPP.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using IState = Awaken.TG.Main.Utility.StateMachines.IState;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.AI {
    [Il2CppEagerStaticClassConstruction]
    public partial class NpcAI : Element<NpcElement>, IAIEntity, IGrounded {
        public static readonly List<NpcAI> AllWorkingAI = new(32);
        const float MaxDistanceToTargetSqr = 6 * 6;
        const float GetHitNotifyDelay = 0.8f;

        public sealed override bool IsNotSaved => true;

        bool _heroVisible, _wantToExitCombat, _isExitingCombat, _isEnteringCombat;
        float _timeWhenHeroLost = -999;
        float _lostViewOnTarget;
        float _heroVisibility;
        bool _isHeroSummon;
        VisionDetectionSetup[] _visionDetectionSetups;
        VisionDetectionType _visionDetectionType = VisionDetectionType.None;
        StatTweak _movementSpeedTweak;

        // === References
        public GameObject MachineGameObject { get; }

        public NpcData Data { get; private set; }
        public NpcBehaviour Behaviour { get; private set; }
        
        // == Variables
        
        public TargetRange TargetRange { get; set; }
        
        public bool Working { get; set; }
        public bool InCombat { get; private set; }
        public bool InAlert { get; set; }
        public bool InAlertWithWeapons { get; set; } = true;
        public bool InCrimeReaction { [UnityEngine.Scripting.Preserve] get; set; }
        public bool InIdle { get; set; }
        public bool InFlee { get; set; }
        public bool InReturningToSpawn { get; set; }
        public bool IsRunningToSpawn { get; set; }
        public bool InWyrdConversion { get; set; }
        public bool InSpawn { get; set; }
        public bool IsOrWillBeInNonPacifistState => InCombat || InAlert || AlertValue > 0 || InFlee || InWyrdConversion;

        public float HeroVisibility {
            get => _heroVisibility;
            set {
                var toSet = Mathf.Clamp01(value);
                if (Mathf.Approximately(_heroVisibility, toSet)) {
                    return;
                }

                UpdateHeroVisibility(toSet);
            }
        }

        public bool CanLoseTargetBasedOnVisibility { get; set; }
        public bool ObserveAlertTarget { get; set; }
        public bool PerceptionUpdateEnabled { get; private set; } = true;

        // -- IEntityInAIWorld
        public IWithFaction WithFaction => ParentModel;
        public Vector3 VisionDetectionOrigin => ParentModel.Head.position;
        public VisionDetectionSetup[] VisionDetectionSetups {
            get {
                switch (_visionDetectionType) {
                    case VisionDetectionType.HeadAndTorso:
                        _visionDetectionSetups![0] = new(ParentModel.Head.position, 0, VisionDetectionTargetType.Main);
                        _visionDetectionSetups![1] = new(ParentModel.Torso.position, 0, VisionDetectionTargetType.Additional);
                        break;
                    case VisionDetectionType.Head:
                        _visionDetectionSetups![0] = new(ParentModel.Head.position, 0, VisionDetectionTargetType.Main);
                        break;
                    case VisionDetectionType.Torso:
                        _visionDetectionSetups![0] = new(ParentModel.Torso.position, 0, VisionDetectionTargetType.Main);
                        break;
                    case VisionDetectionType.None:
                    default:
                        break;
                }
                return _visionDetectionSetups;
            }
        }
        
        // === Getters
        public NpcElement NpcElement => ParentModel;
        [UnityEngine.Scripting.Preserve] public Location Location => NpcElement.ParentModel;
        [UnityEngine.Scripting.Preserve] public VariableDeclarations SceneVariables => Variables.Scene(MachineGameObject);
        [UnityEngine.Scripting.Preserve] public VariableDeclarations ObjectVariables => Variables.Object(MachineGameObject);
        public AlertStack AlertStack { get; }
        
        public float AlertValue => AlertStack.AlertValue;
        public Vector3 AlertTarget => AlertStack.CurrentTarget;

        public Vector3 Coords => ParentModel.Coords;
        public Quaternion Rotation => ParentModel.Rotation;
        public float SqrDistanceToLastIdlePoint { [UnityEngine.Scripting.Preserve] get; private set; }
        public float SqrDistanceToOutOfCombatPoint { get; private set; }

        public bool HeroVisible {
            get => _heroVisible || Time.time < _timeWhenHeroLost + this.LoseTargetDelayByDistanceToLastIdlePoint();
            set {
                if (_heroVisible == value) {
                    return;
                }
                if (_heroVisible) {
                    _timeWhenHeroLost = Time.time;
                }
                _heroVisible = value;
            }
        }

        public float MaxHeroVisibilityGain { get; set; }

        [UnityEngine.Scripting.Preserve] public bool CanTriggerAggroMusic => Working && ParentModel.CanTriggerAggroMusic;
        bool CanEnterCombat(bool forceCombat) => Working && ParentModel.CanEnterCombat(forceCombat);

        // === Initialization

        public NpcAI(GameObject machineGameObject) {
            MachineGameObject = machineGameObject;
            AlertStack = AddElement<AlertStack>();
        }

        protected override void OnInitialize() {
            SetupVisionDetectionArray();
        }

        protected override void OnFullyInitialized() {
            _isHeroSummon = ParentModel.HasElement<NpcHeroSummon>();
            Behaviour = new NpcBehaviour(this);
            var compassMarker = ParentModel.ParentModel.TryGetElement<LocationMarker>()?.CompassElement as NpcCompassMarker;
            compassMarker?.RegisterAI(this);

            try {
                Data = ParentModel.Template.Data ?? throw new Exception("Missing NpcData in template");
                Behaviour.Init();
                Behaviour.Enter();
                ParentModel.GetOrCreateTimeDependent().WithUpdate(Update);
            } catch (Exception e) {
                Log.Important?.Error("Exception below happened on initialization of npc state machine. It will prevent it from working at all.", ParentModel.Template);
                Debug.LogException(e, ParentModel.ParentModel.ViewParent);
            }

            ParentModel.HealthElement.ListenTo(HealthElement.Events.OnDamageTaken, OnDamageTaken, this);

            ParentModel.Trigger(Events.AIInitialized, this);

            _movementSpeedTweak = StatTweak.Multi(ParentModel.CharacterStats.MovementSpeedMultiplier, 1, parentModel: this);
            
            CanLoseTargetBasedOnVisibility = ParentModel.ParentModel.TryGetElement<EnemyBaseClass>()?.CanLoseTargetBasedOnVisibility ?? true;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel.GetTimeDependent()?.WithoutUpdate(Update);
            Behaviour.Exit();
        }

        public float GetTargetRange(RangeBetween between) {
            return TargetRange.GetRange(between, ParentModel);
        }
        
        // == Events
        public new static class Events {
            public static readonly Event<ICharacter, NpcAI> AIInitialized = new(nameof(AIInitialized));
            public static readonly Event<NpcElement, Change<IState>> NpcStateChanged = new(nameof(NpcStateChanged));
            public static readonly Event<NpcAI, NpcAI> HeroVisibilityChanged = new(nameof(HeroVisibilityChanged));
            public static readonly Event<NpcAI, bool> HeroSeenChanged = new(nameof(HeroSeenChanged));
        }

        // === Update
        void Update(float deltaTime) {
            UpdateDistanceToLastIdlePoint();

            if (!_isHeroSummon) {
                Behaviour.Update(deltaTime);
            }
            UpdateMovementSpeedTweak();
        }

        void UpdateMovementSpeedTweak() {
            var requiredModifier = this.MovementModifierByDistanceToLastIdlePoint();
            if (!Mathf.Approximately(requiredModifier, _movementSpeedTweak.Modifier)) {
                _movementSpeedTweak.SetModifier(requiredModifier);
            }
        }

        // === Actions
        public void FoundTarget() {
            _wantToExitCombat = false;
            var target = ParentModel.GetCurrentTarget();
            if (target == null) {
                return;
            }
            AlertStack.NewPoi(AlertStack.AlertStrength.Strong, target);
            if (target is Hero) {
                HeroVisibility = 1;
            }
        }

        public void UpdateHeroVisibility(float toSet, bool force = false) {
            // if value changes to or from 1, trigger event
            bool newValueIsVisible = Mathf.Approximately(1, toSet);
            bool visibleChanged = newValueIsVisible != Mathf.Approximately(1, _heroVisibility);
            
            _heroVisibility = toSet;
            this.Trigger(Events.HeroVisibilityChanged, this);
            if (visibleChanged) {
                this.Trigger(Events.HeroSeenChanged, newValueIsVisible);
            }

            if (force) {
                _heroVisible = newValueIsVisible;
                _timeWhenHeroLost = -1000;
            }

            TryEnterCombatWithHero();
        }

        public void TryEnterCombatWithHero() {
            if (!InCombat && Mathf.Approximately(_heroVisibility, 1) && AlertValue >= StateAlert.Alert2Combat) {
                EnterCombatWith(Hero.Current);
            }
        }
        
        public void EnterCombatWith(ICharacter target, bool forceChange = false) {
            if (NpcElement.IsUnconscious || target is NpcElement { IsUnconscious: true }) {
                return;
            }
            
            if (InReturningToSpawn) {
                return;
            }

            if (_isEnteringCombat) {
                return;
            }
            
            bool isInCombat = InCombat && !forceChange;
            if (!CanEnterCombat(forceChange) || isInCombat || target == NpcElement) {
                return;
            }

            if (target is Hero h && (_isHeroSummon || (!ParentModel.IsHostileTo(h) && ParentModel.Faction == h.Faction))) {
                return;
            }
            
            _isEnteringCombat = true;
            if (target != null) {
                AlertStack.NewPoi(AlertStack.AlertStrength.Max, target);
                if (target is Hero) {
                    _heroVisible = true;
                    HeroVisibility = 1;
                }
                if (!NpcElement.ForceAddCombatTarget(target, forceChange)) {
                    _wantToExitCombat = false;
                    _isEnteringCombat = false;
                    return;
                }
            } else {
                NpcElement.RecalculateTarget(true);
                if (NpcElement.GetCurrentTarget() == null) {
                    _wantToExitCombat = false;
                    _isEnteringCombat = false;
                    return;
                }
            }

            ParentModel.Trigger(ICharacter.Events.CombatEntered, NpcElement);
            InCombat = true;
            _wantToExitCombat = false;
            _isEnteringCombat = false;
        }

        public void ExitCombat(bool force = false, bool exitToIdle = false, bool canBeVictorious = true) {
            if (!InCombat || _isExitingCombat) return;

            var currentTarget = ParentModel.GetOrSearchForTarget();

            if (force) {
                _isExitingCombat = true;
                ParentModel.ForceEndCombat();
            } else {
                if (!_wantToExitCombat) {
                    _lostViewOnTarget = Time.time;
                    _wantToExitCombat = true;
                }

                if (currentTarget != null && NpcElement.WantToFight(currentTarget) && (
                        // --- Time (We just lost our target from view - chase it till TargetLoseDelay has passed)
                        _lostViewOnTarget + this.LoseTargetDelayByDistanceToLastIdlePoint() > Time.time
                        // --- Or Distance (We are close enough to our target - be in combat with it even though we don't see it)
                        || (currentTarget.Coords - ParentModel.Coords).sqrMagnitude < MaxDistanceToTargetSqr)) {
                    return;
                }
                
                var hookResult = ICharacter.Events.TryingToExitCombat.RunHooks(ParentModel, NpcElement);
                if (hookResult.Prevented) {
                    return;
                }

                _isExitingCombat = true;
                if (currentTarget != null) {
                    ParentModel.RemoveCombatTarget(currentTarget);
                }
            }
            
            if (currentTarget == null || !currentTarget.IsAlive || currentTarget.HasBeenDiscarded) {
                if (canBeVictorious) {
                    ParentModel.Trigger(ICharacter.Events.CombatVictory, NpcElement);
                    AlertStack.Reset();
                    HeroVisibility = 0;
                    exitToIdle = false;
                } else {
                    ParentModel.Trigger(ICharacter.Events.CombatDisengagement, NpcElement);
                }
            }

            _wantToExitCombat = false;

            if (exitToIdle) {
                AlertStack.Reset();
                HeroVisibility = 0;
                ParentModel.Trigger(ICharacter.Events.ForceEnterStateIdle, ParentModel);
            }
            ParentModel.Trigger(ICharacter.Events.CombatExited, NpcElement);
            InCombat = false;
            _isExitingCombat = false;
        }

        public void UpdateDistanceToLastIdlePoint() {
            Vector3 lastIdlePoint = ParentModel.LastIdlePosition;
            SqrDistanceToLastIdlePoint = (Coords - lastIdlePoint).sqrMagnitude;

            Vector3 lastOutOfCombatPoint = ParentModel.LastOutOfCombatPosition;
            SqrDistanceToOutOfCombatPoint = (Coords - lastOutOfCombatPoint).sqrMagnitude;
        }

        public void ReceiveHostileAction(ICharacter attacker, Item item, DamageType damageSource) {
            OnHostileAction(attacker, item, damageSource);
        }

        public void SetActivePerceptionUpdate(bool active) {
            PerceptionUpdateEnabled = active;
        }
        
        // === Damage responses
        void OnDamageTaken(DamageOutcome damageOutcome) {
            var damage = damageOutcome.Damage;
            var attacker = damage.DamageDealer;
            if (attacker == NpcElement) {
                // Can't perform hostile action towards themself
                return;
            }
            var item = damage.Item;
            var damageSource = damage.Type;
            OnHostileAction(attacker, item, damageSource);
        }

        void OnHostileAction(ICharacter attacker, Item item, DamageType damageSource) {
            if (attacker != null) {
                var currentTarget = NpcElement.GetCurrentTarget();

                if (currentTarget != attacker) {
                    ApplyAntagonism(attacker);
                }

                if (InCombat) {
                    OnDamageTakenInFight(attacker);
                } else if (CrimeReactionUtils.IsFleeing(NpcElement)) {
                    NpcElement.Trigger(NpcDangerTracker.Events.CharacterDangerNearby, new NpcDangerTracker.DirectDangerData(NpcElement, attacker));
                } else {
                    OnDamageTakenOutsideFight(attacker, item, damageSource);
                }
            }
            MakeGetHitNoise(attacker).Forget();
            this.TryNotifyAboutOngoingFight(attacker).Forget();
        }

        void OnDamageTakenInFight(ICharacter attacker) {
            var currentTarget = NpcElement.GetCurrentTarget();

            if (currentTarget == attacker) {
                return;
            }
            
            bool heroSummonTargetCondition = currentTarget == null || !currentTarget.HasElement<NpcHeroSummon>();
            if (heroSummonTargetCondition && (attacker?.HasElement<NpcHeroSummon>() ?? false)) {
                EnterCombatWith(attacker, true);
                return;
            }
            
            bool heroTargetCondition = currentTarget is not Hero && heroSummonTargetCondition;
            if (heroTargetCondition && attacker is Hero) {
                EnterCombatWith(attacker, true);
                return;
            }

            // Change Target if damage dealer has bigger Fit.
            // It's harder to do if our current target is also attacking us.
            float fitDifference = (currentTarget is NpcElement { HasBeenDiscarded: false } npc && npc.GetCurrentTarget() == ParentModel)
                ? AITargetingUtils.NormalFitDifference
                : AITargetingUtils.TakenDamageFitDifference;
            bool shouldChangeTarget = currentTarget == null || 
                                           ParentModel.IsBetterFitThanTarget(currentTarget, attacker, fitDifference);
            if (shouldChangeTarget) {
                EnterCombatWith(attacker, true);
                return;
            }

            ParentModel.TryAddPossibleCombatTarget(attacker);
        }

        void OnDamageTakenOutsideFight(ICharacter attacker, Item item, DamageType damageSource) {
            if (item != null) {
                if (item.IsRanged) {
                    OnRangedDamage(attacker);
                } else if (item.IsMelee) {
                    OnMeleeDamage(attacker);
                } else if (item.IsMagic) {
                    OnSpellDamage(attacker);
                } else {
                    OnDamageTakenFromSource(damageSource, attacker); 
                }
            } else {
                OnDamageTakenFromSource(damageSource, attacker); 
            }
        }

        void OnDamageTakenFromSource(DamageType damageSource, ICharacter attacker) {
            switch (damageSource) {
                case DamageType.PhysicalHitSource:
                    OnPhysicalDamage(attacker);
                    break;
                case DamageType.MagicalHitSource:
                    OnMagicalDamage(attacker);
                    break;
                case DamageType.Trap:
                    OnTrapDamage(attacker);
                    break;
                case DamageType.Environment:
                    OnEnvironmentDamage(attacker);
                    break;
            }
        }

        void OnRangedDamage(ICharacter attacker) {
            LookForDamageDealer(attacker);
        }

        void OnMeleeDamage(ICharacter attacker) {
            LookForDamageDealer(attacker);
            if (attacker != null) {
                EnterCombatWith(attacker);
            }
        }
        
        void OnSpellDamage(ICharacter attacker) {
            LookForDamageDealer(attacker);
        }

        void OnPhysicalDamage(ICharacter attacker) {
            LookForDamageDealer(attacker);
        }
        
        void OnMagicalDamage(ICharacter attacker) {
            LookForDamageDealer(attacker);
        }

        void OnTrapDamage(ICharacter attacker) {
            LookForDamageDealer(attacker);
        }

        void OnEnvironmentDamage(ICharacter attacker) {
            LookForDamageDealer(attacker, false);
        }
        
        void LookForDamageDealer(ICharacter attacker, bool applyAntagonism = true) {
            if (attacker == null || NpcElement.IsUnconscious) {
                return;
            }

            if (applyAntagonism) {
                ApplyAntagonism(attacker);
            }

            AlertStack.NewPoi(AlertStack.AlertStrength.Strong, attacker);
        }

        void ApplyAntagonism(IWithFaction damageDealer) {
            if (damageDealer.Faction != ParentModel.Faction) {
                AntagonismMarker.TryApplySingleton(
                    new FactionAntagonism(AntagonismLayer.Default, AntagonismType.To, damageDealer.Faction, Antagonism.Hostile),
                    new UntilIdle(NpcElement),
                    ParentModel
                );
            }
        }

        async UniTaskVoid MakeGetHitNoise(ICharacter attacker) {
            // Wait a bit to allow "sneaky" hits/kills
            if (!await AsyncUtil.DelayTime(this, GetHitNotifyDelay)) {
                return;
            }

            if (!ParentModel.IsAlive) {
                return;
            }

            float maxInformRange = NpcElement.NpcAI.Data.perception.MaxInformRange;
            var hearingNpcs = Services.Get<NpcGrid>().GetHearingNpcs(Coords, maxInformRange);
            // 
            var poiPosition = AINoises.GetPosition(attacker, this);
            foreach (var npc in hearingNpcs) {
                if (npc != NpcElement && npc.NpcAI.Working && npc.IsFriendlyTo(NpcElement)) {
                    AINoises.MakeNoise(maxInformRange, NoiseStrength.VeryStrong, false, Coords, npc.NpcAI, poiPosition);
                }
            }
        }
        
        // === Helpers
        void SetupVisionDetectionArray() {
            int size = 0;
            if (ParentModel.Head == null) {
                Log.Critical?.Error($"Npc incorrectly configured: no head. {LogUtils.GetDebugName(ParentModel)}", ParentModel.ParentModel.ViewParent.gameObject);
            } else {
                _visionDetectionType = VisionDetectionType.Head;
                size++;
            }

            if (ParentModel.Torso == null) {
                Log.Critical?.Error($"Npc incorrectly configured: no torso. {LogUtils.GetDebugName(ParentModel)}", ParentModel.ParentModel.ViewParent.gameObject);
            } else {
                _visionDetectionType = size == 0 ? VisionDetectionType.Torso : _visionDetectionType | VisionDetectionType.Torso;
                size++;
            }
            _visionDetectionSetups = new VisionDetectionSetup[size];
        }
        
        [Flags]
        enum VisionDetectionType {
            None = 1 << 0,
            Head = 1 << 1,
            Torso = 1 << 2,
            HeadAndTorso = Head | Torso
        }
    }
}
