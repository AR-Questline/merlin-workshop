using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Animations.FSM.Heroes.Modifiers;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.NewThings;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.Main.Heroes.Items.Buffs;
using Awaken.TG.Main.Heroes.Items.Gems;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Heroes.Items.Tooltips;
using Awaken.TG.Main.Heroes.Items.Weapons;
using Awaken.TG.Main.Heroes.Sketching;
using Awaken.TG.Main.Heroes.Skills;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Storage;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Gems;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.Item;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.Utility.Skills;
using Awaken.TG.Main.Utility.TokenTexts;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Relations;
using Awaken.TG.MVC.UI.Handlers.Tooltips;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using Awaken.TG.Main.Memories.FilePrefs;
using Awaken.TG.Main.Scenes.SceneConstructors;
using FMODUnity;

namespace Awaken.TG.Main.Heroes.Items {
    /// <summary>
    /// Main model for any type of Item.
    /// Created from ItemTemplate.
    /// Can hold any quantity of itself.
    /// It's effects are implemented as skills assigned to template, categorized by possible actions (use, drop, equip)
    /// </summary>
    public sealed partial class Item : Model, ITagged, INamed, ITextVariablesContainer, IWithStats, IModelNewThing {
        public override ushort TypeForSerialization => SavedModels.Item;

        public override Domain DefaultDomain => Domain.Gameplay;
        
        // === Fields
        [Saved] public ItemTemplate Template { get; private set; }
        [Saved] public int Quantity { get; private set; }
        [Saved] public Stat Level { get; private set; }
        [Saved] public Stat WeightLevel { get; private set; }
        [Saved] public long PickupTimestamp { get; private set; }

        [Saved] FrugalList<EquipmentSlotType> _equippedInSlots;
        [Saved] AttachmentTracker _attachmentTracker;

        TokenText _itemName;
        TokenText _flavor;
        TokenText _baseDescription;
        TokenText _fullDescription;
        TokenText _requirementsDescription;
        TooltipConstructorTokenText _tooltip;
        Dictionary<string, Skill> _variablesById = new();
        List<Skill> _cachedSkills = new();
        ItemSpawningDataRuntime _itemSpawningData;
        bool _awaitingSetupTexts;
        ItemSkillsInvoker _skillsInvoker;

        // === Properties
        public IItemOwner Owner => RelatedValue(IItemOwner.Relations.OwnedBy).Get();
        
        public IInventory Inventory => Owner?.Inventory;
        [CanBeNull] public ICharacter Character => Owner?.Character;
        [CanBeNull] public ICharacterInventory CharacterInventory => Character?.Inventory ?? Inventory as ICharacterInventory;
        [CanBeNull] public EquipmentSlotType EquippedInSlotOfType => _equippedInSlots.FirstOrDefault();
        public FrugalList<EquipmentSlotType> EquippedInSlotOfTypes => _equippedInSlots;

        public string DisplayName => GetDisplayName();
        public string DebugName => Template?.GUID ?? "Template Null";
        public IEnumerable<Keyword> Keywords => Template.Keywords.Concat(ActiveSkills.SelectMany(s => s.Keywords));
        public ItemQuality Quality => Template.Quality;
        public CrimeItemValue CrimeValue => Template.CrimeValue;
        public string Flavor => _flavor.GetValue(Character, this);
        public string BaseDescriptionFor(ICharacter character) => _baseDescription.GetValue(character, this);
        public string DescriptionFor(ICharacter character) => _fullDescription.GetValue(character, this);
        public string RequirementsDescriptionFor(ICharacter character) => _requirementsDescription.GetValue(character, this);
        [UnityEngine.Scripting.Preserve] public TooltipConstructor TooltipDescription => _tooltip.GetTooltip(Character, this);
        public bool CanStack => Template.CanStack;
        public ShareableSpriteReference Icon => Template.IconReference;
        public EquipmentType EquipmentType => TryGetElement<ItemEquip>()?.EquipmentType; //?? throw new Exception();
        [UnityEngine.Scripting.Preserve] public Tool Tool => TryGetElement<Tool>();
        public bool IsEquipped => _equippedInSlots.Count > 0;
        public ICollection<string> Tags => Template.Tags;

