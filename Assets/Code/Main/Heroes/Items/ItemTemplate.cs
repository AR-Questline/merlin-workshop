using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Crafting.AlchemyCrafting;
using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.Main.Crafting.HandCrafting;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Buffs;
using Awaken.TG.Main.Heroes.Items.Gems;
using Awaken.TG.Main.Heroes.Items.Tooltips;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Main.Utility.Tags;
using Awaken.TG.Utility.Attributes;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Collections;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Sessions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items {
    public class ItemTemplate : Template, IComparable<ItemTemplate>, IIconized, ITagged, IAttachmentGroup {
        public const int MinimumItemLevel = -1;
        const string MagicSettings = "Magic Description";
        const string MiscSettings = "Misc Settings";
        const string EconomySettingsGroup = "Economy Settings";
        const string EconomySettingsBuyPriceGroup = EconomySettingsGroup + "/Buy Price";
        const string EconomySettingsBuyCapGroup = EconomySettingsGroup + "/Hard Cap";
        
        // === Unity editor fields
        [Tags(TagsCategory.Item), PropertyOrder(0), HideLabel, Space]
        public string[] tags = Array.Empty<string>();

        [IconRenderingSettings, ShowAssetPreview, ARAssetReferenceSettings(new[] {typeof(Texture2D), typeof(Sprite)}, true, AddressableGroup.ItemsIcons), PropertyOrder(0)]
        public ShareableSpriteReference iconReference;
        [LocStringCategory(Category.Item), PropertyOrder(0), HideLabel]
        public LocString itemName;
        [SerializeField, Toggle(nameof(OptionalLocString.toggled)), PropertyOrder(0)] 
        OptionalLocString flavor;

        [SerializeField, Toggle(nameof(OptionalLocString.toggled)), PropertyOrder(0)] 
        OptionalLocString description;
#if UNITY_EDITOR
        // ReSharper disable once InconsistentNaming
        [SerializeField, ReadOnly] string EDITOR_bakedDescription;
#endif
        
        [SerializeField, FoldoutGroup(MagicSettings)]
        MagicItemTemplateInfo lightCastInfo;
        [SerializeField, FoldoutGroup(MagicSettings)]
        MagicItemTemplateInfo heavyCastInfo;
        
        [Space]
        [RichEnumExtends(typeof(ItemQuality)), SerializeField, PropertyOrder(0)] 
        RichEnumReference quality = ItemQuality.Normal;
        
        [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(10)"), ShowInInspector, PropertyOrder(2)] string _space_ODIN3;
        
        [PropertyOrder(2), LabelWidth(190), FoldoutGroup(EconomySettingsGroup)]
        public int basePrice = 5;
        [PropertyOrder(2), LabelWidth(190), HorizontalGroup(EconomySettingsBuyPriceGroup, width: 210)]
        public bool overrideBuyPrice;
        [PropertyOrder(2), HorizontalGroup(EconomySettingsBuyPriceGroup), ShowIf(nameof(overrideBuyPrice)), HideLabel]
        public int buyPrice;
        [PropertyOrder(2), LabelWidth(190), FoldoutGroup(EconomySettingsGroup), ShowIf(nameof(CanHaveItemLevel))]
        public float priceLevelMultiplier = 1;
        [PropertyOrder(2), LabelWidth(190), HorizontalGroup(EconomySettingsBuyCapGroup, width: 210)]
        public bool overrideBuyPriceMultiplier;
        [PropertyOrder(2), HorizontalGroup(EconomySettingsBuyCapGroup), ShowIf(nameof(overrideBuyPriceMultiplier)), HideLabel]
        [MinValue(0.0f)] public float buyPriceMultiplier = 1f;
        [PropertyOrder(2), LabelWidth(190), FoldoutGroup(EconomySettingsGroup), ShowInInspector]
        float AbstractBuyPriceMultiplier => ResolveBuyPriceMultiplier();
        
        [Space]
        
        [RichEnumExtends(typeof(Keyword)), SerializeField, PropertyOrder(2), FoldoutGroup(MiscSettings)]
        List<RichEnumReference> keywords = new();
        [ShowIf(nameof(CanHaveItemLevel)), PropertyOrder(2), FoldoutGroup(MiscSettings)] 
        public int levelBonus = 0;
        [Space]
        [PropertyOrder(2), FoldoutGroup(MiscSettings)]
        public float weight = 5;
        [PropertyOrder(2), ShowIf(nameof(IsArmor)), FoldoutGroup(MiscSettings)] 
        public float weightLoss = 0.5f;
        [SerializeField, Range(0f, 1f), ShowIf(nameof(IsEquippable)), PropertyOrder(2), FoldoutGroup(MiscSettings)] 
        float upgradeChance = 0.5f;
        [SerializeField, PropertyOrder(2), FoldoutGroup(MiscSettings), PropertySpace(0, 10)] 
        CrimeItemValue crimeItemValue = CrimeItemValue.None;
        
        [Space, PropertyOrder(3)]
        public bool canStack;
        [PropertyOrder(3)]
        public bool cannotBeDropped;
        [PropertyOrder(3)]
        public bool hiddenOnUI;
        [PropertySpace(0, 10), PropertyOrder(3), ShowIf(nameof(hiddenOnUI))]
        public bool visibleOnUIForLoadout;
        
        [RichEnumExtends(typeof(SurfaceType)), SerializeField, PropertyOrder(4)]
        RichEnumReference damageSurfaceType = SurfaceType.DamageMetal;

        [SerializeField, PropertyOrder(10), HideIf(nameof(IsAbstract)), PropertySpace(0, 15)]
        [ARAssetReferenceSettings(new[] {typeof(GameObject)}, true, AddressableGroup.DroppableItems), ValidateInput(nameof(ValidDropPrefab), "Drop prefab must have a collider")]
        ShareableARAssetReference shareableDropPrefab;

        Cached<ItemTemplate, List<string>> _tags = new(static template => template.WithAbstractTags(template.tags ?? Enumerable.Empty<string>()));
        public ICollection<string> Tags => _tags.Get(this);
        public ICollection<string> EditorTagsNoRefresh => _tags.GetNoRefresh(this);
        
        [ShowInInspector, PropertyOrder(1)] 
        TierHelper.Tier Tier {
            get => TierHelper.GetTier(tags, TierHelper.ItemTiers);
            set => TierHelper.SetTier(ref tags, value, TierHelper.ItemTiers);
        }
        
        public string ItemName => itemName;
        public string Description => description.LocString;
        public LocString DescriptionLoc => description.LocString;
        public string Flavor => flavor.LocString;
        [UnityEngine.Scripting.Preserve] public LocString FlavorLoc => flavor.LocString;
        public ItemQuality Quality => quality.EnumAs<ItemQuality>() ?? ItemQuality.Normal;
        public IEnumerable<Keyword> Keywords => keywords.Select(k => k.EnumAs<Keyword>());
        public bool CanStack => canStack;
        public bool CanHaveItemLevel => IsEquippable && !IsArrow && !IsThrowable;
        public int LevelBonus => CanHaveItemLevel ? levelBonus : 0;
        public int BasePrice => basePrice;
        public float BuyPrice => (overrideBuyPrice ? buyPrice : basePrice) * BuyPriceMultiplier;
        public float PriceLevelMultiplier => priceLevelMultiplier;
        public float BuyPriceMultiplier => ResolveBuyPriceMultiplier();
        public float Weight => weight;
        public float WeightLoss => weightLoss;
        [UnityEngine.Scripting.Preserve] public float BaseUpgradeChance => upgradeChance;

        public bool CannotBeDropped => cannotBeDropped;
        public bool HiddenOnUI => hiddenOnUI;
        public bool VisibleOnUIForLoadout => !hiddenOnUI || visibleOnUIForLoadout;
        public CrimeItemValue CrimeValue => crimeItemValue;
        public bool IsArmor => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractArmor);
        public bool IsLightArmor => IsArmor && this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractLightWeight);
        public bool IsMediumArmor => IsArmor && this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractMediumWeight);
        public bool IsHeavyArmor => IsArmor && this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractHeavyWeight);
        
        public bool IsWeapon => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractWeapon);
        public bool IsMelee => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractWeaponMelee);
        public bool IsOneHanded => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractWeapon1H);
        public bool IsTwoHanded => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractWeapon2H);
        public bool IsDefaultFists => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractWeaponDefaultFists);
        public bool IsFists => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractWeaponFists);
        public bool IsDagger => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractWeaponDagger);
        public bool IsAxe => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractWeaponAxe);
        public bool IsSword => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractWeaponSword);
        public bool IsBlunt => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractWeaponBlunt);
        public bool IsPolearm => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractWeaponPolearm);
        public bool IsShield => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractShield);
        public bool IsRod => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractRod);
        public bool IsRanged => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractWeaponRanged);
        public bool IsArrow => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractArrow);
        public bool IsThrowable => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractThrowable);
        public bool IsSpectralWeapon => TagUtils.HasRequiredTag(Tags, "weapons:spectral");
        public bool IsMagic => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractMagic);
        public bool IsCastMagic => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractMagicCastArtillery)
                                   || this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractMagicCastChaingun)
                                   || this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractMagicCastPistol)
                                   || this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractMagicCastRailgun)
                                   || this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractMagicCastRocketLauncher)
                                   || this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractMagicCastShotgun);
        public bool IsChaingun => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractMagicCastChaingun);
        public bool IsSoulCube => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractSoulCube);
        public MagicItemTemplateInfo LightCastInfo => lightCastInfo;
        public MagicItemTemplateInfo HeavyCastInfo => heavyCastInfo;
        
        public bool IsJewelry => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractJewelry);
        public bool IsComponent => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractComponent);
        public bool IsCrafting => IsAlchemyComponent || IsCraftingComponent || IsCookingComponent;
        public bool IsCookingComponent => TagUtils.HasRequiredKind(Tags, ExperimentalCooking.RequiredKind);
        public bool IsAlchemyComponent => TagUtils.HasRequiredKind(Tags, Alchemy.RequiredKind);
        public bool IsCraftingComponent => TagUtils.HasRequiredKind(Tags, Handcrafting.RequiredKind);

        public bool IsConsumable => (this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractConsumable)
                                     || GetAttachments()
                                        .OfType<IItemEffectsSpec>()
                                        .Any(e => ItemActionType.IsConsumableAction(e.ActionType)))
                                    && GetAttachment<ItemReadSpec>() == null;
        public bool IsPlainFood => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractPlainFood);
        public bool IsDish => TagUtils.HasRequiredTag(Tags, "item:dish");
        public bool IsFish => TagUtils.HasRequiredTag(Tags, "cook:seafood");
        public bool IsPotion => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractPotion);
        public bool IsRecipe => GetAttachment<RecipeItemAttachment>() != null || TagUtils.HasRequiredTag(Tags, "item:recipe");
        public bool IsBuffApplier => GetAttachment<ItemBuffApplierAttachment>() != null;
        public bool IsReadable => GetAttachment<ItemReadSpec>() != null;
        public bool IsGem => GetAttachment<GemAttachment>() != null;
        public bool IsKey => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractKey);
        public bool IsTool => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractTool);
        [UnityEngine.Scripting.Preserve] public bool IsAlcohol => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractAlcohol);
        public bool IsImportantItem => this.IsQuestItem() || IsKey;
        public bool ConsumableModifiesHealth => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractConsumableHealth);
        public bool ConsumableModifiesMana => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractConsumableMana);
        public bool ConsumableStamina => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractConsumableStamina);
        public bool ConsumablePotionOther => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractConsumablePotionOther);
        public bool ConsumableModifiesStat => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractConsumableStat);

        public bool IsEquippable => GetAttachment<ItemEquipSpec>();
        public EquipmentType EquipmentType => GetAttachment<ItemEquipSpec>()?.EquipmentType ?? throw new Exception();

        public SurfaceType DamageSurfaceType => damageSurfaceType.EnumAs<SurfaceType>();

        public ItemUpgradeConfigData ItemUpgradeConfigConfig => GetCostConfig(GameConstants.Get.defaultSharpeningConfig, GameConstants.Get.sharpeningConfigs);
        public ItemUpgradeConfigData WeightReductionConfig => GetCostConfig(GameConstants.Get.defaultWeightReductionConfig, GameConstants.Get.weightReductionConfigs);
        
        public PooledList<ItemTemplate> AbstractTypes => this.Abstracts<ItemTemplate>();
        public ShareableARAssetReference DropPrefab => shareableDropPrefab;
        public ShareableSpriteReference IconReference => iconReference;

        public T GetAttachment<T>() {
            return GetComponent<T>();
        }
        
        public int CompareTo(ItemTemplate other) => string.Compare(name, other.name, StringComparison.Ordinal);
        
        ItemUpgradeConfigData GetCostConfig(ItemUpgradeConfig defaultConfig, List<ItemUpgradesPerTier> dedicatedConfig) {
            bool hasTierConfig = dedicatedConfig.Any(config => config.tier == Tier);
            
            if (Tier == TierHelper.Tier.None || !hasTierConfig) {
                return new ItemUpgradeConfigData(defaultConfig.IngredientsPerLevel, defaultConfig.MoneyPerLevel);
            }
            
            var tierConfig = dedicatedConfig.Single(config => config.tier == Tier);
            bool hasAbstractConfig = tierConfig.configsPerAbstract.Any(config => AbstractTypes.CheckContainsAndRelease(config.abstractItemTemplate.Get<ItemTemplate>()));

            if (hasAbstractConfig) {
                List<ItemUpgradePerAbstract> itemUpgradesPerAbstract = tierConfig.configsPerAbstract.Where(config => AbstractTypes.CheckContainsAndRelease(config.abstractItemTemplate.Get<ItemTemplate>())).ToList();

                foreach (var itemUpgradePerAbstract in itemUpgradesPerAbstract) {
                    if (itemUpgradePerAbstract.itemUpgradeConfig == null) {
                        Log.Important?.Error($"No upgrade config in tier {Tier} for abstract {itemUpgradePerAbstract.abstractItemTemplate.Get<ItemTemplate>()}, fix it in Game Constants!");
                        return GetItemUpgradeData (defaultConfig, tierConfig.defaultConfigOfTier);
                    }
                }
                    
                return GetItemUpgradeData (defaultConfig, tierConfig.defaultConfigOfTier, itemUpgradesPerAbstract);
            }
            
            return GetItemUpgradeData (defaultConfig, tierConfig.defaultConfigOfTier);
        }

        ItemUpgradeConfigData GetItemUpgradeData(ItemUpgradeConfig defaultConfig, ItemUpgradeConfig tierConfig, List<ItemUpgradePerAbstract> abstractConfigs = null) {
            List<IngredientPerLevel> ingredients = new();
            AnimationCurve currencyPerLevel = null;

            if (abstractConfigs != null) {
                foreach (var config in abstractConfigs) {
                    ingredients.AddRange(config.itemUpgradeConfig.IngredientsPerLevel.Where(x => ingredients.All(y => y.IngredientType != x.IngredientType)));
                }
            }
            
            foreach (IngredientType ingredientType in Enum.GetValues(typeof(IngredientType))) {
                if (ingredients.All(i => i.IngredientType != ingredientType)) {
                    ingredients.AddRange(tierConfig.IngredientsPerLevel.Where(i => i.IngredientType == ingredientType));
                }
            }
            
            foreach (IngredientType ingredientType in Enum.GetValues(typeof(IngredientType))) {
                if (ingredients.All(i => i.IngredientType != ingredientType)) {
                    ingredients.AddRange(defaultConfig.IngredientsPerLevel.Where(i => i.IngredientType == ingredientType));
                }
            }

            if (abstractConfigs?.First().itemUpgradeConfig is {CostType:  MoneyCostType.Own}) {
                currencyPerLevel = abstractConfigs.First().itemUpgradeConfig.MoneyPerLevel;
            } else if (abstractConfigs?.First().itemUpgradeConfig is not {CostType: MoneyCostType.None}) {
                if (tierConfig.CostType == MoneyCostType.Own) {
                    currencyPerLevel = tierConfig.MoneyPerLevel;
                } else if (tierConfig.CostType == MoneyCostType.Inherited && defaultConfig.CostType != MoneyCostType.None) {
                    currencyPerLevel = defaultConfig.MoneyPerLevel;
                }
            } 
            
            return new ItemUpgradeConfigData(ingredients, currencyPerLevel);
        }
        
        float ResolveBuyPriceMultiplier() {
            if (overrideBuyPriceMultiplier) {
                return buyPriceMultiplier;
            }

            var abstractTypes = AbstractTypes;
            var overrides = abstractTypes.value.Where(t => t.overrideBuyPriceMultiplier).ToArray();
            abstractTypes.Release();
            float abstractMultiplier = overrides.Length > 0 ? overrides.Max(t => t.buyPriceMultiplier) : 1.0f;

            float constantsMultiplier = GameConstants.Get.priceMultiplierConfigs.FirstOrDefault(c => c.IsMatching(this))?.multiplier ?? 1f;
            return abstractMultiplier * constantsMultiplier;
        }

        // === Editor tools
