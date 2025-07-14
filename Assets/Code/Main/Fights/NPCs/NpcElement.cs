using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using Awaken.Kandra;
using Awaken.TG.Assets;
using Awaken.TG.Fights.NPCs;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.AI.Debugging;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.AI.Idle.Interactions.Saving;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Animations.FSM.Npc.States.Combat;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.Modifiers;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Fights.NPCs.Providers;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Mobs;
using Awaken.TG.Main.Maps.Markers;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.UI.Bugs;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Animations.FightingStyles;
using Awaken.TG.Main.Utility.Animations.Gestures;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.Main.Utility.VFX;
using Awaken.TG.Main.Wyrdnessing;
using Awaken.TG.Main.Wyrdnessing.WyrdEmpowering;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Serialization;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.TG.VisualScripts.Units.Events;
using Awaken.TG.VisualScripts.Units.Utils;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Maths;
using Awaken.VendorWrappers.Salsa;
using CrazyMinnow.SALSA;
using DG.Tweening;
using FMODUnity;
using JetBrains.Annotations;
using Pathfinding;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using UnityEngine.Animations;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Fights.NPCs {
    [Il2CppEagerStaticClassConstruction]
    public partial class NpcElement : Element<Location>, ICharacter, ILocationElement, IRefreshedByAttachment<NpcAttachment>, INpcEquipTarget, ILocationElementWithActor, IWithCrimeNpcValue, IWithLookAt, IWyrdnessReactor {
        public override ushort TypeForSerialization => SavedModels.NpcElement;

        const float InteractionColliderMultiplier = 1.25f;
        const string HipsString = "Hips";
        const string PelvisString = "Pelvis";
        static NpcRegistry NpcRegistry => World.Services.Get<NpcRegistry>();
        public static bool DEBUG_DoNotSpawnAI;
        
        // === Serialized State
        [Saved] FactionContainer _factionContainer = new();
        [Saved] List<Item> _equipmentCache = new(10);
        [Saved(false)] bool _itemsAddedToInventory;
        [Saved] NpcFightingStyle _overridenFightingStyle;
        [Saved] public SavedInteractionData SavedInteractionData { get; set; }
        [Saved] public Vector3 LastIdlePosition { get; set; }
        [Saved] public Vector3 LastOutOfCombatPosition { get; set; }
        [Saved(false)] public bool WyrdConverted { get; private set; }
        
        // === Runtime state
        NpcInitializer _initializer;
        ShareableARAssetReference _hitVFX, _criticalVFX, _backStabVFX, _deathVFX;
        ReferenceInstance<GameObject> _spawnedVisualPrefab;
        TemplateReference[] _predefinedClothes;
        IEnumerable<ItemSpawningDataRuntime> _inventoryItems;
        TemporarySet<int> _temporaryDistanceBandOverrides = new(8, -1);

        bool _isInDialogue;
        int _currentDistanceBand;
        bool _shouldSpawnDeathVfx;
        bool _isAlive = true;
        bool _eyesClosed;
        bool _inNpcRegistry;
        bool? _isVisible;
        float _height = 1.8f;
        Tweener _eyesTween;
        GameObject _interactionCollider;
        NpcWyrdConversionMarker _wyrdConversionMarker;
        // -- Elements cache
        NpcStats _cachedNpcStats;
        CharacterStats _cachedCharacterStats;
        AliveStats _cachedAliveStats;
        EnemyBaseClass _cachedEnemyBaseClass;
        DeathElement _cachedDeathElement;
        NpcCanMoveHandler _cachedCanMoveHandler;
        NpcIsGroundedHandler _cachedIsGroundedHandler;
        TimeDependent _timeDependent;
        NpcItems _cachedItems;
        
        public NpcAngularSpeedMultiplier AngularSpeedMultiplier { get; set; }
        
        public NpcCanMoveHandler CanMoveHandler => TryGetCachedElementWithChecks(ref _cachedCanMoveHandler);
        public NpcIsGroundedHandler IsGroundedHandler => TryGetCachedElementWithChecks(ref _cachedIsGroundedHandler);
        public TimeDependent TimeDependent => ParentModel?.TryGetCachedElementWithChecks(ref _timeDependent);

        // === Properties
        public bool IsInDialogue {
            get => _isInDialogue;
            set {
                _isInDialogue = value;
                this.Trigger(Events.NpcIsInDialogueChanged, value);
            } 
        }

        public bool IsUnique { get; private set; }
        public bool IsInRagdoll { get; set; }
        public bool IsVisible => _isVisible ?? false;
        public bool CanBeWyrdEmpowered => Template.CanBeWyrdEmpowered;
        public bool CanBeWyrdConverted => CanBeWyrdEmpowered && !WyrdConverted && !IsSafeFromWyrdness;
        public bool CanBeOutWyrdConverted => CanBeWyrdEmpowered && WyrdConverted;
        public bool StartInSpawn { get; set; }
        public bool CanMoveInSpawn { get; set; } = false;
        public bool IsSafeFromWyrdness { get; set; }
        public bool IsAlwaysPiercedByArrows { get; set; }
        public bool CanEquipWeaponsThroughBehaviour => EnemyBaseClass is { WeaponsAlwaysEquipped: false } enemyBaseClass && enemyBaseClass.TryGetElement<EquipWeaponBehaviour>();
        public bool IsEquippingWeapons => EnemyBaseClass?.CurrentBehaviour.Get() is EquipWeaponBehaviour;
        public ReturnToSpawnPointArchetype ReturnToSpawnPointArchetype => Template.ReturnToSpawnPointArchetype;
        public NpcType NpcType => Template.NpcType;
        public NpcTemplate Template { get; private set; }
        public NpcPresence NpcPresence { get; private set; }
        public NpcController Controller { get; private set; }
        public ShareableARAssetReference HitVFX => TryGetElement<HitVFXOverride>()?.HitVFX ?? _hitVFX;
        public ShareableARAssetReference CriticalVFX => _criticalVFX.IsSet ? _criticalVFX : Services.Get<GameConstants>().DefaultCriticalVFX;
        public ShareableARAssetReference BackStabVFX => _backStabVFX.IsSet ? _backStabVFX : Services.Get<GameConstants>().DefaultBackStabVFX;
        public ShareableARAssetReference DeathVFX => _deathVFX.IsSet ? _deathVFX : Services.Get<GameConstants>().DefaultDeathVFX;
        public bool ShouldSpawnDeathVFX => _shouldSpawnDeathVfx && !IsSummon;
        public StoryBookmark StoryOnDeath { get; private set; }
        public SpriteReference NpcIcon { get; private set; }
        public GameObject SpawnedVisualPrefab => _spawnedVisualPrefab.Instance;
        public VoiceOversEventEmitter VoiceOversEmitter { get; private set; }
        public SalsaFmodEventEmitter SalsaEmitter { get; private set; }
        ARAssetReference VisualPrefab { get; set; }
        ShareableARAssetReference SimplifiedDeadBodyPrefab { get; set; }
        Eyes Eyes { get; set; }
        
        HeroDamageTimestamp HeroDamageTimestamp { get; set; }
        public bool WasLastDamageFromHero => HeroDamageTimestamp?.CountAsHeroKill() ?? false;
        public bool DisableTargetRecalculation { get; set; }
        public int RecalculationFrameCooldown { get; set; }
        public int AutoRecalculationFrameCooldown { get; set; }
        public bool KeepCorpseAfterDeath { get; set; }
        public Actor Actor { get; private set; }
        public Transform ActorTransform => ParentTransform;
        public Transform LookAtTarget => Head;

        public NpcChunk NpcChunk { get; set; }
        public CharacterHandBase MainHandWeapon { get; private set; }
        public CharacterHandBase OffHandWeapon { get; private set; }
        public Transform ParentTransform { get; private set; }
        public Transform MainHand { get; private set; }
        public Transform AdditionalMainHand { get; private set; }
        public Transform OffHand { get; private set; }
        public Transform AdditionalOffHand { get; private set; }
        public Transform Head { get; private set; }
        public Transform Torso { get; private set; }
        public Transform Hips { get; private set; }
        public VFXBodyMarker VFXBodyMarker { get; private set; }
        public Transform MainHandEqSlot { get; private set; }
        public Transform OffHandEqSlot { get; private set; }
        public Transform BackWeaponEqSlot { get; private set; }
        public Transform BackEqSlot { get; private set; }
        public NpcWeaponsHandler WeaponsHandler { get; private set; }
        public bool CanDetachWeaponsToBelts { get; private set; }
        public Transform[] BeltItemSockets { get; private set; }
        public bool IsKandraHidden { get; private set; }
        
        public string Name => ParentModel.DisplayName;
        public Faction Faction => _factionContainer.Faction;
        public FactionTemplate GetFactionTemplateForSummon() => _factionContainer.GetFactionTemplateForSummon();
        public CrimeOwners GetCurrentCrimeOwnersFor(CrimeArchetype crime) => ParentModel.GetCurrentCrimeOwnersFor(crime);
        CrimeOwnerTemplate ICharacter.DefaultCrimeOwner => ParentModel.DefaultOwner;
        public Vector3 Coords => ParentModel.Coords;
        public Quaternion Rotation => ParentModel.Rotation;
        public CrimeNpcValue CrimeValue => Template.CrimeValue;
        public ICollection<string> Tags => Template.Tags;
        public int Tier { get; private set; }
        public int MusicTier { get; private set; }
        public IBaseClothes<IItemOwner> Clothes => NpcClothes;
        public CharacterStats CharacterStats => CachedElement(ref _cachedCharacterStats);
        public CharacterStats.ITemplate CharacterStatsTemplate => Template;
        public StatusStats StatusStats => Element<StatusStats>();
        public StatusStats.ITemplate StatusStatsTemplate => Template;
        public NpcStats NpcStats => CachedElement(ref _cachedNpcStats);
        public ProficiencyStats ProficiencyStats => throw new ArgumentException("NPCs cannot have proficiency stats");
        public AliveStats AliveStats => CachedElement(ref _cachedAliveStats);
        public AliveStats.ITemplate AliveStatsTemplate => Template;
        public LimitedStat Health => AliveStats.Health;
        public LimitedStat HealthStat => Health;
        public Stat MaxHealth => AliveStats.MaxHealth;
        public EnemyBaseClass EnemyBaseClass => ParentModel.TryGetCachedElement(ref _cachedEnemyBaseClass);
        public DeathElement DeathElement => TryGetCachedElement(ref _cachedDeathElement);
        public ICharacterInventory Inventory => NpcItems;
        public NpcItems NpcItems => ParentModel.CachedElementWithChecks(ref _cachedItems);
        public IEnumerable<ItemTemplate> OwnedItemTemplates => _itemsAddedToInventory ? Inventory.Items.Select(i => i.Template) : InventoryItems.Select(i => i.ItemTemplate);
        public NpcMovement Movement => TryGetElement<NpcMovement>();
        public NpcAI NpcAI { get; private set; }
        public CharacterStatuses Statuses => Element<CharacterStatuses>();
        public ICharacterSkills Skills => Element<CharacterSkills>();
        public NpcClothes NpcClothes => ParentModel.TryGetElement<NpcClothes>();
        public bool CanEquip => true;
        public HealthElement HealthElement => Element<HealthElement>();
        public CharacterDealingDamage CharacterDealingDamage => Element<CharacterDealingDamage>();
        public NpcInteractor Interactor => Element<NpcInteractor>();
        public IdleBehaviours Behaviours => Element<IdleBehaviours>();
        public GesturesSerializedWrapper GesturesWrapper => ParentModel.TryGetElement<DialogueAction>()?.GesturesWrapper;
        public GesturesSerializedWrapper InteractionGestures => Interactor.CurrentInteraction?.Gestures;
        public ICharacterView CharacterView => ParentModel.LocationView;
        public IAIEntity AIEntity => NpcAI;

        public bool IsAlive => !HasBeenDiscarded && _isAlive;
        public bool IsUnconscious => (TryGetElement<UnconsciousElement>()?.IsUnconscious ?? false) || !IsAlive;
        public bool IsDisappeared => HasElement<NpcDisappearedElement>();
        public bool IsDying => Health.ModifiedValue <= 0;
        public bool Grounded => !Template.IsNotGrounded;
        [UnityEngine.Scripting.Preserve] public bool IsBlocking => HasElement<AIBlock>();
        public bool IsBlinded => HasElement<TargetBlindedElement>();
        public bool IsSummon => HasElement<INpcSummon>();
        public bool IsHeroSummon { get; set; }
        public bool IsMuted => HasElement<MutedMarker>();
        public bool IsStunned => HasElement<StunnedCharacterElement>();
        public bool CanRewardExp => !HasElement<PreventExpRewardMarker>() && !IsSummon;
        public bool BlockLootingDeadBody => !Template.isDeadBodyLootable || IsSummon;
        public bool HasPerception => NpcAI?.Working ?? false;
        public float Radius => Movement.Controller.RichAI.radius;
        public bool UseRichAISlowdownTime => Template.UseRichAISlowdownTime;
        public bool Staggered => EnemyBaseClass.CurrentBehaviour.TryGet(out var behaviourBase) && behaviourBase is StaggerBehaviour;
        public bool IsVisualSet => VisualPrefab is {IsSet: true};
        public int CombatSlotsLimit => Template.CombatSlotsLimit;
        public bool CanEnterCombat(bool ignoreInvisibility) => Template.CanEnterCombat && !HasElement<BlockEnterCombatMarker>() && !IsUnconscious && !CrimeReactionUtils.IsFleeing(this) && (ignoreInvisibility || !HasElement<Invisibility>());
        public bool IgnoresEnviroDanger => HasElement<IgnoreEnviroDangerMarker>();
        public float TotalArmor(DamageSubType damageType) => AliveStats.ArmorMultiplier * AliveStats.Armor;
        public float Height => _height;
        public bool UsesCombatMovementAnimations => EnemyBaseClass?.UsesCombatMovementAnimations ?? false;
        public bool UsesAlertMovementAnimations => EnemyBaseClass?.UsesAlertMovementAnimations ?? false;
        [UnityEngine.Scripting.Preserve] public GenericAttackData GenericAttackData => 
            EnemyBaseClass?.GenericAttackData ?? GenericAttackData.Default;
        public LayerMask HitLayerMask => TryGetElement<NpcHandOwner>()?.HitLayerMask ?? new LayerMask();
        public bool ARAnimancerLoaded { get; private set; }
        public bool CanDealDamageToFriendlies => Template.canDealDamageToFriendlies || HasElement<TargetBlindedElement>();
        public Vector3 HorizontalVelocity => Controller.CurrentVelocity.ToHorizontal3();
        public float RelativeForwardVelocity => Controller.ARNpcAnimancer.VelocityForward;
        public float RelativeRightVelocity => Controller.ARNpcAnimancer.VelocityHorizontal;
        public float DefaultDesiredDistanceToTarget => FightingStyle == null ? VHeroCombatSlots.FirstLineCombatSlotOffset : FightingStyle.desiredDistanceToTarget;
        
        IEnumerable<ItemSpawningDataRuntime> InventoryItems => _inventoryItems ??= Template.InventoryItems(this);
        public int CurrentDistanceBand { get; private set; }

        [UnityEngine.Scripting.Preserve] public bool HasVisualLoaded => _initializer.HasVisualLoaded;
        public bool HasCompletelyInitialized => _initializer.HasCompletelyInitialized;
        public bool ItemsAddedToInventory => _itemsAddedToInventory;

        // === Fighting
        FightingPair.LeftStorage _possibleTargets;
        FightingPair.RightStorage _possibleAttackers;

        public ref FightingPair.LeftStorage PossibleTargets => ref _possibleTargets;
        public ref FightingPair.RightStorage PossibleAttackers => ref _possibleAttackers;
        public bool RequiresPathToTarget => Template.requiresPathToTarget && 
                                            !(Inventory?.Items?.Any(i => i.IsRanged) ?? false);
        public NpcDangerTracker DangerTracker => NpcAI.Behaviour.DangerTracker;

        // === Animations
        public bool CanUseExternalCustomDeath => DeathElement.CanUseExternalCustomDeath;
        public NpcFightingStyle FightingStyle => _overridenFightingStyle != null
            ? _overridenFightingStyle
            : Template.FightingStyle;
        
        // === Audio
        public bool CanTriggerAggroMusic => Template.CanTriggerAggroMusic && !HasElement<DisableAggroMusicMarker>();
        public AliveAudio AliveAudio => ParentModel.TryGetElement<AliveAudio>();
        public SurfaceType AudioSurfaceType => Template.SurfaceType;
        // === VFX
        public AliveVfx AliveVfx => ParentModel.TryGetElement<AliveVfx>();
        
        // === Events
        [Il2CppEagerStaticClassConstruction]
        public new static class Events {
            public static readonly Event<Location, NpcElement> BeforeNpcInVisualBand = new(nameof(BeforeNpcInVisualBand));
            public static readonly Event<Location, NpcElement> AfterNpcInVisualBand = new(nameof(AfterNpcInVisualBand));
            public static readonly Event<Location, NpcElement> BeforeNpcOutOfVisualBand = new(nameof(BeforeNpcOutOfVisualBand));
            public static readonly Event<Location, NpcElement> AfterNpcOutOfVisualBand = new(nameof(AfterNpcOutOfVisualBand));
            public static readonly Event<Location, bool> AfterNpcVisibilityChanged = new(nameof(AfterNpcVisibilityChanged));
            public static readonly Event<NpcElement, NpcElement> ItemsAddedToInventory = new(nameof(ItemsAddedToInventory));
            public static readonly Event<NpcElement, NpcElement> AnimatorEnteredAttackState = new(nameof(AnimatorEnteredAttackState));
            public static readonly Event<NpcElement, NpcElement> AnimatorExitedAttackState = new(nameof(AnimatorExitedAttackState));
            public static readonly Event<NpcElement, NpcPresence> PresenceChanged = new(nameof(PresenceChanged));
            public static readonly Event<NpcElement, NpcElement> AnimatorEnteredSpawnState = new(nameof(AnimatorEnteredSpawnState));
            public static readonly Event<NpcElement, NpcElement> NpcSpawning = new(nameof(NpcSpawning));
            public static readonly Event<NpcElement, bool> NpcIsInDialogueChanged = new(nameof(NpcIsInDialogueChanged));
        }

        // === Constructors
        public NpcElement() {
            _initializer = new NpcInitializer(this);
        }
        
        public void InitFromAttachment(NpcAttachment spec, bool isRestored) {
            IsUnique = spec.IsUnique;
            Template = spec.NpcTemplate;
            VisualPrefab = spec.VisualPrefab.DeepCopy();
            SimplifiedDeadBodyPrefab = spec.SimplifiedDeadBodyPrefab;
            _predefinedClothes = Array.Empty<TemplateReference>();
            _hitVFX = spec.HitVFXReference;
            _criticalVFX = spec.CriticalHitVFXReference;
            _backStabVFX = spec.BackStabHitVFXReference;
            _deathVFX = spec.DeathVFXReference;
            _shouldSpawnDeathVfx = spec.ShouldSpawnDeathVfx;
            Actor = spec.GetActor().Get();
            if (StoryBookmark.ToInitialChapter(spec.StoryOnDeath, out var storyOnDeath)) {
                StoryOnDeath = storyOnDeath;
            }
            NpcIcon = spec.NpcIcon.DeepCopy();
        }

        // === Initialization
        protected override void OnInitialize() {
            Tier = TagUtils.TryFindTagValueAsInt(Tags, "Tier") ?? 0;
            MusicTier = TagUtils.TryFindTagValueAsInt(Tags, "MusicTier") ?? 0;
            
            _factionContainer.SetDefaultFaction(Template.Faction);
            _possibleTargets = new(this);
            _possibleAttackers = new(this);
            
            ParentModel.OnVisualLoaded(t => {
                LastIdlePosition = LastOutOfCombatPosition = Coords;
                AfterVisualLoaded(t, false);
            });
            
            _temporaryDistanceBandOverrides.changed += RecalculateCurrentDistanceBand;
            
            _initializer.Initialize();
        }

        protected override void OnRestore() {
            Tier = TagUtils.TryFindTagValueAsInt(Tags, "Tier") ?? 0;
            MusicTier = TagUtils.TryFindTagValueAsInt(Tags, "MusicTier") ?? 0;
            
            _factionContainer.SetDefaultFaction(Template.Faction);
            _possibleTargets = new(this);
            _possibleAttackers = new(this);
            
            ParentModel.OnVisualLoaded(t => {
                AfterVisualLoaded(t, true);
            });
            
            _temporaryDistanceBandOverrides.changed += RecalculateCurrentDistanceBand;
            
            _initializer.Restore();
        }

        protected override void OnFullyInitialized() {
            NpcRegistry.RegisterNpc(this);
            _inNpcRegistry = true;
            _initializer.FullyInitialize();
            
            HealthElement.ListenTo(HealthElement.Events.TakingDamage, OnTakingDamage, this);
            this.ListenTo(AITargetingUtils.Relations.IsTargetedBy.Events.AfterAttached, data => this.TryAddPossibleCombatTarget((ICharacter) data.to), this);
            this.ListenTo(AITargetingUtils.Relations.Targets.Events.AfterDetached, _ => this.RecalculateTarget(), this);
            ParentModel.ListenTo(Model.Events.AfterFullyInitialized, () => {
                Inventory.ListenTo(ICharacterInventory.Events.AfterEquipmentChanged, () => Skills.UpdateContext(), this);
            }, this);
            ParentModel.ListenTo(GroundedEvents.TeleportRequested, _ => this.Trigger(GroundedEvents.TeleportRequested, this), this);
            ParentModel.ListenTo(GroundedEvents.AfterTeleported, _ => this.Trigger(GroundedEvents.AfterTeleported, this), this);
            ParentModel.ListenTo(GroundedEvents.BeforeTeleported, _ => this.Trigger(GroundedEvents.BeforeTeleported, this), this);
            ParentModel.ListenTo(GroundedEvents.AfterMoved, _ => this.Trigger(GroundedEvents.AfterMoved, this), this);
            ParentModel.ListenTo(GroundedEvents.AfterMovedToPosition, coords => this.Trigger(GroundedEvents.AfterMovedToPosition, coords), this);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (_inNpcRegistry) {
                NpcRegistry.UnregisterNpc(this);
                _inNpcRegistry = false;
            }

            ParentModel.StopEmittingSounds();
            _spawnedVisualPrefab?.ReleaseInstance();
            _spawnedVisualPrefab = null;
            
            _temporaryDistanceBandOverrides.changed -= RecalculateCurrentDistanceBand;

            _cachedNpcStats = null;
            _cachedCharacterStats = null;
            _cachedAliveStats = null;
            
            PossibleTargets.Clear();
            PossibleAttackers.Clear();
        }

        public void OnARAnimancerLoaded() => ARAnimancerLoaded = true;
        public void OnVisualLoaded(NpcInitializer.NpcVisualLoadedDelegate action) => _initializer.OnVisualLoaded(action);
        public void OnCompletelyInitialized(NpcInitializer.NpcCompletelyInitialized action) => _initializer.OnCompletelyInitialized(action);

        // === Operations
        public void ChangeNpcPresence(NpcPresence presence) {
            var previous = NpcPresence;
            NpcPresence = null;
            if (IsAlive) {
                var reason = presence == null
                    ? NpcPresenceDetachReason.MySceneUnloading
                    : NpcPresenceDetachReason.ChangePresence;
                previous?.Detach(this, reason);
                NpcPresence = presence;
                NpcPresence?.Attach(this);
            } else {
                previous?.Detach(this, NpcPresenceDetachReason.Death);
            }

            OnPresenceChanged(NpcPresence);
            this.Trigger(Events.PresenceChanged, NpcPresence);
        }

        void OnPresenceChanged(NpcPresence presence) {
            if (World.Services.TryGet(out CullingSystem cullingSystem) == false) {
                return;
            }
            if (World.Services.Get<SceneService>().IsAdditiveScene) {
                var npcLocation = ParentModel;
                bool pause;
                var locationCullingGroup = cullingSystem.GetCullingGroupInstance<LocationCullingGroup>();
                if (presence != null) {
                    var presenceLocation = presence.ParentModel;
                    locationCullingGroup.TryGetElementPausedStatus(presenceLocation, out pause);
                } else {
                    pause = false;
                }
                if (pause) {
                    locationCullingGroup.PauseElement(npcLocation);
                    foreach (var view in npcLocation.Views) {
                        var aiBase = view.gameObject.GetComponentInChildren<AIBase>(true);
                        if (aiBase) {
                            aiBase.Pause();
                        }
                    }
                } else {
                    locationCullingGroup.UnpauseElement(npcLocation);
                    foreach (var view in npcLocation.Views) {
                        var aiBase = view.gameObject.GetComponentInChildren<AIBase>(true);
                        if (aiBase) {
                            aiBase.Unpause();
                        }
                    }
                }
            }
        }
        
        public void ResetNpcPresence() {
            var newPresence = World.All<NpcPresence>()
                .Where(presence => !presence.HasBeenDiscarded && presence.IsMine(this) && presence.Available)
                .MinBy(presence => ParentModel.Coords.SquaredDistanceTo(presence.ParentModel.Coords), true);
            ChangeNpcPresence(newPresence);
        }
        
        [UnityEngine.Scripting.Preserve]
        void OverrideFightingStyle(EnemyBaseClass enemyBaseClass, NpcFightingStyle newFightingStyle) {
            if (FightingStyle == newFightingStyle) return;
            if (enemyBaseClass == null || enemyBaseClass.HasBeenDiscarded) return;
            
            _overridenFightingStyle = newFightingStyle;
            enemyBaseClass.RefreshFightingStyle();
        }

        public void AttachWeapon(CharacterHandBase characterHand) {
            if (Inventory.EquippedItem(EquipmentSlotType.MainHand) == characterHand.Item) {
                MainHandWeapon = characterHand;
            } else if (Inventory.EquippedItem(EquipmentSlotType.OffHand) == characterHand.Item) {
                OffHandWeapon = characterHand;
            }
        }

        public void DetachWeapon(CharacterHandBase characterHand) {
            if (MainHandWeapon == characterHand) {
                MainHandWeapon = null;
            } else if (OffHandWeapon == characterHand) {
                OffHandWeapon = null;
            }
        }
        
        public void PlayAudioClip(AliveAudioType audioType, bool asOneShot = false, params FMODParameter[] eventParams) =>
            CharacterView?.PlayAudioClip(audioType, asOneShot, Head == null ? null : Head.gameObject, eventParams);

        public void PlayAudioClip(EventReference eventReference, bool asOneShot = false, params FMODParameter[] eventParams) =>
            CharacterView?.PlayAudioClip(eventReference, asOneShot, Head == null ? null : Head.gameObject, eventParams);
        
        public void OverrideFaction(FactionTemplate faction, FactionOverrideContext context = FactionOverrideContext.Default) {
            _factionContainer.OverrideFaction(faction, context);
            this.Trigger(FactionService.Events.AntagonismChanged, this);
            RecalculateTargeting();
        }
        
        public void ResetFactionOverride(FactionOverrideContext context = FactionOverrideContext.Default) {
            _factionContainer.ResetFactionOverride(context);
            this.Trigger(FactionService.Events.AntagonismChanged, this);
            RecalculateTargeting();
        }

        void RecalculateTargeting() {
            foreach (var target in this.GetTargeting().ToList()) {
                if (target is NpcElement npc) {
                    npc.RecalculateTarget();
                }
            }
            this.RecalculateTarget();
        }

        public void SetAnimatorState(NpcFSMType fsmType, NpcStateType stateType, float? overrideCrossFadeTime = null, Action<ITransition> onNodeLoaded = null) {
            GetAnimatorSubstateMachine(fsmType).SetCurrentState(stateType, overrideCrossFadeTime, onNodeLoaded: onNodeLoaded);
        }

        public NpcAnimatorSubstateMachine GetAnimatorSubstateMachine(NpcFSMType fsmType) =>
            fsmType switch {
                NpcFSMType.GeneralFSM => Element<NpcGeneralFSM>(),
                NpcFSMType.AdditiveFSM => Element<NpcAdditiveFSM>(),
                NpcFSMType.CustomActionsFSM => Element<NpcCustomActionsFSM>(),
                NpcFSMType.TopBodyFSM => Element<NpcTopBodyFSM>(),
                NpcFSMType.OverridesFSM => Element<NpcOverridesFSM>(),
                _ => throw new ArgumentOutOfRangeException(nameof(fsmType), fsmType, null)
            };

        // === Stumbling & Ragdoll
        [UnityEngine.Scripting.Preserve]
        public void EnableRagdoll(Vector3 force, float forceModifier = 1, float durationLeft = 0, bool forceEnable = true) {
            EnemyBaseClass?.EnableRagdoll(force, forceModifier, durationLeft, forceEnable);
        }
        
        [UnityEngine.Scripting.Preserve]
        public void EnableStumble(Vector3 forceDirection, float forceModifier = 1, float durationLeft = 0, bool forceEnable = true) {
            EnemyBaseClass?.EnableStumble(forceDirection, forceModifier, durationLeft, forceEnable);
        }

        public void EnterParriedState() {
            EnemyBaseClass?.EnterParriedState();
        }
        
        public void EnterStaggerState(float? duration = null) {
            EnemyBaseClass?.EnterStagger(duration);
        }

        public void DealPoiseDamage(NpcStateType getHitType, float poiseAmount, bool isCritical, bool isDamageOverTime) {
            EnemyBaseClass?.DealPoiseDamage(getHitType, poiseAmount, isCritical, isDamageOverTime);
        }

        // === Callbacks

        void OnTakingDamage(HookResult<HealthElement, Damage> hook) {
            if (hook.Value.DamageDealer is Hero hero) {
                HeroDamageTimestamp = new HeroDamageTimestamp(hero);
            }
            
            else if (hook.Value.DamageDealer is NpcElement npc && npc.TryGetElement(out NpcHeroSummon summon)) {
                HeroDamageTimestamp = new HeroDamageTimestamp((Hero)summon.Owner);
            }
        }

        public void DieFromDamage(DamageOutcome damageOutcome) {
            _isAlive = false;
            if (NpcAI) {
                NpcAI.HeroVisibility = 0;
            }

            // Non system critical features
            try {
                DeathNonCriticalFunctions(damageOutcome);
            } catch (Exception e) {
                Log.Important?.Error("Caught exception below while following was killed: " + LogUtils.GetDebugName(ParentModel));
                Debug.LogException(e);
            }
            
            // Try provide player with loot despite exception
            if (!ParentModel.HasElement<NpcDummy>()) {
                try {
                    ParentModel.AddElement(new NpcDummy(this, _spawnedVisualPrefab, SimplifiedDeadBodyPrefab, BlockLootingDeadBody, !KeepCorpseAfterDeath));
                } catch (Exception e) {
                    Log.Important?.Error("Caught exception below while retrying to create body to loot for player: " + LogUtils.GetDebugName(ParentModel));
                    Debug.LogException(e);
                    AutoBugReporting.SendAutoReport("NPC.ElementDieFromDamage -> Loot fail", "Player was unable to get npc loot!");
                }
            }
            _spawnedVisualPrefab = null;
                
            // Required actions, if these break then systems will be in incorrect states
            try {
                DeathCriticalFunctions(damageOutcome);
            } catch (Exception e) {
                Log.Critical?.Error("Caught exception below while finalizing killing of: " + LogUtils.GetDebugName(ParentModel));
                Debug.LogException(e);
            }
            
            // a dead npc element that is not discarded will cause serious issues. If all else fails this should be attempted anyway
            this.Discard();
        }
        
        void DeathNonCriticalFunctions(DamageOutcome damageOutcome) {
            SafeGraph.Trigger(DeathUnit.Hook, this.GetMachineGO());

            DisableTargetRecalculation = true;
            if (WasLastDamageFromHero && CanRewardExp) {
                var hero = HeroDamageTimestamp.Hero;
                hero.Experience.IncreaseBy(Template.GetExpReward() * hero.HeroMultStats.KillExpMultiplier);
            }

            if (StoryOnDeath != null) {
                Story.StartStory(StoryConfig.Base(StoryOnDeath, null));
            }

            ParentModel.RemoveElementsOfType<LocationMarker>();
            ParentModel.RemoveElementsOfType<PickpocketAction>();

            DeathElement.OnDeath(damageOutcome);
            ParentModel.AddElement(new NpcDummy(this, _spawnedVisualPrefab, SimplifiedDeadBodyPrefab, BlockLootingDeadBody, !KeepCorpseAfterDeath, DeathElement.KeepBody));

            _spawnedVisualPrefab = null;
            if (!ParentModel.HasElement<NonCriminalDeathMarker>()) {
                ParentModel.AddElement(new Corpse(this, damageOutcome.Attacker));
            }
            NavMeshCuttingSetActive(false);
            DisableEyes();
            if (SalsaEmitter != null) {
                SalsaEmitter.TriggerEmotion(SalsaEmotion.Dead);
            }
            EnableKandra();
        }
        
        void DeathCriticalFunctions(DamageOutcome? damageOutcome) {
            ParentModel.MoveToDomain(Domain.CurrentScene());
            
            NpcRegistry.UnregisterNpc(this);
            NpcRegistry.MarkAsDead(ParentModel.Template);
            _inNpcRegistry = false;
            NpcPresence?.Detach(this, NpcPresenceDetachReason.Death);

            if (damageOutcome != null) {
                ((IAlive)this).CallDieEvents(damageOutcome.Value);
            }

            PossibleTargets.Clear();
            PossibleAttackers.Clear();
            NpcHistorian.NotifyStates(this, "Died");
        }
        
        void EnableEyes() {
            if (Eyes != null && !_eyesClosed) {
                Eyes.enabled = true;
                Eyes.lookTarget = Hero.Current.Head;
            }
        }
        
        void DisableEyes() {
            if (Eyes != null && !_eyesClosed) {
                Eyes.enabled = false;
                Eyes.lookTarget = null;
            }
        }
        
        public void CloseEyes() {
            if (Eyes != null) {
                Eyes.enabled = false;
                _eyesClosed = true;
                var blendShape = Eyes.blinklids[0].expData.controllerVars[0].blendIndex;
                var skinnedMeshRenderer = Eyes.blinklids[0].expData.controllerVars[0].smr;
                if (skinnedMeshRenderer == null) {
                    Log.Minor?.Error($"SkinnedMeshRenderer is null for Eyes of {ParentModel}", ParentModel?.ViewParent);
                    return;
                }
                float value = skinnedMeshRenderer.GetBlendShapeWeight(blendShape);
                _eyesTween.Kill();
                _eyesTween = DOTween.To(() => value, v => skinnedMeshRenderer.SetBlendShapeWeight(blendShape, v), 100f, 1f);
            }
        }

        public void OpenEyes(bool instant = false) {
            if (Eyes != null) {
                _eyesClosed = false;
                var blendShape = Eyes.blinklids[0].expData.controllerVars[0].blendIndex;
                var skinnedMeshRenderer = Eyes.blinklids[0].expData.controllerVars[0].smr;
                if (skinnedMeshRenderer == null) {
                    Log.Minor?.Error($"SkinnedMeshRenderer is null for Eyes of {ParentModel}", ParentModel?.ViewParent);
                    return;
                }
                float value = skinnedMeshRenderer.GetBlendShapeWeight(blendShape);
                _eyesTween.Kill();
                if (instant) {
                    skinnedMeshRenderer.SetBlendShapeWeight(blendShape, 0);
                    Eyes.enabled = true;
                } else {
                    _eyesTween = DOTween.To(() => value, v => skinnedMeshRenderer.SetBlendShapeWeight(blendShape, v), 0, 1f)
                        .OnComplete(() => Eyes.enabled = true);
                }
            }
        }

        void AddItemsToInventory() {
            if (Template.fistsTemplate?.IsSet == true) {
                AddItem(new(Template.fistsTemplate.Get<ItemTemplate>(new TemplateReference.ProxyDebugTargetSource(this, TemplateTypeFlag.System))));
            }
            foreach (var itemData in InventoryItems) {
                if (itemData.ItemTemplate == null) {
                    continue;
                }
                if (_predefinedClothes.Length > 0 && itemData.ItemTemplate.IsArmor) {
                    continue;
                }
                AddItem(new Item(itemData));
            }
            foreach (var template in _predefinedClothes) {
                AddItem(new Item(template.Get<ItemTemplate>(this), 1));
            }
            _itemsAddedToInventory = true;
            _inventoryItems = null;
            this.Trigger(Events.ItemsAddedToInventory, this);

            void AddItem(Item item) {
                if (item is not {HasBeenDiscarded: false}) {
                    Log.Minor?.Error($"Item could not be added to inventory of {LogUtils.GetDebugName(this)} because it was missing or has been discarded.");
                    return;
                }
                item = Inventory.Add(item);
                if (item is not {HasBeenDiscarded: false}) return;
                // --- Weapons are equipped in combat behaviours
                if (item.IsNPCEquippable && !item.IsWeapon) {
                    Inventory.Equip(item);
                }
            }
        }

        // === Visual loading
        public ARAsyncOperationHandle<GameObject> LoadVisual(GameObject visualInstance) {
            if (!IsVisualSet) return new ARAsyncOperationHandle<GameObject>();
            _spawnedVisualPrefab?.ReleaseInstance();

            _spawnedVisualPrefab = new ReferenceInstance<GameObject>(VisualPrefab);
            return _spawnedVisualPrefab.Instantiate(parent: visualInstance.transform.Find("Visuals"), ValidateLoadedGO);
        }

        void ValidateLoadedGO(GameObject go) {
            if (go == null) {
                _spawnedVisualPrefab?.ReleaseInstance();
                _spawnedVisualPrefab = null;
            }
        }
        
        void AfterVisualLoaded(Transform parentTransform, bool isRestoring) {
            var kandraRenderers = parentTransform.GetComponentsInChildren<KandraRenderer>(true);
            foreach (var kandraRenderer in kandraRenderers) {
                kandraRenderer.EnsureInitialized();
            }
            GatherComponentsFromVisual(parentTransform);
            _initializer.NotifyVisualLoaded();
        }

        void GatherComponentsFromVisual(Transform parentTransform) {
            Controller = parentTransform.GetComponent<NpcController>();
            Controller.Init();
            _height = Controller.RichAI.height;
            
            ParentTransform = parentTransform;
            AssignReferencesFromTags();
            
            Eyes = _spawnedVisualPrefab?.Instance.GetComponent<Eyes>();
            if (Hips == null) {
                Log.Critical?.Error($"There is no Hip in Npc prefab. Hips are required! {this}", parentTransform.gameObject);
            }
            
            if (MainHandEqSlot != null && OffHandEqSlot != null && BackEqSlot != null && BackWeaponEqSlot != null) {
                BeltItemSockets = new [] { MainHandEqSlot, OffHandEqSlot, BackWeaponEqSlot, BackEqSlot };
                CanDetachWeaponsToBelts = true;
            }
            
            VFXBodyMarker = parentTransform.GetComponentInChildren<VFXBodyMarker>();
            if (VFXBodyMarker == null) {
                Log.Critical?.Error($"There is no VFXBodyMarker in Npc prefab. VFXBodyMarker is required! {this}", parentTransform.gameObject);
            }

            VoiceOversEmitter = parentTransform.GetComponentInChildren<VoiceOversEventEmitter>();
            SalsaEmitter = VoiceOversEmitter as SalsaFmodEventEmitter;
            
            if (VoiceOversEmitter) {
                VoiceOversEmitter.SetHeadTransform(Head);
            }
            
            var handMarkers = ParentTransform.GetComponentsInChildren<AdditionalHandMarker>();
            foreach (var handMarker in handMarkers) {
                if (handMarker.Hand == AdditionalHand.AdditionalMainHand) {
                    AdditionalMainHand = handMarker.transform;
                } else if (handMarker.Hand == AdditionalHand.AdditionalOffHand) {
                    AdditionalOffHand = handMarker.transform;
                }
            }
        }
        
        static (string, Action<NpcElement, Transform>)[] s_tagToFieldAssignment = {
            ("MainHand", static (npc, transform) => npc.MainHand = transform),
            ("OffHand", static (npc, transform) => npc.OffHand = transform),
            ("Head", static (npc, transform) => npc.Head = transform),
            ("Torso", static (npc, transform) => npc.Torso = transform),
            ("Hips", static (npc, transform) => npc.Hips = transform),
            ("MainHandEqSlot", static (npc, transform) => npc.MainHandEqSlot = transform),
            ("OffHandEqSlot", static (npc, transform) => npc.OffHandEqSlot = transform),
            ("BackWeaponEqSlot", static (npc, transform) => {
                Transform offsetBackWeaponSlot = new GameObject("OffsetBackWeaponSlot").transform;
                offsetBackWeaponSlot.SetParent(transform);
                offsetBackWeaponSlot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                offsetBackWeaponSlot.localScale = Vector3.one;
                npc.BackWeaponEqSlot = offsetBackWeaponSlot;
            }),
            ("BackEqSlot", static (npc, transform) => {
                Transform offsetBackSlot = new GameObject("OffsetBackSlot").transform;
                offsetBackSlot.SetParent(transform);
                offsetBackSlot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                offsetBackSlot.localScale = Vector3.one;
                npc.BackEqSlot = offsetBackSlot;
            }),
        };

        void AssignReferencesFromTags() {
            RemovableSpan<(string, Action<NpcElement, Transform>)> tags = new(ref s_tagToFieldAssignment);
            AssignReferencesByTagsRecursively(ParentTransform, tags);
        }
        
        bool AssignReferencesByTagsRecursively(Transform parentTransform, RemovableSpan<(string, Action<NpcElement, Transform>)> tags) {
            for (int i = 0; i < parentTransform.childCount; i++) {
                Transform child = parentTransform.GetChild(i);
                string tag = child.gameObject.tag;

                for (int j = 0; j < tags.length; j++) {
                    if (tags[j].Item1 == tag) {
                        tags[j].Item2(this, child);
                        tags.RemoveAtSwapBack(j);
                        
                        if (tags.length == 0) {
                            if (!ReferenceEquals(Hips, null)) {
                                return true;
                            } 
                        }
                        break;
                    }
                }
                
                if (ReferenceEquals(Hips, null) 
                    && (child.name.Equals(HipsString, StringComparison.InvariantCultureIgnoreCase) 
                    || child.name.Equals(PelvisString, StringComparison.InvariantCultureIgnoreCase))) {
                    Hips = child;
                    if (tags.length == 0) {
                        return true;
                    }
                }

                if (AssignReferencesByTagsRecursively(child, tags)) {
                    return true;
                }
            }

            return false;
        }
        
        public void ConvertToWyrd() {
            if (WyrdConverted) {
                return;
            }

            WyrdConverted = true;
            _wyrdConversionMarker.EnableWyrdObjects();
            EnemyBaseClass?.OnWyrdConversionStarted();
            HandleWyrdEmpowerment();
            ChangeWyrdTattoos(true);
            CharacterStats.Stamina.SetToFull();
        }
        
        public void UnConvertFromWyrd() {
            if (!WyrdConverted) {
                return;
            }
            
            WyrdConverted = false;
            if (_wyrdConversionMarker != null) {
                _wyrdConversionMarker.DisableWyrdObjects();
            }
            
            HandleWyrdEmpowerment();
            ChangeWyrdTattoos(false);
            CharacterStats.Stamina.SetToFull();
        }

        public void RefreshDistanceBand(int band) {
            _currentDistanceBand = band;
            RecalculateCurrentDistanceBand();
        }
        
        public void SetTemporaryDistanceBand(int band, int frames) {
            _temporaryDistanceBandOverrides.Add(band, AsyncUtil.DelayFrame(this, frames));
        }

        void RecalculateCurrentDistanceBand() {
            if (HasBeenDiscarded) {
                return;
            }

            int band = IsHeroSummon ? 0 : _temporaryDistanceBandOverrides.TryGetMin(out var min) ? min : _currentDistanceBand;
            CurrentDistanceBand = band;

            if (ParentTransform != null) {
                if (LocationCullingGroup.InNpcVisibilityBand(band)) {
                    TurnVisibilityOn();
                } else {
                    TurnVisibilityOff();
                }
            }
            
            bool inLogicBand = LocationCullingGroup.InActiveLogicBands(band);
            if (!inLogicBand && NpcAI) {
                NpcAI.HeroVisibility = 0;
            }

            bool inCloseBand = LocationCullingGroup.InEyesEnabledBand(band);
            if (inCloseBand) {
                EnableEyes();
            } else {
                DisableEyes();
            }
            
            Controller.enabled = inLogicBand;
            Element<BodyFeatures>().RefreshDistanceBand(band);
        }

        public void DisableKandra() {
            if (IsKandraHidden) {
                return;
            }
            IsKandraHidden = true;
            foreach (var kandra in ParentTransform.GetComponentsInChildren<KandraRenderer>()) {
                if (kandra.GetComponent<VFXBodyMarker>()) {
                    continue;
                }
                kandra.enabled = false;
            }
        }

        public void EnableKandra() {
            if (!IsKandraHidden) {
                return;
            }
            foreach (var kandra in ParentTransform.GetComponentsInChildren<KandraRenderer>(true)) {
                if (kandra.GetComponent<VFXBodyMarker>()) {
                    continue;
                }
                kandra.enabled = true;
            }
            IsKandraHidden = false;
        }

        void TurnVisibilityOn() {
            if (_isVisible == true) {
                return;
            }
            _isVisible = true;
            ParentModel.Trigger(Events.BeforeNpcInVisualBand, this);
            ParentModel.SetCulled(false);

            if (!_itemsAddedToInventory) {
                AddItemsToInventory();
            } else {
                foreach (var item in _equipmentCache) {
                    if (item is { HasBeenDiscarded: false }) {
                        Inventory.Equip(item);
                    }
                }
            }
            _equipmentCache.Clear();

            Element<BodyFeatures>().Show();
            EnemyBaseClass?.AfterVisualInBand(this);
            ParentModel.Trigger(Events.AfterNpcInVisualBand, this);
            ParentModel.Trigger(Events.AfterNpcVisibilityChanged, true);
        }

        void TurnVisibilityOff() {
            if (_isVisible == false) {
                return;
            }
            _isVisible = false;
            ParentModel.Trigger(Events.BeforeNpcOutOfVisualBand, this);
            EnemyBaseClass?.BeforeOutOfVisualBand(this);
            ParentModel.SetCulled(true);
            if (_equipmentCache.Count == 0) {
                foreach (var item in Inventory.DistinctEquippedItems()) {
                    _equipmentCache.Add(item);
                    Inventory.Unequip(item);
                }
            }
            Element<BodyFeatures>().Hide();
            ParentModel.Trigger(Events.AfterNpcOutOfVisualBand, this);
            ParentModel.Trigger(Events.AfterNpcVisibilityChanged, false);
        }

        void ChangeWyrdTattoos(bool isWyrd) {
            var bodyFeatures = Element<BodyFeatures>();
            if (!isWyrd) {
                bodyFeatures.BodyTattoo = null;
                bodyFeatures.FaceTattoo = null;
                return;
            }

            if (Template.tattooType != TattooType.None) {
                var config = CommonReferences.Get.TattooConfigs.FirstOrDefault(c => c.type == Template.tattooType);
                if (!config.Equals(default)) {
                    bodyFeatures.BodyTattoo = new BodyTattooFeature(config.Copy());
                    bodyFeatures.FaceTattoo = new FaceTattooFeature(config.Copy());
                }
            }
        }

        void HandleWyrdEmpowerment() {
            WyrdEmpowermentUtil.CheckEmpowermentNeed(this, WyrdConverted);
        }
        
        // === Helpers
        public Stat Stat(StatType statType) =>
            statType switch {
                StatusStatType statusStat => statusStat.RetrieveFrom(this),
                CharacterStatType characterStat => characterStat.RetrieveFrom(this),
                AliveStatType aliveStats => aliveStats.RetrieveFrom(this),
                NpcStatType npcStats => npcStats.RetrieveFrom(this),
                _ => null
            };

        public void NavMeshCuttingSetActive(bool active) {
            NavmeshCut navMeshCut = ParentTransform.GetComponentInChildren<NavmeshCut>(true);
            if (navMeshCut != null) {
                navMeshCut.gameObject.SetActive(active);
            }
        }

        /// <summary>
        /// Toggles colliders that stays in initial interaction position and adds/removes collider attached to Hips.
        /// </summary>
        /// <param name="isInInteraction"></param>
        public void SetActiveCollidersForInteractions(bool isInInteraction) {
            NpcController controller = Controller;
            controller.AlivePrefab.SetActive(!isInInteraction && IsAlive);
            controller.forceAllowOverlapRecovery = isInInteraction;
            if (Hips == null) {
                return;
            }
            
            if (isInInteraction && _interactionCollider == null) {
                _interactionCollider = new GameObject("SimpleInteraction Collider") {
                    layer = RenderLayers.AIs
                };
                _interactionCollider.transform.SetParent(controller.transform, false);
                PositionConstraint positionConstraint = _interactionCollider.AddComponent<PositionConstraint>();
                positionConstraint.AddSource(new ConstraintSource {
                    sourceTransform = Hips,
                    weight = 1
                });
                positionConstraint.constraintActive = true;
                positionConstraint.weight = 1;
                BoxCollider col = _interactionCollider.AddComponent<BoxCollider>();
                RichAI richAI = Movement.Controller.RichAI;
                float radius = richAI.radius * InteractionColliderMultiplier;
                col.size = new Vector3(radius, richAI.height * InteractionColliderMultiplier, radius);
            } else if (!isInInteraction) {
                DestroyInteractionCollider();
            }
        }

        public void DestroyInteractionCollider() {
            if (_interactionCollider != null) {
                Object.Destroy(_interactionCollider);
                _interactionCollider = null;
            }
        }
        
        public bool IsOnSceneWithDomain(in Domain sceneDomain) {
            if (IsUnique) {
                if (Behaviours.CurrentUnwrappedInteraction is ChangeSceneInteraction changeSceneInteraction) {
                    return changeSceneInteraction.Scene.Name == sceneDomain.Name;
                } else {
                    return NpcPresence?.CurrentDomain == sceneDomain;
                }
            } else {
                return ParentModel.CurrentDomain == sceneDomain; 
            }
        }

        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public void Destroy() {
            if (HasBeenDiscarded) {
                return;
            }
            
            DeathCriticalFunctions(null);
            ParentModel.Discard();
        }

        void OnBeforeWorldSerialize() {
            SavedInteractionData = Behaviours.CurrentUnwrappedInteraction is ISavableInteraction savableInteraction ? savableInteraction.SaveData(this) : null;
        }

        public readonly struct InitializationAccessor {
            readonly NpcElement _npc;
            
            public InitializationAccessor(NpcElement npc) {
                _npc = npc;
            }

            public NpcWeaponsHandler WeaponsHandler {
                [UnityEngine.Scripting.Preserve] get => _npc.WeaponsHandler;
                set => _npc.WeaponsHandler = value;
            }
            public NpcAI NpcAI {
                [UnityEngine.Scripting.Preserve] get => _npc.NpcAI;
                set => _npc.NpcAI = value;
            }

            public ref ReferenceInstance<GameObject> SpawnedVisualPrefab => ref _npc._spawnedVisualPrefab;
            public ref NpcWyrdConversionMarker WyrdConversionMarker => ref _npc._wyrdConversionMarker;

            public void ChangeWyrdTattoos(bool isWyrd) => _npc.ChangeWyrdTattoos(isWyrd);
            public void HandleWyrdEmpowerment() => _npc.HandleWyrdEmpowerment();
        }
    }
}
