using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Graphics;
using Awaken.TG.Graphics.MapServices;
using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.AI.Barks;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.Crafting;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.WyrdStalker;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Observers;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Maps.Markers;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Main.Utility.Tags;
using Awaken.TG.Main.Utility.VFX;
using Awaken.TG.Main.Wyrdnessing.WyrdEmpowering;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Collections;
using FMODUnity;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.General.Configs {
    [Searchable(FilterOptions = SearchFilterOptions.PropertyName)]
    public class GameConstants : ScriptableObject, IService, ILocalizedSO {
        public static GameConstants Get {
            get {
                GameConstants constants = World.Services?.TryGet<GameConstants>();
#if UNITY_EDITOR
                if (constants == null) {
                    constants = UnityEditor.AssetDatabase
                                           .LoadAssetAtPath<GameConstants>("Assets/Data/Settings/ScenarioGameConstants.asset");
                }
#endif

                return constants;
            }
        }
        
        public string gameVersion;
        public SceneReference initialScene;
        
        [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(20)"), ShowInInspector] string _space_ODIN1;
        [FoldoutGroup("Application")] public string wipeSavesOnVersion;
        [FoldoutGroup("Application")] public AnimationCurve gamepadAimTiltMultiplier;

        [FoldoutGroup("Gamepad Triggers Effects")] public List<DualSenseEffectData> melee1HDualSenseEffects;
        [FoldoutGroup("Gamepad Triggers Effects")] public List<DualSenseEffectData> melee2HDualSenseEffects;
        [FoldoutGroup("Gamepad Triggers Effects")] public List<DualSenseEffectData> blockDualSenseEffects;
        [FoldoutGroup("Gamepad Triggers Effects")] public List<DualSenseEffectData> bowDualSenseEffects;
        [FoldoutGroup("Gamepad Triggers Effects")] public List<DualSenseEffectData> magicDualSenseEffects;
        
        [FoldoutGroup("Gamepad Triggers Effects")] public List<VibrationData> heavyAttackWaitXboxVibrations;
        [FoldoutGroup("Gamepad Triggers Effects")] public List<VibrationData> heavyAttackStartFirstHalfXboxVibrations;
        [FoldoutGroup("Gamepad Triggers Effects")] public List<VibrationData> heavyAttackStartSecondHalfXboxVibrations;
        [FoldoutGroup("Gamepad Triggers Effects")] public List<VibrationData> meleeAttackEnterXboxVibrations;
        [FoldoutGroup("Gamepad Triggers Effects")] public List<VibrationData> magicPerformMidCastXboxVibrations;
        [FoldoutGroup("Gamepad Triggers Effects")] public List<VibrationData> meleeEnviroHitFirstXboxVibrations;
        [FoldoutGroup("Gamepad Triggers Effects")] public List<VibrationData> meleeNpcHitXboxVibrations;
        [FoldoutGroup("Gamepad Triggers Effects")] public List<VibrationData> magicLightXboxVibrations;
        [FoldoutGroup("Gamepad Triggers Effects")] public List<VibrationData> magicHeavyXboxVibrations;
        [FoldoutGroup("Gamepad Triggers Effects")] public List<VibrationData> magicHeavyLoopXboxVibrations;
        [FoldoutGroup("Gamepad Triggers Effects")] public List<VibrationData> magicCancelCastXboxVibrations;
        [FoldoutGroup("Gamepad Triggers Effects")] public List<VibrationData> bowReleaseXboxVibrations;
        [FoldoutGroup("Gamepad Triggers Effects")] public List<VibrationData> bowPullXboxVibrations;
        [FoldoutGroup("Gamepad Triggers Effects")] public List<VibrationData> bowHoldXboxVibrations;
        [FoldoutGroup("Gamepad Triggers Effects")] public List<VibrationData> magicPerformXboxVibrations;

        [FoldoutGroup("Aim Assist")] public AimAssistData lowAssistData;
        [FoldoutGroup("Aim Assist")] public AimAssistData highAssistData;

        [FoldoutGroup("Heroes")] public int maxHeroLevel = 20;
        [FoldoutGroup("Heroes")] public int talentEveryNLevel = 1;
        [FoldoutGroup("Heroes")] public List<RPGStatParams> rpgHeroStats;
        [FoldoutGroup("Heroes")] public ArmorWeightScoreParams[] armorWeightScoreParams = Array.Empty<ArmorWeightScoreParams>();
        [FoldoutGroup("Heroes")] public Gender defaultHeroGender = Gender.Male;
        [field: FoldoutGroup("Heroes/ArmorWeight"), SerializeField] public float HeavyArmorThreshold { get; private set; } = 0.12f;
        [field: FoldoutGroup("Heroes/ArmorWeight"), SerializeField] public float MediumArmorThreshold { get; private set; } = 0.06f;
        [field: FoldoutGroup("Heroes/ArmorWeight"), SerializeField] public float LightArmorThreshold { get; private set; } = 0.03f;
        
        [FoldoutGroup("AI")] public LayerMask obstaclesMask;
        [FoldoutGroup("AI")] public float maxNpcToTargetDistanceSqr = 2500;
        [FoldoutGroup("AI")] public float heroToCloseDistanceSqr = 900;
        [FoldoutGroup("AI")] public float heroDetectionHalfExtent = 0.2f;
        [field: FoldoutGroup("AI"), SerializeField] public float HeroVisibilityCoyoteTime { get; private set; } = 1f;
        [field: FoldoutGroup("AI"), SerializeField] public float DeathNoiseDelay { get; private set; } = 0.5f;
        [field: FoldoutGroup("AI"), SerializeField] public float DeathNoiseRange { get; private set; } = 5;
        [field: FoldoutGroup("AI"), SerializeField] public float CorpsesAlertStrength { get; private set; } = 20;
        [field: FoldoutGroup("AI"), SerializeField] public float AlertEnterInformRange { get; private set; } = 7;
        [FoldoutGroup("AI")] public float npcHealthRegenerationOutsideCombat = 0.01f;
        [FoldoutGroup("AI")] public float npcHealthRegenerationUnconscious = 0.05f;
        [FoldoutGroup("AI")] public float npcRegenerationDelay = 5f;

        [FoldoutGroup("Combat")] public int evasionCap = 50;
        [FoldoutGroup("Combat"), Range(1f, 3f)] public float poiseCriticalDamageMultiplier = 1.5f;
        [FoldoutGroup("Combat")] public int maxEnemiesPerUnit = 5;
        [FoldoutGroup("Combat"), ARAssetReferenceSettings(new[] {typeof(ARHeroStateToAnimationMapping)}, group: AddressableGroup.AnimatorOverrides)]
        public ShareableARAssetReference defaultDualWieldingMainHand, defaultDualWieldingOffHand;
        [FoldoutGroup("Combat"), ARAssetReferenceSettings(new[] {typeof(ARHeroStateToAnimationMapping)}, group: AddressableGroup.AnimatorOverrides)]
        public ShareableARAssetReference defaultDualWieldingMainHandTpp, defaultDualWieldingOffHandTpp;
        [FoldoutGroup("Combat"), ARAssetReferenceSettings(new[] {typeof(ARHeroStateToAnimationMapping)}, group: AddressableGroup.AnimatorOverrides)]
        public ShareableARAssetReference defaultMeleeOffHand, defaultMeleeOffHandTpp;
        [FoldoutGroup("Combat"), TemplateType(typeof(StatusTemplate)), SerializeField] TemplateReference staminaDepletedStatusTemplate;
        [FoldoutGroup("Combat")] public LayerMask projectileHitMask;
        [FoldoutGroup("Combat")] public LayerMask defaultHeroPenetratingObstaclesMask;
        [Space]
        [FoldoutGroup("Combat")] public int heroMineLimit = 5;
        [FoldoutGroup("Combat")] public int npcMineLimit = 2;
        [FoldoutGroup("Combat")] public int npcSummonLimit = 30;
        [FoldoutGroup("Combat")] public int tooStrongEnemyLevelDiff = 10;
        [FoldoutGroup("Combat"), Range(0, 0.8f), OnValueChanged(nameof(CameraShakeWeightChanged))] public float screenShakeAnimationLayerWeight = 0.8f;
        [FoldoutGroup("Combat"), Title("Force")] public float npcForceRagdollMultiplier = 2;
        [FoldoutGroup("Combat")] public float criticalForceDamageMultiplier = 2;
        [FoldoutGroup("Combat")] public float criticalRagdollForceMultiplier = 2;
        [FoldoutGroup("Combat")] public float weakspotForceDamageMultiplier = 2;
        [FoldoutGroup("Combat")] public float weakspotRagdollForceMultiplier = 2;
        [FoldoutGroup("Combat"), Title("UsingItems"), ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)] public ShareableARAssetReference defaultHealingItem;
        [FoldoutGroup("Combat"), ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)] public ShareableARAssetReference defaultHealingVFX;
        [FoldoutGroup("Combat"), Title("Duels"), TemplateType(typeof(FactionTemplate))] public TemplateReference duelFaction;
        [FoldoutGroup("Combat"), AnimancerAnimationsAssetReference] public ARAssetReference defeatedDuelistAnimations;
        [FoldoutGroup("Combat"), Title("Stamina Regen")] public float shortStaminaRegenPreventDuration = 0.1f;
        [FoldoutGroup("Combat")] public float mediumStaminaRegenPreventDuration = 0.33f;
        [FoldoutGroup("Combat")] public float maxStaminaRegenPreventDuration = 6f;
        [FoldoutGroup("Combat")] public float mediumPreventAfterStaminaConsumed = 5f;
        [FoldoutGroup("Combat")] public float maxPreventAfterStaminaConsumed = 50f;
        [FoldoutGroup("Combat"), Title("HitStops")] public float additionalFlatHitStopDurationThatAllowsNextAttack = 0.15f;
        [FoldoutGroup("Combat"), Title("Traps")] public float spikeTrapDamageDelay = 0.1f;
        [FoldoutGroup("Combat"), Title("Hero Knockdown")] public AnimationCurve heroKnockDownForceCurve = AnimationCurve.EaseInOut(0, 1, 5, 1);
        [FoldoutGroup("Combat")] public float minDirectionalShakesStrength;
        [FoldoutGroup("Combat")] public float maxDirectionalShakesStrength;
        [FoldoutGroup("Combat"), Range(0f, 1f)] public float directionalShakesHealthCutoff;

        [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(20)"), ShowInInspector] string _space_ODIN;
        [FoldoutGroup("Gems")] public int addGemSlotCost = 1000;
        [FoldoutGroup("Gems")] public int attachGemCost = 1000;
        [FoldoutGroup("Gems")] public int retrieveGemSlotCost = 1000;
        [FoldoutGroup("Gems")] public int sharpeningBaseCost = 1000;
        [FoldoutGroup("Gems")] public int sharpeningHeroIngredientMultiplier = 2;
        
        [FoldoutGroup("Bonfire")] public int bonfireUpgradeCost;
        [FoldoutGroup("Bonfire")] public int bonfireUpgradeCostReduced;
        [FoldoutGroup("Bonfire")] [UnityEngine.Scripting.Preserve] public int bonfirePrayCost;
        [FoldoutGroup("Bonfire")] public int bonfireWyrdSkillRestoreCost;
        [FoldoutGroup("Bonfire")] public float additionalWaterDepthCheck = 0.65f;
        [FoldoutGroup("Bonfire/Talents"), TemplateType(typeof(CraftingTemplate)), SerializeField]
        TemplateReference[] bonfireCraftingUpgrades = Array.Empty<TemplateReference>();

        [FoldoutGroup("HeroProgression")] public float minutesTimestampToCountKill = 10;
        [field: FoldoutGroup("HeroProgression"), SerializeField] public float TinyEnemyExpMulti { get; private set; }
        [field: FoldoutGroup("HeroProgression"), SerializeField] public float LowEnemyExpMulti { get; private set; }
        [field: FoldoutGroup("HeroProgression"), SerializeField] public float MidEnemyExpMulti { get; private set; }
        [field: FoldoutGroup("HeroProgression"), SerializeField] public float HighEnemyExpMulti { get; private set; }
        [field: FoldoutGroup("HeroProgression"), SerializeField] public float EpicEnemyExpMulti { get; private set; }
        [field: FoldoutGroup("HeroProgression"), SerializeField] public float ProficiencyLvlHeroExpMulti { get; private set; }
        [field: FoldoutGroup("HeroProgression"), SerializeField] public float RecipeLearnExpMulti { get; private set; }
        [field: FoldoutGroup("HeroProgression"), SerializeField] public float RecipeLearnCraftingExp { get; private set; }
        [field: FoldoutGroup("HeroProgression"), SerializeField] public float FishingRodIncreasedDurabilityMultiplier { get; private set; }
        [field: FoldoutGroup("HeroProgression/WyrdSkill"), Tags(TagsCategory.Flag), SerializeField] public string[] wyrdWhisperFlags = Array.Empty<string>();
        [field: FoldoutGroup("HeroProgression/ItemRequirements"), SerializeField] public float DamageDecreasePerMissingPoint { get; private set; } = 0.025f;
        [field: FoldoutGroup("HeroProgression/ItemRequirements"), SerializeField] public float ManaCostIncreasePerMissingPoint { get; private set; } = 0.02f;
        [field: FoldoutGroup("HeroProgression/ItemRequirements"), SerializeField] public float BlockDamageReductionPerMissingPoint { get; private set; } = 0.04f;
        [field: FoldoutGroup("HeroProgression/ItemRequirements"), SerializeField] public float ArmorReductionPerMissingPoint { get; private set; } = 0.025f;
        
        [FoldoutGroup("UI")] public float questMarkerMaxDistance = 20f;
        [FoldoutGroup("UI")] public float questMarkerMinDistance = 5f;
        [FoldoutGroup("UI"), SerializeField] List<CompassMarkerData> mapMarkersList = new();
        [FoldoutGroup("UI")] public float mapGamepadMoveSpeed = 75f;
        [FoldoutGroup("UI")] public float mapGamepadScrollSpeed = 1f;
        [FoldoutGroup("UI")] public float mapGamepadSnapPower = 0.75f;
        [FoldoutGroup("UI")] public int mapMarkerFullHdSize = 56;
        [FoldoutGroup("UI"), Min(0)] public float mapZoomIn = 0.15f;
        [FoldoutGroup("UI")] public float mapZoomOut = 0.4f;
        [FoldoutGroup("UI")] public float tooltipDelay = 1.2f;
        [FoldoutGroup("Time")] public int gameStartYear, gameStartMonth, gameStartDay, gameStartHour, gameStartMinute;
        [FoldoutGroup("Time")] public float dayDurationInMinutes = 20f;
        
        [FoldoutGroup("Systems")] public NpcGridSetupData npcGrid;
        
        [FoldoutGroup("Crafting")] public float craftedItemLevelProficiencyMultiplier = 0.1f;
        [FoldoutGroup("Crafting")] public float craftingFactor = 0.03f;
        [FoldoutGroup("Crafting/Talents")] public float consumeLessAlcoholInAlchemyMultiplier = 0.5f;

        [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(20)"), ShowInInspector] string _space_ODIN2;
        
        [field: FoldoutGroup("Items"), SerializeField] public int DropPopupThreshold { get; private set; } = 5;
        [FoldoutGroup("Items"), TemplateType(typeof(LocationTemplate)), SerializeField] TemplateReference defaultItemDropPrefab;
        [FoldoutGroup("Items"), TemplateType(typeof(LocationTemplate)), SerializeField] TemplateReference defaultDeadBodyReplacedPrefab;
        [FoldoutGroup("Items"), SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.Items)] ShareableARAssetReference defaultDeadBodyReplacedVisualPrefab;
        [FoldoutGroup("Items")] public ItemUpgradeConfig defaultSharpeningConfig;
        [FoldoutGroup("Items")] public ItemUpgradeConfig defaultWeightReductionConfig;
        [FoldoutGroup("Items")] public List<ItemUpgradesPerTier> sharpeningConfigs;
        [FoldoutGroup("Items")] public List<ItemUpgradesPerTier> weightReductionConfigs;
        [FoldoutGroup("Items"), TemplateType(typeof(ItemTemplate)), SerializeField] TemplateReference magicArrowTemplate;
        [FoldoutGroup("Items"), TemplateType(typeof(StatusTemplate)), SerializeField] TemplateReference defaultItemBuffStatus;
        [FoldoutGroup("Items/Spyglass"), SerializeField] float spyglassFovMultiplier = 0.15f;
        [FoldoutGroup("Items/Spyglass"), SerializeField] float spyglassVolumeChangeSpeed = 2f;
        [FoldoutGroup("Items/Sketching"), SerializeField, TemplateType(typeof(ItemTemplate))] TemplateReference sketchItemTemplate;
        [FoldoutGroup("Items/Sketching"), SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.Items)] ShareableARAssetReference sketchVolumePrefabReference;
        [FoldoutGroup("Items/Sketching"), SerializeField] int sketchHandsRenderersTempLayer;
        [FoldoutGroup("Items/Sketching"), SerializeField] int newCameraStabilisationDelayFramesCount = 2;
        [FoldoutGroup("Items/Sketching"), SerializeField] int sketchVolumeStabilisationDelayFramesCount = 2;
        [FoldoutGroup("Items/Sketching"), SerializeField] int sketchingExposureStabilisationDelayFramesCount = 12;
        
        [FoldoutGroup("Items"), SerializeField] List<ItemLevelData> itemLevelDataList = new ();
        
        [FoldoutGroup("Shops"), SerializeField] public List<PriceMultiplierConfig> priceMultiplierConfigs = new();

        [Header("Proficiency xp per level parameters")]
        [FoldoutGroup("Proficiencies")] public float skillImproveMult;
        [FoldoutGroup("Proficiencies")] public float skillImproveMOffset;
        [Header("Per proficiency settings"), ListDrawerSettings(ShowPaging = true, NumberOfItemsPerPage = 5, ListElementLabelName = nameof(ProficiencyParams.ProficiencyName))]
        [FoldoutGroup("Proficiencies"), Searchable] public ProficiencyParams[] proficiencyParams = Array.Empty<ProficiencyParams>();

        [FoldoutGroup("Proficiencies")] public float sneakNearbyEnemiesRadius = 25f;
        
        [Title("Dialogue camera movement")]
        [FoldoutGroup("Camera"), LabelText("Max angle"), Range(0, 45), SuffixLabel("°")] public int dialogueCameraMaxAngle = 20;
        [FoldoutGroup("Camera"), LabelText("Look area size"), Range(0,50),SuffixLabel("%")] public float dialogueLookAreaSize = 1f;
        [FoldoutGroup("Camera"), LabelText("Reset delay")] public float dialogueCameraResetDelay = 2F;
        [FoldoutGroup("Camera"), LabelText("Reset speed")] public float dialogueCameraResetSpeed = 0.2F;
        [FoldoutGroup("Camera"), LabelText("Look speed multiplier in dialogues")] public float dialogueCameraLookSpeedMultiplier = 0.1f;
        [FoldoutGroup("Camera"), LabelText("Speed curve")] [UnityEngine.Scripting.Preserve] public AnimationCurve dialogueCameraSpeedCurve;
        [Title("World camera movement")]
        [FoldoutGroup("Camera"), LabelText("Look speed multiplier outside of dialogues")] public float gamepadWorldCameraLookSpeedMultiplier = 60;
        
        [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(20)"), ShowInInspector] string _space_ODIN3;
        // === SlowMo
        [FoldoutGroup("SlowMo")]
        [FoldoutGroup("SlowMo/Critical")] public float criticalSlowMoDuration = 1f;
        [FoldoutGroup("SlowMo/Critical"), Range(0.5f, 1.5f)] public float criticalSlowMoFovMultiplier = 1f;
        [FoldoutGroup("SlowMo/Critical")] public AnimationCurve criticalSlowMoCurve;
        
        [FoldoutGroup("SlowMo"), CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(10)"), ShowInInspector] string _space_ODIN4;
        
        [FoldoutGroup("SlowMo/Parry")] public float parrySlowMoDuration = 1f;
        [FoldoutGroup("SlowMo/Parry"), Range(0.5f, 1.5f)] public float parryFovMultiplier = 1f;
        [FoldoutGroup("SlowMo/Parry")] public AnimationCurve parrySlowMoCurve;
        
        [FoldoutGroup("SlowMo"), CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(10)"), ShowInInspector] string _space_ODIN5;
        
        [FoldoutGroup("SlowMo/Chonky")]
        [FoldoutGroup("SlowMo/Chonky/Critical")] public float chonkyCriticalSlowMoDuration = 1f;
        [FoldoutGroup("SlowMo/Chonky/Critical"), Range(0.5f, 1.5f)] public float chonkyCriticalSlowMoFovMultiplier = 1f;
        [FoldoutGroup("SlowMo/Chonky/Critical")] public AnimationCurve chonkyCriticalSlowMoCurve;
        
        [FoldoutGroup("SlowMo/Chonky"), CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(10)"), ShowInInspector] string _space_ODIN6;
        
        [FoldoutGroup("SlowMo/Chonky/Parry")] public float chonkyParrySlowMoDuration = 1f;
        [FoldoutGroup("SlowMo/Chonky/Parry"), Range(0.5f, 1.5f)] public float chonkyParryFovMultiplier = 1f;
        [FoldoutGroup("SlowMo/Chonky/Parry")] public AnimationCurve chonkyParrySlowMoCurve;

        public CraftingTemplate GetBonfireCraftingUpgrade(int level) => bonfireCraftingUpgrades[math.clamp(level - 1, 0, bonfireCraftingUpgrades.Length - 1)].Get<CraftingTemplate>(this);
        public ItemTemplate DefaultMagicArrowTemplate => magicArrowTemplate.Get<ItemTemplate>(this);
        public LocationTemplate DefaultItemDropPrefab => defaultItemDropPrefab.Get<LocationTemplate>(this);
        public LocationTemplate DefaultDeadBodyReplacedPrefab => defaultDeadBodyReplacedPrefab.Get<LocationTemplate>(this);
        public ARAssetReference DefaultDeadBodyReplacedVisualPrefab => defaultDeadBodyReplacedVisualPrefab.Get();
        public StatusTemplate StaminaDepletedStatusTemplate => staminaDepletedStatusTemplate.Get<StatusTemplate>(this);
        public StatusTemplate DefaultItemBuffStatus => defaultItemBuffStatus.Get<StatusTemplate>(this);
        public float SpyglassFovMultiplier => spyglassFovMultiplier;
        public float SpyglassVolumeChangeSpeed => spyglassVolumeChangeSpeed;
        public ItemTemplate SketchItemTemplate => sketchItemTemplate.Get<ItemTemplate>(this);
        public ShareableARAssetReference SketchVolumePrefabReference => sketchVolumePrefabReference;
        public int SketchHandsRenderersTempLayer => sketchHandsRenderersTempLayer;
        public int NewCameraStabilisationDelayFramesCount => newCameraStabilisationDelayFramesCount;
        public int SketchingExposureStabilisationDelayFramesCount => sketchingExposureStabilisationDelayFramesCount;
        public int SketchVolumeStabilisationDelayFramesCount => sketchVolumeStabilisationDelayFramesCount;

        // === Hero RPG Stats
        Dictionary<HeroRPGStatType, RPGStatParams> rpgStatsByType;

        public Dictionary<HeroRPGStatType, RPGStatParams> RPGStatParamsByType {
            get {
                rpgStatsByType ??= rpgHeroStats.ToDictionary(k => k.RPGStat, v => v);
                return rpgStatsByType;
            }
        }
        
        // === Item Level Affixes
        Dictionary<int, ItemLevelData> _itemLevelDatas;
        public Dictionary<int, ItemLevelData> ItemLevelDatas {
            get {
                _itemLevelDatas ??= itemLevelDataList.ToDictionary(k => k.itemLevel, v => v);
                return _itemLevelDatas;
            }
        }
        
        // === VFX
        [FoldoutGroup("VFX"), SerializeField] ItemVfxContainerWrapper defaultItemVfxContainer;
        [FoldoutGroup("VFX"), SerializeField, ARAssetReferenceSettings(new[] {typeof(GameObject)}, true, AddressableGroup.VFX)] ShareableARAssetReference defaultCriticalVFX;
        [FoldoutGroup("VFX"), SerializeField, ARAssetReferenceSettings(new[] {typeof(GameObject)}, true, AddressableGroup.VFX)] ShareableARAssetReference defaultBackStabVFX;
        [FoldoutGroup("VFX"), SerializeField, ARAssetReferenceSettings(new[] {typeof(GameObject)}, true, AddressableGroup.VFX)] ShareableARAssetReference defaultDeathVFX;
        [FoldoutGroup("VFX")] public ComputeShader uniformMeshPreparerComputeShader;
        [FoldoutGroup("VFX")] public ComputeShader uniformMeshSamplerComputeShader;
        [FoldoutGroup("VFX")] public DepthTextureStreamingParams depthTextureStreamingParams = DepthTextureStreamingParams.Default;
#if UNITY_EDITOR
        [FoldoutGroup("VFX")] public Material EDITOR_hdrpUnlitMaterial;
#endif
        public ItemVfxContainer DefaultItemVfxContainer => defaultItemVfxContainer.Data;
        public ShareableARAssetReference DefaultCriticalVFX => defaultCriticalVFX;
        public ShareableARAssetReference DefaultBackStabVFX => defaultBackStabVFX;
        public ShareableARAssetReference DefaultDeathVFX => defaultDeathVFX;
        // === Markers
        private Dictionary<CompassMarkerType, CompassMarkerData> _mapMarkersData;

        public Dictionary<CompassMarkerType, CompassMarkerData> MapMarkersData {
            get {
                _mapMarkersData ??= mapMarkersList.ToDictionary(k => k.compassMarkerType, v => v);
                return _mapMarkersData;
            }
        }

        // === Lockpicking
        [FoldoutGroup("Lockpicking")] public float pickAngleSpeed = 80f;
        [FoldoutGroup("Lockpicking")] public float lockAngleSpeed = 160f;
        [FoldoutGroup("Lockpicking")] public float lockSuccessDelay = 200;
        [FoldoutGroup("Lockpicking")] public float lockResetDelay = 300;
        [FoldoutGroup("Lockpicking")] public AnimationCurve lockMaxOpenAngleRemap = AnimationCurve.Linear(0, 1, 30, 0);
        
        // === Pickpocketing
        [FoldoutGroup("Pickpocketing")] public float minProficiencyAlertMultiplier = 0.8f;
        [FoldoutGroup("Pickpocketing")] public float maxProficiencyAlertMultiplier = 0.19f;
        [FoldoutGroup("Pickpocketing")] public float pickpocketAlertLose = 0.05f;

        [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(20)"), ShowInInspector] string _space_ODIN7;
        // === Map
        [FoldoutGroup("Map")]  [UnityEngine.Scripting.Preserve] 
        public AnimationCurve MapMarkerSizeByCameraSize = AnimationCurve.Linear(0, 1, 800, 20);
        [FoldoutGroup("Map")] public ComputeShader fogOfWarShader;
        [FoldoutGroup("Map")] public FogOfWarParams fogOfWarParams;
        // === Weather
        [field: FoldoutGroup("Weather"), SerializeField] public WeatherController.PrecipitationPreset[] WeatherPresets { get; set; }
        [field: FoldoutGroup("Weather"), SerializeField] public float HeavyRainThreshold { get; private set; } = 0.3f;
        
        [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(20)"), ShowInInspector] string _space_ODIN8;
        
        // === Wyrdness
        [FoldoutGroup("Wyrdness"), RichEnumExtends(typeof(OperationType))]  [UnityEngine.Scripting.Preserve] 
        public RichEnumReference wyrdWhispersOperationType = OperationType.Add;
        [FoldoutGroup("Wyrdness")] [UnityEngine.Scripting.Preserve] public float wyrdWhispersStatBonus = 1f;
        [field: FoldoutGroup("Wyrdness"), SerializeField] public WyrdEmpoweredStat[] WyrdEmpoweredStats { get; private set; }
        [FoldoutGroup("Wyrdness"), SerializeField, InlineProperty] public BarkConfig wyrdConvertedConfig;
        [FoldoutGroup("Wyrdness")] public LootTableWrapper wyrdConvertedFallbackLoot;
        [FoldoutGroup("Wyrdness")] public List<EnemyVariantPerHeroLevel> wyrdspawnVariants;
        [FoldoutGroup("Wyrdness/WyrdStalker")] public FloatRange wyrdStalkerSpawnRange = new(33f, 66f);
        [FoldoutGroup("Wyrdness/WyrdStalker")] public float wyrdStalkerAfterHiddenNextShowCooldown = 30;
        [FoldoutGroup("Wyrdness/WyrdStalker")] public float visibilityCoyoteTime = 2f;
        [FoldoutGroup("Wyrdness/WyrdStalker")] public float visibilityCoyoteTimeDecreaseSpeed = 3f;
        [FoldoutGroup("Wyrdness/WyrdStalker/Audio")] public FloatRange wyrdStalkerAudioRange = new(15f, 60f);
        [FoldoutGroup("Wyrdness/WyrdStalker/Audio")] public float wyrdStalkerNeverSeenMaxAudioThreshold = 0.5f;
        [FoldoutGroup("Wyrdness/WyrdStalker/Audio")] public EventReference wyrdStalkerSpawnAudioCue;
        [FoldoutGroup("Wyrdness/WyrdStalker/Audio")] public EventReference wyrdStalkerOnSightAudioCue;
        [FoldoutGroup("Wyrdness/WyrdStalker/Audio")] public EventReference wyrdStalkerHideAudioCue;
        [FoldoutGroup("Wyrdness/WyrdStalker/Passive")] public float wyrdStalkerPassiveSpawnChance = 0.1f;
        [FoldoutGroup("Wyrdness/WyrdStalker/Passive"), ShowInInspector] float SpawnChancePassivePerMinuteInWyrdness => 1f - Mathf.Pow(1f - wyrdStalkerPassiveSpawnChance, 60f / WyrdStalkerControllerBase.UnspawnedCheckInterval);
        [FoldoutGroup("Wyrdness/WyrdStalker/Passive")] public float tooLongVisibleTimeout = 3f;
        [FoldoutGroup("Wyrdness/WyrdStalker/Passive")] public float tooLongVisibleCoyoteTimeDecreaseSpeed = 5f;
        [FoldoutGroup("Wyrdness/WyrdStalker/Passive")] public FloatRange passiveWyrdStalkerAliveRangeSqr = new(12f * 12f, 100f * 100f);
        [FoldoutGroup("Wyrdness/WyrdStalker/Passive"), ShowInInspector] FloatRange PassiveWyrdStalkerAliveRange => new (Mathf.Sqrt(passiveWyrdStalkerAliveRangeSqr.min), Mathf.Sqrt(passiveWyrdStalkerAliveRangeSqr.max));
        [FoldoutGroup("Wyrdness/WyrdStalker/Active")] public float wyrdStalkerActiveSpawnChance = 0.4f;
        [FoldoutGroup("Wyrdness/WyrdStalker/Active"), ShowInInspector] float SpawnChanceActivePerMinuteInWyrdness => 1f - Mathf.Pow(1f - wyrdStalkerActiveSpawnChance, 60f / WyrdStalkerControllerBase.UnspawnedCheckInterval);
        [FoldoutGroup("Wyrdness/WyrdStalker/Active")] public StatEffect[] wyrdStalkerStatsModifierPerSoulCount = Array.Empty<StatEffect>();
        
        // === Weapon Slots
        [FoldoutGroup("WeaponSlots")] public List<BackWeaponSlotOffsetByItem> BackWeaponSlotOffset = new();
        
        void CameraShakeWeightChanged() {
            if (Application.isPlaying) {
                World.Only<ScreenShakesProactiveSetting>().Debug_SetIntensity(screenShakeAnimationLayerWeight);
            }
        }
    }
    
    [Serializable]
    public struct ItemUpgradesPerTier {
       public TierHelper.Tier tier;
       public ItemUpgradeConfig defaultConfigOfTier;
       public List<ItemUpgradePerAbstract> configsPerAbstract;
    }

    [Serializable]
    public struct ItemUpgradePerAbstract {
        [TemplateType(typeof(ItemTemplate))] public TemplateReference abstractItemTemplate;
        public ItemUpgradeConfig itemUpgradeConfig;
    }
    
    [Serializable]
    public struct BackWeaponSlotOffsetByItem {
        [SerializeField, TemplateType(typeof(ItemTemplate))]
        TemplateReference[] itemAbstracts;
        [SerializeField] Vector3 offset;
        [SerializeField] Vector3 rotation;

        // === Properties
        public IEnumerable<ItemTemplate> ItemAbstracts => itemAbstracts.Select(n => n.Get<ItemTemplate>());
        
        public bool TryGetBackSlotOffset(Item item, out Vector3 slotOffset, out Vector3 slotRotation) {
            bool abstractFulfilled = true;
            PooledList<ItemTemplate> templateAbstractTypes = item.Template.AbstractTypes;
            foreach (var itemAbstract in ItemAbstracts) {
                if (!templateAbstractTypes.value.Contains(itemAbstract)) {
                    abstractFulfilled = false;
                    break;
                }
            }
            templateAbstractTypes.Release();
            slotOffset = abstractFulfilled ? offset : Vector3.zero;
            slotRotation = abstractFulfilled ? rotation : Vector3.zero;
            return abstractFulfilled;
        }
    }
}
