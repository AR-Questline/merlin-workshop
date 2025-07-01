using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.CommonInterfaces;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.ActionLogs;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Animations.FSM.Heroes.Utils;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Fights.Mounts;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Animations;
using Awaken.TG.Main.Heroes.Audio;
using Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Crosshair;
using Awaken.TG.Main.Heroes.Development;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Heroes.Development.WyrdPowers;
using Awaken.TG.Main.Heroes.Fishing;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.TG.Main.Heroes.Items.Weapons;
using Awaken.TG.Main.Heroes.MovementSystems;
using Awaken.TG.Main.Heroes.Setup;
using Awaken.TG.Main.Heroes.Spyglass;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Controls;
using Awaken.TG.Main.Heroes.Stats.Observers;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Storage;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Heroes.VolumeCheckers;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Mobs;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.Main.Locations.Shops.Prices;
using Awaken.TG.Main.Maps.Markers;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Scenes.SceneConstructors.SceneInitialization;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Skills.Units.Effects;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Tutorials;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.Utility.VFX;
using Awaken.TG.Main.Wyrdnessing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Enums;
using Awaken.Utility.Extensions;
using Awaken.Utility.Serialization;
using Cysharp.Threading.Tasks;
using FMODUnity;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Pathfinding;
using Sirenix.Utilities;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using Compass = Awaken.TG.Main.Maps.Compasses.Compass;
using Log = Awaken.Utility.Debugging.Log;

namespace Awaken.TG.Main.Heroes {
    [SpawnsView(typeof(VHeroController), order = 0)]
    [SpawnsView(typeof(VHeroHUD), isMainView = false, order = 1)]
    [SpawnsView(typeof(VHeroKeys), isMainView = false, order = 2)]
    [Il2CppEagerStaticClassConstruction]
    public partial class Hero : Model, ICharacter, IMerchant, IAIEntity, IWithActor, IWyrdnessReactor {
        public override ushort TypeForSerialization => SavedModels.Hero;

        const string FemaleSounds = "FemaleSounds";
        const string MaleSounds = "MaleSounds";
        public override Domain DefaultDomain => Domain.Gameplay;
        public static Hero Current { get; private set; }

        // === State
        public static bool TppActive { get; set; }

        [Saved] public HeroTemplate Template { get; private set; }

        [Saved] FactionContainer _factionContainer = new();
        
        readonly VisionDetectionSetup[] _visionDetectionSetups = new VisionDetectionSetup[2];

        // -- Caches
        float _heroDetectionHalfExtent;
        CharacterStats _characterStats;
        FinisherHandlingElement _cachedFinisherHandlingElement;
        VHeroController _heroController;
        HealthElement _cachedHealthElement;
        AliveStats _cachedAliveStats;
        HeroStats _cachedHeroStats;
        HeroWyrdNight _cachedHeroWyrdNight;
        TimeDependent _cachedTimeDependent;
        GravityMarker _cachedGravityMarker;
        ToolInteractionFSM _toolInteractionFSM;
        HeroCombat _heroCombat;
        HeroItems _heroItems;
        AnimatorSharedData _animatorSharedData;

        Action _onVisualLoaded;
        
        /// <summary>
        /// Where the player currently is on the map.
        /// </summary>
        [Saved] public Vector3 Coords { get; private set; }
        public NpcChunk NpcChunk { get; set; }
        public Vector3 CoordsOnNavMesh => ClosestPointOnNavmesh.node != null ? ClosestPointOnNavmesh.position : Coords;
        public NNInfo ClosestPointOnNavmesh { get; private set; }
        [Saved] public Quaternion Rotation { get; set; }
        public bool Grounded => CachedView(ref _heroController)?.Grounded ?? true;
        public Vector3 HorizontalVelocity => CachedView(ref _heroController)?.HorizontalVelocity ?? Vector3.zero;
        public Vector2 RelativeVelocity => CachedView(ref _heroController)?.LocalVelocity ?? Vector2.zero;
        public float HorizontalSpeed => CachedView(ref _heroController)?.HorizontalSpeed ?? 0;
        public HeroControllerData Data => Template.heroControllerData;
        public CharacterGroundedData GroundedData => Template.heroGroundedData;
        public VHeroController VHeroController => CachedView(ref _heroController);
        
        public ShareableARAssetReference HitVFX => null;
        /// <summary>
        /// Hitboxes will be taken from this transform
        /// </summary>
        public Transform ParentTransform => CachedView(ref _heroController).transform;
        public Transform MainHand => CachedView(ref _heroController).MainHand;
        public Transform OffHand => CachedView(ref _heroController).OffHand;
        public Transform Head => CachedView(ref _heroController).Head;
        public Transform Torso => CachedView(ref _heroController).Torso;
        public Transform Hips => CachedView(ref _heroController).Hips;
        public Transform LeftElbow => CachedView(ref _heroController).LeftElbow;
        public VFXBodyMarker VFXBodyMarker => CachedView(ref _heroController).VFXBodyMarker;
        public HeroHandClothes HandClothes => TryGetElement<HeroHandClothes>();
        public HeroBodyClothes BodyClothes => TryGetElement<HeroBodyClothes>();
        public AnimatorSharedData AnimatorSharedData => CachedElement(ref _animatorSharedData);
        public float Radius => CachedView(ref _heroController).Controller.radius;
        public float Height => CachedView(ref _heroController).Controller.height;
        public int Tier => 0;
        
        public IWithFaction WithFaction => this;
        public Vector3 VisionDetectionOrigin => Head.position;
        public VisionDetectionSetup[] VisionDetectionSetups {
            get {
                _visionDetectionSetups[0] = new(Head.position, _heroDetectionHalfExtent, VisionDetectionTargetType.Main);
                _visionDetectionSetups[1] = new(Hips.position, 0, VisionDetectionTargetType.Additional);
                return _visionDetectionSetups;
            }
        }

