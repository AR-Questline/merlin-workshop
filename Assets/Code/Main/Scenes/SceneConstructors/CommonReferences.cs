using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Kandra.AnimationPostProcessing;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.Saturation;
using Awaken.TG.Graphics.Scene;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.Main.AudioSystem.Notifications;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.FastTravel;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.CharacterCreators.PresetSelection;
using Awaken.TG.Main.Heroes.Development;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Heroes.Development.WyrdPowers;
using Awaken.TG.Main.Heroes.Fishing;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Heroes.Stats.Observers;
using Awaken.TG.Main.Heroes.Stats.StatConfig;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Maps.Markers;
using Awaken.TG.Main.Memories.Journal;
using Awaken.TG.Main.SocialServices;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Tutorials;
using Awaken.TG.Main.Utility.Animations.FightingStyles;
using Awaken.TG.Main.Utility.Animations.Gestures;
using Awaken.TG.Main.Utility.InputToText;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.Serialization;
using UnityEngine.TextCore.Text;

namespace Awaken.TG.Main.Scenes.SceneConstructors {
    [Searchable(FilterOptions = SearchFilterOptions.PropertyName)]
    public class CommonReferences : MonoBehaviour, IService {
        
        // === Public API
        public static CommonReferences Get {
            get {
                CommonReferences references = World.Services?.TryGet<CommonReferences>();
#if UNITY_EDITOR
                if (references == null) {
                    references = UnityEditor.AssetDatabase
                        .LoadAssetAtPath<GameObject>("Assets/Data/Settings/CommonReferences.prefab")
                        .GetComponent<CommonReferences>();
                }
#endif

                return references;
            }
        }
        
        public void RegisterServices() {
            World.Services.Register(inputToTextMapping);
            World.Services.Register(keyMapping).Init();
            World.Services.Register(audioCore);
            World.Services.Register(notificationsAudioService);
            World.Services.Register(outsideFoVAttacksService);
        }

        public void InitGameplay() {
            if (_genderGestures != null) return;
            _genderGestures = new Dictionary<Gender, GestureOverridesTemplate> {
                {Gender.Male, ManGestures},
                {Gender.Female, WomanGestures}
            };
        }

        public void LateInit() {
            World.Services.Register(presenceTrackerService).Init();
        }
        
        [field:SerializeField] public TemplateService TemplateService { get; private set; }
        
        // === Fields
        [SerializeField, FoldoutGroup("Heroes"), TemplateType(typeof(ItemTemplate))] TemplateReference mainHandFistsTemplate;
        [SerializeField, FoldoutGroup("Heroes"), TemplateType(typeof(ItemTemplate))] TemplateReference offHandFistsTemplate;
        [SerializeField, FoldoutGroup("Heroes"), TemplateType(typeof(StatusTemplate))] TemplateReference finisherStatusTemplate;
        [SerializeField, FoldoutGroup("Heroes"), TemplateType(typeof(FactionTemplate))] TemplateReference invisibleHeroFaction;
        [SerializeField, FoldoutGroup("Heroes"), TemplateType(typeof(StatusTemplate))] TemplateReference wyrdNightStatusTemplate;
        [SerializeField, FoldoutGroup("Heroes")] public PresetSelectorConfig presetSelectorConfig;
        [SerializeField, FoldoutGroup("Heroes/WyrdArthur"), TemplateType(typeof(TalentTreeTemplate))] TemplateReference wyrdArthurTalentTreeTemplate;
        [SerializeField, FoldoutGroup("Heroes/WyrdArthur")] WyrdSoulFragment[] soulFragments = Array.Empty<WyrdSoulFragment>();
        
        [SerializeField, FoldoutGroup("HeroProgression")] HeroExpPerLevelSchema heroExpPerLevelSchema;
        [SerializeField, FoldoutGroup("HeroProgression"), TemplateType(typeof(JournalTemplate))] TemplateReference journal;
        
