using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.Combat.Behaviours.CustomBehaviours;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Development;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Heroes.Stats.StatConfig;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Animations.FightingStyles;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Sessions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Fights.NPCs {
    public class NpcTemplate : Template, ITagged, CharacterStats.ITemplate, AliveStats.ITemplate, StatusStats.ITemplate {
        [SerializeField, TemplateType(typeof(FactionTemplate)), PropertyOrder(-10), HideLabel]
        TemplateReference faction;
        [SerializeField, TemplateType(typeof(CrimeOwnerTemplate)), PropertyOrder(-10), HideLabel]
        TemplateReference defaultCrimeOwner;

#if UNITY_EDITOR
        [ShowInInspector, DisplayAsString(15), HideLabel, EnableGUI, PropertyOrder(-9), InlineButton("FindUsages", "Find Usages"), PropertySpace(5, 5), Indent]
        string EditorDisplayName => StringUtil.NicifyName(gameObject.name.Replace("NPCTemplate_", ""));
#endif
        
        [SerializeField, InlineButton("SetHumanStats"), InlineButton("SetBigHumanStats"), InlineButton("SetDefaultPoiseThreshold"), FoldoutGroup("Combat Behaviour")]
        int level = 1;
        [SerializeField, FoldoutGroup("Combat Behaviour")]
        NpcData npcData;
        [SerializeField, FoldoutGroup("Combat Behaviour"), TemplateType(typeof(NpcFightingStyle))]
        TemplateReference fightingStyle;
        [FoldoutGroup("Combat Behaviour"), Space] 
        public bool isNotGrounded;
        [FoldoutGroup("Combat Behaviour")] 
        public bool useRichAISlowdownTime = true;
        [FoldoutGroup("Combat Behaviour")]
        public bool requiresPathToTarget = true;
        [FoldoutGroup("Combat Behaviour"), Space] 
        public bool isSpellCaster;
        [FoldoutGroup("Combat Behaviour")] 
        public bool canDealDamageToFriendlies;
        [SerializeField, FoldoutGroup("Combat Behaviour")]
        ReturnToSpawnPointArchetype returnToSpawnPointArchetype = ReturnToSpawnPointArchetype.Defensive;

        [FoldoutGroup("Stats"), SerializeField] 
        int maxHealth = 100;
        [FoldoutGroup("Stats"), SerializeField] 
        float healthRegen;
        [FoldoutGroup("Stats"), SerializeField, Space]
        int maxStamina = 100; 
        [FoldoutGroup("Stats"), SerializeField] 
        float staminaRegenPerTick = 0.25f;
        [FoldoutGroup("Stats"), SerializeField] 
        float staminaUsageMultiplier = 1f;
        // Currently obsolete, use isSpellCaster bool.
        // [FoldoutGroup("Stats"), SerializeField]
        // int maxMana = 100;
        // [FoldoutGroup("Stats"), SerializeField] 
        // float manaUsageMultiplier = 0;
        
        [FoldoutGroup("Stats"), ShowInInspector]
        public int EffectiveHealth => (int)CalculateEffectiveHealth();
        [FoldoutGroup("Stats"), ShowInInspector]
        public string ArmorReduction => Damage.GetArmorDamageReduction(armor).ToString("P");
        [FoldoutGroup("Stats"), ShowInInspector]
        public float DifficultyScore => EffectiveHealth + StrengthLinear * 5;
        
        [FoldoutGroup("Combat Stats"), SerializeField] 
        bool canBeWyrdEmpowered = true;
        [FoldoutGroup("Combat Stats"), SerializeField] 
        bool canEnterCombat = true;
        [FoldoutGroup("Combat Stats"), SerializeField, Indent]
        public float meleeDamage = 10;
        [FoldoutGroup("Combat Stats"), SerializeField, Indent]
        public float rangedDamage = 10;
        [FoldoutGroup("Combat Stats"), SerializeField, Indent]
        public float magicDamage = 10;
        [FoldoutGroup("Combat Stats"), SerializeField, Space] 
        int armor;
        [FoldoutGroup("Combat Stats"), SerializeField] 
        float armorMultiplier = 1;
        [FoldoutGroup("Combat Stats"), ListDrawerSettings(CustomAddFunction = nameof(AddDefaultDamageReceivedMultiplier)), SerializeField]
        List<DamageReceivedMultiplierDataConfig> damageReceivedMultipliers = new ();
        [FoldoutGroup("Combat Stats"), SerializeField] 
        int statusResistance;
        [FoldoutGroup("Combat Stats"), SerializeField, Title("Force")] 
        float forceStumbleThreshold = 15;
        [FoldoutGroup("Combat Stats"), Tooltip("This value sets npc ragdoll weight")]
        public int npcWeight = 80;
        [FoldoutGroup("Combat Stats")] 
        public LayerMask npcHitMask = (1 << 24) | (1 << 31);
        [FoldoutGroup("Combat Stats")] 
        public float poiseThreshold = 35;
        [FoldoutGroup("Combat Stats"), Range(0, 100)] 
        public int blockValue = 50;
        [FoldoutGroup("Combat Stats")] 
        public float blockPenaltyMultiplier = 1f;
        [FoldoutGroup("Combat Stats")] 
        public int combatSlotsLimit = 2;
        [FoldoutGroup("Combat Stats")] 
        public int heroKnockBack = 50;
        
        [SerializeField, InlineProperty, FoldoutGroup("Status Stats"), HideLabel]
        StatusStatsValues statusStats = new();
        
        [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(10)"), ShowInInspector] string _space_ODIN;

        [SerializeField, FoldoutGroup("Crimes")]
        CrimeNpcValue crimeNpcValue = CrimeNpcValue.None;
#if UNITY_EDITOR
        [InfoBox("Must have exactly one GuardIntervention", InfoMessageType.Error, nameof(GuardWitNotOneGuardBehaviour))]
        [InfoBox("Has no GuardIntervention story", InfoMessageType.Error, nameof(GuardWithoutGuardStory))]
#endif
        [SerializeField, FoldoutGroup("Crimes")] CrimeReactionArchetype crimeReactionArchetype = CrimeReactionArchetype.None;
        [SerializeField, FoldoutGroup("Crimes")] NpcType npcType = NpcType.Normal;
        
        [FoldoutGroup("Rewards"), SerializeField]
        int expLevel;
        [FoldoutGroup("Rewards"), SerializeField] 
        StatDefinedRange expTier;
        [FoldoutGroup("Rewards"), SerializeField, ShowIf(nameof(IsExpCustom))]
        int expReward;
        [FoldoutGroup("Rewards"), ShowInInspector, HideIf(nameof(IsExpCustom))]
        public int ExpReward => GetExpReward();
        bool IsExpCustom => expTier == StatDefinedRange.Custom;

        [FoldoutGroup("Items"), SerializeField] public bool isPickpocketable = true;
        [FoldoutGroup("Items"), SerializeField] public bool isDeadBodyLootable = true;
        [FoldoutGroup("Items"), SerializeField, TemplateType(typeof(ItemTemplate)), InlineButton(nameof(TryGetFistFromInventoryItems), "Get From Inventory")] 
        public TemplateReference fistsTemplate;
        [SerializeField, InlineProperty, HideLabel]
        [FoldoutGroup("Items/Inventory Items")] public LootTableWrapper inventoryItems;
        [Space]
        [FoldoutGroup("Items"), SerializeField] public List<LootTableWrapper> lootTables = new();
        [FoldoutGroup("Items"), SerializeField] public List<LootTableWrapper> corpseLootTables = new();
        [FoldoutGroup("Items"), SerializeField] List<LootTableWrapper> wyrdConvertedLootTables = new();

        [FoldoutGroup("Tags"), Tags(TagsCategory.EnemyType), SerializeField]
        string[] tags = Array.Empty<string>();
        [FoldoutGroup("Tags"), Tags(TagsCategory.Location), SerializeField, Space]
        string difficultyTag;

        [CustomValueDrawer("@Awaken.TG.EditorOnly.OdinHelpers.Space(10)"), ShowInInspector] string _space_ODIN3;
        
        [FoldoutGroup("Audio"), RichEnumExtends(typeof(SurfaceType)), SerializeField] RichEnumReference surfaceType = SurfaceType.HitFlesh;
        [FoldoutGroup("Audio"), SerializeField] bool canTriggerAggroMusic = true;
        [FoldoutGroup("VFX"), ARAssetReferenceSettings(new[] {typeof(GameObject)}, true, AddressableGroup.VFX)] [UnityEngine.Scripting.Preserve]
        public ShareableARAssetReference onHitCriticalVFX;
        [FoldoutGroup("VFX")] public TattooType tattooType;
        [FoldoutGroup("Animations"), SerializeField, ClipTransitionAssetReference] ARAssetReference dummyDeathClipTransition;
        
        public FactionTemplate Faction => faction.Get<FactionTemplate>(this) ?? World.Services.Get<FactionProvider>().Root;
        public FactionTemplate FactionEditorContext => faction.Get<FactionTemplate>();
        public CrimeOwnerTemplate DefaultCrimeOwner => defaultCrimeOwner.TryGet<CrimeOwnerTemplate>();
        public NpcData Data => npcData;
        public CrimeNpcValue CrimeValue => crimeNpcValue;
        public CrimeReactionArchetype CrimeReactionArchetype => crimeReactionArchetype;
        public ReturnToSpawnPointArchetype ReturnToSpawnPointArchetype => returnToSpawnPointArchetype;
        public NpcType NpcType => npcType;
        public int MaxHealth => maxHealth;
        public int Level => level;
        public int TalentPoints => 0;
        public int BaseStatPoints => 0;
        public int MaxStamina => maxStamina;
        public float StaminaRegen => staminaRegenPerTick;
        public float StaminaUsageMultiplier => staminaUsageMultiplier;
        public int MaxMana => isSpellCaster ? int.MaxValue : 0;
        public float ManaUsageMultiplier => isSpellCaster ? 1f : 0f;
        public float ManaRegen => 0f;
        public float ManaRegenPercentage => 0f;
        public float Strength => 1f;
        public float StrengthLinear => 0f;
        public float Evasion => 0f;
        public float Resistance => 0f;
        public int Armor => armor;
        public float ArmorMultiplier => armorMultiplier;
        public float StatusResistance => statusResistance;
        public float ForceStumbleThreshold => forceStumbleThreshold;
        public float TrapDamageMultiplier => 1f;
        public SurfaceType SurfaceType => surfaceType.EnumAs<SurfaceType>();
        public bool CanTriggerAggroMusic => canTriggerAggroMusic;
        public bool CanBeWyrdEmpowered => canBeWyrdEmpowered;
        public bool CanEnterCombat => canEnterCombat;
        public bool IsNotGrounded => isNotGrounded;
        public bool UseRichAISlowdownTime => useRichAISlowdownTime;
        public int CombatSlotsLimit => combatSlotsLimit;
        public ARAssetReference DummyDeathClipTransition => dummyDeathClipTransition;

        public IEnumerable<ILootTable> Loot => lootTables?.Select(lt => lt.LootTable(this));
        public IEnumerable<ILootTable> CorpseLoot => corpseLootTables?.Select(lt => lt.LootTable(this));
        public IEnumerable<ILootTable> WyrdConvertedLoot => wyrdConvertedLootTables.IsNotNullOrEmpty()
            ? wyrdConvertedLootTables?.Select(lt => lt.LootTable(this))
            : GameConstants.Get.wyrdConvertedFallbackLoot.LootTable(this).Yield();

        public string DifficultyTag => difficultyTag;

        Cached<NpcTemplate, List<string>> _tags = new(static template => {
            var tags = template.WithAbstractTags(template.tags ?? Enumerable.Empty<string>());
            if (!string.IsNullOrWhiteSpace(template.difficultyTag)) {
                tags.Add(template.difficultyTag);
            }
            return tags;
        });
        public ICollection<string> Tags => _tags.Get(this);

        public PooledList<NpcTemplate> AbstractTypes => this.Abstracts<NpcTemplate>();
        public bool IsPreyAnimal => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractPreyAnimal);
        public bool IsHumanoid => this.InheritsFrom(CommonReferences.Get.TemplateService.AbstractHumanoid);
        
        public ref StatusStatsValues StatusStats => ref statusStats;

        Cached<NpcTemplate, NpcFightingStyle> _fightingStyle = new(static template => {
            var fightingStyle = template.fightingStyle.Get<NpcFightingStyle>();
            if (fightingStyle != null) {
                using var abstractTypes = template.AbstractTypes;
                foreach (var abstractType in abstractTypes.value) {
                    fightingStyle = abstractType.fightingStyle.Get<NpcFightingStyle>();
                    if (fightingStyle != null) {
                        return fightingStyle;
                    }
                }
            }
            return fightingStyle;
        });
        public NpcFightingStyle FightingStyle => _fightingStyle.Get(this);
        
        public DamageReceivedMultiplierData GetDamageReceivedMultiplierData() {
            var parts = new DamageReceivedMultiplierDataPart[damageReceivedMultipliers.Count];
            for (int i = 0; i < damageReceivedMultipliers.Count; i++) {
                parts[i] = DamageReceivedMultiplierDataConfig.Construct(damageReceivedMultipliers[i]);
            }
            return new DamageReceivedMultiplierData(parts);
        }
        
        DamageReceivedMultiplierDataConfig AddDefaultDamageReceivedMultiplier() => DamageReceivedMultiplierDataConfig.Default;

        float CalculateEffectiveHealth() {
            float reduction = Damage.GetArmorDamageReduction(armor * armorMultiplier);
            return MaxHealth / (1 - reduction);
        }

        public int GetExpReward() {
            if (expTier == StatDefinedRange.Custom) {
                return expReward;
            }
            float expForLvl = HeroDevelopment.RequiredExpFor(Mathf.Max(expLevel, 2));
            float expMulti = GetExpMultiplier();
            float reward = expForLvl * expMulti;
            return HeroDevelopment.RoundExp(reward);
        }

        float GetExpMultiplier() {
            var constants = GameConstants.Get;
            float multiplier = expTier switch {
                StatDefinedRange.Custom => 0,
                StatDefinedRange.Tiny => constants.TinyEnemyExpMulti,
                StatDefinedRange.Low => constants.LowEnemyExpMulti,
                StatDefinedRange.Mid => constants.MidEnemyExpMulti,
                StatDefinedRange.High => constants.HighEnemyExpMulti,
                StatDefinedRange.Epic => constants.EpicEnemyExpMulti,
                _ => throw new ArgumentOutOfRangeException()
            };
            return multiplier;
        }

        public IEnumerable<ItemSpawningDataRuntime> InventoryItems(IModel debugTarget) {
            try {
                return inventoryItems?.LootTable(debugTarget)?.PopLoot(debugTarget)?.items ?? Enumerable.Empty<ItemSpawningDataRuntime>();
            } catch (Exception e) {
                Log.Important?.Error($"Exception below happened on popping loot from InventoryItems of NpcTemplate ({GUID})", this);
                Debug.LogException(e, this);
                return Enumerable.Empty<ItemSpawningDataRuntime>();
            }
        }

        [UnityEngine.Scripting.Preserve]
        public bool TryGetInventoryItems(out IEnumerable<ItemSpawningDataRuntime> items) {
            try {
                items = inventoryItems?.LootTable(null)?.PopLoot(null)?.items;
            } catch (Exception e) {
                Log.Important?.Error($"Exception below happened on popping loot from InventoryItems of ({this.DebugName})", this);
                Debug.LogException(e, this);
                items = null;
            }
            return items != null;
        }

        void TryGetFistFromInventoryItems() {
#if UNITY_EDITOR
            if (inventoryItems == null) return;
            
            ItemTemplate fistsTemplate = CommonReferences.Get.TemplateService.AbstractWeaponFists;
            var foundHand = inventoryItems.LootTable(this)?.EDITOR_PopLootData()?.FirstOrDefault(i => i.Template.AbstractTypes.CheckContainsAndRelease(fistsTemplate));
            if (foundHand != null) {
                this.fistsTemplate = foundHand.template;
                UnityEditor.EditorUtility.SetDirty(this);
                Log.Important?.Info($"Found hand item in inventory items of {this.DebugName}", this.gameObject);
            }
#endif
        }

        public static NpcTemplate FromNpcOrDummy(Location location) {
            if (location.TryGetElement<NpcElement>(out var npc)) {
                return npc.Template;
            }

            if (location.TryGetElement<NpcDummy>(out var npcDummy)) {
                return npcDummy.Template;
            }
            return null;
        }

#if UNITY_EDITOR
        public TemplateReference EDITOR_fightingStyle {
            get => fightingStyle;
            set => fightingStyle = value;
        }
        public void OverrideHP(int value) {
            maxHealth = value;
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        public void OverrideStamina(int value) {
            maxStamina = value;
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        public void OverrideMeleeDamage(float value) {
            meleeDamage = value;
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        public void OverrideRangedDamage(float value) {
            rangedDamage = value;
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        public void OverrideMagicDamage(float value) {
            magicDamage = value;
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        public void OverrideArmor(int value) {
            armor = value;
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        public void OverridePoiseThreshold(float value) {
            poiseThreshold = value;
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        public void OverrideForceStumbleThreshold(float value) {
            forceStumbleThreshold = value;
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        public void OverrideHeroKnockBck(int value) {
            heroKnockBack = value;
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        public IEnumerable<ItemSpawningDataRuntime> TryGetInventoryItemsWithoutException() {
            try {
                return inventoryItems?.LootTable(null)?.PopLoot(null)?.items;
            } catch {
                return Enumerable.Empty<ItemSpawningDataRuntime>();
            }
        }
        
        void SetHumanStats() {
            SetHP(level);
            SetSP();
            SetMP();
            SetXP(level + 1, StatDefinedRange.Low);
            UnityEditor.PrefabUtility.SavePrefabAsset(this.gameObject);
        }

        void SetBigHumanStats() {
            SetHP(level + 5, 150);
            SetSP(-25);
            SetMP(-100);
            SetXP(level + 3, StatDefinedRange.Mid);
            UnityEditor.PrefabUtility.SavePrefabAsset(this.gameObject);
        }

        void SetHP(int level, int bonusHP = 0) {
            maxHealth = bonusHP + 40 + (level - 1) * 15;
        }

        void SetSP(int bonusSP = 0) {
            maxStamina = bonusSP + (1 + Mathf.FloorToInt(maxHealth / 300f)) * 100;
        }

        void SetMP(int bonusMP = 0) {
            //maxMana = bonusMP + (1 + Mathf.FloorToInt(maxHealth / 100f)) * 50;
        }

        void SetXP(int level, StatDefinedRange range) {
            expLevel = level + 1;
            expTier = range;
        }
        
        bool IsGuard => crimeReactionArchetype is CrimeReactionArchetype.Guard;
        bool IsGuardWithIntervention(out GuardIntervention guardIntervention) {
            guardIntervention = null;
            return IsGuard && TryGetOnlyGuardIntervention(out guardIntervention);
        }
        bool GuardWitNotOneGuardBehaviour => IsGuard && !TryGetOnlyGuardIntervention(out GuardIntervention _);
        bool GuardWithoutGuardStory => IsGuardWithIntervention(out var intervention) 
                                       && intervention.GuardStory is not { story: { IsSet: true } };

        bool TryGetOnlyGuardIntervention(out GuardIntervention guardIntervention) {
            guardIntervention = null;
            return FightingStyle != null && UnityEditor.AssetDatabase
                .LoadAssetAtPath<AREnemyBehavioursMapping>(UnityEditor.AssetDatabase.GUIDToAssetPath(FightingStyle.BaseBehaviours.AssetGUID)).CombatBehaviours
                .TryGetOnly(out guardIntervention);
        }

        public bool SetDefaultPoiseThreshold() {
            switch (NpcType) {
                case NpcType.Critter:
                    break;
                case NpcType.Trash:
                case NpcType.Normal:
                    poiseThreshold = 0.33f * maxHealth;
                    return true;
                case NpcType.Elite:
                case NpcType.MiniBoss:
                case NpcType.Boss:
                    poiseThreshold = 0.15f * maxHealth;
                    return true;
                default:
                    break;
            }

            return false;
        }
        
        public void EditorFactionSet(FactionTemplate template) {
            faction = new TemplateReference(template);
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        public void EditorCrimeOwnerSet(CrimeOwnerTemplate template) {
            defaultCrimeOwner = new TemplateReference(template);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        void FindUsages() {
            // Invoke menu item for guid search
            UnityEditor.EditorApplication.ExecuteMenuItem("Assets/TG/Find by GUID");
        }
        
        public readonly struct EDITOR_Accessor {
            readonly NpcTemplate _template;

            public EDITOR_Accessor(NpcTemplate template) {
                _template = template;
            }

            public ref string[] tags => ref _template.tags;
        }
#endif
    }
}