        public bool JustTeleported { [UnityEngine.Scripting.Preserve] get; private set; } = true;

        // === Elements
        public CharacterStats CharacterStats => CachedElement(ref _characterStats);
        public CharacterStats.ITemplate CharacterStatsTemplate => Template;
        public StatusStats StatusStats => Element<StatusStats>();
        public StatusStats.ITemplate StatusStatsTemplate => Template;
        public ProficiencyStats ProficiencyStats => Element<ProficiencyStats>();
        public AliveStats AliveStats => CachedElement(ref _cachedAliveStats);
        public AliveStats.ITemplate AliveStatsTemplate => Template;
        public HeroMultStats HeroMultStats => Element<HeroMultStats>();
        public HeroStats HeroStats => CachedElement(ref _cachedHeroStats);
        public HeroRPGStats HeroRPGStats => Element<HeroRPGStats>();
        public MerchantStats MerchantStats => Element<MerchantStats>();
        public HeroCombat HeroCombat => CachedElement(ref _heroCombat);
        public HeroCombatSlots CombatSlots => Element<HeroCombatSlots>();
        public CharacterDealingDamage CharacterDealingDamage => Element<CharacterDealingDamage>();

        public ICharacterView CharacterView => VHeroController;
        public ICharacterSkills Skills => Element<CharacterSkills>();
        public CharacterStatuses Statuses => Element<CharacterStatuses>();
        public HeroDevelopment Development => Element<HeroDevelopment>();
        public HeroTalents Talents => Element<HeroTalents>();
        public HeroItems HeroItems => CachedElement(ref _heroItems);
        public HeroStorage Storage => Element<HeroStorage>();
        public HeroFoV FoV => Element<HeroFoV>();
        public HeroDirectionalBlur DirectionalBlur => Element<HeroDirectionalBlur>();
        public ICharacterInventory Inventory => HeroItems;
        public HeroTweaks HeroTweaks => Element<HeroTweaks>();
        public HeroDash HeroDash => Element<HeroDash>();
        public HeroWyrdNight HeroWyrdNight => CachedElement(ref _cachedHeroWyrdNight);
        public FinisherHandlingElement FinisherHandling => CachedElement(ref _cachedFinisherHandlingElement);
        public ToolInteractionFSM ToolInteractionFSM => TryGetCachedElementWithChecks(ref _toolInteractionFSM);
        public TimeDependent TimeDependent => TryGetCachedElementWithChecks(ref _cachedTimeDependent);
        public GravityMarker GravityMarker => TryGetCachedElementWithChecks(ref _cachedGravityMarker);
        HeroRagdollElement HeroRagdollElement => Element<HeroRagdollElement>();
        [CanBeNull] public HeroKnockdown HeroKnockdown => TryGetElement<HeroKnockdown>();
        
        public HeroLogicModifiers LogicModifiers { get; private set; }
        
        public Stat Stat(StatType statType) {
            return statType switch {
                ProfStatType profStats => profStats.RetrieveFrom(this),
                StatusStatType statusStat => statusStat.RetrieveFrom(this),
                CharacterStatType characterStat => characterStat.RetrieveFrom(this),
                AliveStatType aliveStats => aliveStats.RetrieveFrom(this),
                HeroRPGStatType heroBaseStatType => heroBaseStatType.RetrieveFrom(this),
                HeroStatType heroStats => heroStats.RetrieveFrom(this),
                MerchantStatType merchantStats => merchantStats.RetrieveFrom(this),
                _ => null
            };
        }

        public AliveAudio AliveAudio => TryGetElement<AliveAudio>();
        public NonSpatialVoiceOvers NonSpatialVoiceOvers => TryGetElement<NonSpatialVoiceOvers>();
        public IAIEntity AIEntity => this;
        
        // === Various states

        public bool IsSafeFromWyrdness { get; set; }
        public bool IsAlive => Health > 0;
        public bool ShouldDie => HealthElement.IsDead || Health.ModifiedValue <= 0;
        public bool IsDying => IsBeingDiscarded;
        public bool IsPortaling { get; private set; }
        public bool AllowNpcTeleport { get; set; }
        public bool IsWeaponEquipped => (MainHandItem != null || OffHandItem != null) && WeaponsVisible;
        public bool IsPhotoModeEnabled => Grounded && CanUseEquippedWeapons && !World.Any<SpyglassMask>();
        
        // Movement states
        [Saved] public bool IsCrouching { get; set; }
        [Saved] public HeroMovementSystem MovementSystem { get; private set; }
        public bool Mounted => MovementSystem.Type == MovementType.Mounted;
        public bool CanUseEquippedWeapons => MovementSystem is HumanoidMovementBase && !IsSwimming && !IsInInteractAnimation;
        public bool CanUseTeleport => MovementSystem.CanCurrentlyBeOverriden || MovementSystem.Type == MovementType.Teleport;
        public bool IsSwimming => CachedView(ref _heroController)?.IsSwimming ?? false;
        public bool IsSprinting => CachedView(ref _heroController)?.IsSprinting ?? false;
        public bool IsStoryCrouching => CachedView(ref _heroController)?.StoryCrouched ?? false;
        public bool IsUnderWater { get; private set; }
        public bool IsPerformingAction { get; private set; }