#if UNITY_EDITOR
        [ShowInInspector, InlineButton(nameof(EditorRefreshRecipes)), FoldoutGroup(MiscSettings + "/Crafting"), PropertyOrder(2)]
        public List<BaseRecipe> EditorRecipes { get; set; }
        void EditorRefreshRecipes() => EditorRecipeCache.ResetCache();

        [PropertySpace]
        [Button("Add to hero inventory", ButtonSizes.Medium), GUIColor(94 / 255f, 223 / 255f, 106 / 255f)]
        [ShowIf(nameof(EDITOR_CanUseContexts))]
        void EDITOR_AddToHeroInventory() {
            TemplatesUtil.EDITOR_AssignGuid(this, gameObject);

            Hero hero = Hero.Current;
            this.ChangeQuantity(hero.Inventory, 1);
        }

        [Button("Add to hero inventory 10 times"), GUIColor(94 / 255f, 163 / 255f, 106 / 255f)]
        [ShowIf(nameof(EDITOR_CanUseContexts))]
        void EDITOR_AddToHeroInventory10Times() {
            TemplatesUtil.EDITOR_AssignGuid(this, gameObject);

            Hero hero = Hero.Current;
            this.ChangeQuantity(hero.Inventory, 10);
        }
        
        [Button("Upgrade in hero Inventory", ButtonSizes.Medium)]
        [ShowIf(nameof(EDITOR_CanUseContexts))]
        void EDITOR_UpgradeInHeroInventory() {
            TemplatesUtil.EDITOR_AssignGuid(this, gameObject);

            Hero hero = Hero.Current;
            var itemToUpgrade = hero.Inventory.Items.FirstOrDefault(item => item.Template == this);
            if (itemToUpgrade == null) {
                Log.Important?.Error($"Hero doesn't have {this} in inventory");
                return;
            }

            if (ItemUpgradeConfigConfig != null) {
                itemToUpgrade.Level.IncreaseBy(1);
            } else {
                Log.Important?.Error($"Can't upgrade {this}");
            }
        }

        [Button("Debug Equip")]
        [ShowIf(nameof(EDITOR_CanUseContexts))]
        void EDITOR_Equip(EquipmentSlotType equipmentType) {
            TemplatesUtil.EDITOR_AssignGuid(this, gameObject);

            Hero hero = Hero.Current;
            var item = new Item(this);
            hero.Inventory.Add(item);
            hero.Inventory.Equip(item, equipmentType);
        }

        bool EDITOR_CanUseContexts() {
            return Application.isPlaying && Hero.Current != null && ((ITemplate) this).TemplateType is not TemplateType.System and not TemplateType.ForRemoval;
        }