        public FinisherType FinisherType => TryGetElement<ItemEquip>()?.FinisherType ?? FinisherType.None;
        public HitsToHitStop HitsToHitStop => TryGetElement<ItemEquip>()?.HitsToHitStop ?? HitsToHitStop.Blunt;
        public SurfaceType DamageSurfaceType => Template.DamageSurfaceType;
        
        // --- IVariablesContainer
        public float? GetVariable(string variable, int index = 0, ICharacter owner = null) => SkillForVariable(variable, index)?.GetVariable(variable, owner ?? Character);
        public StatType GetEnum(string id, int index = 0) => SkillForVariable(id, index)?.GetRichEnum(id);

        // --- Helper booleans
        [UnityEngine.Scripting.Preserve] public bool IsArmor => Template.IsArmor;
        [UnityEngine.Scripting.Preserve] public bool IsWeapon => Template.IsWeapon;
        [UnityEngine.Scripting.Preserve] public bool IsMelee => Template.IsMelee;
        [UnityEngine.Scripting.Preserve] public bool IsOneHanded => Template.IsOneHanded;
        [UnityEngine.Scripting.Preserve] public bool IsTwoHanded => Template.IsTwoHanded;
        [UnityEngine.Scripting.Preserve] public bool IsDefaultFists => Template.IsDefaultFists;
        [UnityEngine.Scripting.Preserve] public bool IsFists => Template.IsFists;
        [UnityEngine.Scripting.Preserve] public bool IsDagger => Template.IsDagger;
        [UnityEngine.Scripting.Preserve] public bool IsAxe => Template.IsAxe;
        [UnityEngine.Scripting.Preserve] public bool IsSword => Template.IsSword;
        [UnityEngine.Scripting.Preserve] public bool IsBlunt => Template.IsBlunt;
        [UnityEngine.Scripting.Preserve] public bool IsPolearm => Template.IsPolearm;
        [UnityEngine.Scripting.Preserve] public bool IsShield => Template.IsShield;
        [UnityEngine.Scripting.Preserve] public bool IsRod => Template.IsRod;
        [UnityEngine.Scripting.Preserve] public bool IsBlocking => IsShield || IsRod;
        [UnityEngine.Scripting.Preserve] public bool CanBeUsedAsShield => IsBlocking || IsFists;
        [UnityEngine.Scripting.Preserve] public bool IsRanged => Template.IsRanged;
        [UnityEngine.Scripting.Preserve] public bool IsArrow => Template.IsArrow;
        [UnityEngine.Scripting.Preserve] public bool IsThrowable => Template.IsThrowable;
        [UnityEngine.Scripting.Preserve] public bool IsSpectralWeapon => Template.IsSpectralWeapon;
        [UnityEngine.Scripting.Preserve] public bool IsMagic => Template.IsMagic;
        [UnityEngine.Scripting.Preserve] public bool IsCastMagic => Template.IsCastMagic;
        [UnityEngine.Scripting.Preserve] public bool IsSoulCube => Template.IsSoulCube;
        [UnityEngine.Scripting.Preserve] public bool IsGear => IsArmor || IsBlocking || IsWeapon || IsArrow;
        [UnityEngine.Scripting.Preserve] public bool IsCrafting => Template.IsCrafting;
        [UnityEngine.Scripting.Preserve] public bool IsCommonComponent => (IsCookingComponent ? 1 : 0) + (IsCraftingComponent ? 1 : 0) + (IsAlchemyComponent ? 1 : 0) >= 2;
        [UnityEngine.Scripting.Preserve] public bool IsCookingComponent => Template.IsCookingComponent;
        [UnityEngine.Scripting.Preserve] public bool IsCraftingComponent => Template.IsCraftingComponent;
        [UnityEngine.Scripting.Preserve] public bool IsAlchemyComponent => Template.IsAlchemyComponent;
        [UnityEngine.Scripting.Preserve] public bool IsBuffApplier => HasElement<ItemBuffApplier>();
        [UnityEngine.Scripting.Preserve] public bool IsGem => HasElement<GemUnattached>();
        [UnityEngine.Scripting.Preserve] public bool IsArmorGem => TryGetElement<GemUnattached>()?.GemType == GemType.Armor;
        [UnityEngine.Scripting.Preserve] public bool IsWeaponGem => TryGetElement<GemUnattached>()?.GemType == GemType.Weapon;
        [UnityEngine.Scripting.Preserve] public bool IsReadable => HasElement<ItemRead>();
        [UnityEngine.Scripting.Preserve] public bool HasSeedData => HasElement<ItemSeed>();
        [UnityEngine.Scripting.Preserve] public bool IsEdible => DefinedActionTypes.Any(ItemActionType.IsEdible);
        [UnityEngine.Scripting.Preserve] public bool IsUsable => DefinedActionTypes.Any(ItemActionType.IsUsable);
        [UnityEngine.Scripting.Preserve] public bool IsConsumable => Template.IsConsumable;
        [UnityEngine.Scripting.Preserve] public bool IsPlainFood => Template.IsPlainFood;
        [UnityEngine.Scripting.Preserve] public bool IsDish => Template.IsDish;
        [UnityEngine.Scripting.Preserve] public bool IsFish => Template.IsFish;
        [UnityEngine.Scripting.Preserve] public bool IsPotion => Template.IsPotion && !HasElement<ItemBuffApplier>();
        [UnityEngine.Scripting.Preserve] public bool IsJewelry => Template.IsJewelry;
        [UnityEngine.Scripting.Preserve] public bool IsRecipe => Template.IsRecipe;
        [UnityEngine.Scripting.Preserve] public bool IsKey => Template.IsKey;
        [UnityEngine.Scripting.Preserve] public bool IsEquippable => HasElement<ItemEquip>();
        [UnityEngine.Scripting.Preserve] public bool IsNPCEquippable => TryGetElement<ItemEquip>() is { } ie && ie.EquipmentType != EquipmentType.QuickUse;
        [UnityEngine.Scripting.Preserve] public bool IsStolen => HasElement<StolenItemElement>();
        [UnityEngine.Scripting.Preserve] public bool IsQuestItem => Template.IsQuestItem();
        [UnityEngine.Scripting.Preserve] public bool IsUnidentified => HasElement<UnidentifiedItem>();
        [UnityEngine.Scripting.Preserve] public bool IsStashed => Inventory is HeroStorage;
        [UnityEngine.Scripting.Preserve] public bool CannotBeDropped => Template.CannotBeDropped || IsQuestItem;
        [UnityEngine.Scripting.Preserve] public bool HasCharges => HasElement<IItemWithCharges>();
        [UnityEngine.Scripting.Preserve] public bool HiddenOnUI => Template.HiddenOnUI;
        [UnityEngine.Scripting.Preserve] public bool VisibleOnUIForLoadout => Template.VisibleOnUIForLoadout;
        [UnityEngine.Scripting.Preserve] public bool Locked => HasElement<LockItemSlot>();
        public ItemStats ItemStats => TryGetElement<ItemStats>();
        public ItemStatsRequirements StatsRequirements => TryGetElement<ItemStatsRequirements>();
        public float Weight => ItemStats?.Weight.ModifiedValue ?? Template.Weight;
        public float WeightLoss => Template.WeightLoss;
        public MagicItemTemplateInfo LightCastInfo => Template.LightCastInfo;
        public MagicItemTemplateInfo HeavyCastInfo => Template.HeavyCastInfo;
        