        public bool DisableTargetRecalculation {
            get => true;
            set { }
        }
        public bool CanDealDamageToFriendlies => true;
        public float RelativeForwardVelocity => RelativeVelocity.y;
        public float RelativeRightVelocity => RelativeVelocity.x;
        public bool IsInInteractAnimation => ToolInteractionFSM?.IsInInteractAnimation ?? false;
        public bool IsInToolAnimation => ToolInteractionFSM?.IsInToolAnimation ?? false;
        public bool MainViewInitialized { get; private set; }
        public bool PullingRangedWeapon => TryGetElement<BowFSM>()?.PullingRangedWeapon ?? false;
        public bool IsAnimatorInAttackState => Elements<MeleeFSM>().Any(m => m.GeneralStateType is (HeroGeneralStateType.LightAttack or HeroGeneralStateType.HeavyAttack));
        public bool IsInHitStop => Elements<MeleeFSM>().Any(m => m.IsInHitStop && m.IsLayerActive);
        public bool IsInDualHandedAttack => Element<DualHandedFSM>().IsInDualHandedAttack;
        public bool IsDualWielding => Inventory.IsDualWielding();
        public bool HasMount => OwnedMount.TryGet(out MountElement ownedMount) && ownedMount is { HasBeenDiscarded: false };
        
        // --- IItemOwner
        public IBaseClothes<IItemOwner> Clothes => TppActive ? BodyClothes : null;
        
        // === Other properties

        [Saved] UnicodeString UnicodeName { get; set; }
        [Saved] public Guid HeroID { get; private set; }
        
        public string Name {
            get => UnicodeName;
            set => UnicodeName = value;
        }
        
        public Actor Actor => DefinedActor.Hero.Retrieve();
        public Transform ActorTransform => ParentTransform;
        public Item MainHandItem => Inventory.EquippedItem(EquipmentSlotType.MainHand);
        public Item OffHandItem => Inventory.EquippedItem(EquipmentSlotType.OffHand);
        
        [Saved] public WeakModelRef<MountElement> OwnedMount { get; set; }

        // --- Audio
        public SurfaceType AudioSurfaceType => SurfaceType.HitFlesh;
        public void PlayAudioClip(AliveAudioType audioType, bool asOneShot = false, params FMODParameter[] eventParams) => CharacterView?.PlayAudioClip(audioType, asOneShot, null, eventParams);
        public void PlayAudioClip(EventReference eventReference, bool asOneShot = false, params FMODParameter[] eventParams) => CharacterView?.PlayAudioClip(eventReference, asOneShot, null, eventParams);
        // === VFX
        public AliveVfx AliveVfx => TryGetElement<AliveVfx>();
        
        // === Stats
        public HealthElement HealthElement => CachedElement(ref _cachedHealthElement);
        public LimitedStat Health => AliveStats.Health;
        public Stat HealthRegen => AliveStats.HealthRegen;
        public LimitedStat HealthStat => Health;
        public LimitedStat Stamina => CharacterStats.Stamina;
        public LimitedStat Mana => CharacterStats.Mana;
        public LimitedStat MaxManaReservation => HeroStats.MaxManaReservation;
        public LimitedStat WyrdSkillDuration => HeroStats.WyrdSkillDuration;
        public Stat StaminaRegen => CharacterStats.StaminaRegen;
        public float ManaRegen => !CharacterStats.IsInitialized
            ? 0
            : CharacterStats.ManaRegen + CharacterStats.MaxMana.ModifiedValue * CharacterStats.ManaRegenPercentage.ModifiedValue;
        public float PredictedManaRegen => !CharacterStats.IsInitialized
            ? 0
            : CharacterStats.ManaRegen.PredictedModification +
              CharacterStats.MaxMana.ModifiedValue *
              CharacterStats.ManaRegenPercentage.PredictedModification;
        public float MaxManaWithReservation => !CharacterStats.IsInitialized
            ? 0
            : MaxMana.ModifiedValue +
              MaxManaReservation.ModifiedValue;
        public LimitedStat Experience => HeroStats.XP;
        public Stat Level => CharacterStats.Level;
        public Stat MaxHealth => AliveStats.MaxHealth;
        public Stat MaxStamina => CharacterStats.MaxStamina;
        public Stat MaxMana => CharacterStats.MaxMana;
        public ItemStats BlockRelatedStats {
            get {
                var mainHandItem = Inventory.EquippedItem(EquipmentSlotType.MainHand);
                var offHandItem = Inventory.EquippedItem(EquipmentSlotType.OffHand);
                return offHandItem is {IsBlocking: true} ? offHandItem.ItemStats : mainHandItem?.ItemStats;
            }
        }

        public CurrencyStat Wealth => MerchantStats.Wealth;
        public CurrencyStat Cobweb => MerchantStats.Cobweb;

        public float TotalArmor(DamageSubType damageType) => ArmorValue();

        public bool IsBlocking => TryGetElement<HeroBlock>() != null;
        public bool IsBlinded => false;
        public bool IsEncumbered => TryGetElement<HeroEncumbered>()?.IsEncumbered ?? false;

        public bool WeaponsVisible { get; private set; }
        
        // === Fighting
        FightingPair.LeftStorage _possibleTargets;
        FightingPair.RightStorage _possibleAttackers;
        
        public ref FightingPair.LeftStorage PossibleTargets => ref _possibleTargets;
        public ref FightingPair.RightStorage PossibleAttackers => ref _possibleAttackers;
        [UnityEngine.Scripting.Preserve] public int PossibleAttackersCount => PossibleAttackers.Count();
        public bool HasArrows => Inventory.EquippedItem(EquipmentSlotType.Quiver) is {Quantity: > 0};
        
        // === Tags
        public ICollection<string> Tags => _tags;
        string[] _tags = new[] {"Type:Hero"}; // tag expected for hero

        // === Restoration
        public Func<Vector3, Vector3> ModifyRestorePosition { get; set; } = static position => position;

