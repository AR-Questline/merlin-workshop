using System;
using System.Globalization;
using System.Linq;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items.Tooltips.Components;
using Awaken.TG.Main.Heroes.Items.Weapons;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility.TokenTexts;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors {
    public interface IItemTypeSpecificDescriptor {
        public static IItemTypeSpecificDescriptor ItemTypeSpecificDescriptor(Item item) {
            ItemStats itemStats = item.ItemStats;

            return item switch {
                { IsArrow: true } => new ArrowDescriptor(itemStats.BaseMinDmg.ModifiedInt),
                { IsMagic: true } => new MagicDescriptor(item),
                { IsWeapon: true } or { IsBlocking: true } or { IsThrowable: true } => new WeaponDescriptor(item),
                { IsArmor: true } => new ArmorDescriptor(item),
                _ => new GenericDescriptor()
            };
        }

        public void SetupStatTexts(ItemTooltipStatsComponent stats, ItemTooltipMagicStatsComponent magicStats, IItemDescriptor descriptorToCompare);

        public class WeaponDescriptor : IItemTypeSpecificDescriptor {
            public int BaseMinDamage { get; }
            public int BaseMaxDamage { get; }
            public int MinDamage { get; }
            public int MaxDamage { get; }
            public int StaminaCost { get; }
            public bool IsDamagePerSecond { get; [UnityEngine.Scripting.Preserve] protected set; }
            public int Block { get; }
            public int ModifiedBlock { get; }

            public WeaponDescriptor(Item item) {
                var itemStats = item.ItemStats;
                Damage.GetStatModifiers(Hero.Current, itemStats, out float multi, out float linear);
                
                BaseMinDamage = itemStats.BaseMinDmg.BaseInt;
                BaseMaxDamage = itemStats.BaseMaxDmg.BaseInt;
                MinDamage = Mathf.CeilToInt(itemStats.BaseMinDmg * multi + linear);
                MaxDamage = Mathf.CeilToInt(itemStats.BaseMaxDmg * multi + linear);
                StaminaCost = itemStats.LightAttackCost.ModifiedInt;
                
                //for now, block is only for shields and rods
                float block = itemStats.ParentModel.IsBlocking ? itemStats.Block.ModifiedInt : 0;
                Block = Mathf.CeilToInt(block);
                ModifiedBlock = Mathf.CeilToInt(block * ItemRequirementsUtils.GetBlockDamageReductionMultiplier(Hero.Current, item));
            }

            public virtual void SetupStatTexts(ItemTooltipStatsComponent stats, ItemTooltipMagicStatsComponent magicStats, IItemDescriptor descriptorToCompare) {
                magicStats?.SetActiveState(false);
                string dmg = GetDamageText(descriptorToCompare);
                string block = GetBlockText(descriptorToCompare);
                string staminaCost = StaminaCost == 0 ? string.Empty : $"{StaminaCost.ToString().ColoredText(stats.sideStatsColor)} {LocTerms.StaminaAbbreviation.Translate()}";
                stats.SetupStats(dmg, Block != 0 ? block : null, staminaCost);
            }

            protected string GetDamageText(IItemDescriptor descriptorToCompare, bool withoutUnit = false) {
                var other = descriptorToCompare is {TypeSpecificDescriptor: WeaponDescriptor } ? descriptorToCompare.TypeSpecificDescriptor as WeaponDescriptor : null;
                
                var baseMinValue = BaseMinDamage.ToString().ColoredText(ItemUtils.ColorEqual);
                var baseMaxValue = BaseMaxDamage.ToString().ColoredText(ItemUtils.ColorEqual);
                
                string baseDamageText = BaseMinDamage == BaseMaxDamage ? baseMinValue : $"{baseMinValue}-{baseMaxValue}";
                
                // Compare to other item or base item stats if other is null
                var minValue = MinDamage.ToString().ColoredText(ItemUtils.StatColor(MinDamage, other?.MinDamage ?? BaseMinDamage));
                var maxValue = MaxDamage.ToString().ColoredText(ItemUtils.StatColor(MaxDamage, other?.MaxDamage ?? BaseMaxDamage));
                
                bool showModifiedDamage = BaseMinDamage != MinDamage || BaseMaxDamage != MaxDamage;
                string modifiedDamageText = string.Empty;
                if (showModifiedDamage) {
                    if (MinDamage == MaxDamage) {
                        modifiedDamageText = $" ({minValue})";
                    } else {
                        modifiedDamageText = $" ({minValue}-{maxValue})";
                    }
                }
                
                var value = $"{baseDamageText}{modifiedDamageText}";
                
                if (withoutUnit) {
                    return value;
                }
                
                string damageUnit = IsDamagePerSecond ? LocTerms.UnitDamagePerSecond.Translate() : LocTerms.UnitDamage.Translate();
                return $"{value} {damageUnit}";
            }
            
            protected string GetBlockText(IItemDescriptor descriptorToCompare) {
                var other = descriptorToCompare is {TypeSpecificDescriptor: WeaponDescriptor } ? descriptorToCompare.TypeSpecificDescriptor as WeaponDescriptor : null;
                bool showModifiedBlock = Block != ModifiedBlock;

                if (showModifiedBlock) {
                    var baseValue = Block.ToString().ColoredText(ItemUtils.ColorEqual);
                    var modifiedValue = ModifiedBlock.ToString().ColoredText(ItemUtils.StatColor(ModifiedBlock, other?.ModifiedBlock ?? Block));
                    return $"{baseValue} ({modifiedValue}) {LocTerms.UnitBlock.Translate()}";
                } else {
                    var baseValue = Block.ToString().ColoredText(ItemUtils.StatColor(Block, other?.ModifiedBlock));
                    return $"{baseValue} {LocTerms.UnitBlock.Translate()}";
                }
            }
        }

        public class ArrowDescriptor : IItemTypeSpecificDescriptor {
            int Damage { get; }

            public ArrowDescriptor(int damage) {
                Damage = math.clamp(damage, 0, 999);
            }

            public void SetupStatTexts(ItemTooltipStatsComponent stats, ItemTooltipMagicStatsComponent magicStats, IItemDescriptor descriptorToCompare) {
                magicStats?.SetActiveState(false);
                var other = descriptorToCompare is {TypeSpecificDescriptor: ArrowDescriptor } ? descriptorToCompare.TypeSpecificDescriptor as ArrowDescriptor : null;
                var value = Damage.ToString().ColoredText(ItemUtils.StatColor(Damage, other?.Damage));
                value = $"+{value}";
                string text = $"{value} {LocTerms.UnitDamage.Translate()}";
                stats.SetupStats(text);
            }
        }

        public class ArmorDescriptor : IItemTypeSpecificDescriptor {
            float BaseArmor { get; }
            float ModifiedArmor { get; }

            public ArmorDescriptor(Item item) {
                BaseArmor = item.ItemStats.Armor.BaseValue;
                ModifiedArmor = ItemRequirementsUtils.GetArmorAfterReduction(Hero.Current, item);
            }
            
            public void SetupStatTexts(ItemTooltipStatsComponent stats, ItemTooltipMagicStatsComponent magicStats, IItemDescriptor descriptorToCompare) {
                magicStats.SetActiveState(false);
                var other = descriptorToCompare is {TypeSpecificDescriptor: ArmorDescriptor } ? descriptorToCompare.TypeSpecificDescriptor as ArmorDescriptor : null;
                bool showModifiedArmor = BaseArmor != ModifiedArmor;

                if (showModifiedArmor) {
                    string baseArmorText = BaseArmor.ToString("F1").ColoredText(ItemUtils.ColorEqual);
                    string modifiedArmorText = ModifiedArmor.ToString("F1").ColoredText(ItemUtils.StatColor(ModifiedArmor, other?.ModifiedArmor ?? BaseArmor));

                    string armorStatText = $"{baseArmorText} ({modifiedArmorText}) {LocTerms.UnitArmor.Translate()}";
                    stats.SetupStats(armorStatText);
                } else {
                    string baseArmorText = BaseArmor.ToString("F1").ColoredText(ItemUtils.StatColor(BaseArmor, other?.ModifiedArmor));
                    string armorStatText = $"{baseArmorText} {LocTerms.UnitArmor.Translate()}";
                    stats.SetupStats(armorStatText);
                }
            }
        }

        public class MagicDescriptor : IItemTypeSpecificDescriptor {
            int BaseMinDamage { get; }
            int BaseMaxDamage { get; }
            float BaseManaCost { get; }
            float ModifiedManaCost { [UnityEngine.Scripting.Preserve] get; }
            float LightManaCost { get; }
            float HeavyManaCost { get; }
            float HeavyManaCostPerSecond { get; }
            float CostModifier { get; }
            MagicItemTemplateInfo LightCast { get; }
            ItemTooltipMagicStatsComponent.VisibilityConfig LightVisibilityConfig { get; } = new();
            MagicItemTemplateInfo HeavyCast { get; }
            ItemTooltipMagicStatsComponent.VisibilityConfig HeavyVisibilityConfig { get; } = new();
            bool UseCastMagic { get; }
            Item Item { get; }

            public MagicDescriptor(Item item) {
                Item = item;
                LightCast = item.LightCastInfo;
                HeavyCast = item.HeavyCastInfo;
                UseCastMagic = item.IsCastMagic;
                
                var itemStats = item.ItemStats;
                BaseMinDamage = itemStats.BaseMinDmg.BaseInt;
                BaseMaxDamage = itemStats.BaseMaxDmg.BaseInt;
                
                // Mana
                ModifiedManaCost = MagicUtils.GetModifiedManaCost(Hero.Current, item, out float baseMana, out _);
                BaseManaCost = baseMana;
                
                LightManaCost = item?.Stat(ItemStatType.LightCastManaCost) ?? 0f;
                HeavyManaCost = item?.Stat(ItemStatType.HeavyCastManaCost) ?? 0f;
                HeavyManaCostPerSecond = item?.Stat(ItemStatType.HeavyCastManaCostPerSecond) ?? 0f;
                CostModifier = MagicUtils.GetManaCostMultiplier(Hero.Current, item);
            }

            public void SetupStatTexts(ItemTooltipStatsComponent stats, ItemTooltipMagicStatsComponent magicStats, IItemDescriptor descriptorToCompare) {
                if (UseCastMagic) {
                    stats.SetupStats();
                    
                    if (magicStats != null) {
                        SetupNewMagic(magicStats, descriptorToCompare);
                        magicStats.SetActiveState(true);
                    }
                } else {
                    magicStats?.SetActiveState(false);
                    SetupOldMagic(stats, descriptorToCompare);
                }
            }
            
            void SetupNewMagic(ItemTooltipMagicStatsComponent magicStats, IItemDescriptor descriptorToCompare) {
                ConstructMagicInfo(LightCast, LightVisibilityConfig, descriptorToCompare, LightManaCost, out string lightEffect, out string lightCost, out string lightDescription);
                ConstructMagicInfo(HeavyCast, HeavyVisibilityConfig, descriptorToCompare, HeavyManaCost, out string heavyEffect, out string heavyCost, out string heavyDescription, HeavyManaCostPerSecond);
                
                magicStats.SetupCommonSection();
                magicStats.SetupLightCast(LightCast, lightEffect, lightCost, lightDescription, LightVisibilityConfig);
                magicStats.SetupHeavyCast(HeavyCast, heavyEffect, heavyCost, heavyDescription, HeavyVisibilityConfig);
            }
            
            void ConstructMagicInfo(MagicItemTemplateInfo magicInfo, ItemTooltipMagicStatsComponent.VisibilityConfig visibilityConfig, IItemDescriptor descriptorToCompare, float cost, out string effectInfo, out string costInfo, out string description, float additionalCost = 0f) {
                effectInfo = string.Empty;
                costInfo = string.Empty;
                description = string.Empty;
                
                visibilityConfig.WholeContentEnabled = magicInfo.IsActive;
                if (!visibilityConfig.WholeContentEnabled) {
                    return;
                }
                
                effectInfo = GetEffectText(magicInfo, visibilityConfig, descriptorToCompare);
                costInfo = GetCostText(magicInfo, visibilityConfig, cost, additionalCost);
                description = new TokenText(magicInfo.MagicDescription).GetValue(Item.Character, Item);
            }

            string GetEffectText(MagicItemTemplateInfo magicInfo, ItemTooltipMagicStatsComponent.VisibilityConfig visibilityConfig, IItemDescriptor descriptorToCompare) {
                bool isPerSecond = magicInfo.MagicType == MagicType.Channeled;
                string effectInfo;
                string effectUnit = string.Empty;
                
                visibilityConfig.EffectEnabled = magicInfo.EffectType != MagicEffectType.None;
                if (!visibilityConfig.EffectEnabled) {
                    return string.Empty;
                }
                
                if (magicInfo.IsEffectOverriden) {
                    var effectTokenResolver = new TokenText(magicInfo.EffectOverridenToken);
                    effectTokenResolver.Refresh();
                    var effectTokens = effectTokenResolver.Tokens.ToArray();
                    
                    if (effectTokens.Length > 0 && float.TryParse(effectTokens[0].GetValue(Item.Character, Item), NumberStyles.Float, LocalizationHelper.SelectedCulture, out float effectMin)) {
                        if (effectTokens.Length > 1 && float.TryParse(effectTokens[1].GetValue(Item.Character, Item), NumberStyles.Float, LocalizationHelper.SelectedCulture, out float effectMax)) {
                            effectInfo = CreateEffectText((int)effectMin, (int)effectMax, descriptorToCompare);
                        } else {
                            effectInfo = CreateEffectText((int)effectMin, (int)effectMin, descriptorToCompare);
                        }
                    } else {
                        Log.Important?.Error($"Failed to parse magic effect token for item {Item.DisplayName}");
                        effectInfo = CreateEffectText(BaseMinDamage, BaseMaxDamage, descriptorToCompare);
                    }
                } else {
                    effectInfo = CreateEffectText(BaseMinDamage, BaseMaxDamage, descriptorToCompare);
                }
                
                if (magicInfo.EffectType == MagicEffectType.Health) {
                    effectUnit = isPerSecond ? LocTerms.HealthPerSecond.Translate() : LocTerms.Health.Translate();
                } else if (magicInfo.EffectType == MagicEffectType.Damage) {
                    effectUnit = isPerSecond ? LocTerms.UnitDamagePerSecond.Translate() : LocTerms.UnitDamage.Translate();
                }
                
                return $"{effectInfo} {effectUnit}";
            }

            string GetCostText(MagicItemTemplateInfo magicInfo, ItemTooltipMagicStatsComponent.VisibilityConfig visibilityConfig, float manaCost,  float additionalCost = 0f) {
                bool isPerSecond = magicInfo.MagicType == MagicType.Channeled;
                string costUnit = string.Empty;

                float cost = magicInfo.IsCostOverriden
                    ? float.Parse(new TokenText(magicInfo.CostOverridenToken).GetValue(Item.Character, Item))
                    : manaCost;

                visibilityConfig.CostEnabled = cost != 0 || additionalCost != 0;
                if (!visibilityConfig.CostEnabled) {
                    return string.Empty;
                }
                
                string costInfo = CreateCostText(cost);
                
                if (magicInfo.CostType == AliveStatType.Health) {
                    costUnit = isPerSecond ? LocTerms.HealthCostPerSecond.Translate() : $"{LocTerms.Health.Translate()} {LocTerms.Cost.Translate()}";
                } else if (magicInfo.CostType == CharacterStatType.Mana) {
                    costUnit = isPerSecond ? LocTerms.ManaCostPerSecond.Translate() : $"{LocTerms.Mana.Translate()} {LocTerms.Cost.Translate()}";
                } else if (magicInfo.CostType == CharacterStatType.Stamina) {
                    costUnit = isPerSecond ? LocTerms.StaminaCostPerSecond.Translate() : $"{LocTerms.Stamina.Translate()} {LocTerms.Cost.Translate()}";
                }
                
                if (additionalCost > 0) {
                    string additionalCostText = CreateCostText(additionalCost);
                    
                    if (cost > 0) {
                        additionalCostText = $"+{additionalCostText}";
                        return $"{costInfo}{additionalCostText} {costUnit}";
                    }

                    return $"{additionalCostText} {costUnit}";
                }
                
                return $"{costInfo} {costUnit}";
            }
            
            string CreateCostText(float cost) {
                string costLabel;
                float costWithModifier = cost * CostModifier;
                string baseManaText = $"{cost:0.#}".ColoredText(ARColor.MainWhite);

                if (Math.Abs(cost - costWithModifier) > 0.1) {
                    string modifiedManaText = $"{costWithModifier:0.#}".ColoredText(ItemUtils.StatColor(cost, costWithModifier));
                    costLabel = $"{baseManaText} ({modifiedManaText})";
                } else {
                    costLabel = $"{baseManaText}";
                }

                return costLabel;
            }
            
            string CreateEffectText(int baseMin, int baseMax, IItemDescriptor descriptorToCompare) {
                Damage.GetStatModifiers(Hero.Current, Item.ItemStats, out float multi, out float linear);
                int modifiedMin = Mathf.CeilToInt(baseMin * multi + linear);
                int modifiedMax = Mathf.CeilToInt(baseMax * multi + linear);
                
                var other = descriptorToCompare is {TypeSpecificDescriptor: WeaponDescriptor } ? descriptorToCompare.TypeSpecificDescriptor as WeaponDescriptor : null;
                
                var baseMinValue = baseMin.ToString().ColoredText(ItemUtils.ColorEqual);
                var baseMaxValue = baseMax.ToString().ColoredText(ItemUtils.ColorEqual);
                
                string baseDamageText = baseMin == baseMax ? baseMinValue : $"{baseMinValue}-{baseMaxValue}";
                
                // Compare to other item or base item stats if other is null
                var minValue = modifiedMin.ToString().ColoredText(ItemUtils.StatColor(modifiedMin, other?.MinDamage ?? baseMin));
                var maxValue = modifiedMax.ToString().ColoredText(ItemUtils.StatColor(modifiedMax, other?.MaxDamage ?? baseMax));
                
                bool showModifiedDamage = baseMin != modifiedMin || baseMax != modifiedMax;
                string modifiedDamageText = string.Empty;
                if (showModifiedDamage) {
                    modifiedDamageText = modifiedMin == modifiedMax ? $" ({minValue})" : $" ({minValue}-{maxValue})";
                }
                
                return $"{baseDamageText}{modifiedDamageText}";
            }
            
            //TODO: after setup spells item template to use new magic cast, remove this method
            void SetupOldMagic(ItemTooltipStatsComponent stats, IItemDescriptor descriptorToCompare) {
                string main = null;
                
                if (BaseMaxDamage > 0) {
                    main = CreateEffectText(BaseMinDamage, BaseMaxDamage, descriptorToCompare) + " " + LocTerms.UnitDamage.Translate();
                }
                
                string mana = CreateCostText(BaseManaCost) + " " + LocTerms.Mana.Translate();
                stats.SetupStats(main, null, mana);
            }
        }

        public class GenericDescriptor : IItemTypeSpecificDescriptor {
            public void SetupStatTexts(ItemTooltipStatsComponent stats, ItemTooltipMagicStatsComponent magicStats, IItemDescriptor descriptorToCompare) {
                magicStats.SetActiveState(false);
                stats.SetupStats();
            }
        }
    }
}