        // --- Gems
        [UnityEngine.Scripting.Preserve] public bool HasGemAttached => HasElement<GemAttached>();
        public bool CanHaveRelics => (IsWeapon || IsArmor) && MaxGemSlots > 0;
        public int MaxGemSlots => TryGetElement<ItemEquip>()?.MaxGemSlots ?? 0;
        public int FreeGemSlots => TryGetElement<ItemGems>()?.FreeSlots ?? 0;
        
        // --- Actions
        IEnumerable<ItemActionType> DefinedActionTypes => Elements<IItemAction>()
            .Where(ia => ia.Type != null)
            .Select(ia => ia.Type)
            .OrderBy(t => t.Priority);

        public IEnumerable<IItemAction> ActionsFor(ItemActionType type) => Elements<IItemAction>().Where(ia => ia.Type == type).OrderByDescending(ia => ia.Priority());

        // --- Effects
        public IEnumerable<Skill> ItemEffectsSkills => TryGetElement<ItemEffects>()?.Skills ?? Enumerable.Empty<Skill>();
        public IEnumerable<Skill> Effects => ItemEffectsSkills;
        public IEnumerable<Skill> EffectsForDescription => ItemEffectsSkills.Concat(Elements<ISkillProvider>().GetManagedEnumerator().SelectMany(p => p.Skills));
        public IEnumerable<Skill> GemSkills => Elements<GemAttached>().GetManagedEnumerator().SelectMany(g => g.Skills);
        public IEnumerable<Skill> BuffSkills => Elements<AppliedItemBuff>().GetManagedEnumerator().SelectMany(g => g.Skills);
        public IEnumerable<Skill> ActiveSkills => Effects.Concat(GemSkills).Concat(BuffSkills);
        public Skill CastAbleSkill => Effects.FirstOrDefault();