        // === Events

        public new static class Events {
            public static readonly Event<Hero, int> LevelUp = new(nameof(LevelUp));
            public static readonly Event<Hero, DamageOutcome> Died = new(nameof(Died));
            public static readonly Event<Hero, Hero> Revived = new(nameof(Revived));
            public static readonly Event<Hero, Hero> HeroLongTeleported = new(nameof(HeroLongTeleported));
            public static readonly Event<Hero, Hero> WalkedThroughPortal = new(nameof(WalkedThroughPortal));
            public static readonly Event<Hero, Portal> ArrivedAtPortal = new(nameof(ArrivedAtPortal));
            public static readonly Event<Hero, Hero> FastTraveled = new(nameof(FastTraveled));
            public static readonly Event<Hero, int> BeforeHeroRested = new(nameof(BeforeHeroRested));
            public static readonly Event<Hero, int> AfterHeroRested = new(nameof(AfterHeroRested));
            public static readonly Event<Hero, RegionChangedData> FactionRegionEntered = new(nameof(FactionRegionEntered));
            public static readonly Event<Hero, RegionChangedData> FactionRegionExited = new(nameof(FactionRegionExited));

            public static readonly Event<Hero, bool> HeroSlid = new(nameof(HeroSlid));
            public static readonly Event<Hero, bool> HeroJumped = new(nameof(HeroJumped));
            public static readonly Event<Hero, float> HeroLanded = new(nameof(HeroLanded));
            public static readonly Event<Hero, bool> HeroDashed = new(nameof(HeroDashed));
            public static readonly Event<Hero, bool> HeroAttacked = new(nameof(HeroAttacked));
            public static readonly Event<Hero, int> HeroFootstep = new(nameof(HeroFootstep));
            public static readonly Event<Hero, bool> SneakingStateChanged = new(nameof(SneakingStateChanged));
            public static readonly Event<Hero, bool> AfterHeroDashed = new(nameof(AfterHeroDashed));
            public static readonly Event<Hero, bool> AfterHeroPommel = new(nameof(AfterHeroPommel));
            public static readonly Event<Hero, bool> AfterHeroBackStab = new(nameof(AfterHeroBackStab));
            public static readonly Event<Hero, bool> HeroSprintingStateChanged = new(nameof(HeroSprintingStateChanged));
            public static readonly Event<Hero, bool> HeroSwimmingStateChanged = new(nameof(HeroSwimmingStateChanged));
            public static readonly Event<Hero, bool> HeroCrouchToggled = new(nameof(HeroCrouchToggled));
            public static readonly Event<Hero, bool> HeroWalkingStateChanged = new(nameof(HeroWalkingStateChanged));
            public static readonly Event<Hero, float> HeroParriedDamage = new(nameof(HeroParriedDamage));
            public static readonly Event<Hero, float> HeroBlockedDamage = new(nameof(HeroBlockedDamage));
            public static readonly Event<Hero, bool> ShowWeapons = new(nameof(ShowWeapons));
            public static readonly Event<Hero, bool> HideWeapons = new(nameof(HideWeapons));
            public static readonly Event<Hero, bool> OnWeaponBeginEquip = new(nameof(OnWeaponBeginEquip));
            public static readonly Event<Hero, bool> OnWeaponBeginUnEquip = new(nameof(OnWeaponBeginUnEquip));
            public static readonly Event<Hero, WyrdSoulFragmentType> WyrdSoulFragmentCollected = new(nameof(WyrdSoulFragmentCollected));
            public static readonly Event<Hero, bool> WyrdskillToggled = new(nameof(WyrdskillToggled));
            public static readonly Event<Hero, StatType> StatUseFail = new(nameof(StatUseFail));
            public static readonly Event<Hero, float> NotEnoughMana = new(nameof(NotEnoughMana));
            public static readonly Event<Hero, SpendManaCostUnit.ManaSpendData> ManaSpend = new(nameof(ManaSpend));
            
            public static readonly Event<Hero, VHeroController> MainViewInitialized = new(nameof(MainViewInitialized));
            public static readonly Event<Hero, bool> DashForward = new(nameof(DashForward));
            
            public static readonly Event<Hero, AnimationSpeedParams> ProcessAnimationSpeed = new(nameof(ProcessAnimationSpeed));
            public static readonly Event<Hero, bool> StopProcessingAnimationSpeed = new(nameof(StopProcessingAnimationSpeed));
            public static readonly Event<Hero, bool> ArthurMemoryReminded = new(nameof(ArthurMemoryReminded));
            public static readonly Event<Hero, Hero> HouseBought = new(nameof(HouseBought));
            
            public static readonly Event<Hero, bool> HeroPerspectiveChanged = new(nameof(HeroPerspectiveChanged));
        }

        // === Constructors

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        Hero() {
            SetupModelElements();
        }

        Hero(HeroTemplate template) {
            Template = template;
            SetupModelElements();
        }

        void SetupModelElements() {
            ModelElements.SetInitCapacity(80);
        }

        // === Initialization and lifecycle

        protected override void OnInitialize() {
            Current = this;
            TppActive = World.Any<PerspectiveSetting>()?.IsTPP ?? false;
            
            AddNotSavedElementsOnInitialize();
            AddSavedElementsOnInitialize();
            
            HeroID = Guid.NewGuid();
            InitCommon();
            
            Services.Get<SceneInitializer>().SceneInitializationHandle.OnInitialized += SetStartingPosition;
            this.ListenTo(HeroRPGStats.Events.HeroRpgStatsFullyInitialized, RefillBaseStats);
            this.AfterFullyInitialized(() => {
                Inventory.Add(new Item(CommonReferences.Get.CobwebCraftingIngredientTemplate, 0));
                HandleEAExtras(Template);
            });
            
            Log.Marking?.Warning("Initialized hero");
        }

