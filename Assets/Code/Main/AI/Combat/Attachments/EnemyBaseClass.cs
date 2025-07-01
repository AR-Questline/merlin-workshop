#if DEBUG && !NPC_LOGIC_DEBUGGING
#define NPC_LOGIC_DEBUGGING
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours;
using Awaken.TG.Main.AI.Combat.Behaviours.CustomBehaviours;
using Awaken.TG.Main.AI.Combat.Behaviours.MagicBehaviours;
using Awaken.TG.Main.AI.Idle;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.Combat;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Providers;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.Main.Utility.Animations.FightingStyles;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.IL2CPP.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UniversalProfiling;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.AI.Combat.Attachments {
    [Il2CppEagerStaticClassConstruction]
    public abstract partial class EnemyBaseClass : Element<Location>, IBehavioursOwner, ICanMoveProvider {
        const float ReleaseCombatSlotAtDistance = 15f;
        const float EnterCombatNotifyDelay = 0.5f;
        const float SlotsReserveRange = 2.75f;
        const float DecreaseFatigueAtDistance = VHeroCombatSlots.FirstLineCombatSlotOffset + VHeroCombatSlots.CombatSlotOffset;
        const float DecreaseFatigueInterval = 1.5f;
        const float NotifyAboutCombatInterval = 2f;
        const float StaminaPercentThresholdToStagger = 0.3f;
        const int MaxFatigue = 5;

        static readonly List<IBehaviourBase> ReusableBehaviours = new(16);
        protected static StructList<Item> sReusableWeapons = new(4);

        public bool DEBUG_Disable;

        // -- Cache
        [BoxGroup("Combat Settings")] public bool overrideDistanceToReleaseCombatSlotAt;
        [BoxGroup("Combat Settings"), ShowIf(nameof(overrideDistanceToReleaseCombatSlotAt))] public float releaseCombatSlotAtDistance = 15;
        [BoxGroup("Movement Settings")] public bool canBeSlidInto = true;
        NpcElement _cachedNpcElement;
        IEnumerable<AdditionalHandMarker> _cachedAdditionalHandMarkers;
        AnimationAndBehaviourMappingEntry _currentCombatData;
        [Saved] WeakModelRef<AIBlock> _aiBlock;

        public virtual bool UsesCombatMovementAnimations => false;
        public virtual bool UsesAlertMovementAnimations => false;

        public virtual bool CanMove => CurrentBehaviour.Get()?.CanMove ?? true;
        public float DistanceToTarget { get; private set; } = float.MaxValue;

        public bool KeepsInSecondLine {
            get {
                var behaviours = Elements<IBehaviourBase>();
                foreach (var behaviour in behaviours) {
                    if (behaviour is KeepSecondLinePositionBehaviour) {
                        return true;
                    }
                }

                return false;
            }
        }
        public Vector3 Coords => ParentModel.Coords;
        public Vector3 DesiredPosition { get; private set; }
        public NpcElement NpcElement => ParentModel.CachedElement(ref _cachedNpcElement);
        public AnimationAndBehaviourMappingEntry CurrentCombatData {
            get => _currentCombatData;
            private set {
                _currentCombatData = value;
                if (NpcAnimancer != null) {
                    NpcAnimancer.UpdateCurrentCombatData(value, StatsItem).Forget();
                }
            }
        }
        public CharacterStats CharacterStats => NpcElement.CharacterStats;
        public NpcAI NpcAI => NpcElement.NpcAI;
        public NpcMovement NpcMovement => NpcElement?.Movement;
        public ARNpcAnimancer NpcAnimancer { get; private set; }
        public Transform DefaultVFXParent => ParentModel.MainView.transform;
        public bool Staggered => CurrentBehaviour.Get() is StaggerBehaviour || (_shouldEnterStaggerOrRest.HasValue && _shouldEnterStaggerOrRest.Value);
        public bool CanBeStaggered => TryGetElement<StaggerBehaviour>();
        public float StaggerDurationElapsedNormalized => CurrentBehaviour.Get() is StaggerBehaviour stagger ? stagger.DurationElapsedNormalized : 0;
        public bool RequiresCombatSlot => Elements<IBehaviourBase>().Any(static c => c.RequiresCombatSlot);
        public int OwnedCombatSlotIndex { get; internal set; } = -1;
        public Item StatsItem => MainHandItem ?? OffHandItem;
        public Item MainHandItem => NpcElement.Inventory.EquippedItem(EquipmentSlotType.MainHand);
        public virtual Item OffHandItem => NpcElement.Inventory.EquippedItem(EquipmentSlotType.OffHand);
        public WeakModelRef<IBehaviourBase> CurrentBehaviour { get; private set; }
        public bool CanBeAggressive => CurrentBehaviour.Get()?.CanBeAggressive ?? true;
        public float AggressionScore { get; private set; }
        public bool IsBehindHero => AIUtils.IsBehindHero(AIUtils.HeroDotToTarget(Coords));
        public bool IsInHeroFov => !IsBehindHero;
        public virtual bool CanLoseTargetBasedOnVisibility => true;
        protected virtual ModelsSet<IBehaviourBase> Behaviours => Elements<IBehaviourBase>();
        protected abstract bool CanBePushed { get; }
        public bool IsSummoner => Behaviours.Any(static b => b is SummonAllyBehaviour);
        public virtual CrimeReactionArchetype CrimeReactionArchetype => NpcElement.Template.CrimeReactionArchetype;
        public bool IsGuard => CrimeReactionArchetype is CrimeReactionArchetype.Guard;
        public bool IsDefender => CrimeReactionArchetype is CrimeReactionArchetype.Defender;
        public bool IsVigilante => CrimeReactionArchetype is CrimeReactionArchetype.Vigilante;
        public bool IsFleeingPeasant => CrimeReactionArchetype is CrimeReactionArchetype.FleeingPeasant;
        public bool IsAlwaysFleeing => CrimeReactionArchetype is CrimeReactionArchetype.AlwaysFleeing;
        public GenericAttackData GenericAttackData { get; set; }
        protected bool ShouldWeaponsBeEquipped => WeaponsAlwaysEquipped || NpcElement.NpcAI is { InAlert: true, InAlertWithWeapons: true } or { InCombat: true } or { IsRunningToSpawn: true } or { InWyrdConversion: true };
        public bool WeaponsAlwaysEquipped => WeaponsAlwaysEquippedBase || NpcElement.WyrdConverted;
        protected bool WeaponsAlwaysEquippedBase { private get; set; }
        public bool BaseBehavioursLoaded => _baseBehavioursLoaded;
        protected List<EnemyBehaviourBase> TemporaryBehaviours { get; set; } = new();
        protected List<EnemyBehaviourBase> CombatBehaviours { get; set; } = new();
        protected List<ARAssetReference> CombatBehavioursReferences { get; set; } = new();
        RestBehaviour Rest => Element<RestBehaviour>();
        StaggerBehaviour Stagger => Element<StaggerBehaviour>();
        StumbleBehaviour Stumble => Element<StumbleBehaviour>();
        RagdollBehaviour Ragdoll => Element<RagdollBehaviour>();
        UnconsciousBehaviour Unconscious => Element<UnconsciousBehaviour>();
        HeroCombatSlots HeroCombatSlots => Hero.Current.CombatSlots;
        bool CanBeStumbled => HasElement<StumbleBehaviour>();
        bool CanBeRagdolled => HasElement<RagdollBehaviour>();
        LimitedStat Stamina => CharacterStats?.Stamina;
        LimitedStat PoiseThreshold => NpcElement?.NpcStats?.PoiseThreshold;
        LimitedStat ForceStumbleThreshold => NpcElement?.NpcStats?.ForceStumbleThreshold;
        bool IsBlocking => _aiBlock.Exists();
        
        protected bool? _shouldEnterStaggerOrRest;

        IEventListener _targetAttackListener;
        CancellationTokenSource _combatDataCancellationToken;
        ARAssetReference _baseBehavioursAssetReference;
        bool _baseBehavioursLoaded;
        bool _wasInCombatPreviously;
        int _fatigueCounter;
        float _lastFatigueUpdate;
        float _nextNotifyAboutCombatTime;
        bool _visualLoaded;

        // === Events
        [Il2CppEagerStaticClassConstruction]
        public new static class Events {
            public static readonly Event<IItemOwner, bool> BehaviourReset = new(nameof(BehaviourReset));
            public static readonly Event<IItemOwner, bool> AfterWeaponFullyLoaded = new(nameof(AfterWeaponFullyLoaded));
            public static readonly Event<IItemOwner, bool> AttackInterrupted = new(nameof(AttackInterrupted));
            public static readonly Event<NpcElement, NpcElement> Staggered = new(nameof(Staggered));
            public static readonly Event<NpcElement, bool> StaggerAnimExitEnded = new(nameof(StaggerAnimExitEnded));
            public static readonly Event<NpcElement, bool> ParriedAnimEnded = new(nameof(ParriedAnimEnded));
            public static readonly Event<NpcElement, bool> StandUpFinished = new(nameof(StandUpFinished));
            public static readonly Event<EnemyBaseClass, IBehaviourBase> BehaviourStarted = new(nameof(BehaviourStarted));
            public static readonly Event<EnemyBaseClass, EnemyBaseClass> BaseBehavioursLoaded = new(nameof(BaseBehavioursLoaded));
            public static readonly Event<IBehavioursOwner, ARAnimationEvent> AnimationEvent = new(nameof(AnimationEvent));
            public static readonly Event<EnemyBaseClass, bool> ToggleWeakSpot = new(nameof(ToggleWeakSpot));
            public static readonly Event<EnemyBaseClass, bool> TogglePreventDamageState = new(nameof(TogglePreventDamageState));
        }

        // === Initialization
        protected override void OnInitialize() {
            NpcElement.ListenTo(Model.Events.BeforeDiscarded, Discard, this);
            ParentModel.GetOrCreateTimeDependent().WithUpdate(OnUpdate);
            NpcElement.ListenTo(NpcElement.Events.ItemsAddedToInventory, OnItemsAddedToInventory, this);
            NpcElement.ListenTo(Events.BehaviourReset, _ => StopCurrentBehaviour(true), this);
            NpcElement.ListenTo(Stat.Events.ChangingStat(CharacterStatType.Stamina), OnStaminaChanged, this);
            NpcElement.ListenTo(Events.AttackInterrupted, ReleaseCombatSlots, this);
            NpcElement.ListenTo(UnconsciousElement.Events.LoseConscious, EnterUnconscious, this);
            NpcCanMoveHandler.AddCanMoveProvider(NpcElement, this);
            GenericAttackData = GenericAttackData.Default;
        }

        public void OnVisualLoaded(Transform parentTransform) {
            NpcElement.HealthElement.ListenTo(HealthElement.Events.OnDamageTaken, OnDamageTaken, this);
            NpcAnimancer = parentTransform.GetComponentInChildren<ARNpcAnimancer>();
            if (CurrentCombatData != null) {
                NpcAnimancer.UpdateCurrentCombatData(CurrentCombatData, StatsItem).Forget();
            } else {
                CurrentCombatData = NpcElement.FightingStyle.RetrieveCombatData(this);
            }
            LoadBaseBehaviours();
            // --- Add Persistent Behaviours
            AddPersistentBehaviours();
            _cachedAdditionalHandMarkers = parentTransform.GetComponentsInChildren<AdditionalHandMarker>();
            AfterVisualLoaded(parentTransform);
            _visualLoaded = true;
        }
        
        public void OnInventoryInitialized() {
            AfterItemsAddedToInventory();
        }

        public abstract void OnWyrdConversionStarted();
        public virtual void RefreshFightingStyle() {
            foreach (var behaviour in Elements<EnemyBehaviourBase>().Reverse()) {
                if (behaviour is PersistentEnemyBehaviour) {
                    continue;
                }
                behaviour.Discard();
            }

            NpcAnimancer.UpdateFightingStyle(NpcElement.FightingStyle).Forget();
            CurrentCombatData = NpcElement.FightingStyle.RetrieveCombatData(this);
            LoadBaseBehaviours();
            ChangeCombatData(true);
        }
        protected abstract UniTaskVoid ChangeCombatData(bool force = false);
        
        void OnItemsAddedToInventory(NpcElement _) {
            AfterItemsAddedToInventory();
        }

        // === Events
        public void TriggerAnimationEvent(ARAnimationEvent animationEvent) {
            CurrentBehaviour.Get()?.TriggerAnimationEvent(animationEvent);
            
            if (animationEvent.actionType == ARAnimationEvent.ActionType.TeleportIn) {
                NpcElement.Trigger(ICharacter.Events.SwitchCharacterVisibility, false);
            } else if (animationEvent.actionType == ARAnimationEvent.ActionType.TeleportOut) {
                var npcElement = NpcElement;
                npcElement.Trigger(ICharacter.Events.SwitchCharacterVisibility, true);
                Vector3 destination = Coords;
                if (animationEvent.teleportType == ARAnimationEvent.TeleportType.InFrontOfTarget) {
                    var target = npcElement.GetCurrentTarget();
                    if (target == null) {
                        Log.Minor?.Warning($"Trying to teleport out in front of target but it was null {this}");
                        return;
                    }
                    var targetPosition = target.Coords;
                    Vector3 dirFromTarget = destination - targetPosition;
                    destination = targetPosition + dirFromTarget.normalized * DistancesToTargetHandler.DesiredDistanceToTarget(npcElement, target);
                } else if (animationEvent.teleportType == ARAnimationEvent.TeleportType.Dash) {
                    destination = npcElement.Right() * RandomUtil.UniformFloat(3, 6) * RandomUtil.RandomSign();
                }

                NpcTeleporter.Teleport(npcElement, destination, TeleportContext.FromCombat);
            }
        }

        public void TryPlayAttackOutsideFOVWarning() {
            Services.Get<OutsideFoVAttacksService>().TryPlayAttackOutsideFOVWarning(NpcElement);
        }

        protected virtual void OnDamageTaken(DamageOutcome damageOutcome) {
            if (NpcElement.IsDying || Staggered) {
                return;
            }

            Vector3 forceDirection = damageOutcome.Damage.ForceDirection ?? (NpcElement.Forward() * -1);
            if (damageOutcome.Damage.ForceDamage > 0) {
                float forceDamage = damageOutcome.Damage.ForceDamage;
                float ragdollForce = damageOutcome.Damage.RagdollForce;
                
                if (damageOutcome.Damage.WeakSpotHit) {
                    forceDamage *= GameConstants.Get.weakspotForceDamageMultiplier;
                    ragdollForce *= GameConstants.Get.weakspotRagdollForceMultiplier;
                } else if (damageOutcome.Damage.Critical) {
                    forceDamage *= GameConstants.Get.criticalForceDamageMultiplier;
                    ragdollForce *= GameConstants.Get.criticalRagdollForceMultiplier;
                }

                bool isPush = damageOutcome.Damage.IsPush;
                ApplyForce(forceDirection, forceDamage, ragdollForce, isPush);
            }
        }

        void OnStaminaChanged(HookResult<IWithStats, Stat.StatChange> statChangeHook) {
            var statChange = statChangeHook.Value;
            if (statChange.value >= 0) {
                return;
            }

            if (CurrentBehaviour.Get() is StaggerBehaviour) {
                return;
            }
            
            if (statChange.context is {reason: ChangeReason.CombatDamage} && Stamina.ModifiedValue + statChange.value < StaminaPercentThresholdToStagger * Stamina.UpperLimit) {
                _shouldEnterStaggerOrRest = HasElement<StaggerBehaviour>();
            } else if (_shouldEnterStaggerOrRest == null && Stamina.ModifiedValue + statChange.value <= 0) {
                // If character doesn't have stamina regen the rest behaviour is pointless, it needs to regan stamina by falling into stagger
                if (CharacterStats.StaminaRegen.ModifiedValue <= 0) {
                    if (!HasElement<StaggerBehaviour>()) {
                        Log.Important?.Error($"Npc {NpcElement} cannot regen stamina and has no stagger behaviour, it can't regen stamina.", ParentModel.MainView.gameObject);
                        return;
                    }
                    _shouldEnterStaggerOrRest = HasElement<StaggerBehaviour>();
                    return;
                }

                _shouldEnterStaggerOrRest = false;
            }
        }

        // === LifeCycle
        void OnUpdate(float deltaTime) {
#if NPC_LOGIC_DEBUGGING
            if (DEBUG_Disable || NpcElement.DEBUG_DoNotSpawnAI) {
                return;
            }
#endif
            var npcElement = NpcElement;
            var npcAI = npcElement.NpcAI;
            if (!_visualLoaded || npcAI.InWyrdConversion || npcAI.InSpawn) {
                return;
            }
            
            // Allow stamina regen if current behavior permits it
            if (CurrentBehaviour.Get()?.AllowStaminaRegen ?? true) {
                RegenStamina(deltaTime);
            }
            
            // Check whether NPC is in combat
            bool isInCombat = npcAI.InCombat;
            UpdateCombatStatus(isInCombat);

            // Verify Combat Slot
            if (!isInCombat) {
                CurrentBehaviour.Get()?.NotInCombatUpdate(deltaTime);
                // Perform update logic for when NPC is not in combat
                NotInCombatUpdate(deltaTime);
                return;
            }

            var target = npcElement.GetCurrentTarget();
            UpdateCombatSlotStatus(target);

            UpdateDistanceToTarget(deltaTime, target);
            
            if (DistanceToTarget >= DecreaseFatigueAtDistance) {
                _lastFatigueUpdate += deltaTime;
                if (_lastFatigueUpdate >= DecreaseFatigueInterval) {
                    DecreaseFatigueInternal(true);
                }
            }

            if (target is not Hero) {
                CombatUpdate(deltaTime);
            }
        }

        public void CombatUpdate(float? deltaTime) {
            var npcElement = NpcElement;
            var npcAI = npcElement.NpcAI;
            if (npcAI.InWyrdConversion | npcAI.InSpawn) {
                return;
            }
            
            deltaTime ??= this.GetDeltaTime();

            Tick(deltaTime.Value, npcElement);
            CurrentBehaviour.Get()?.Update(deltaTime.Value);

            // Enter stagger if there is queued action
            if (_shouldEnterStaggerOrRest != null) {
                EnterStaggerOrRest(_shouldEnterStaggerOrRest.Value);
                _shouldEnterStaggerOrRest = null;
            }
        }
        
        protected void UpdateDistanceToTarget(float deltaTime, ICharacter target) {
            if (target != null) {
                DistanceToTarget = Vector3.Distance(target.Coords + target.HorizontalVelocity * deltaTime, Coords);
            } else if (NpcAI.AlertTarget != Coords) {
                DistanceToTarget = Vector3.Distance(NpcAI.AlertTarget, Coords);
            } else {
                DistanceToTarget = float.MaxValue;
            }
        }
        
        void UpdateCombatStatus(bool isInCombat) {
            if (isInCombat) {
                // NPC has entered combat, take appropriate action
                if (!_wasInCombatPreviously) {
                    if (RequiresCombatSlot && OwnedCombatSlotIndex == -1) {
                        HeroCombatSlots.TryGetAndOccupyCombatSlot(this, DesiredPositionTowardsHero(), SlotsReserveRange, out int occupiedCombatSlot);
                        OwnedCombatSlotIndex = occupiedCombatSlot;
                    }
                    OnEnterCombat();
                    NotifyAlliesAboutFightStart();
                }
                _wasInCombatPreviously = true;
                if (_nextNotifyAboutCombatTime < Time.time) {
                    NotifyAlliesAboutFight();
                }
            } else {
                // NPC has exited combat, take appropriate action
                if (_wasInCombatPreviously) {
                    OnExitCombat();
                    if (CurrentBehaviour.Get() is not UnEquipWeaponBehaviour) {
                        StopCurrentBehaviour(false);
                    }
                    HeroCombatSlots.ReleaseCombatSlot(this);
                    CombatDirector.RemoveEnemyFromFight(this);
                    _fatigueCounter = 0;
                }
                _wasInCombatPreviously = false;
            }
        }

        void NotifyAlliesAboutFightStart() {
            _nextNotifyAboutCombatTime = Time.time + NotifyAboutCombatInterval;
            ICharacter target = NpcElement.GetCurrentTarget();
            if (target != null) {
                NpcElement.NpcAI.TryNotifyAboutOngoingFight(target).Forget();
                MakeEnterCombatNoise(target).Forget();
            }
        }
        
        void NotifyAlliesAboutFight() {
            _nextNotifyAboutCombatTime = Time.time + NotifyAboutCombatInterval;
            ICharacter target = NpcElement.GetCurrentTarget();
            if (target != null) {
                NpcElement.NpcAI.TryNotifyAboutOngoingFight(target).Forget();
            }
        }
        
        async UniTaskVoid MakeEnterCombatNoise(ICharacter target) {
            // Wait a bit to allow "sneaky" hits/kills
            if (!await AsyncUtil.DelayTime(this, EnterCombatNotifyDelay)) {
                return;
            }
            if (!NpcElement.IsAlive || !NpcElement.IsInCombat()) {
                return;
            }

            NpcAI.NotifyAlliesAboutFightStart(target);
        }
        
        public bool InRangeWithCombatSlot(float range) {
            return TryGetCombatSlotPosition(out var combatSlotPosition) && (combatSlotPosition.ToHorizontal3() - Coords.ToHorizontal3()).sqrMagnitude <= range * range;
        }

        public bool TryGetCombatSlotPosition(out Vector3 position) {
            if (OwnedCombatSlotIndex == -1) {
                position = default;
                return false;
            }
            position = HeroCombatSlots.GetSlotWorldPosition(OwnedCombatSlotIndex);
            return true;
        }
        
        void UpdateCombatSlotStatus(ICharacter target) {
            if (target is not Hero) {
                return;
            }

            if (CurrentBehaviour.Get() is GuardIntervention or WaitForTargetInStoryBehaviour) {
                return;
            }
            
            float maxDistance = ReleaseCombatSlotAtDistance;
            if (overrideDistanceToReleaseCombatSlotAt) {
                maxDistance = releaseCombatSlotAtDistance;
            }
            
            if (TryGetCombatSlotPosition(out var combatSlotPosition)) {
                bool tooFarFromSlot = Vector3.Distance(combatSlotPosition, Coords) > maxDistance;
                if (tooFarFromSlot) {
                    ReleaseCombatSlots();
                }
                return;
            }

            if (DistanceToTarget < maxDistance) {
                HeroCombatSlots.TryGetAndOccupyCombatSlot(this, DesiredPositionTowardsHero(), SlotsReserveRange, out int occupiedCombatSlot);
                OwnedCombatSlotIndex = occupiedCombatSlot;
            }
        }

        void RegenStamina(float deltaTime) {
            var stamina = Stamina; // Cache for performance
            if (!stamina.IsMax) {
                stamina.IncreaseBy(CharacterStats.StaminaRegen.ModifiedValue * deltaTime);
            }
        }

        public void SetAnimatorState(NpcStateType stateType, NpcFSMType fsmType = NpcFSMType.GeneralFSM, float? overrideCrossFadeTime = null) {
            NpcElement?.SetAnimatorState(fsmType, stateType, overrideCrossFadeTime);
        }

        // === Behaviours
        [UnityEngine.Scripting.Preserve]
        protected void OnBaseBehavioursLoaded(Action<EnemyBaseClass> callback) {
            if (_baseBehavioursLoaded) {
                callback.Invoke(this);
            } else {
                this.ListenTo(Events.BaseBehavioursLoaded, callback.Invoke, this);
            }
        }
        
        protected void LoadBaseBehaviours() {
            _baseBehavioursAssetReference = NpcElement.FightingStyle.BaseBehaviours.GetAndLoad<AREnemyBehavioursMapping>(r => {
                if (r.Status == AsyncOperationStatus.Failed) {
                    Log.Critical?.Error($"Failed to load BaseBehaviours for: {ParentModel.Spec}!", ParentModel.Spec);
                    return;
                }

                foreach (var behaviour in r.Result.CombatBehaviours) {
                    if (behaviour == null) {
                        Log.Minor?.Error($"Null behaviour assigned to: {ParentModel.Spec}!", ParentModel.Spec);
                        continue;
                    }
                    AddElement(behaviour.Copy());
                }
                
                _baseBehavioursLoaded = true;
                this.Trigger(Events.BaseBehavioursLoaded, this);
                UpdateSummonerCombatAntagonism();
            });
        }

        public void AddTemporaryBehaviour(EnemyBehaviourBase behaviour) {
            TemporaryBehaviours.Add(behaviour);
            AddElement(behaviour);
        }

        public void RemoveTemporaryBehaviour(EnemyBehaviourBase behaviour) {
            TemporaryBehaviours.Remove(behaviour);
            behaviour.Discard();
        }
        
        protected async UniTask<bool> TryChangeCombatData(AnimationAndBehaviourMappingEntry newCombatData) {
            CombatBehaviours.ForEach(b => b.Discard());
            CombatBehaviours.Clear();

            CleanupCurrentBehaviourData();

            _combatDataCancellationToken = new CancellationTokenSource();
            var cancellationToken = _combatDataCancellationToken.Token;

            CurrentCombatData = newCombatData;
            if (newCombatData == null) {
                return true;
            }
            
            CombatBehavioursReferences.EnsureCapacity(CurrentCombatData.CombatBehaviours.Length);
            CurrentCombatData.CombatBehaviours.ForEach(shareableARAssetReference => {
                CombatBehavioursReferences.Add(shareableARAssetReference.Get());
            });

            var tasks = CombatBehavioursReferences.Select(r => r.LoadAsset<AREnemyBehavioursMapping>().ToUniTask());
            (bool canceled, AREnemyBehavioursMapping[] behaviours) = await UniTask.WhenAll(tasks)
                .AttachExternalCancellation(cancellationToken)
                .SuppressCancellationThrow();
            
            if (canceled || cancellationToken.IsCancellationRequested || HasBeenDiscarded) {
                return false;
            }

            if (behaviours == null) {
                Log.Critical?.Error("Failed to load Behaviours Mapping! This NPC will not work properly!");
                return false;
            }
            
            foreach (AREnemyBehavioursMapping mapping in behaviours) {
                foreach (var behaviour in mapping.CombatBehaviours) {
                    if (behaviour == null) {
                        Log.Minor?.Error($"Null behaviour assigned to: {ParentModel.Spec}!");
                        continue;
                    }
                    CombatBehaviours.Add(AddElement(behaviour.Copy()));
                }
            }

            UpdateSummonerCombatAntagonism();
            return true;
        }

        void CleanupCurrentBehaviourData() {
            _combatDataCancellationToken?.Cancel();
            for (int i = 0; i < CombatBehavioursReferences.Count; i++) {
                CombatBehavioursReferences[i].ReleaseAsset();
            }

            CombatBehavioursReferences.Clear();
        }

        void UpdateSummonerCombatAntagonism() {
            bool isSummoner = IsSummoner;
            if (isSummoner && !NpcElement.HasElement<SummonerCombatAntagonism>()) {
                NpcElement.AddElement(new SummonerCombatAntagonism());
            } else if (!isSummoner && NpcElement.TryGetElement(out SummonerCombatAntagonism summonerCombatAntagonism)) {
                summonerCombatAntagonism.Discard();
            }
        }
        
        public bool TryToStartNewBehaviour() {
            return SelectNewBehaviour();
        }
        
        public bool TryToStartNewBehaviourExcept(IBehaviourBase toSkip = null, IBehaviourBase toSkip2 = null) {
            return SelectNewBehaviour(toSkip, toSkip2);
        }

        protected bool SelectNewBehaviour(IBehaviourBase toSkip = null, IBehaviourBase toSkip2 = null) {
            if (NpcElement.IsUnconscious) {
                return false;
            }

            int maxPriority = int.MinValue;
            ReusableBehaviours.Clear();
            foreach (var behaviour in Behaviours) {
                if (behaviour == toSkip || behaviour == toSkip2 || behaviour.Weight < 0f) {
                    continue;
                }
                if (behaviour.CanBeInvoked) {
                    var priority = behaviour.Priority;
                    if (priority > maxPriority) {
                        maxPriority = priority;
                        ReusableBehaviours.Clear();
                        ReusableBehaviours.Add(behaviour);
                    } else if (priority == maxPriority) {
                        ReusableBehaviours.Add(behaviour);
                    }
                }
            }
            if (ReusableBehaviours.Count > 0) {
                IBehaviourBase behaviour = RandomUtil.WeightedSelect(ReusableBehaviours, b => b.Weight);
                ReusableBehaviours.Clear();
                StartBehaviour(behaviour);
                return true;
            }
            return false;
        }

        public bool StartBehaviour(IBehaviourBase behaviour) {
            if (NpcElement.IsUnconscious || NpcElement.NpcAI.InWyrdConversion || NpcElement.NpcAI.InSpawn) {
                return false;
            }
            
            StopCurrentBehaviour(false);
            return StartBehaviourInternal(behaviour);
        }
        
        public bool TryStartSpecialAttackBehaviour(Type type, int specialAttackIndex) {
            foreach (var element in Elements(type)) {
                if (element is EnemyBehaviourBase enemyBehaviourBase && enemyBehaviourBase.SpecialAttackIndex == specialAttackIndex) {
                    return StartBehaviour(enemyBehaviourBase);
                }
            }
            return false;
        }
        
        public bool TryStartSpecialAttackBehaviour<T>(int specialAttackIndex) where T : EnemyBehaviourBase {
            foreach (var element in Elements<T>()) {
                if (element.SpecialAttackIndex == specialAttackIndex) {
                    return StartBehaviour(element);
                }
            }
            return false;
        }
        
        public bool TryStartBehaviour(Type t) {
            return TryGetElement(t) is IBehaviourBase behaviourBase && StartBehaviour(behaviourBase);
        }

        public bool TryStartBehaviour<T>() where T : class, IBehaviourBase {
            return TryGetElement(out T element) && StartBehaviour(element);
        }
        
        public void StopCurrentBehaviour(bool selectNew) {
            if (CurrentBehaviour.TryGet(out var currentBehaviour)) {
                currentBehaviour.Stop();
                CurrentBehaviour = null;
                OnBehaviourStopped(currentBehaviour);
            }

            if (selectNew) {
                SelectNewBehaviour();
            }
        }
        
        void InterruptBehaviourWith(IBehaviourBase behaviour, bool force = false) {
            var currentBehaviour = CurrentBehaviour.Get();
            bool canBeInterrupted = currentBehaviour?.CanBeInterrupted ?? true;
            if (!force && (!canBeInterrupted || NpcElement.IsUnconscious)) {
                return;
            }
            currentBehaviour?.Interrupt();
            OnBehaviourInterrupted(currentBehaviour);
            NpcElement.Trigger(Events.AttackInterrupted, false);
            StartBehaviourInternal(behaviour);
        }

        bool StartBehaviourInternal(IBehaviourBase behaviour) {
            if (behaviour is { HasBeenDiscarded: false } && behaviour.Start()) {
                CurrentBehaviour = new WeakModelRef<IBehaviourBase>(behaviour);
                OnBehaviourStarted(behaviour);
                SetActiveBlocking(behaviour);
                this.Trigger(Events.BehaviourStarted, behaviour);
                return true;
            }

            return false;
        }

        public void StartWaitBehaviour() {
            bool isInCombat = NpcAI?.InCombat ?? false;
            if (!isInCombat) {
                StopCurrentBehaviour(false);
                // Wait Behaviour exits from NpcWait state.
                // Since we cannot enter WaitBehaviour outside combat we need to exit to idle manually here.
                SetAnimatorState(NpcStateType.Idle);
                return;
            }

            if (!TryStartBehaviour<IWaitBehaviour>()) {
                StopCurrentBehaviour(true);
            }
        }

        public void ReleaseCombatSlots() {
            HeroCombatSlots.ReleaseCombatSlot(this);
        }

        public bool TrySetBetterCombatSlot(float reserveRangeMultiplier, out Vector3 slotWorldPosition) {
            float reserveRange = SlotsReserveRange * reserveRangeMultiplier;
            if (CurrentBehaviour.Get() is GuardIntervention or WaitForTargetInStoryBehaviour && World.HasAny<IHeroInvolvement>() && OwnedCombatSlotIndex != -1) {
                slotWorldPosition = HeroCombatSlots.GetSlotWorldPosition(OwnedCombatSlotIndex);;
                return true;
            }

            var heroCombatSlots = HeroCombatSlots;
            if (heroCombatSlots.TryGetAndOccupyBetterCombatSlot(this, DesiredPositionTowardsHero(), reserveRange, out var slotIndex)) {
                slotWorldPosition = heroCombatSlots.GetSlotWorldPosition(slotIndex);
                OwnedCombatSlotIndex = slotIndex;
                return true;
            }

            slotWorldPosition = default;
            return false;
        }

        // === Stumbling & Ragdoll
        protected virtual void ApplyForce(Vector3 direction, float forceDamage, float ragdollForce, bool isPush, float duration = 0.5f) {
            if (!CanBePushed || CurrentBehaviour.Get() is IInterruptBehaviour || NpcElement.IsUnconscious) {
                return;
            }

            forceDamage *= NpcElement.NpcStats.ForceDamageMultiplier;
            direction.Normalize();
            float forceRagdollThreshold = GameConstants.Get.npcForceRagdollMultiplier * ForceStumbleThreshold.UpperLimit;
            if (forceDamage >= forceRagdollThreshold && CanBeRagdolled) {
                EnableRagdoll(direction, ragdollForce, 5);
                ForceStumbleThreshold.SetTo(0);
                PoiseThreshold.SetTo(0);
                return;
            }
            
            ForceStumbleThreshold.IncreaseBy(forceDamage);
            if (ForceStumbleThreshold.IsMaxFloat && CanBeStumbled) {
                ForceStumbleThreshold.SetTo(0);
                PoiseThreshold.SetTo(0);
                Stumble.SetStumbleParams(new Force(direction.ToHorizontal3() * ragdollForce, duration), isPush);
                InterruptBehaviourWith(Stumble);
            }
        }

        void EnterStaggerOrRest(bool shouldEnterStagger, float? duration = null) {
            if (CurrentBehaviour.Get() is StaggerBehaviour) {
                return;
            }

            if (shouldEnterStagger && HasElement<StaggerBehaviour>()) {
                Stagger.UpdateStaggerDuration(duration);
                InterruptBehaviourWith(Stagger);
            } else if (!shouldEnterStagger && HasElement<RestBehaviour>()) {
                Rest.UpdateRestDuration(duration);
                InterruptBehaviourWith(Rest);
            }
        }
        
        public void EnableRagdoll(Vector3 force, float forceModifier = 1, float durationLeft = 0, bool forceBehaviour = false) {
            if (!HasElement<RagdollBehaviour>()) {
                Log.Important?.Info($"{ParentModel.DisplayName} can't ragdoll because RagdollBehaviour does not exist!");
                return;
            }

            if (CurrentBehaviour.Get() is RagdollBehaviour) {
                return;
            }

            if (NpcElement.IsUnconscious) {
                return;
            }

            if (NpcElement.NpcAI.InWyrdConversion) {
                return;
            }

            RagdollMovement ragdollMovement = new(force, forceModifier, durationLeft);
            Ragdoll.SetRagdollParams(ragdollMovement);
            InterruptBehaviourWith(Ragdoll, forceBehaviour);
        }
        
        public void EnableRagdoll(RagdollMovement ragdollMovement, bool forceBehaviour = false) {
            if (!HasElement<RagdollBehaviour>()) {
                Log.Important?.Info($"{ParentModel.DisplayName} can't ragdoll because RagdollBehaviour does not exist!");
                return;
            }
            
            Ragdoll.SetRagdollParams(ragdollMovement);
            InterruptBehaviourWith(Ragdoll, forceBehaviour);
        }

        public void EnableStumble(Vector3 forceDirection, float forceModifier = 1, float duration = 0, bool forceBehaviour = false) {
            if (!HasElement<StumbleBehaviour>()) {
                Log.Important?.Info($"{ParentModel.DisplayName} can't stumble because StumbleBehaviour does not exist!");
                return;
            }
            
            Stumble.SetStumbleParams(new Force(forceDirection * forceModifier, duration), false);
            InterruptBehaviourWith(Stumble, forceBehaviour);
        }

        void EnterUnconscious() {
            if (!HasElement<UnconsciousBehaviour>()) {
                Log.Important?.Info($"{ParentModel.DisplayName} can't be unconscious because UnconsciousBehaviour is not existing!");
                return;
            }

            RagdollMovement ragdollMovement = new(Vector3.zero, 0, float.PositiveInfinity, false);
            Unconscious.SetRagdollParams(ragdollMovement);
            UpdateCombatStatus(false);
            InterruptBehaviourWith(Unconscious, true);
            
            if (!WeaponsAlwaysEquipped) {
                UnEquipWeapons();
            }
        }
        
        public virtual void EnterParriedState() {
            if (!HasElement<ParriedBehaviour>()) {
                return;
            }
            
            if (Staggered) {
                return;
            }
            
            InterruptBehaviourWith(Element<ParriedBehaviour>());
        }

        public void EnterStagger(float? duration) {
            if (CurrentBehaviour.Get() is StaggerBehaviour || !HasElement<StaggerBehaviour>()) {
                return;
            }

            Stagger.UpdateStaggerDuration(duration);
            InterruptBehaviourWith(Stagger);
        }
        
        public void AttemptToDodge(NpcStateType dodgeDirection) {
            if (TryGetElement(out DodgeBehaviour dodgeBehaviour)) {
                dodgeBehaviour.UpdateDodgeDirection(dodgeDirection);
                InterruptBehaviourWith(dodgeBehaviour);
            }
        }

        public virtual void DealPoiseDamage(NpcStateType getHitType, float poiseDamage, bool isCritical, bool isDamageOverTime) {
            // --- Don't enter get hit animations when dying.
            if (NpcElement.IsDying) {
                return;
            }
            
            if (!HasElement<PoiseBreakBehaviour>()) {
                if (!isDamageOverTime) {
                    SetAnimatorState(NpcStateType.GetHit, NpcFSMType.AdditiveFSM, 0f);
                }
                return;
            }

            if (isCritical) {
                poiseDamage *= GameConstants.Get.poiseCriticalDamageMultiplier;
            }
            
            PoiseThreshold.IncreaseBy(poiseDamage);
            bool canEnterPoise = PoiseThreshold.IsMaxFloat;
            if (canEnterPoise) {
                bool hasEquippedWeapon = NpcElement.MainHandWeapon != null || NpcElement.OffHandWeapon != null;
                canEnterPoise = hasEquippedWeapon && CurrentBehaviour.Get() is not EquipWeaponBehaviour;
            }
            if (canEnterPoise) {
                PoiseThreshold.SetTo(0);
                EnterPoise(getHitType, NpcAI?.InCombat ?? false);
            } else if (!isDamageOverTime) {
                SetAnimatorState(NpcStateType.GetHit, NpcFSMType.AdditiveFSM, 0f);
            }
        }

        void EnterPoise(NpcStateType poiseBreakDirection, bool isInCombat) {
            _shouldEnterStaggerOrRest = null;

            if (!isInCombat) {
                SetAnimatorState(poiseBreakDirection, NpcFSMType.GeneralFSM, 0f);
                return;
            }
            
            PoiseBreakBehaviour poiseBreakBehaviour = Element<PoiseBreakBehaviour>();
            poiseBreakBehaviour.UpdatePoiseBreakDirection(poiseBreakDirection);
            InterruptBehaviourWith(poiseBreakBehaviour);
        }
        
        // === Combat Directing
        public void IncreaseFatigue() {
            _fatigueCounter = Mathf.Min(++_fatigueCounter, MaxFatigue);
            _lastFatigueUpdate = 0;
        }

        public void DecreaseFatigue() {
            DecreaseFatigueInternal(false);
        }
        
        public void SetDesiredPosition(Vector3 position) {
            DesiredPosition = position;
        }
        
        public float UpdateAggressionScore(Vector3 heroForward) {
            const float BaseScore = 100;

            float targetMultiplier = NpcElement.IsTargetingHero() ? 1.25f : 1f;
            // --- Angle and distance to hero
            Vector3 directionToTarget = Coords - Hero.Current.Coords;
            float distanceToHero = directionToTarget.magnitude;
            float heroDotToTarget = Vector3.Dot(heroForward, directionToTarget / distanceToHero);
            // --- On/Off screen score
            float inViewConeScore = AIUtils.IsInHeroViewCone(heroDotToTarget) ? 0.5f : 0.25f;
            // --- Angle score
            float t = Mathf.InverseLerp(AIUtils.CosHeroHalfHorizontalPerception, 1, heroDotToTarget);
            float angleScore = t.Remap(0, 1, 0, 0.5f);
            
            float actionMultiplier = Mathf.Clamp01(inViewConeScore + angleScore);
            // --- Distance score
            float distanceMultiplier = Mathf.Max(0.05f, Mathf.Pow(Mathf.InverseLerp(10, 1, distanceToHero), 3.5f));
            // --- Aggression score
            float canBeAggressive = CanBeAggressive ? 1 : 0.25f;
            // --- Fatigue
            float fatigueCost = _fatigueCounter.RemapInt(0, MaxFatigue, 0, BaseScore, true);

            AggressionScore = BaseScore * canBeAggressive * targetMultiplier * actionMultiplier * distanceMultiplier - fatigueCost;
            return AggressionScore;
        }

        void DecreaseFatigueInternal(bool calledInternal) {
            _fatigueCounter = Mathf.Max(--_fatigueCounter, 0);
            if (calledInternal) {
                _lastFatigueUpdate -= DecreaseFatigueInterval;
            } else {
                _lastFatigueUpdate = 0;
            }
        }

        // === Blocking
        protected void SetActiveBlocking(IBehaviourBase behaviour) {
            if (behaviour.CanBlockDamage && !IsBlocking) {
                _aiBlock = NpcElement.AddElement<AIBlock>();
            } else if (!behaviour.CanBlockDamage && IsBlocking) {
                _aiBlock.Get().Discard();
                _aiBlock = null;
            }
        }
        
        // === Discarding
        protected override void OnDiscard(bool fromDomainDrop) {
            if (NpcElement is { HasBeenDiscarded: false }) {
                NpcCanMoveHandler.RemoveCanMoveProvider(NpcElement, this);
            }

            CleanupCurrentBehaviourData();

            _baseBehavioursAssetReference?.ReleaseAsset();
            _baseBehavioursAssetReference = null;
            
            ParentModel.GetTimeDependent()?.WithoutUpdate(OnUpdate);
            _cachedNpcElement = null;
            
            World.EventSystem.TryDisposeListener(ref _targetAttackListener);
        }
        
        // === Helpers
        public Transform GetAdditionalHand(AdditionalHand hand) {
            return _cachedAdditionalHandMarkers.FirstOrDefault(m => m.Hand == hand)?.transform;
        }
        
        Vector3 DesiredPositionTowardsHero() {
            Vector3 directionFromHero = (Coords - Hero.Current.Coords).normalized;
            return Hero.Current.Coords + directionFromHero * DistancesToTargetHandler.DesiredDistanceToTarget(NpcElement);
        }

        // === Virtuals
        protected virtual void AddPersistentBehaviours() {
            AddElement<EquipWeaponBehaviour>();
            AddElement<WaitForTargetInStoryBehaviour>();
            AddElement<UnconsciousBehaviour>();
        }
        protected virtual void AfterVisualLoaded(Transform parentTransform) { }
        protected virtual void AfterItemsAddedToInventory() { }

        protected virtual void OnEnterCombat() {
            EquipWeapons(false, out _);
        }

        protected virtual void OnExitCombat() {
            _aiBlock.Get()?.Discard();
            _aiBlock = null;
            
            if (WeaponsAlwaysEquipped) {
                return;
            }

            if (!ShouldWeaponsBeEquipped) {
                UnEquipWeapons();
            }
        }
        
        protected virtual bool EquipWeapons(bool getResults, out List<Item> equippedItems, bool forceMelee = false, bool forceRanged = false) {
            if (CurrentBehaviour.Get() is UnEquipWeaponBehaviour unEquipWeaponBehaviour) {
                unEquipWeaponBehaviour.isExitingToCombat = true;
                StopCurrentBehaviour(false);
            }
            
            sReusableWeapons.Clear();
            Item fist = null;
            foreach (var item in NpcElement.NpcItems.Items) {
                if (item.IsFists) {
                    fist ??= item;
                } else {
                    var weaponRequirement = !(forceMelee | forceRanged) && item.IsWeapon;
                    var meleeRequirement = forceMelee && item.IsMelee;
                    var rangedRequirement = forceRanged && item.IsRanged;
                    if (weaponRequirement || meleeRequirement || rangedRequirement) {
                        sReusableWeapons.Add(item);
                    }
                }
            }
            
            bool anyItems = sReusableWeapons.Count > 0;

            if (!anyItems && !forceRanged && fist) {
                sReusableWeapons.Add(fist);
                anyItems = true;
            }

            if (!getResults) {
                foreach (var weapon in sReusableWeapons) {
                    NpcElement.Inventory.Equip(weapon);
                }
                equippedItems = null;
                return anyItems;
            }

            equippedItems = new List<Item>(sReusableWeapons.Count);
            foreach (var weapon in sReusableWeapons) {
                if (NpcElement.Inventory.Equip(weapon)) {
                    equippedItems.Add(weapon);
                }
            }
            
            sReusableWeapons.Clear();
            return anyItems;
        }

        protected virtual void UnEquipWeapons() {
            if (TryStartBehaviour<UnEquipWeaponBehaviour>()) {
                return;
            }
            NpcElement.Inventory.Unequip(EquipmentSlotType.MainHand);
            NpcElement.Inventory.Unequip(EquipmentSlotType.OffHand);
        }

        public void AfterVisualInBand(NpcElement npc) {
            if (WeaponsAlwaysEquipped) {
                EquipWeapons(false, out _);
                if (NpcElement.CanDetachWeaponsToBelts) {
                    EquipWeaponBehaviour.AttachWeaponsToHands(MainHandItem, OffHandItem, NpcElement);
                }
            }
        }
        public void BeforeOutOfVisualBand(NpcElement npc) { }
        
        protected virtual void NotInCombatUpdate(float deltaTime) { }
        protected virtual void Tick(float deltaTime, NpcElement npc) { }
        protected virtual void OnBehaviourStarted(IBehaviourBase behaviour) { }
        protected virtual void OnBehaviourStopped(IBehaviourBase behaviour) { }
        protected virtual void OnBehaviourInterrupted(IBehaviourBase behaviour) { }
    }
}