using System.Linq;
using Awaken.TG.Editor.Assets.Templates;
using Awaken.TG.Editor.DataViews.Data;
using Awaken.TG.Editor.SceneCaches.Items;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Gems;
using Awaken.TG.Main.Heroes.Items.Tools;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.Tags;
using UnityEngine;

namespace Awaken.TG.Editor.DataViews.Headers {
    public static partial class DataViewHeaders {
        public static readonly DataViewHeader[] ItemTemplateHeaders = {
            Computable<ItemTemplate>("DisplayName", item => item.itemName, BigCellWidth),
            Computable<ItemTemplate>("Flavor", item => item.Flavor, BigCellWidth),
            Computable<ItemTemplate>("Description", item => item.Description, BigCellWidth),
            Computable<ItemTemplate>("DescriptionFilled", ItemTemplateUtils.GetDebugDescription, BigCellWidth),
            Computable<ItemTemplate>("DescriptionBaked", item => new ItemTemplate.EditorAccessor(item).BakedDescription, BigCellWidth),
            TierProperty<ItemTemplate>(item => ref item.tags, TierHelper.ItemTiers),
            Property<ItemTemplate>("quality", MediumCellWidth),
            Property<ItemTemplate>("canStack", SmallCellWidth),
            Property<ItemTemplate>("basePrice", SmallCellWidth),
            Property<ItemTemplate>("priceLevelMultiplier", SmallCellWidth),
            Property<ItemTemplate>("weight", SmallCellWidth),
            Property<ItemTemplate>("weightLoss", SmallCellWidth),
            Property<ItemTemplate>("hiddenOnUI", SmallCellWidth),
            Property<ItemTemplate>("crimeItemValue", SmallCellWidth),
            Property<Template>("templateType", MediumCellWidth),
            Computable<ItemTemplate>("IsPlainFood", item => item.IsPlainFood, SmallCellWidth),
            Computable<ItemTemplate>("IsWeapon", item => item.IsWeapon, SmallCellWidth),
            Computable<ItemTemplate>("IsArmor", item => item.IsArmor, SmallCellWidth),
            Computable<ItemTemplate>("IsGem", item => item.IsGem, SmallCellWidth),
            Computable<ItemTemplate>("IsJewelry", item => item.IsJewelry, SmallCellWidth),
            Computable<ItemTemplate>("IsConsumable", item => item.IsConsumable, SmallCellWidth),
            Computable<ItemTemplate>("IsDagger", item => item.IsDagger, SmallCellWidth),
            Computable<ItemTemplate>("IsShield", item => item.IsShield, SmallCellWidth),
            Computable<ItemTemplate>("IsOneHanded", item => item.IsOneHanded, SmallCellWidth),
            Computable<ItemTemplate>("IsTwoHanded", item => item.IsTwoHanded, SmallCellWidth),
            Computable<ItemTemplate>("IsMelee", item => item.IsMelee, SmallCellWidth),
            Computable<ItemTemplate>("IsRanged", item => item.IsRanged, SmallCellWidth),
            Computable<ItemTemplate>("IsMagic", item => item.IsMagic, SmallCellWidth),
            Computable<ItemTemplate>("IsTool", item => item.IsTool, SmallCellWidth),
            Computable<ItemTemplate>("IsArrow", item => item.IsArrow, SmallCellWidth),
            Computable<ItemTemplate>("IsThrowable", item => item.IsThrowable, SmallCellWidth),
            Computable<ItemTemplate>("IsReadable", item => item.IsReadable, SmallCellWidth),
            ComputableObject<ItemTemplate, GameObject>("DropPrefab", item => GetShareableAsset<GameObject>(item.DropPrefab), BigCellWidth),
            ComputableObject<ItemTemplate, BaseRecipe>("CraftingRecipe", item => item.EditorRecipes?.FirstOrDefault(), BigCellWidth),
            Computable<ItemTemplate>("LootAmount", item => LootCache.Get.GetLootCache(item)?.TotalPredictedAmount ?? 0, SmallCellWidth),
            Computable<ItemTemplate>("GrindableAmount", item => LootCache.Get.GetLootCache(item)?.TotalGrindable ?? 0, SmallCellWidth),
            Computable<ItemTemplate>("LootAmountRange", item => LootCache.Get.GetLootCache(item)?.LootAmountRange, SmallCellWidth),
            Computable<ItemTemplate>("LootAmountInPrologue", item => LootCache.Get.GetLootCache(item)?.GetTotalPredictedAmountInRegion("Prologue_Jail") ?? 0, SmallCellWidth),
            Computable<ItemTemplate>("LootAmountInHoS", item => LootCache.Get.GetLootCache(item)?.GetTotalPredictedAmountInRegion("CampaignMap_HOS") ?? 0, SmallCellWidth),
            Computable<ItemTemplate>("LootAmountInCuanacht", item => LootCache.Get.GetLootCache(item)?.GetTotalPredictedAmountInRegion("CampaignMap_Cuanacht") ?? 0, SmallCellWidth),
            Computable<ItemTemplate>("LootAmountInForlorn", item => LootCache.Get.GetLootCache(item)?.GetTotalPredictedAmountInRegion("CampaignMap_Forlorn") ?? 0, SmallCellWidth),
        };