        public void OnVisualLoaded(Action onVisualLoaded) {
            var vHeroController = VHeroController;
            if (vHeroController != null && vHeroController.BodyData != null) {
                onVisualLoaded.Invoke();
                return;
            }

            _onVisualLoaded += onVisualLoaded;
        }

        public void VisualLoaded() {
            _onVisualLoaded?.Invoke();
            _onVisualLoaded = null;
        }

        protected override void OnRestore() {
            Current = this;
            TppActive = World.Any<PerspectiveSetting>()?.IsTPP ?? false;
            
            AddNotSavedElementsOnInitialize();
            
            MovementSystem ??= AddElement<DefaultMovement>();
            InitCommon();
            
            // These are necessary so that lambda doesn't save a function pointer instead of the actual values
            Vector3 readCoords = Coords;
            Quaternion readRotation = Rotation;
            var gender = this.BodyFeatures().Gender;
            CommonReferences.RefreshLocsGender(gender);
            Hero.LoadGenderSoundBanks(gender);
            Services.Get<SceneInitializer>().SceneInitializationHandle.OnInitialized += () => RestoreStartingPosition(readCoords, readRotation);

            if (AstarPath.active != null) {
                ClosestPointOnNavmesh = AstarPath.active.GetNearest(Coords);
            }
            
            Log.Marking?.Warning("Restored hero");
        }

        protected override void OnFullyInitialized() {
            AddNotSavedElementsOnFullyInitialize();
            OnVisualLoaded(() => {
                Element<BodyFeatures>().Show();
            });
        }

        void AddNotSavedElementsOnInitialize() {
            AddElement(new HeroMarker());
            AddElement(new HeroHealthElement());
            AddElement(new CharacterDealingDamage());
            AddElement(new HeroCombatAntagonism());
            AddElement(new HeroCombat());
            AddElement(new HeroCombatSlots());
            AddElement(new HeroPushForce());
            AddElement(new HeroCameraShakes());
            AddElement(new HeroFoV());
            AddElement(new HeroDirectionalBlur());
            AddElement(new ProficiencyEventListener());
            AddElement(new TrespassingTracker());
            AddElement(new BountyTracker());
            AddElement(new HeroOxygenLevel());
            AddElement(new HeroToolAction());
            AddElement(new HeroFallDamage());
            AddElement(new NonSpatialVoiceOvers());
            AddElement(new HeroWeaponEvents());
            AddElement(new GamepadEffects());
            AddElement(new HeroOverlapRecoveryHandler());
            AddElement(new FinisherHandlingElement());
            LogicModifiers = new HeroLogicModifiers();
        }

        void AddSavedElementsOnInitialize() {
            AliveStats.Create(this);
            CharacterStats.Create(this);
            StatusStats.Create(this);
            HeroMultStats.CreateFromHeroTemplate(this);
            HeroStats.CreateFromHeroTemplate(this);
            MerchantStats.Create(this);
            HeroRPGStats.CreateFromHero(this);
            ProficiencyStats.CreateFromFightStats(this);
            
            AddElement(new BodyFeatures { Gender = GameConstants.Get.defaultHeroGender });
            AddElement(new HeroItems());
            AddElement(new HeroStorage());
            AddElement(new HeroRecipes());
            AddElement(new HeroFurnitures());
            AddElement(new HeroCaughtFish());
            AddElement(new HeroReadables());
            AddElement(new HeroTalents());
            AddElement(new HeroDevelopment());
            AddElement(new CharacterSkills());
            AddElement(new CharacterStatuses());
            AddElement(new HeroTweaks());

            AddElement(new Compass());
            AddElement(new QuestTracker());
            AddElement(new HeroStaminaUsedUpEffect());
            AddElement(new HeroHorseArmorHandler());
            _cachedHeroWyrdNight = AddElement(new HeroWyrdNight());
            
            MovementSystem = AddElement(new DefaultMovement());
        }

        void AddNotSavedElementsOnFullyInitialize() {
            AddElement(new HeroCrosshair());
            AddElement(new IllegalActionTracker());
            AddElement(new HeroHandOwner());
            AddElement(new ArmorWeight());
            AddElement(new HeroDash());
            AddElement(new DifficultyObserver());
            AddElement(new HeroRescueOnDeath());
            AddElement(new ToolsTutorials());
            AddElement(new PointMapMarker(new WeakModelRef<IGrounded>(this), () => {
                var name = Name;
                return string.IsNullOrWhiteSpace(name) ? "Hero (You)" : name;
            }, Services.Get<CommonReferences>().HeroMapMarker, MapMarkerOrder.Hero.ToInt(), true, true, true));
        }