#endif
        
        bool ValidDropPrefab() {
            bool validDropPrefab = shareableDropPrefab is {IsSet: true};
            if (IsAbstract) return !validDropPrefab;
            
#if UNITY_EDITOR
            if (!validDropPrefab) return false;
            var reference = shareableDropPrefab.Get();
            var instance = reference.EditorLoad<GameObject>();
            if (instance == null) {
                return false;
            }

            var colliders = instance.GetComponentsInChildren<Collider>();
            if (colliders.Length == 0 || colliders.All(c => c.isTrigger || !c.enabled)) {
                return false;
            }
#endif
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            return validDropPrefab;
        }

        // === Possible Attachments (EDITOR)
        const string PossibleAttachmentsGroup = "Possible Attachments";

        static Dictionary<AttachmentCategory, PossibleAttachmentsGroup> s_possibleAttachments;
        static Dictionary<AttachmentCategory, PossibleAttachmentsGroup> PossibleAttachments => s_possibleAttachments ??= PossibleAttachmentsUtil.Get(typeof(ItemTemplate));
        
        [FoldoutGroup(PossibleAttachmentsGroup, order: 999, expanded: true), ShowInInspector, HideReferenceObjectPicker]
        [LabelText(nameof(AttachmentCategory.Common), icon: SdfIconType.StarFill, IconColor = ARColor.EditorLightYellow)]
        PossibleAttachmentsGroup CommonGroup {
            get => PossibleAttachments.TryGetValue(AttachmentCategory.Common, out var group) ? group.WithContext(this) : null;
            set => PossibleAttachments[AttachmentCategory.Common] = value;
        }
        
        [FoldoutGroup(PossibleAttachmentsGroup), ShowInInspector, HideReferenceObjectPicker]
        [LabelText(nameof(AttachmentCategory.Rare), icon: SdfIconType.InfoCircleFill, IconColor = ARColor.EditorMediumBlue)]
        PossibleAttachmentsGroup RareGroup {
            get => PossibleAttachments.TryGetValue(AttachmentCategory.Rare, out var group) ? group.WithContext(this) : null;
            set => PossibleAttachments[AttachmentCategory.Rare] = value;
        }
        
        [FoldoutGroup(PossibleAttachmentsGroup), ShowInInspector, HideReferenceObjectPicker]
        [LabelText(nameof(AttachmentCategory.ExtraCustom), icon: SdfIconType.InfoCircleFill, IconColor = ARColor.EditorDarkBlue, NicifyText = true)]
        PossibleAttachmentsGroup CustomGroup {
            get => PossibleAttachments.TryGetValue(AttachmentCategory.ExtraCustom, out var group) ? group.WithContext(this) : null;
            set => PossibleAttachments[AttachmentCategory.ExtraCustom] = value;
        }

        // === Icon renderer (EDITOR)
        ShareableSpriteReference IIconized.GetIconReference() => IconReference;
        void  IIconized.SetIconReference(ShareableSpriteReference iconRef) => this.iconReference = iconRef;
        
        GameObject IIconized.InstantiateProp(Transform parent) {
            ARAssetReference assetReference = GetMeshAssetReference(this);
            if (assetReference == null) {
                Log.Important?.Error($"IconRenderer: Can't get asset reference from {this.name}");
                return null;
            }
#if UNITY_EDITOR
            return assetReference.EditorInstantiate<GameObject>(parent);
#else
            Log.Important?.Error($"IconRenderer: You can not instantiate icon renderer props in play mode.");
            return null;
#endif
        }
        
        static ARAssetReference GetMeshAssetReference(ItemTemplate itemTemplate) {
            return UseItemEquipPrefab(itemTemplate)
                ? GetMeshAssetReferenceFromMobItems(itemTemplate)
                : itemTemplate.DropPrefab.Get();
        }
        static bool UseItemEquipPrefab(ItemTemplate item) {
            bool isEquippable = item.TryGetComponent<ItemEquipSpec>(out _);
            return isEquippable && item.IsArmor;
        }
        static ARAssetReference GetMeshAssetReferenceFromMobItems(ItemTemplate itemTemplate) {
            ItemRepresentationByNpc[] mobItems = itemTemplate.GetAttachment<ItemEquipSpec>()?.RetrieveMobItemsInstance();
            return mobItems == null ? null : ItemEquip.GetDebugHeroItem(mobItems);
        }

#if UNITY_EDITOR
        public readonly struct EditorAccessor {
            public readonly ItemTemplate template;
            
            public EditorAccessor(ItemTemplate template) {
                this.template = template;
            }
            
            public ref string BakedDescription => ref template.EDITOR_bakedDescription;
        }
#endif
    }
}