        public static readonly DataViewHeader[] ItemEquipSpecHeaders = {
            Property<ItemEquipSpec>("equipmentType", MediumCellWidth),
            Property<ItemEquipSpec>("gemSlots", SmallCellWidth),
            Property<ItemEquipSpec>("finisherType", MediumCellWidth),
            Property<ItemEquipSpec>("hitsToHitStop", MediumCellWidth),
            ComputableObject<ItemEquipSpec, GameObject>("visualPrefabMale", i => RetrievePrefabFromItemEquipSpec(i, Gender.Male), LargeCellWidth),
            ComputableObject<ItemEquipSpec, GameObject>("visualPrefabFemale", i => RetrievePrefabFromItemEquipSpec(i, Gender.Female), BigCellWidth),
            ComputableObject<ItemEquipSpec, Mesh>("meshMale", i => RetrieveMeshFromItemEquipSpec(i, Gender.Male), BigCellWidth),
            ComputableObject<ItemEquipSpec, Mesh>("meshFemale", i => RetrieveMeshFromItemEquipSpec(i, Gender.Female), BigCellWidth),
            Computable<ItemEquipSpec>("verticesMale", i => RetrieveMeshFromItemEquipSpec(i, Gender.Male)?.vertexCount ?? float.NaN, SmallCellWidth),
            Computable<ItemEquipSpec>("verticesFemale", i => RetrieveMeshFromItemEquipSpec(i, Gender.Female)?.vertexCount ?? float.NaN, SmallCellWidth),
            Computable<ItemEquipSpec>("trianglesMale", i => RetrieveMeshFromItemEquipSpec(i, Gender.Male)?.triangles.Length ?? float.NaN, SmallCellWidth),
            Computable<ItemEquipSpec>("trianglesFemale", i => RetrieveMeshFromItemEquipSpec(i, Gender.Female)?.triangles.Length ?? float.NaN, SmallCellWidth),
        };