        public void InitializeAnimatorElements(Animator animator, ARHeroAnimancer animancer) {
            TryGetElement<HeroHandClothes>()?.Discard();
            AddElement(new HeroHandClothes());

            TryGetElement<HeroBodyClothes>()?.Discard();
            AddElement(new HeroBodyClothes());

            _animatorSharedData = null;
            TryGetElement<AnimatorSharedData>()?.Discard();
            AddElement(new AnimatorSharedData());
            
            TryGetElement<OneHandedFSM>()?.Discard();
            AddElement(new OneHandedFSM(animator, animancer));
            
            TryGetElement<TwoHandedFSM>()?.Discard();
            AddElement(new TwoHandedFSM(animator, animancer));
            
            TryGetElement<DualHandedFSM>()?.Discard();
            AddElement(new DualHandedFSM(animator, animancer));
            
            TryGetElement<BowFSM>()?.Discard();
            AddElement(new BowFSM(animator, animancer));
            
            TryGetElement<MagicMainHandFSM>()?.Discard();
            AddElement(new MagicMainHandFSM(animator, animancer));
            
            TryGetElement<MagicTwoHandedFSM>()?.Discard();
            AddElement(new MagicTwoHandedFSM(animator, animancer));
            
            TryGetElement<MagicOffHandFSM>()?.Discard();
            AddElement(new MagicOffHandFSM(animator, animancer));
            
            TryGetElement<MagicMeleeOffHandFSM>()?.Discard();
            AddElement(new MagicMeleeOffHandFSM(animator, animancer));
            
            TryGetElement<ToolInteractionFSM>()?.Discard();
            AddElement(new ToolInteractionFSM(animator, animancer));
            
            TryGetElement<HeroOverridesFSM>()?.Discard();
            AddElement(new HeroOverridesFSM(animator, animancer));
            
            TryGetElement<CameraShakesFSM>()?.Discard();
            AddElement(new CameraShakesFSM(animator, animancer));
            
            TryGetElement<FishingFSM>()?.Discard();
            AddElement(new FishingFSM(animator, animancer));
            
            TryGetElement<SpyglassFSM>()?.Discard();
            AddElement(new SpyglassFSM(animator, animancer));
            
            TryGetElement<LegsFSM>()?.Discard();
            if (TppActive) {
                AddElement(new LegsFSM(animator, animancer));
            }
        }

        void SetStartingPosition() {
            Portal defaultPortalEntry = Portal.FindDefaultEntry();
            if (defaultPortalEntry == null) throw new Exception("A default entry portal was not added to the scene!");
            
            TeleportTo(defaultPortalEntry.GetDestination());
            SetMainViewInitialized();
        }

        void RestoreStartingPosition(Vector3 desiredPosition, Quaternion restoredRotation) {
            var restoredPosition = ModifyRestorePosition(desiredPosition);
            TeleportTo(restoredPosition, restoredRotation);
            SetMainViewInitialized();
        }

        void SetMainViewInitialized() {
            MainViewInitialized = true;

            var heroController = VHeroController;
            heroController.FinalInit();
            this.Trigger(Events.MainViewInitialized, heroController);
        }

        void InitCommon() {
            InitListeners();
            _factionContainer.SetDefaultFaction(World.Services.Get<FactionProvider>().Hero);
            _possibleTargets = new(this);
            _possibleAttackers = new(this);
            _heroDetectionHalfExtent = Services.Get<GameConstants>().heroDetectionHalfExtent;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            Current = null;
            _characterStats = null;
            _heroController = null;
            Hero.UnloadGenderSoundBanks();
        }
        
        void InitListeners() {
            this.ListenTo(Stats.Stat.Events.ChangingStat(HeroStatType.XP), ChangingStatXp, this);
            this.ListenTo(Stats.Stat.Events.ChangingStat(CurrencyStatType.Wealth), ChangingStatWealth, this);
            this.ListenTo(VCHeroWaterChecker.Events.WaterCollisionStateChanged, OnWaterCollisionChanged, this);
            this.ListenTo(Events.HeroLongTeleported, _ => AfterTeleport().Forget(), this);
            this.ListenTo(Events.OnWeaponBeginEquip, () => WeaponsVisible = true, this);
            this.ListenTo(Events.OnWeaponBeginUnEquip, () => WeaponsVisible = false, this);
            World.EventSystem.ListenTo(EventSelector.AnySource, LoadingScreenUI.Events.SceneInitializationStarted, this, _ => AfterTeleport().Forget());
        }
        
        // === Teleportation

        async UniTaskVoid AfterTeleport() {
            JustTeleported = true;
            await UniTask.DelayFrame(2);
            JustTeleported = false;
        }

        public void WalkThroughPortal() {
            IsPortaling = true;
            this.Trigger(Events.WalkedThroughPortal, this);
        }

        public void ArrivedAtPortal(Portal portal) {
            this.Trigger(Events.ArrivedAtPortal, portal);
            IsPortaling = false;
        }
        
        public void TeleportTo(TeleportDestination destination, Action afterTeleported = null, bool overrideTeleport = false) {
            if (TrySetTeleportMovement(out var teleportMovement)) {
                this.Trigger(GroundedEvents.TeleportRequested, this);
                teleportMovement.AssignDestinationTeleport(destination, _ => {
                    afterTeleported?.Invoke();
                    _heroController.ForceGroundTouchedTimeout();
                }, overrideTeleport);
            }
        }
        
        public void TeleportTo(Vector3 targetPosition, Quaternion? targetRotation = null, Action afterTeleported = null, bool overrideTeleport = false) {
            TeleportDestination destination = new() {
                position = targetPosition,
                Rotation = targetRotation
            };

            TeleportTo(destination, afterTeleported, overrideTeleport);
        }
        
        public void SetPortalTarget(SceneReference scene, string targetTag, Action<Portal> afterTeleported = null) {
            if (TrySetTeleportMovement(out var teleportMovement)) {
                teleportMovement.PauseTeleport();
                teleportMovement.AssignPortalTeleport(targetTag, scene, afterTeleported, true);
                World.EventSystem.LimitedListenTo(EventSelector.AnySource, LoadingScreenUI.Events.SceneInitializationEnded, teleportMovement, _ => teleportMovement.ResumeTeleport(), 1);
            }
        }
        
        public async UniTask<bool> EnsureHeroCanTeleport() {
            return await AsyncUtil.WaitUntil(this, () => CanUseTeleport);
        }

