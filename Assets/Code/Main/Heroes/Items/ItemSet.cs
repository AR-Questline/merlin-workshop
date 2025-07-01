using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.CharacterCreators.PresetSelection;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Heroes.Development.WyrdPowers;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items {
    [CreateAssetMenu(menuName = "TG/Debug/Items Set")]
    public class ItemSet : ScriptableObject, ITemplate {
        // === ITemplate
        [SerializeField] string guid;
        [SerializeField, HideInInspector] TemplateMetadata metadata = new();
        
        string ITemplate.GUID { 
            get => guid;
            set => guid = value;
        }
        TemplateMetadata ITemplate.Metadata => metadata;
        
        string INamed.DisplayName => string.Empty;
        string INamed.DebugName => name;
        // === ItemSet
        [SerializeField, ToggleGroup(nameof(modifiesLevel), groupTitle: "Modifies Level")] bool modifiesLevel;
        [SerializeField, ToggleGroup(nameof(modifiesLevel))] int levelToSet = 2;
        [SerializeField, Range(0, 1f), ToggleGroup(nameof(modifiesLevel))] float percentToNextLevel;
        
        [Space]
        [SerializeField]
        bool modifiesLoadouts;
        [SerializeField, ShowIf(nameof(modifiesLoadouts)), FoldoutGroup("Loadout Set"), InlineProperty, HideLabel]
        LoadoutSet loadoutSet;
        
        [Space]
        [SerializeReference] ILootTable[] items;
        [SerializeField, TemplateType(typeof(IRecipe))] List<TemplateReference> recipesToLearn = new();
        [ListDrawerSettings(CustomAddFunction = nameof(CustomAddTalentPreset))] public TalentPreset[] talents = Array.Empty<TalentPreset>();
        
        [Space]
        public bool modifiesStats;
        [ListDrawerSettings(ShowPaging = true, NumberOfItemsPerPage = 9), ShowIf(nameof(modifiesStats))]
        public StatPreset[] stats = Array.Empty<StatPreset>();
        
        [Space]
        public WyrdSoulFragmentType[] wyrdSoulFragmentTypes = Array.Empty<WyrdSoulFragmentType>();
        [SerializeField]
        TagTargetStatePair[] storyFlagsToSet = Array.Empty<TagTargetStatePair>();

        [PropertySpace]
        [UsedImplicitly, UnityEngine.Scripting.Preserve, Obsolete("Use Apply Full instead. Items are no longer equipped from inventory but from a dedicated loadout set"), EnableIf(nameof(CanUseContexts))]
        public void AddToHeroInventory(bool ignoreLevelSettings = false) {
            Hero hero = Hero.Current;
            ApplyItems(hero);
            ApplyRecipes(hero);
            
            if (!ignoreLevelSettings) {
                ApplyLevelSettings();
            }
        }

        [Button("Apply full", ButtonSizes.Medium), EnableIf(nameof(CanUseContexts))]
        public void ApplyFull(
            bool withEquipping = true, bool ignoreLevelSettings = false, bool withTalents = true, bool withStats = true,
            bool withWyrdSkill = true) {
            Hero hero = Hero.Current;
            
            bool bufferWasBlocked = AdvancedNotificationBuffer.AllNotificationsSuspended;
            AdvancedNotificationBuffer.AllNotificationsSuspended = true;
            try {
                ApplyRecipes(hero);

                if (!ignoreLevelSettings) {
                    ApplyLevelSettings();
                }

                if (withStats) {
                    ApplyStats(hero, stats);
                }

                if (withTalents) {
                    ApplyTalents(hero);
                }

                ApplyItems(hero);
                ApplyLoadout(withEquipping);

                if (withWyrdSkill) {
                    ApplyWyrdSoulFragments();
                }

                if (withStats || withTalents) {
                    hero.RestoreStats();
                }
                
                foreach (var flag in storyFlagsToSet) {
                    if (TagUtils.IsValidTag(flag.flag)) {
                        StoryFlags.Set(flag.flag, flag.state);
                    } else {
                        Log.Minor?.Warning("Invalid flag " + flag.flag);
                    }
                }
            } finally {
                AdvancedNotificationBuffer.AllNotificationsSuspended = bufferWasBlocked;
            }
        }

        public void ApplyWyrdSoulFragments() {
            if (wyrdSoulFragmentTypes == null) {
                return;
            }
            
            foreach (var wyrdSoulType in wyrdSoulFragmentTypes) {
                Hero.Current.Development.WyrdSoulFragments.Unlock(wyrdSoulType);
            }
        }
        
        [Button("Apply loadout", ButtonSizes.Medium), EnableIf(nameof(CanUseContexts))]
        public void ApplyLoadout(bool withEquipping = true) {
            if (!modifiesLoadouts || !loadoutSet.IsSet) return;
            
            HeroItems heroItems = Hero.Current.HeroItems;
            (ItemSpawningDataRuntime, ItemSpawningDataRuntime)[] loadout = loadoutSet.Loadouts;
            
            for (int i = 0; i < loadout.Length; i++) {
                if (withEquipping) {
                    heroItems.ActivateLoadout(i);
                }
                
                heroItems.Unequip(EquipmentSlotType.MainHand, heroItems.LoadoutAt(i));
                heroItems.Unequip(EquipmentSlotType.OffHand, heroItems.LoadoutAt(i));

                Item item1 = null;
                if (loadout[i].Item1?.ItemTemplate != null) {
                    Item item1InInventory = heroItems.Items.FirstOrDefault(item => item.Template == loadout[i].Item1.ItemTemplate 
                                                                                   && item.Level == loadout[i].Item1.itemLvl);
                    if (item1InInventory == null) {
                        item1 = new Item(loadout[i].Item1);
                        World.Add(item1);
                        Hero.Current.Inventory.Add(item1);
                    } else {
                        item1 = item1InInventory;
                    }

                    if (withEquipping) {
                        heroItems.Equip(item1, EquipmentSlotType.MainHand);
                    }
                }

                if (loadout[i].Item2?.ItemTemplate != null) {
                    Item item2;
                    var item2InInventory = heroItems.Items.FirstOrDefault(item => item.Template == loadout[i].Item2.ItemTemplate 
                                                                                  && item.Level == loadout[i].Item2.itemLvl 
                                                                                  && item != item1);
                    if (item2InInventory == null) {
                        item2 = new Item(loadout[i].Item2);
                        World.Add(item2);
                        Hero.Current.Inventory.Add(item2);
                    } else {
                        item2 = item2InInventory;
                    }

                    if (withEquipping) {
                        heroItems.Equip(item2, EquipmentSlotType.OffHand);
                    }
                }
            }

            ItemTemplate[] loadoutSetArmorSet = loadoutSet.ArmorSet;
            for (int i = 0; i < loadoutSetArmorSet.Length; i++) {
                var item = World.Add(new Item(loadoutSetArmorSet[i]));
                Hero.Current.Inventory.Add(item);
                if (withEquipping && item.IsEquippable && !item.IsEquipped) {
                    heroItems.Equip(item);
                }
            }
            
            heroItems.ActivateLoadout(0);
        }

        public void ApplyItems(Hero hero) {
            foreach (Item item in SpawnItems()) {
                World.Add(item);
                hero.Inventory.Add(item);
            }
        }

        public void ApplyRecipes(Hero hero) {
            HeroRecipes heroRecipes = hero.Element<HeroRecipes>();
            foreach (TemplateReference recipeTemp in recipesToLearn) {
                if (recipeTemp is { IsSet: true }) {
                    heroRecipes.LearnRecipe(recipeTemp.Get<IRecipe>());
                }
            }
        }

        public void ApplyLevelSettings() {
            if (modifiesLevel) {
                Hero.Current.Development.LevelUpTo(levelToSet);
                Hero.Current.Development.RewardExpAsPercentOfNextLevel(percentToNextLevel);
            }
        }

        public void ApplyTalents(Hero hero) {
            if (talents.IsNotNullOrEmpty()) {
                Hero.Current.Talents.Reset();
                for (int i = 0; i < talents.Length; i++) {
                    TalentTemplate talent = talents[i].Talent;
                    if (talent == null) {
                        Log.Minor?.Warning("Talent not set on preset " + name);
                        continue;
                    }

                    ItemSet.LevelUpTalent(talent, talents[i].level);
                }
            }
        }
        
        public static bool ApplyStats(Hero hero, StatPreset[] stats, bool ignoreLevelSetting = false) {
            bool levelHasBeenSet = false;
            foreach (var stat in stats) {
                if (stat.StatType == CharacterStatType.Level) {
                    if (ignoreLevelSetting) continue;
                    // This is a special case, we don't want to set the level directly
                    hero.Development.LevelUpTo((int) stat.baseValue);
                    levelHasBeenSet = true;
                    continue;
                }
                
                Stat targetStat = hero.Stat(stat.StatType);
                if (targetStat == null) {
                    Log.Important?.Error($"Stat {stat.StatType} not found on hero");
                    continue;
                }
                targetStat.SetTo(stat.baseValue);
            }
            return levelHasBeenSet;
        }

        [Obsolete, EnableIf(nameof(CanUseContexts))] [UnityEngine.Scripting.Preserve]
        public void EquipToHeroInventory() {
            HeroItems heroItems = Hero.Current.HeroItems;
            int initialIndex = heroItems.CurrentLoadoutIndex;
            
            foreach (Item item in SpawnItems()) {
                World.Add(item);
                Hero.Current.Inventory.Add(item);
                
                if (item.HasBeenDiscarded) {
                    // stackables case
                    continue;
                }
                
                if (item.IsEquippable && !item.IsEquipped) {
                    var equip = item.Element<ItemEquip>();
                    var slot = equip.GetBestEquipmentSlotType();
                    bool mainHand = slot == EquipmentSlotType.MainHand;
                    bool offHand = slot == EquipmentSlotType.OffHand;

                    if (heroItems.CurrentLoadout is HeroLoadout currentLoadout &&
                        heroItems.CurrentLoadoutIndex < HeroLoadout.Count - 2 &&
                        (mainHand && !currentLoadout.PrimaryItem.IsFists ||
                         offHand && !currentLoadout.SecondaryItem.IsFists)) {
                        heroItems.ActivateLoadout(heroItems.CurrentLoadoutIndex + 1);
                    }

                    Hero.Current.HeroItems.Equip(item);
                }
            }
            heroItems.ActivateLoadout(initialIndex);
        }
        
        /// <param name="talent">Failure</param>
        public static bool LevelUpTalent(TalentTemplate talent, int levels) {
            Talent heroTalent = Hero.Current.Talents.TalentOf(talent, CharacterStatType.TalentPoints);
            
            if (heroTalent == null) {
                Log.Important?.Error("Talent not found on hero " + talent);
                return true;
            }
            bool failed = false;
            for (int i = heroTalent.Level; i < levels; i++) {
                if (LevelUpTalent(heroTalent)) {
                    failed = true;
                    break;
                }
            }
            heroTalent.ApplyTemporaryLevels();
            return failed;
        }
        
        /// <summary>
        /// Recursively levels up a talent and its parent if required.
        /// Will error if player does not have enough points to level up.
        /// </summary>
        /// <returns>Failure</returns>
        static bool LevelUpTalent(Talent talent) {
            if (talent.CanAcquireNextLevel(out var reason)) {
                talent.AcquireNextTemporaryLevel();
                return false;
            }
            
            if (reason is Talent.AcquiringProblem.RowNotAccessible or Talent.AcquiringProblem.ParentLocked) {
                LevelUpTalent(talent.Parent, 1);
                
                // Try again
                if (talent.CanAcquireNextLevel(out reason)) {
                    talent.AcquireNextTemporaryLevel();
                    return false;
                }
            } 
            
            Log.Important?.Error("Cannot level up talent " + talent.Template + " because " + reason);
            return true;
        }

        IEnumerable<Item> SpawnItems() {
            return items.SelectMany(lootTable => lootTable.PopLoot(this).items
                .Where(static itemSpawningData => itemSpawningData?.ItemTemplate != null)
                .Select(static itemSpawningData => new Item(itemSpawningData)));
        }

        bool CanUseContexts() {
            return Application.isPlaying && Hero.Current != null;
        }
        
        [Serializable]
        struct TagTargetStatePair {
            [Tags(TagsCategory.Flag), HideLabel]
            public string flag;
            public bool state;
        }
        
        // === Odin
        TalentPreset CustomAddTalentPreset() {
            var preset = new TalentPreset {
                level = 1
            };
            return preset;
        }
        
        [Serializable]
        public struct LoadoutSet {
            const float LevelPropertyWidth = 75;
            const float LevelLabelWidth = 35;
            const string LevelLabel = "Level";
            
            // Loadout 1
            [TemplateType(typeof(ItemTemplate)), SerializeField, BoxGroup("Loadout 1"), HorizontalGroup("Loadout 1/Item 1"), HideLabel] TemplateReference l1Slot1;
            [SerializeField, BoxGroup("Loadout 1"), HorizontalGroup("Loadout 1/Item 1", Width = LevelPropertyWidth), LabelText(LevelLabel), LabelWidth(LevelLabelWidth)] uint l1Slot1Lv;
            [TemplateType(typeof(ItemTemplate)), SerializeField, BoxGroup("Loadout 1"), HorizontalGroup("Loadout 1/Item 2"), HideLabel] TemplateReference l1Slot2;
            [SerializeField, BoxGroup("Loadout 1"), HorizontalGroup("Loadout 1/Item 2", Width = LevelPropertyWidth), LabelText(LevelLabel), LabelWidth(LevelLabelWidth)] uint l1Slot2Lv;
            
            // Loadout 2
            [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(20)"), ShowInInspector] string _space_ODIN;
            [TemplateType(typeof(ItemTemplate)), SerializeField, BoxGroup("Loadout 2"), HorizontalGroup("Loadout 2/Item 1"), HideLabel] TemplateReference l2Slot1;
            [SerializeField, BoxGroup("Loadout 2"), HorizontalGroup("Loadout 2/Item 1", Width = LevelPropertyWidth), LabelText(LevelLabel), LabelWidth(LevelLabelWidth)] uint l2Slot1Lv;
            [TemplateType(typeof(ItemTemplate)), SerializeField, BoxGroup("Loadout 2"), HorizontalGroup("Loadout 2/Item 2"), HideLabel] TemplateReference l2Slot2;
            [SerializeField, BoxGroup("Loadout 2"), HorizontalGroup("Loadout 2/Item 2", Width = LevelPropertyWidth), LabelText(LevelLabel), LabelWidth(LevelLabelWidth)] uint l2Slot2Lv;
            
            // Loadout 3
            [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(20)"), ShowInInspector] string _space_ODIN2;
            [TemplateType(typeof(ItemTemplate)), SerializeField, BoxGroup("Loadout 3"), HorizontalGroup("Loadout 3/Item 1"), HideLabel] TemplateReference l3Slot1;
            [SerializeField, BoxGroup("Loadout 3"), HorizontalGroup("Loadout 3/Item 1", Width = LevelPropertyWidth), LabelText(LevelLabel), LabelWidth(LevelLabelWidth)] uint l3Slot1Lv;
            [TemplateType(typeof(ItemTemplate)), SerializeField, BoxGroup("Loadout 3"), HorizontalGroup("Loadout 3/Item 2"), HideLabel] TemplateReference l3Slot2;
            [SerializeField, BoxGroup("Loadout 3"), HorizontalGroup("Loadout 3/Item 2", Width = LevelPropertyWidth), LabelText(LevelLabel), LabelWidth(LevelLabelWidth)] uint l3Slot2Lv;
            
            // Loadout 4
            [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(20)"), ShowInInspector] string _space_ODIN3;
            [TemplateType(typeof(ItemTemplate)), SerializeField, BoxGroup("Loadout 4"), HorizontalGroup("Loadout 4/Item 1"), HideLabel] TemplateReference l4Slot1;
            [SerializeField, BoxGroup("Loadout 4"), HorizontalGroup("Loadout 4/Item 1", Width = LevelPropertyWidth), LabelText(LevelLabel), LabelWidth(LevelLabelWidth)] uint l4Slot1Lv;
            [TemplateType(typeof(ItemTemplate)), SerializeField, BoxGroup("Loadout 4"), HorizontalGroup("Loadout 4/Item 2"), HideLabel] TemplateReference l4Slot2;
            [SerializeField, BoxGroup("Loadout 4"), HorizontalGroup("Loadout 4/Item 2", Width = LevelPropertyWidth), LabelText(LevelLabel), LabelWidth(LevelLabelWidth)] uint l4Slot2Lv;
            
            [TemplateType(typeof(ItemTemplate)), SerializeField] TemplateReference[] armorSet;

            // ReSharper disable InconsistentNaming
            ItemSpawningDataRuntime l1Slot1Item => new(l1Slot1.TryGet<ItemTemplate>(), 1, (int)l1Slot1Lv);
            ItemSpawningDataRuntime l1Slot2Item => new(l1Slot2.TryGet<ItemTemplate>(), 1, (int)l1Slot2Lv);

            ItemSpawningDataRuntime l2Slot1Item => new(l2Slot1.TryGet<ItemTemplate>(), 1, (int)l2Slot1Lv);
            ItemSpawningDataRuntime l2Slot2Item => new(l2Slot2.TryGet<ItemTemplate>(), 1, (int)l2Slot2Lv);

            ItemSpawningDataRuntime l3Slot1Item => new(l3Slot1.TryGet<ItemTemplate>(), 1, (int)l3Slot1Lv);
            ItemSpawningDataRuntime l3Slot2Item => new(l3Slot2.TryGet<ItemTemplate>(), 1, (int)l3Slot2Lv);

            ItemSpawningDataRuntime l4Slot1Item => new(l4Slot1.TryGet<ItemTemplate>(), 1, (int)l4Slot1Lv);
            ItemSpawningDataRuntime l4Slot2Item => new(l4Slot2.TryGet<ItemTemplate>(), 1, (int)l4Slot2Lv);
            // ReSharper restore InconsistentNaming

            public ItemTemplate[] ArmorSet => armorSet.Select(reference => reference.TryGet<ItemTemplate>()).WhereNotNull().ToArray();

            public (ItemSpawningDataRuntime, ItemSpawningDataRuntime)[] Loadouts => new[] {
                (l1Slot1Item, l1Slot2Item),
                (l2Slot1Item, l2Slot2Item),
                (l3Slot1Item, l3Slot2Item),
                (l4Slot1Item, l4Slot2Item)
            };

            public bool IsSet => ReferenceIsSet(l1Slot1) || ReferenceIsSet(l1Slot2) ||
                                 ReferenceIsSet(l2Slot1) || ReferenceIsSet(l2Slot2) ||
                                 ReferenceIsSet(l3Slot1) || ReferenceIsSet(l3Slot2) ||
                                 ReferenceIsSet(l4Slot1) || ReferenceIsSet(l4Slot2);

            bool ReferenceIsSet(TemplateReference template) {
                return template != null && template.IsSet;
            }
        }
    }
}
