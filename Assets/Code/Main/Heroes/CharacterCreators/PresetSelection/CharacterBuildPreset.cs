using System;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterCreators.PresetSelection {
    [Serializable]
    public struct CharacterBuildPreset {
        [LocStringCategory(Category.CharacterCreator)]
        public LocString name;
        [LocStringCategory(Category.CharacterCreator)]
        public LocString description;
        public bool cleanPreset;

        [UIAssetReference, ShowAssetPreview]
        public SpriteReference icon;
        
        [Space]
        [InfoBox("Changes to talents and stats in this preset will override the ones set in the starting gear")]
        public ItemSet startingGear;
        public ItemSet[] itemsOnly;
        
        public TalentPreset[] talents;
        [Space]
        public bool modifiesStats;
        [ListDrawerSettings(ShowPaging = true, NumberOfItemsPerPage = 9), ShowIf(nameof(modifiesStats))]
        public StatPreset[] stats;
        
        public bool EditorUpdate() {
            if (!modifiesStats) {
                stats = Array.Empty<StatPreset>();
                return true;
            }
            
            if (stats.IsNullOrEmpty()) {
                stats = DefaultStats;
                return true;
            }
            return false;
        }

        public void Apply() {
            // Uninitialized struct
            if (name == null) return;
            
            Hero hero = Hero.Current;
            if (hero == null) {
                Log.Important?.Error("Cannot apply preset without a hero");
                return;
            }

            bool bufferWasBlocked = AdvancedNotificationBuffer.AllNotificationsSuspended;
            AdvancedNotificationBuffer.AllNotificationsSuspended = true;
            try {
                // So that we can use existing presets that set level and be able to override it
                bool levelHasBeenSet = false;

                if (modifiesStats && stats.IsNotNullOrEmpty()) {
                    levelHasBeenSet = ItemSet.ApplyStats(hero, stats);
                }

                if (startingGear != null) {
                    startingGear.ApplyFull(
                        ignoreLevelSettings: levelHasBeenSet,
                        withTalents: talents.IsNullOrEmpty(),
                        withStats: !modifiesStats);
                }

                if (!itemsOnly.IsNullOrUnityEmpty()) {
                    for (int i = 0; i < itemsOnly.Length; i++) {
                        ItemSet itemSet = itemsOnly[i];
                        if (itemSet == null) {
                            continue;
                        }
                        itemSet.ApplyItems(hero);
                        itemSet.ApplyRecipes(hero);
                    }
                }

                if (talents.IsNotNullOrEmpty()) {
                    Hero.Current.Talents.Reset();
                    for (int i = 0; i < talents.Length; i++) {
                        TalentTemplate talent = talents[i].Talent;
                        if (talent == null) {
                            Log.Minor?.Error("Talent not set on preset " + name);
                            continue;
                        }

                        ItemSet.LevelUpTalent(talent, talents[i].level);
                    }
                }

                // Restore stats that have had their max values modified
                hero.RestoreStats();
                if (cleanPreset == false) {
                    hero.Development.BaseStatPoints.SetTo(0);
                }
            } finally {
                AdvancedNotificationBuffer.AllNotificationsSuspended = bufferWasBlocked;
            }
        }
        
        [Button]
        void EditorApply() {
            Hero hero = Hero.Current;
            if (hero == null) {
                Log.Important?.Error("Cannot apply preset without a hero");
                return;
            }
            
            Hero.Current.Talents.Reset();
            foreach (var item in Hero.Current.Inventory.Items.ToList()) {
                item.Discard();
            }
            
            Apply();

            ItemSet basicItems = Resources.Load<ItemSet>("Data/ItemSets/BasicItems");
            if (basicItems != null) {
                basicItems.ApplyFull(hero);
            }
        }

        static readonly StatPreset[] DefaultStats = new[] {
            new StatPreset {
                stat = new RichEnumReference(CharacterStatType.Level),
                baseValue = 20
            },
            new StatPreset {
                stat = new RichEnumReference(HeroRPGStatType.Strength),
                baseValue = 0
            },
            new StatPreset {
                stat = new RichEnumReference(HeroRPGStatType.Dexterity),
                baseValue = 0
            },
            new StatPreset {
                stat = new RichEnumReference(HeroRPGStatType.Spirituality),
                baseValue = 0
            },
            new StatPreset {
                stat = new RichEnumReference(HeroRPGStatType.Perception),
                baseValue = 0
            },
            new StatPreset {
                stat = new RichEnumReference(HeroRPGStatType.Endurance),
                baseValue = 0
            },
            new StatPreset {
                stat = new RichEnumReference(HeroRPGStatType.Practicality),
                baseValue = 0
            }
        };
    }
    
    [Serializable]
    public struct StatPreset {
        [RichEnumExtends(typeof(StatType))]
        public RichEnumReference stat;
        public float baseValue;
        
        public StatType StatType => stat.EnumAs<StatType>();
    }
    
    [Serializable]
    public struct TalentPreset {
        [SerializeField, TemplateType(typeof(TalentTemplate))]
        TemplateReference talent;
        public int level;
        
        public TalentTemplate Talent => talent?.Get<TalentTemplate>();
    }
}