        public static readonly DataViewHeader[] ItemStatsAttachmentHeaders = {
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.lightAttackStaminaCost), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.heavyAttackStaminaCost), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.heavyAttackHoldCostPerTick), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.drawBowStaminaCostPerTick), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.holdItemStaminaCostPerTick), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.pushStaminaCost), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.blockStaminaCostMultiplier), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.parryStaminaCost), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.minDamage), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.maxDamage), SmallCellWidth),
            Computable<ItemStatsAttachment>("AvgDamage", stats => (stats.minDamage + stats.maxDamage) * 0.5f, SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.damageGain), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.heavyAttackDamageMultiplier), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.pushDamageMultiplier), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.backStabDamageMultiplier), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.armorPenetration), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.damageIncreasePerCharge), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.damageType), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.armor), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.armorGain), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.blockDamageReductionPercent), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.blockAngle), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.blockGain), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.forceDamage), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.forceDamageGain), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.ragdollForce), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.poiseDamage), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.poiseDamageGain), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.poiseDamageHeavyAttackMultiplier), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.poiseDamagePushMultiplier), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.rangedZoomModifier), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.bowDrawSpeedModifier), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.attacksPerSecond), SmallCellWidth),
            Property<ItemStatsAttachment>(nameof(ItemStatsAttachment.magicHeldSpeedMultiplier), SmallCellWidth),
            Computable<ItemStatsAttachment>("DPS", stats => stats.Dps, SmallCellWidth),
        };

        public static readonly DataViewHeader[] ItemEffectsHeaders = {
            ItemSkillVariable("ModifyValue"),
            ItemSkillVariable("GainValue"),
            ItemSkillVariable("LoseValue"),
            ItemSkillEnum("StatEnum"),
            ItemSkillVariable("Buildup"),
            ItemSkillVariable("StatusBuildup"),
            ItemSkillVariable("Duration"),
            ItemSkillVariable("AddValue"),
            Computable<ItemEffectsSpec>("HealValue", i => GetVariable(i, "AddValue") * GetVariable(i, "Duration"), SmallCellWidth),
        };
        
        public static readonly DataViewHeader[] ItemProjectileHeaders = {
            // TODO Mateusz Sabat pls fix this - Jakub Kaminski 2024
            // Property<ItemProjectileAttachment>("data.logicPrefab.AssetGUID", "LogicPrefabGUID", MediumCellWidth), Tego nie musisz fixować ale fajnie wiedzieć czemu nie wyświetla
            // Property<ItemProjectileAttachment>("data.visualPrefab.AssetGUID", "VisualPrefabGUID", MediumCellWidth),  
            Computable<ItemProjectileAttachment>("LogicPrefab", ipa => ipa != null && ipa.data?.logicPrefab is {IsSet: true} ? ipa.data.logicPrefab.Get()?.EditorLoad<GameObject>()?.name ?? "-" : "-", BigCellWidth),
            Computable<ItemProjectileAttachment>("VisualPrefab", ipa => ipa != null && ipa.data?.visualPrefab is {IsSet: true} ? ipa.data.visualPrefab.Get()?.EditorLoad<GameObject>()?.name ?? "-" : "-", BigCellWidth),
            // Property<ItemProjectileAttachment>("data.logicData.lifetime", SmallCellWidth), // Brak Nice Name psuje scriptable object
            // Property<ItemProjectileAttachment>("data.logicData.piercing", SmallCellWidth),
            // Property<ItemProjectileAttachment>("data.logicData.limitedPiercing", SmallCellWidth),
            // Property<ItemProjectileAttachment>("data.logicData.piercingLimit", SmallCellWidth),
            // Property<ItemProjectileAttachment>("data.logicData.dealDirectDamageOnContact", SmallCellWidth),
            // Property<ItemProjectileAttachment>("data.logicData.explodeOnContact", SmallCellWidth),
            // Property<ItemProjectileAttachment>("data.logicData.explodeOnEnviroHit", SmallCellWidth),
            // Property<ItemProjectileAttachment>("data.logicData.explodeOnLifetimeEnd", SmallCellWidth),
            ItemProjectileSkillVariable("Buildup"),
        };

        public static readonly DataViewHeader[] GemAttachmentHeaders = {
            Property<GemAttachment>("type", SmallCellWidth),
        };

        public static readonly DataViewHeader[] NpcTemplateHeaders = {
            Computable<NpcTemplate>("largeName", npc => npc.name.Replace("NPCTemplate_", "").Replace('_', ' '), LargeCellWidth),
            Property<NpcTemplate>("level", SmallCellWidth),
            Property<NpcTemplate>("npcData", BigCellWidth),
            Property<NpcTemplate>("crimeNpcValue", SmallCellWidth),
            Property<NpcTemplate>("crimeReactionArchetype", SmallCellWidth),
            Property<NpcTemplate>("maxHealth", SmallCellWidth),
            Property<NpcTemplate>("healthRegen", SmallCellWidth),
            Property<NpcTemplate>("maxStamina", SmallCellWidth),
            Property<NpcTemplate>("staminaRegenPerTick", SmallCellWidth),
            Property<NpcTemplate>("staminaUsageMultiplier", SmallCellWidth),
            Computable<NpcTemplate>("EffectiveHealth", npc => npc.EffectiveHealth, SmallCellWidth),
            Computable<NpcTemplate>("ArmorReduction", npc => npc.ArmorReduction, SmallCellWidth),
            Property<NpcTemplate>("canEnterCombat", SmallCellWidth),
            Property<NpcTemplate>("armor", SmallCellWidth),
            Property<NpcTemplate>("armorMultiplier", SmallCellWidth),
            Property<NpcTemplate>("statusResistance", SmallCellWidth),
            Property<NpcTemplate>("forceStumbleThreshold", SmallCellWidth),
            Property<NpcTemplate>("canDealDamageToFriendlies", SmallCellWidth),
            Property<NpcTemplate>("poiseThreshold", SmallCellWidth),
            Property<NpcTemplate>("blockValue", SmallCellWidth),
            Property<NpcTemplate>("isNotGrounded", SmallCellWidth),
            Property<NpcTemplate>("combatSlotsLimit", SmallCellWidth),
            Property<NpcTemplate>("expLevel", SmallCellWidth),
            Property<NpcTemplate>("expTier", MediumCellWidth),
            Property<NpcTemplate>("expReward", SmallCellWidth),
            Computable<NpcTemplate>("ExpRewordComputed", npc => npc.ExpReward, SmallCellWidth),
            Property<NpcTemplate>("difficultyTag", SmallCellWidth),
            Property<NpcTemplate>("surfaceType", MediumCellWidth),
            Property<NpcTemplate>("canTriggerAggroMusic", SmallCellWidth),
            ComputableObject<NpcTemplate, FactionTemplate>("faction", npc => npc.FactionEditorContext, (template, factionTemplate) => template.EditorFactionSet(factionTemplate), BigCellWidth),
            ComputableObject<NpcTemplate, CrimeOwnerTemplate>("crimeOwner", npc => npc.DefaultCrimeOwner, (template, crimeOwnerTemplate) => template.EditorCrimeOwnerSet(crimeOwnerTemplate), BigCellWidth),
        };

        public static readonly DataViewHeader[] TalentHeaders = {
            Computable<TalentTemplate>("Description", talent => talent.GetDebugDescription(), LargeCellWidth),
        };

        public static readonly DataViewHeader[] LootCacheHeaders = {
            LootData<ItemSource, string>("SceneName", MediumCellWidth),
            LootData<ItemSource, string>("scenePath", MediumCellWidth),
            LootData<ItemSource, string>("OpenWorldRegion", MediumCellWidth),
            LootData<ItemLootData, int>("minCount", SmallCellWidth),
            LootData<ItemLootData, int>("maxCount", SmallCellWidth),
            LootData<ItemLootData, float>("probability", SmallCellWidth),
            LootData<ItemLootData, bool>("Grindable", SmallCellWidth),
            LootData<ItemLootData, bool>("OnlyNight", SmallCellWidth),
            LootData<ItemLootData, bool>("Conditional", SmallCellWidth),
            LootData<ItemLootData, bool>("OwnedByNpc", SmallCellWidth),
            LootData<ItemLootData, bool>("AffectedByLootChanceMultiplier", SmallCellWidth),
        };
        
        public static readonly DataViewHeader[] QuestHeaders = {
            Computable<QuestTemplateBase>("Name", q => q.displayName.ToString(), MediumCellWidth),
            Computable<QuestTemplateBase>("Description", q => q.description.ToString(), LargeCellWidth),
            Property<QuestTemplate>("questType", SmallCellWidth),
            Property<QuestTemplateBase>("targetLvl", SmallCellWidth),
            Property<QuestTemplateBase>("xpGainRange", SmallCellWidth),
            Property<QuestTemplateBase>("experiencePoints", "Custom Experience Points", SmallCellWidth),
            Computable<QuestTemplateBase>("CalculatedExp", obj => obj.CalculatedExpRange.ToStringInt(), MediumCellWidth),
            Property<QuestTemplate>("autoCompleteLeftObjectives", SmallCellWidth),
            Property<QuestTemplate>("autoCompletion", SmallCellWidth),
            ComputableObject<QuestTemplateBase, FactionTemplate>("RelatedFaction", q => q.RelatedFaction, BigCellWidth),
        };

        public static readonly DataViewHeader[] QuestObjectivesHeaders = {
            ComputableObject<ObjectiveSpec, QuestTemplateBase>("QuestTemplate", objective => objective.GetComponentInParent<QuestTemplateBase>(true), BigCellWidth),
            Computable<ObjectiveSpec>("QuestName", obj => obj.GetComponentInParent<QuestTemplateBase>(true).displayName.ToString(), MediumCellWidth),
            Computable<ObjectiveSpec>("Description", obj => obj.Description.ToString(), LargeCellWidth),
            Property<ObjectiveSpec>("overrideTargetLevel", SmallCellWidth),
            Property<ObjectiveSpec>("targetLevel", SmallCellWidth),
            Computable<ObjectiveSpec>("FinalTargetLevel", obj => obj.TargetLevel, SmallCellWidth),
            Property<ObjectiveSpec>("xpGainRange", SmallCellWidth),
            Property<ObjectiveSpec>("experiencePoints", "Custom Experience Points", SmallCellWidth),
            Computable<ObjectiveSpec>("CalculatedExp", obj => obj.CalculatedExpRange.ToStringInt(), MediumCellWidth),
            Computable<ObjectiveSpec>("MarkerScene", obj => obj.TargetScene.Name, MediumCellWidth),
            Computable<ObjectiveSpec>("MarkerLocations", obj => obj.TargetLocationReference.ToString(), LargeCellWidth),
        };

        public static readonly DataViewHeader[] StatusTemplateHeaders = {
            Property<StatusTemplate>("statusType", SmallCellWidth),
            Computable<StatusTemplate>("IsPositive", status => status.IsPositive, SmallCellWidth),
            Property<StatusTemplate>("notSaved", SmallCellWidth),
            Property<StatusTemplate>("hiddenOnUI", SmallCellWidth),
            Property<StatusTemplate>("addType", SmallCellWidth),
            ComputableObject<StatusTemplate, SkillGraph>("skill", status => status.skill?.skillGraphRef?.Get<SkillGraph>(), BigCellWidth),
        };

        public static DataViewArchetype.HeaderProvider ItemTagsHeaders => TagsHeaders(
            () => TemplatesSearcher.FindAllOfType<ItemTemplate>(),
            template => ref template.tags
        );

        public static DataViewArchetype.HeaderProvider NpcTagsHeaders => TagsHeaders(
            () => TemplatesSearcher.FindAllOfType<NpcTemplate>(),
            npc => ref new NpcTemplate.EDITOR_Accessor(npc).tags
        );
    }
}