        // --- Price
        public float ExactPrice => Template.BasePrice + Level * Template.PriceLevelMultiplier;
        public float ExactBuyPrice => Template.BuyPrice + Level * Template.PriceLevelMultiplier;
        public int Price => Mathf.RoundToInt(ExactPrice);

        public string NewThingId => Template?.GUID;
        public bool DiscardAfterMarkedAsSeen => false;

        // === Events
        public new static class Events {
            public static readonly Event<Item, ItemActionEvent> BeforeActionPerformed = new(nameof(BeforeActionPerformed));
            public static readonly Event<Item, ItemActionEvent> ActionPerformed = new(nameof(ActionPerformed));
            public static readonly Event<Item, QuantityChangedData> QuantityChanged = new(nameof(QuantityChanged));
            public static readonly Event<Item, QuantityChangedData> QuantityIncreased = new(nameof(QuantityIncreased));
            public static readonly Event<Item, QuantityChangedData> QuantityDecreased = new(nameof(QuantityDecreased));
            public static readonly Event<Item, SharpeningUI.SharpeningChangeData> ItemSharpened = new(nameof(ItemSharpened));

            public static readonly Event<Item, EquipmentSlotType> Equipped = new(nameof(Equipped));
            public static readonly Event<Item, EquipmentSlotType> Unequipped = new(nameof(Unequipped));
        }
        
        // === Constructors

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        Item() {}
        
        public Item(ItemTemplate template, int quantity = 1) {
            Template = template;
            Quantity = Template.CanStack ? quantity : 1;
            Level = new Stat(this, ItemStatType.Level, template.LevelBonus);
            WeightLevel = new Stat(this, ItemStatType.ItemWeightLevel, 0);
        }

        public Item(ItemTemplate template, int quantity, int itemLevel, int weightLevel = 0) {
            Template = template;
            Quantity = Template.CanStack ? quantity : 1;
            Level = new Stat(this, ItemStatType.Level, itemLevel);
            WeightLevel = new Stat(this, ItemStatType.ItemWeightLevel, weightLevel);
        }
        
        public Item(ItemSpawningDataRuntime itemSpawningData) : this(itemSpawningData.ItemTemplate, itemSpawningData.quantity, itemSpawningData.itemLvl, itemSpawningData.weightLvl) {
            _itemSpawningData = itemSpawningData;
        }

        Item(Item sourceItem, int quantityToTake) : this(sourceItem.Template, quantityToTake, sourceItem.Level.ModifiedInt) {
            if (sourceItem._itemSpawningData != null) {
                _itemSpawningData = sourceItem._itemSpawningData;
                _itemSpawningData.elementsData = sourceItem.TryGetRuntimeData();
            } else {
                _itemSpawningData = new ItemSpawningDataRuntime(sourceItem);
            }
        }

        public static Item JsonCreate() => new Item();
        
        // === Lifetime
        protected override void OnAfterDeserialize() {
            _attachmentTracker.SetOwner(this);
        }

        protected override void OnPreRestore() {
            using var attachmentGroups = Template.GetAttachmentGroups();
            _attachmentTracker.PreRestore(attachmentGroups.value);
        }

        protected override void OnInitialize() {
            _attachmentTracker = new AttachmentTracker();
            _attachmentTracker.SetOwner(this);
            using var attachmentGroups = Template.GetAttachmentGroups();
            _attachmentTracker.Initialize(attachmentGroups.value);
            Init();
        }
        
        protected override void OnRestore() {
            Init();
        }