        bool TrySetTeleportMovement(out HeroTeleportMovement teleportMovement) {
            bool wasMounted = TryDismountBeforeTeleportation();
            
            if (TrySetMovementType(out teleportMovement)) {
                if (wasMounted) {
                    teleportMovement.MarkAsMountedTeleport();
                }
                return true;
            } else {
                Debug.LogException(new InvalidOperationException("Teleport request rejected, movement system is not overridable"));
                return false;
            }
        }

        bool TryDismountBeforeTeleportation() {
            if (MovementSystem is MountedMovement mountedMovement) {
                mountedMovement.Dismount();
                return true;
            }
            return false;
        }

        // === Public interface for creating heroes
        public static Hero Create(HeroTemplate template) {
            var hero = World.Add(new Hero(template));

            // we should not show x floating texts (right after hero was spawned with) build-in actions
            using var silence = World.Only<ActionLog>().SilenceMode;
            
            foreach (var skill in template.InitialSkills) {
                hero.Skills.LearnSkill(skill);
            }
                
            foreach (var data in template.initialItems) {
                var itemTemplate = data.ItemTemplate(hero);
                if (itemTemplate == null) {
                    Log.Minor?.Info($"Failed to load item template {data.itemTemplateReference.GUID} in Hero initial items");
                } else {
                    var item = World.Add(new Item(itemTemplate, data.quantity, data.ItemLvl));
                    hero.Inventory.Add(item);
                    if (item.IsEquippable && !item.HiddenOnUI) {
                        hero.Inventory.Equip(item);
                    }
                }
            }
            
            hero.OnVisualLoaded(() => hero.BodyFeatures().Show());
            return hero;
        }
        
        // === Operations
        public void DieFromDamage(DamageOutcome damageOutcome) {
            foreach (var animator in CachedView(ref _heroController).HeroAnimators) {
                animator.enabled = false;
            }

            NotifyOnDeath(damageOutcome).Forget();
        }

        public async UniTaskVoid NotifyOnDeath(DamageOutcome damageOutcome) {
            if (await AsyncUtil.DelayFrame(this)) {
                HeroRagdollElement.OnDeath(damageOutcome);
                this.Trigger(Events.Died, damageOutcome);
            }
        }

        public void Revive() {
            TryGetElement<HeroOverridesFSM>()?.SetCurrentState(HeroStateType.None);
            HealthElement.Revive();
            HeroRagdollElement.OnRevive();
            this.Trigger(Events.Revived, this);
        }
        
        void Die() {
            if (HasElement<HeroDeath>()) return;
            AddElement(new HeroDeath());
        }

        public void AttachWeapon(CharacterHandBase characterHand) {
            if (characterHand.Item.EquipmentType == EquipmentType.Bow || characterHand.Item.EquipmentType == EquipmentType.TwoHanded) {
                MainHandWeapon = characterHand;
                OffHandWeapon = characterHand;
                return;
            }
            if (HeroItems.EquippedItem(EquipmentSlotType.MainHand) == characterHand.Item) {
                MainHandWeapon = characterHand;
            }
            if (HeroItems.EquippedItem(EquipmentSlotType.OffHand) == characterHand.Item) {
                OffHandWeapon = characterHand;
            }
        }

        public void DetachWeapon(CharacterHandBase characterHand) {
            if (MainHandWeapon == characterHand) {
                MainHandWeapon = null;
            } 
            if (OffHandWeapon == characterHand) {
                OffHandWeapon = null;
            }
        }
        
        public CharacterHandBase MainHandWeapon { get; private set; }
        public CharacterHandBase OffHandWeapon { get; private set; }

        [UnityEngine.Scripting.Preserve]
        public void TogglePerformingAction(bool isPerformingAction) {
            IsPerformingAction = isPerformingAction;
        }
        
        public void RestoreStats() {
            RichEnum.AllValuesOfType<CharacterStatType>().ForEach(RestoreLimitedStatsIfLimitedByStat);
            RichEnum.AllValuesOfType<AliveStatType>().ForEach(RestoreLimitedStatsIfLimitedByStat);
            ((LimitedStat) Stat(HeroStatType.WyrdSkillDuration))?.SetToFull();
            ((LimitedStat) Stat(HeroStatType.OxygenLevel))?.SetToFull();
            
            void RestoreLimitedStatsIfLimitedByStat(StatType statType) {
                Stat stat = Stat(statType);
                if (stat is LimitedStat {UpperLimitedByStat: true} limitedStat) {
                    limitedStat.SetToFull();
                }
            }
        }

        // === Hookable effects

        void ChangingStatXp(HookResult<IWithStats, Stat.StatChange> statChange) {
            // exp multiplier
            if (statChange.Value.value > 0) {
                statChange.Value = new(statChange.Value.stat,
                    statChange.Value.value * HeroMultStats.ExpMultiplier);
            }
        }
        
        void ChangingStatWealth(HookResult<IWithStats, Stat.StatChange> statChange) {
            if (statChange.Value.context?.reason != ChangeReason.Trade && statChange.Value.value > 0) {
                statChange.Value = new(statChange.Value.stat, statChange.Value.value * HeroMultStats.WealthMultiplier);
            }
        }

        public void Show() {
            _heroController.Show();
        }

        public void Hide() {
            _heroController.Hide();
        }

        public void OnWaterCollisionChanged(bool insideWater) {
            IsUnderWater = insideWater;
        }