        [FormerlySerializedAs("inputActionsMapping")] [SerializeField, FoldoutGroup("UI")] InputToTextMapping inputToTextMapping;
        [SerializeField, FoldoutGroup("UI")] UIKeyMapping keyMapping;
        [Space(10f)]
        [SerializeField, FoldoutGroup("UI")] QuestMarkerData questMainCompassMarkerData;
        [SerializeField, FoldoutGroup("UI")] QuestMarkerData questSideCompassMarkerData;
        [SerializeField, FoldoutGroup("UI")] QuestMarkerData questOtherCompassMarkerData;
        [SerializeField, FoldoutGroup("UI")] QuestMarkerData quest3DMarkerData;
        [SerializeField, FoldoutGroup("UI")] MarkerData heroMapMarkerData;
        [SerializeField, FoldoutGroup("UI"), UIAssetReference] ShareableSpriteReference exitDialogIcon;
        [SerializeField, FoldoutGroup("UI"), UIAssetReference] ShareableSpriteReference shopDialogIcon;
        [SerializeField, FoldoutGroup("UI"), TemplateType(typeof(LocationTemplate))] TemplateReference customMarkerReference;
        [SerializeField, FoldoutGroup("UI"), TemplateType(typeof(LocationTemplate))] TemplateReference spyglassMarkerReference;
        [SerializeField, FoldoutGroup("UI"), UIAssetReference] ShareableSpriteReference fastTravelIcon;

        [SerializeField, FoldoutGroup("UI"), ARAssetReferenceSettings(new[] {typeof(SpriteAsset)})] ShareableARAssetReference keyIconsSpriteAssetPCReference;
        [SerializeField, FoldoutGroup("UI"), ARAssetReferenceSettings(new[] {typeof(SpriteAsset)})] ShareableARAssetReference keyIconsSpriteAssetPSReference;
        [SerializeField, FoldoutGroup("UI"), ARAssetReferenceSettings(new[] {typeof(SpriteAsset)})] ShareableARAssetReference keyIconsSpriteAssetXboxReference;

        [SerializeField, FoldoutGroup("Localization")]
        Locale[] nonAlphabetLanguages = Array.Empty<Locale>();
        
        [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(20)"), ShowInInspector] string _space_ODIN;

        [SerializeField, FoldoutGroup("Systems")] public AchievementsReferences achievementsReferences;
        [SerializeField, FoldoutGroup("Systems")] StatDefinedValuesConfig statValuesConfig;
        [SerializeField, FoldoutGroup("Systems")] SceneConfigs sceneConfigs;
        [SerializeField, FoldoutGroup("Systems")] VariablesGroupAsset localizationVariables;
        [SerializeField, FoldoutGroup("Systems")] SceneReference campaignReference;
        [SerializeField, FoldoutGroup("Systems")] TutorialConfig tutorialConfig;
        [SerializeField, FoldoutGroup("Systems")] MapData mapData;

        [SerializeField, FoldoutGroup("DLC")] DlcId[] horseDlcIds = Array.Empty<DlcId>();
        [SerializeField, FoldoutGroup("DLC")] ItemSpawningData horseDlcItem;
        [SerializeField, FoldoutGroup("DLC")] DlcId supportersPackDlcId;

        [SerializeField, FoldoutGroup("Crafting"), TemplateType(typeof(ItemTemplate))] TemplateReference handCraftingGarbageItemTemplateRef;
        [SerializeField, FoldoutGroup("Crafting"), TemplateType(typeof(ItemTemplate))] TemplateReference alchemyGarbageItemTemplateRef;

        [SerializeField, FoldoutGroup("Housing"), ARAssetReferenceSettings(new[] { typeof(GameObject) })]
        public ShareableARAssetReference furnitureSlotAction;

        [SerializeField, FoldoutGroup("Audio")] AudioConfig audioConfig;
        [SerializeField, FoldoutGroup("Audio")] AudioCore audioCore;
        [SerializeField, FoldoutGroup("Audio")] NotificationsAudioService notificationsAudioService;
        [SerializeField, FoldoutGroup("Audio")] OutsideFoVAttacksService outsideFoVAttacksService;
        [SerializeField, FoldoutGroup("Audio")] ARFmodEventEmitter promptAudioEmitter;
        
        [FoldoutGroup("Graphics"), ARAssetReferenceSettings(new []{typeof(Material)}, group: AddressableGroup.VFX)] public ShareableARAssetReference defaultGhostMaterial;
        [FoldoutGroup("Graphics"), ARAssetReferenceSettings(new []{typeof(Material)}, group: AddressableGroup.VFX)] public ShareableARAssetReference[] defaultGhostHeadMaterials = Array.Empty<ShareableARAssetReference>();
        [FoldoutGroup("Graphics"), ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)] public ShareableARAssetReference defaultGhostVfx;
        [SerializeField, FoldoutGroup("Graphics")] SaturationReferences saturationReferences;
        