        protected override bool CanBeRestored() {
            return Owner is { HasBeenDiscarded: false };
        }

        void Init() {
            _skillsInvoker = AddElement(new ItemSkillsInvoker());
            this.ListenTo(Model.Events.AfterFullyInitialized, SetupTexts, this);
            this.ListenTo(IItemOwner.Relations.OwnedBy.Events.AfterAttached, AfterOwnerAdded, this);
            this.ListenTo(IItemOwner.Relations.OwnedBy.Events.BeforeDetached, BeforeOwnerRemoved, this);
            this.ListenTo(Stats.Stat.Events.StatChanged(ItemStatType.Level), SetupTexts, this);
        }

        void AfterOwnerAdded(RelationEventData data) {
            MoveToDomain(data.to.CurrentDomain);
            _itemSpawningData?.TryToRetrieveElements(this);
            _itemSpawningData = null;
            TriggerChange();
        }

        void BeforeOwnerRemoved(RelationEventData data) {
            if (HasBeenDiscarded) return;
            MoveToDomain(Domain.CurrentScene());
            TriggerChange();
        }

        public void RequestSetupTexts() {
            if (_awaitingSetupTexts) {
                return;
            }
            _awaitingSetupTexts = true;
            DelaySetupTexts().Forget();
        }

        async UniTaskVoid DelaySetupTexts() {
            if (await AsyncUtil.DelayFrame(this)) {
                SetupTexts();
            }
            _awaitingSetupTexts = false;
        }

        void SetupTexts() {
            _cachedSkills = EffectsForDescription.ToList();
            _variablesById = SkillsUtils.ConstructVariableCache(_cachedSkills);
            _itemName = ConstructItemName();
            _flavor = new TokenText(Template.Flavor);
            _baseDescription = ConstructBaseDescription();
            _fullDescription = ConstructFullDescription();
            _requirementsDescription = new TokenText(TokenType.RpgStatDescription);
            _tooltip = ConstructTooltip();
        }

        TokenText ConstructItemName() {
            TokenText token = new();
            token.AddToken(new TokenText(Template.ItemName));
            return token;
        }

        TokenText ConstructFullDescription() {
            TokenText token = new(TokenType.TooltipText);
            token.AppendLine(Template.Description);
            return token;
        }

        TokenText ConstructBaseDescription() {
            TokenText token = new(TokenType.TooltipText);
            token.AppendLine(Template.Description);
            return token;
        }

        // === Operations
        public void EquipInSlot(EquipmentSlotType slotType) {
            var firstEquipSlot = _equippedInSlots.Count == 0;
            if (_equippedInSlots.Contains(slotType)) {
                Log.Critical?.Error($"Trying to equip item {this} in slot {slotType} that is already equipped!");
            } else {
                _equippedInSlots.Add(slotType);
                if (firstEquipSlot) {
                    this.Trigger(Events.Equipped, slotType);
                    _skillsInvoker.OnEquip();
                }
            }
        }

        public void UnequipInSlot(EquipmentSlotType slotType) {
            if (!_equippedInSlots.Remove(slotType)) {
                Log.Critical?.Error($"Trying to unequip item {this} from slot {slotType} that is not equipped!");
            } else if (_equippedInSlots.Count == 0) {
                this.Trigger(Events.Unequipped, slotType);
                _skillsInvoker.OnUnequip();
            }
        }

        public void PerformImmediate(ItemActionType actionType) {
            if (actionType == ItemActionType.Eat || actionType == ItemActionType.Use) {
                RewiredHelper.VibrateHighFreq(VibrationStrength.Medium, VibrationDuration.VeryShort);
            } else if (actionType == ItemActionType.Equip) {
                RewiredHelper.VibrateHighFreq(VibrationStrength.Medium, VibrationDuration.VeryShort);
            } else if (actionType == ItemActionType.Unequip) {
                RewiredHelper.VibrateLowFreq(VibrationStrength.Medium, VibrationDuration.VeryShort);
            }
            TryGetElement<ItemSkillsInvoker>()?.PerformImmediate(actionType);
        }

        public void StartPerforming(ItemActionType actionType) {
            TryGetElement<ItemSkillsInvoker>()?.StartPerforming(actionType);
        }
        