        // === Changing movement states
        public bool TrySetMovementType<T>(out T activeMovement, bool shouldReturnToDefault = true) where T : HeroMovementSystem, new() {
            activeMovement = MovementSystem as T;
            if (MovementSystem is T) {
                return true;
            }
            
            if (!MovementSystem.CanCurrentlyBeOverriden) {
                return false;
            }

            if (new T() is not {RequirementsFulfilled: true} newMovementType) {
                if (shouldReturnToDefault) {
                    ReturnToDefaultMovement();
                }
                return false;
            }

            MovementSystem?.Discard();

            MovementSystem = activeMovement = newMovementType;
            
            AddElement(MovementSystem);
            MovementSystem.Init(CachedView(ref _heroController));

            Log.Debug?.Info($"Changed movement system to {MovementSystem.Type.ToStringFast()}", logOption: LogOption.NoStacktrace);
            this.Trigger(HeroMovementSystem.Events.MovementSystemChanged(MovementSystem.Type), MovementSystem);
            return true;
        }
        
        public bool TrySetMovementType<T>(bool shouldReturnToDefault = true) where T : HeroMovementSystem, new() => TrySetMovementType<T>(out _, shouldReturnToDefault);
        
        public void ReturnToDefaultMovement() {
            if (MovementSystem is DefaultMovement) return;
            MovementSystem?.Discard();
            MovementSystem = AddElement<DefaultMovement>();
            MovementSystem.Init(CachedView(ref _heroController));
            
            Log.Debug?.Info($"Changed movement system to {MovementSystem.Type.ToStringFast()}", logOption: LogOption.NoStacktrace);
            this.Trigger(HeroMovementSystem.Events.MovementSystemChanged(MovementSystem.Type), MovementSystem);
        }
        
        [UnityEngine.Scripting.Preserve] public bool HasMovementType<T>() where T : HeroMovementSystem => MovementSystem is T;
        public bool HasMovementType(MovementType movementType) => MovementSystem.Type == movementType;

        // === Movement
        public void MoveTo(Vector3 newCoords) {
            if (newCoords != Coords) {
                Coords = newCoords;
                HeroPosition.Value = newCoords;
                if (AstarPath.active) {
                    ClosestPointOnNavmesh = AstarPath.active.GetNearest(newCoords);
                }
                this.Trigger(GroundedEvents.AfterMoved, this);
                this.Trigger(GroundedEvents.AfterMovedToPosition, newCoords);
            }
        }

        public void CheckForDeath() {
            // hero fighter needs to die first
            if (!UIStateStack.Instance.State.IsMapInteractive) return;
            
            if (ShouldDie) {
                Die();
            }
        }

        public void EnterParriedState() { }
        
        // === Stat Calculation
        public float ArmorValue() {
            float armorMultiplier = AliveStats.ArmorMultiplier;
            float armor = AliveStats.Armor;
            float armorFromItems = EquipmentSlotType.Armors.Sum(slot => {
                Item item = Inventory.EquippedItem(slot);
                return ItemRequirementsUtils.GetArmorAfterReduction(this, item);
            });
            return (armor + armorFromItems) * armorMultiplier;
        }

        // === IMerchant
        public IPriceProvider SellPriceProviderFor(Item item) {
            return new HeroPriceProvider(this);
        }

        public void RefillBaseStats() {
            ContractContext context = new(this, this, ChangeReason.LevelUp);
            Health.SetToFull(context);
            Stamina.SetToFull(context);
            Mana.SetToFull(context);
        }

        public void CallMount() {
            if (Mounted || !OwnedMount.TryGet(out MountElement ownedMount) || ownedMount is {HasBeenDiscarded: true} || !World.Services.Get<SceneService>().IsOpenWorld) {
                return;
            }

            ownedMount.View<VMount>().StartSeekingPlayer();
        }
        
        // === Utils
        
        public static void LoadGenderSoundBanks(Gender gender) {
            if (gender == Gender.Female) {
                FmodRuntimeManagerUtils.LoadSoundBanksAsyncAndForget(FemaleSounds);
            } else {
                FmodRuntimeManagerUtils.LoadSoundBanksAsyncAndForget(MaleSounds);
            }
        }

        public static void UnloadGenderSoundBanks() {
            FmodRuntimeManagerUtils.UnloadSoundBanks(FemaleSounds, MaleSounds);
        }

        static void HandleEAExtras(HeroTemplate template) {
#if !DISABLESTEAMWORKS && !UNITY_GAMECORE && !MICROSOFT_GAME_CORE && !UNITY_PS5
            if (template == null) {
                Log.Important?.Error("Hero template is null");
                return;
            }
            var hero = Hero.Current;
            if (PlatformUtils.IsSteamInitialized) {
                // if (HeathenEngineering.SteamworksIntegration.API.App.Client.GetEarliestPurchaseTime(HeathenEngineering.SteamworksIntegration.API.App.Id) < template.bonusItemsDate) {
                //     foreach (var data in template.bonusItems) {
                //         var itemTemplate = data.ItemTemplate(hero);
                //         if (itemTemplate == null) {
                //             Log.Minor?.Error($"Failed to load item template {data.itemTemplateReference.GUID} in Hero bonus items");
                //         } else {
                //             var item = World.Add(new Item(itemTemplate, data.quantity, data.ItemLvl));
                //             hero.Inventory.Add(item);
                //         }
                //     }
                // }
            }
#endif
        }

        public CrimeOwners GetCurrentCrimeOwnersFor(CrimeArchetype crime) => default;
        CrimeOwnerTemplate ICharacter.DefaultCrimeOwner => null;
        // === IWithFaction
        public Faction Faction => _factionContainer.Faction;
        public FactionTemplate GetFactionTemplateForSummon() => _factionContainer.GetFactionTemplateForSummon();
        public void OverrideFaction(FactionTemplate faction, FactionOverrideContext context = FactionOverrideContext.Default) => _factionContainer.OverrideFaction(faction, context);
        public void ResetFactionOverride(FactionOverrideContext context = FactionOverrideContext.Default) => _factionContainer.ResetFactionOverride(context);
    }
}