        [SerializeField, FoldoutGroup("Gestures & Emotions"), TemplateType(typeof(GestureOverridesTemplate))] TemplateReference manGestures;
        [SerializeField, FoldoutGroup("Gestures & Emotions"), TemplateType(typeof(GestureOverridesTemplate))] TemplateReference womanGestures;
        
        [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(20)"), ShowInInspector] string _space_ODIN1;
        
        [SerializeField, FoldoutGroup("Weapons"), ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.Weapons)] ShareableARAssetReference arrowPrefab;
        [SerializeField, FoldoutGroup("Weapons"), ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.Weapons)] ShareableARAssetReference arrowLogicPrefab;
        [SerializeField, FoldoutGroup("Weapons"), ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.Weapons)] ShareableARAssetReference weaponTrail;
        [SerializeField, FoldoutGroup("Weapons")] AnimationCurve defaultWeaponCurve;
        [SerializeField, FoldoutGroup("Weapons")] AnimationCurve default1HWeaponCurve;
        [SerializeField, FoldoutGroup("Weapons")] AnimationCurve default2HWeaponCurve;
        [SerializeField, FoldoutGroup("Weapons"), TemplateType(typeof(LocationTemplate))] TemplateReference testDummy;

        [SerializeField, FoldoutGroup("Armors"), TemplateType(typeof(StatusTemplate))] TemplateReference lightArmorStatus;
        [SerializeField, FoldoutGroup("Armors"), TemplateType(typeof(StatusTemplate))] TemplateReference mediumArmorStatus;
        [SerializeField, FoldoutGroup("Armors"), TemplateType(typeof(StatusTemplate))] TemplateReference heavyArmorStatus;
        [SerializeField, FoldoutGroup("Armors"), TemplateType(typeof(StatusTemplate))] TemplateReference overloadArmorStatus;
        
        [SerializeField, FoldoutGroup("Dashes"), TemplateType(typeof(StatusTemplate))] TemplateReference dashPersistentOptimalStatus;
        [SerializeField, FoldoutGroup("Dashes"), TemplateType(typeof(StatusTemplate))] TemplateReference dashOptimalStatus;
        [SerializeField, FoldoutGroup("Dashes"), TemplateType(typeof(StatusTemplate))] TemplateReference dashExhaustStatus;
        
        [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(20)"), ShowInInspector] string _space_ODIN2;
        
        [FoldoutGroup("Cutscenes"), ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.NPCs)] public ShareableARAssetReference stealthKillCameraPrefab;
        [FoldoutGroup("Cutscenes"), ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.NPCs)] public ShareableARAssetReference cameraCutscenePrefab;
        [FoldoutGroup("Cutscenes"), ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.NPCs)] public ShareableARAssetReference maleCutscenePrefab;
        [FoldoutGroup("Cutscenes"), ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.NPCs)] public ShareableARAssetReference femaleCutscenePrefab;
        
        [FoldoutGroup("Proficiencies"), SerializeField] ProfAbstractRefs[] proficiencyAbstractRefs = Array.Empty<ProfAbstractRefs>();

        [FoldoutGroup("Proficiencies"), UIAssetReference]
        public ShareableSpriteReference oneHandedIcon,
            twoHandedIcon,
            unarmedIcon,
            shieldIcon,
            athleticsIcon,
            lightArmorIcon,
            mediumArmorIcon,
            heavyArmorIcon,
            archeryIcon,
            evasionIcon,
            acrobaticsIcon,
            sneakIcon,
            theftIcon,
            magicIcon,
            alchemyIcon,
            cookingIcon,
            handcraftingIcon;

        [FoldoutGroup("Statistics"), UIAssetReference]
        public ShareableSpriteReference strengthIcon,
            enduranceIcon,
            dexterityIcon,
            spiritualityIcon,
            practicalityIcon,
            perceptionIcon;
        
        [FoldoutGroup("Pickable Arrows"), SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.Locations)]
        ShareableARAssetReference environmentArrowVisualPrefab;
        
        [FoldoutGroup("Pickable Arrows"), SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.Locations)]
        ShareableARAssetReference environmentArrowVisualPrefabWithCollisions;
        
        [FoldoutGroup("Pickable Arrows")] [SerializeField] [TemplateType(typeof(LocationTemplate))]
        TemplateReference environmentArrowLocationTemplate;
        
        [FoldoutGroup("Pickable Arrows")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference brokenArrowItemTemplate;

        [SerializeField, FoldoutGroup("Crimes"), TemplateType(typeof(StatusTemplate))] TemplateReference jailStatusTemplate;
        
        [FoldoutGroup("Items")] [SerializeField] [TemplateType(typeof(ItemTemplate))] TemplateReference unidentifiedItemTemplate;
        [FoldoutGroup("Items")] [SerializeField] [TemplateType(typeof(ItemTemplate))] TemplateReference coinItemTemplate;
        [FoldoutGroup("Items")] [SerializeField] [TemplateType(typeof(ItemTemplate))] TemplateReference cobwebItemTemplate;
        [FoldoutGroup("Items")] [SerializeField] [TemplateType(typeof(ItemTemplate))] TemplateReference cobwebCraftingIngredientTemplate;
        [FoldoutGroup("Items")] [SerializeField] ItemSpawningData bonfire;
        [FoldoutGroup("Items")] [SerializeField] [TemplateType(typeof(ItemTemplate))] TemplateReference icarusRing;

        [field: FoldoutGroup("Items"), SerializeField, TemplateType(typeof(StatusTemplate))] 
        public TemplateReference OverEncumbranceStatus { get; private set; }
        
        [FoldoutGroup("Fishing"), SerializeField] FishingData fishingData;
        [FoldoutGroup("Fishing"), SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)] public ShareableARAssetReference waterSplashingVfx;
        
        Dictionary<Gender, GestureOverridesTemplate> _genderGestures;

        [FoldoutGroup("Visuals"), SerializeField] Material noRenderMaterial;
        [FoldoutGroup("Visuals"), SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        public ShareableARAssetReference deadBodyHighlightVfx;
        
        [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(20)"), ShowInInspector] string _space_ODIN3;
        
        [FoldoutGroup("Locations"), SerializeField, TemplateType(typeof(LocationTemplate))] TemplateReference emptyLocationTemplate;
        
        [field: FoldoutGroup("Locations"), SerializeField, TemplateType(typeof(LocationTemplate))] 
        public TemplateReference WyrdSphereVoidTemplate { get; private set; }
        [FoldoutGroup("Locations"), SerializeField]
        PresenceTrackerService presenceTrackerService;
        
        [field: FoldoutGroup("Wyrdness"), SerializeField, TemplateType(typeof(NpcFightingStyle))] public TemplateReference HumanoidWyrdFightStyle { [UnityEngine.Scripting.Preserve] get; private set; }
        [field: FoldoutGroup("Wyrdness"), SerializeField, TemplateType(typeof(NpcFightingStyle))] public TemplateReference CustomWyrdFightStyle1H { [UnityEngine.Scripting.Preserve] get; private set; }
        [field: FoldoutGroup("Wyrdness"), SerializeField, TemplateType(typeof(NpcFightingStyle))] public TemplateReference CustomWyrdFightStyle2H { [UnityEngine.Scripting.Preserve] get; private set; }
        [field: FoldoutGroup("Wyrdness"), SerializeField] public TattooConfig[] TattooConfigs { get; private set; }
        [field: FoldoutGroup("Wyrdness"), SerializeField, TemplateType(typeof(LocationTemplate))] public TemplateReference WyrdSpawnTemplate { [UnityEngine.Scripting.Preserve] get; private set; }
        [field: FoldoutGroup("Wyrdness"), SerializeField, TemplateType(typeof(LocationTemplate))] public TemplateReference WyrdStalkerPassiveTemplate { get; private set; }
        [field: FoldoutGroup("Wyrdness"), SerializeField, TemplateType(typeof(LocationTemplate))] public TemplateReference[] WyrdStalkerActiveTemplates { get; private set; }
        [FoldoutGroup("Wyrdness"), SerializeField, ARAssetReferenceSettings(new[] { typeof(GameObject) }, group: AddressableGroup.VFX)]
        public ShareableARAssetReference wyrdStalkerShowVFX;
        [FoldoutGroup("Wyrdness"), SerializeField, ARAssetReferenceSettings(new[] { typeof(GameObject) }, group: AddressableGroup.VFX)]
        public ShareableARAssetReference wyrdStalkerHideVFX;

        [FoldoutGroup("HeroCutoffHand"), SerializeField, TemplateType(typeof(ItemTemplate))] public TemplateReference handCutOffItemTemplate;
        [FoldoutGroup("HeroCutoffHand"), SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.Weapons)] 
        public ShareableARAssetReference cutOffHand;
        [FoldoutGroup("HeroCutoffHand"), SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)] 
        public ShareableARAssetReference cutOffHandVFX;
        [FoldoutGroup("HeroCutoffHand")]
        public AnimationPostProcessingPreset noLeftArmPP;
        
        [Title("FPP")]
        [FoldoutGroup("HeroAnimatorMasks"), SerializeField] AvatarMask heroMainHandMask;
        [FoldoutGroup("HeroAnimatorMasks"), SerializeField] AvatarMask heroOffHandMask;
        [FoldoutGroup("HeroAnimatorMasks"), SerializeField] AvatarMask heroBothHandsMask;
        [FoldoutGroup("HeroAnimatorMasks"), SerializeField] AvatarMask heroHeadOnlyMask;
        
        [Title("TPP")]
        [FoldoutGroup("HeroAnimatorMasks"), SerializeField] AvatarMask tppHeroMainHandMask;
        [FoldoutGroup("HeroAnimatorMasks"), SerializeField] AvatarMask tppHeroMainHandActiveMask;
        [FoldoutGroup("HeroAnimatorMasks"), SerializeField] AvatarMask tppHeroOffHandMask;
        [FoldoutGroup("HeroAnimatorMasks"), SerializeField] AvatarMask tppHeroOffHandActiveMask;
        [FoldoutGroup("HeroAnimatorMasks"), SerializeField] AvatarMask tppHeroBothHandsMask;
        [FoldoutGroup("HeroAnimatorMasks"), SerializeField] AvatarMask tppHeroLegsMask;
        [FoldoutGroup("HeroAnimatorMasks"), SerializeField] AvatarMask tppCameraShakesMask;
        
        [Title("Shared")]
        [FoldoutGroup("HeroAnimatorMasks")] public AvatarMask heroEmptyMask;
        [FoldoutGroup("HeroAnimatorMasks")] public AvatarMask wholeBodyMask;
        
        // === RedDeath
        [SerializeField, FoldoutGroup("RedDeathMaterials"), MaterialAssetReference] ShareableARAssetReference maleRedDeathBody;
        [SerializeField, FoldoutGroup("RedDeathMaterials"), MaterialAssetReference] ShareableARAssetReference maleRedDeathFace;
        [SerializeField, FoldoutGroup("RedDeathMaterials"), MaterialAssetReference] ShareableARAssetReference femaleRedDeathBody;
        [SerializeField, FoldoutGroup("RedDeathMaterials"), MaterialAssetReference] ShareableARAssetReference femaleRedDeathFace;
        
        // === Properties
        
        // --- Heroes
        public ItemTemplate DefaultMainHandFistsTemplate => mainHandFistsTemplate?.Get<ItemTemplate>(this);
        public ItemTemplate DefaultOffHandFistsTemplate => offHandFistsTemplate?.Get<ItemTemplate>(this);
        [UnityEngine.Scripting.Preserve] public TalentTreeTemplate WyrdArthurTalentTreeTemplate => wyrdArthurTalentTreeTemplate?.Get<TalentTreeTemplate>(this);
        
        [UnityEngine.Scripting.Preserve] public StatusTemplate FinisherStatusTemplate => finisherStatusTemplate?.Get<StatusTemplate>(this);
        public FactionTemplate InvisibleHeroFaction => invisibleHeroFaction?.Get<FactionTemplate>(this);
        public StatusTemplate WyrdNightStatus => wyrdNightStatusTemplate?.Get<StatusTemplate>(this);
        public IEnumerable<WyrdSoulFragment> WyrdSoulFragments => soulFragments; 
        public WyrdSoulFragment GetWyrdSoulFragment(WyrdSoulFragmentType type) => soulFragments.FirstOrDefault(f => f.fragmentType == type);
        
        // --- HeroProgression
        public HeroExpPerLevelSchema HeroExpPerLevelSchema => heroExpPerLevelSchema;
        public JournalTemplate Journal => journal.Get<JournalTemplate>(this);
        
        // --- UI
        public QuestMarkerData QuestMainCompassMarkerData => questMainCompassMarkerData;
        public QuestMarkerData QuestSideCompassMarkerData => questSideCompassMarkerData;
        public QuestMarkerData QuestOtherCompassMarkerData => questOtherCompassMarkerData;
        public QuestMarkerData Quest3DMarkerData => quest3DMarkerData;
        public MarkerData HeroMapMarker => heroMapMarkerData;
        public ShareableSpriteReference ExitDialogIcon => exitDialogIcon;
        public ShareableSpriteReference ShopDialogIcon => shopDialogIcon;
        public ShareableSpriteReference FastTravelIcon => fastTravelIcon;
        public LocationTemplate CustomMarkerTemplate => customMarkerReference.Get<LocationTemplate>(this);
        [UnityEngine.Scripting.Preserve] public LocationTemplate EmptyLocationTemplate => emptyLocationTemplate.Get<LocationTemplate>(this);
        public LocationTemplate SpyglassMarkerTemplate => spyglassMarkerReference.Get<LocationTemplate>(this);
        public ShareableARAssetReference KeyIconsSpriteAssetPCReference => keyIconsSpriteAssetPCReference;
        public ShareableARAssetReference KeyIconsSpriteAssetPSReference => keyIconsSpriteAssetPSReference;
        public ShareableARAssetReference KeyIconsSpriteAssetXboxReference => keyIconsSpriteAssetXboxReference;
        
        // --- Localization
        public Locale[] NonAlphabetLanguages => nonAlphabetLanguages;
        
        // --- Systems
        public SceneConfigs SceneConfigs => sceneConfigs;
        public StatDefinedValuesConfig StatValuesConfig => statValuesConfig;
        [UnityEngine.Scripting.Preserve] public SceneReference CampaignReference => campaignReference;
        public TutorialConfig TutorialConfig => tutorialConfig;
        public ref readonly MapData MapData => ref mapData;
        public DlcId[] HorseDlcIds => horseDlcIds;
        public ItemSpawningData HorseDlcItem => horseDlcItem;
        public DlcId SupportersPackDlcId => supportersPackDlcId;
        public AudioConfig AudioConfig => audioConfig;
        public VariablesGroupAsset GlobalLocalizationVariables => localizationVariables;
        
        // --- Crafting
        public ItemTemplate HandcraftingGarbageItemTemplate => handCraftingGarbageItemTemplateRef.Get<ItemTemplate>(this);
        public ItemTemplate AlchemyGarbageItemTemplate => alchemyGarbageItemTemplateRef.Get<ItemTemplate>(this);
        
        // --- Audio
        public ARFmodEventEmitter PromptAudioEmitter => promptAudioEmitter;
        
        // --- Graphics
        public SaturationReferences SaturationReferences => saturationReferences;
        
        // --- Gestures
        public GestureOverridesTemplate ManGestures => manGestures.Get<GestureOverridesTemplate>(this);
        public GestureOverridesTemplate WomanGestures => womanGestures.Get<GestureOverridesTemplate>(this);
        public Dictionary<Gender, GestureOverridesTemplate> GenderGestures => _genderGestures;
        
        // --- Weapons
        public ShareableARAssetReference ArrowPrefab => arrowPrefab;
        public ShareableARAssetReference ArrowLogicPrefab => arrowLogicPrefab;
        public ShareableARAssetReference WeaponTrail => weaponTrail;
        public AnimationCurve DefaultWeaponCurve(Item item) {
            if (item.IsTwoHanded) {
                return default2HWeaponCurve;
            } 
            if (item.IsOneHanded) {
                return default1HWeaponCurve;
            }
            return defaultWeaponCurve;
        }
        public LocationTemplate TestDummy => testDummy.Get<LocationTemplate>(this);

        // --- Armors
        public StatusTemplate ArmorStatus(ItemWeight weight) {
            if (weight == ItemWeight.Light) {
                return lightArmorStatus.Get<StatusTemplate>();
            }
            if (weight == ItemWeight.Medium) {
                return mediumArmorStatus.Get<StatusTemplate>();
            }
            if (weight == ItemWeight.Heavy) {
                return heavyArmorStatus.Get<StatusTemplate>();
            }
            if (weight == ItemWeight.Overload) {
                return overloadArmorStatus.Get<StatusTemplate>();
            }
            return lightArmorStatus.Get<StatusTemplate>();
        }
        
        public StatusTemplate PersistentDashStatus => dashPersistentOptimalStatus.Get<StatusTemplate>(this);
        public StatusTemplate DashStatus(bool positive) {
            return positive ? dashOptimalStatus.Get<StatusTemplate>() : dashExhaustStatus.Get<StatusTemplate>();
        }
        
        // --- Proficiency
        
        public ProfAbstractRefs[] ProficiencyAbstractRefs => proficiencyAbstractRefs;

        public static void RefreshLocsGender(Gender gender) {
            // Set gender to localization variables
            var locVariables = CommonReferences.Get.GlobalLocalizationVariables;
            locVariables.Remove("male");
            locVariables["male"] = new BoolVariable { Value = gender != Gender.Female };
        }
        
        // --- Pickable Arrows
        public ItemTemplate BrokenArrowItemTemplate => brokenArrowItemTemplate.Get<ItemTemplate>(this);
        public LocationTemplate EnvironmentArrowLocationTemplate => environmentArrowLocationTemplate.Get<LocationTemplate>(this);
        public ShareableARAssetReference EnvironmentArrowVisualPrefab => environmentArrowVisualPrefab;
        public ShareableARAssetReference EnvironmentArrowVisualPrefabWithCollisions => environmentArrowVisualPrefabWithCollisions;

        // --- Crimes
        public StatusTemplate JailStatusTemplate => jailStatusTemplate.Get<StatusTemplate>(this);
            
        // --- Items
        [UnityEngine.Scripting.Preserve] public ItemTemplate UnidentifiedItemTemplate => unidentifiedItemTemplate.Get<ItemTemplate>(this);
        public ItemTemplate CoinItemTemplate => coinItemTemplate.Get<ItemTemplate>(this);
        public ItemTemplate CobwebItemTemplate => cobwebItemTemplate.Get<ItemTemplate>(this);
        public ItemTemplate CobwebCraftingIngredientTemplate => cobwebCraftingIngredientTemplate.Get<ItemTemplate>(this);
        public ItemSpawningData Bonfire => bonfire;
        public ItemTemplate IcarusRingTemplate => icarusRing.Get<ItemTemplate>(this);
        public ref readonly FishingData FishingData => ref fishingData;
        
        [UnityEngine.Scripting.Preserve] public Material NoRenderMaterial => noRenderMaterial;
        
        // --- Hero CutoffHand
        public ItemTemplate HandCutOffItemTemplate => handCutOffItemTemplate.Get<ItemTemplate>();
        
        // --- Hero Animator AvatarMasks
        public AvatarMask GetMask(HeroLayerType layerType) {
            if (layerType is HeroLayerType.MainHand or HeroLayerType.DualMainHand) {
                return heroMainHandMask;
            }

            if (layerType is HeroLayerType.OffHand or HeroLayerType.DualOffHand) {
                return heroOffHandMask;
            }

            if (layerType is HeroLayerType.BothHands or HeroLayerType.Tools or HeroLayerType.Fishing
                or HeroLayerType.Spyglass or HeroLayerType.Overrides) {
                return heroBothHandsMask;
            }

            if (layerType is HeroLayerType.CameraShakes or HeroLayerType.HeadMainHand
                or HeroLayerType.HeadOffHand or HeroLayerType.HeadBothHands or HeroLayerType.HeadTools 
                or HeroLayerType.HeadFishing or HeroLayerType.HeadSpyglass or HeroLayerType.HeadOverrides) {
                return heroHeadOnlyMask;
            }

            if (layerType == HeroLayerType.Legs) {
                return tppHeroLegsMask;
            }

            if (layerType is HeroLayerType.ActiveMainHand or HeroLayerType.ActiveOffHand) {
                return null;
            }

            throw new ArgumentOutOfRangeException(nameof(layerType), layerType, null);
        }
        
        public AvatarMask GetTppMask(HeroLayerType layerType) {
            if (layerType is HeroLayerType.MainHand or HeroLayerType.DualMainHand) {
                return tppHeroMainHandMask;
            }

            if (layerType is HeroLayerType.OffHand or HeroLayerType.DualOffHand) {
                return tppHeroOffHandMask;
            }

            if (layerType is HeroLayerType.BothHands or HeroLayerType.Tools or HeroLayerType.Fishing
                or HeroLayerType.Spyglass or HeroLayerType.Overrides) {
                return tppHeroBothHandsMask;
            }

            if (layerType is HeroLayerType.CameraShakes or HeroLayerType.HeadMainHand
                or HeroLayerType.HeadOffHand or HeroLayerType.HeadBothHands or HeroLayerType.HeadTools 
                or HeroLayerType.HeadFishing or HeroLayerType.HeadSpyglass or HeroLayerType.HeadOverrides) {
                return tppCameraShakesMask;
            }

            if (layerType == HeroLayerType.Legs) {
                return tppHeroLegsMask;
            }
            
            if (layerType is HeroLayerType.ActiveMainHand or HeroLayerType.ActiveOffHand) {
                return null;
            }

            throw new ArgumentOutOfRangeException(nameof(layerType), layerType, null);
        }
        
        public AvatarMask GetTppActiveMask(HeroLayerType layerType) =>
            layerType switch {
                HeroLayerType.MainHand or HeroLayerType.DualMainHand => tppHeroMainHandActiveMask,
                HeroLayerType.OffHand or HeroLayerType.DualOffHand => tppHeroOffHandActiveMask,
                _ => null
            };

        // === Red Death
        public ShareableARAssetReference GetRedDeathBodyMaterial(Gender gender) => gender switch {
            Gender.None => maleRedDeathBody,
            Gender.Male => maleRedDeathBody,
            Gender.Female => femaleRedDeathBody,
            _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, null)
        };

        public ShareableARAssetReference GetRedDeathFaceMaterial(Gender gender) => gender switch {
            Gender.None => maleRedDeathFace,
            Gender.Male => maleRedDeathFace,
            Gender.Female => femaleRedDeathFace,
            _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, null)
        };
    }
}