        public void EndPerforming(ItemActionType actionType) {
            TryGetElement<ItemSkillsInvoker>()?.EndPerforming(actionType);
        }
        
        public void CancelPerforming(ItemActionType actionType) {
            TryGetElement<ItemSkillsInvoker>()?.CancelPerforming(actionType);
        }

        public void Use() {
            PlayUseAudio();

            if (IsReadable) {
                PerformImmediate(ItemActionType.Read);
            } else if (IsEdible) {
                PerformImmediate(ItemActionType.Eat);
            } else if (IsUsable) {
                PerformImmediate(ItemActionType.Use);
            } else if (IsEquippable) {
                PerformImmediate(!IsEquipped ? ItemActionType.Equip : ItemActionType.Unequip);
            } else {
                PerformImmediate(ItemActionType.Use);
            }
        }

        string GetDisplayName() {
            string displayName;
            
            if (DebugProjectNames.Basic) {
                displayName = Template.name.Replace("ItemTemplate_", "");
            } else {
                displayName = _itemName?.GetValue(Character, this);
            }

            if (Template.CanHaveItemLevel) {
                if (GameConstants.Get.ItemLevelDatas.TryGetValue(Level.ModifiedInt, out ItemLevelData data)) {
                    return RichTextUtil.SmartFormatParams(data.itemNameAffix.ToString(), displayName);
                }

                if (Level.ModifiedInt != 0) {
                    return $"{displayName} {Level.ModifiedInt:+#;-#}";
                }
            }
            
            return displayName ?? "ItemName Null";
        }

        void PlayUseAudio() {
            if (IsEquippable && EquipmentType != EquipmentType.QuickUse) return;
            
            var audio = IsEquippable
                ? ItemAudioType.UseItem.RetrieveFrom(this)
                : CommonReferences.Get.AudioConfig.LightNegativeFeedbackSound;
            FMODManager.PlayOneShot(audio);
        }
        
        [UnityEngine.Scripting.Preserve]
        public void PlayAudioClip(ItemAudioType itemAudioType, bool asOneShot, params FMODParameter[] eventParams) {
            PlayAudioClip(itemAudioType.RetrieveFrom(this), asOneShot, eventParams);
        }
        
        public void PlayAudioClip(EventReference eventReference, bool asOneShot, params FMODParameter[] eventParams) {
            CharacterHandBase handBase = View<CharacterHandBase>();
            if (handBase != null) {
                handBase.PlayAudioClip(eventReference, asOneShot, eventParams);
            } else if (Owner is ICharacter character) {
                character.PlayAudioClip(eventReference, asOneShot, eventParams);
            }
        }
        
        public string UseActionName { get {
            if (IsReadable) {
                return LocTerms.UIItemsRead.Translate();
            } else if (IsEdible || IsConsumable) {
                return LocTerms.UIItemsEat.Translate();
            } else if (IsUsable) {
                return LocTerms.UIItemsUse.Translate();
            } else if (IsEquippable) {
                if (IsEquipped) {
                    return LocTerms.UIItemsUnequip.Translate();
                } else {
                    return LocTerms.UIItemsEquip.Translate();
                }
            } else {
                return null;
            }
        }}

        [UnityEngine.Scripting.Preserve] public void IncrementQuantity() => ChangeQuantity(1);
        [UnityEngine.Scripting.Preserve] public void DecrementQuantity() => ChangeQuantity(-1);

        [UnityEngine.Scripting.Preserve]
        public void IncrementQuantityWithoutNotification() {
            using var suspendNotification = new AdvancedNotificationBuffer.SuspendNotifications<ItemNotificationBuffer>();
            IncrementQuantity();
        }
        
        public void DecrementQuantityWithoutNotification() {
            using var suspendNotification = new AdvancedNotificationBuffer.SuspendNotifications<ItemNotificationBuffer>();
            DecrementQuantity();
        }
        
        public void SetQuantity(int amount) {
            int diff = amount - Quantity;
            ChangeQuantity(diff);
        }
        
        /// <returns>Whether item was discarded</returns>
        public bool ChangeQuantity(int amount) {
            if (amount > 0) {
                SetPickedTimestamp();
            }
            
            if (CanStack) {
                Quantity += amount;
                if (Inventory is HeroItems heroInv) {
                    ItemUtils.AnnounceGettingItem(Template, amount, heroInv.ParentModel);
                }
            } else {
                Template.ChangeQuantity(Inventory, amount, this);
            }

            if (amount != 0) {
                this.Trigger(amount > 0 ? Events.QuantityIncreased : Events.QuantityDecreased, new(this, amount));
            }

            if (Quantity <= 0) {
                Quantity = 0;
                if (!HasElement<CurrencyAsCraftingIngredient>()) {
                    Discard();
                    return true;
                }
            }

            TriggerChange();
            this.Trigger(Events.QuantityChanged, new(this, amount));
            Inventory?.TriggerChange();

            return false;
        }

        /// <summary>
        /// Item must be stackable and have enough quantity
        /// </summary>
        public Item TakeSome(int quantity) {
            if (!CanStack || quantity > Quantity) return null;

            var takeSome = new Item(this, quantity);
            if (quantity == Quantity && !HasElement<CurrencyAsCraftingIngredient>()) {
                Discard();
            }
            Quantity -= quantity;
            this.Trigger(Events.QuantityChanged, new(this, -quantity));
            Inventory?.TriggerChange();
            return takeSome;
        }

        public ItemElementsDataRuntime TryGetRuntimeData() {
            var data = new ItemElementsDataRuntime();
            bool anyData = false;
            
            if (TryGetElement(out ItemGems itemGems)) {
                anyData = true;
                data.availableSlots = itemGems.AvailableSlots;
                data.maxSlots = itemGems.MaxSlots;
            }

            if (TryGetElement(out GemAttached _)) {
                anyData = true;
                var gems = Elements<GemAttached>();

                foreach (var gem in gems) {
                    data.gemData.Add(new GemTemplateWithSkills {
                        gemTemplate = gem.Template,
                        skillReferences = gem.SkillRefs
                    });
                }
            }
            
            if (TryGetElement(out StolenItemElement stolenItemElement)) {
                anyData = true;
                data.crimeData = stolenItemElement.GetCrimeData();
            }

            if (TryGetElement(out Sketch sketch)) {
                anyData = true;
                data.sketchIndex = sketch.SketchIndex;
            }
            
            return anyData ? data : null;
        }

        // === Helpers
        TooltipConstructorTokenText ConstructTooltip() {
            TooltipConstructorTokenText token = new TooltipConstructorTokenText();
            // title
            TokenText title = new TokenText(TokenType.TooltipTitle, DisplayName);
            title.AppendLine("\n" + ItemUtils.ItemQualityText(this));
            title.AppendLine();
            title.AddToken(new TokenText(TokenType.ItemSkills));
            token.AddToken(title);

            // main text
            TokenText mainTextToken = new TokenText(TokenType.TooltipMainText);
            mainTextToken.AddToken(_fullDescription);
            if (ItemStats != null) {
                mainTextToken.AddToken(new TokenText(TokenType.ItemStats));
            }
            token.AddToken(mainTextToken);
            // keywords
            foreach (var keyword in SkillsUtils.KeywordDescriptions(Template.Description, Keywords)) {
                token.AddToken(new TokenText(TokenType.TooltipText, keyword));
            }
            return token;
        }

        public Stat Stat(StatType statType) {
            return statType switch {
                ItemStatType itemStats => itemStats.RetrieveFrom(this),
                ItemRequirementStatType itemRequirementStats => itemRequirementStats.RetrieveFrom(this),
                _ => null
            };
        }

        public Skill SkillForVariable(string variable, int index) {
            if (index == 0) {
                return _variablesById.GetValueOrDefault(variable);
            }

            return index < _cachedSkills.Count ? _cachedSkills[index] : null;
        }
        
        public void SetPickedTimestamp() {
            PickupTimestamp = DateTime.UtcNow.Ticks;
        }
    }
    
    public class ItemActionEvent {
        public ItemActionType ActionType { get; }
        public Item Item { get; }

        public ItemActionEvent(ItemActionType actionType, Item item) {
            this.ActionType = actionType;
            this.Item = item;
        }
    }

    public struct QuantityChangedData {
        public Item target;
        public int amount;
        
        public int CurrentQuantity => target?.Quantity ?? 0;

        public QuantityChangedData(Item target, int amount) {
            this.target = target;
            this.amount = amount;
        }
    }
}
