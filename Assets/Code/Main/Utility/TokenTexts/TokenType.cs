using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Buffs;
using Awaken.TG.Main.Heroes.Items.Weapons;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Skills;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Enums;
using UnityEngine;

namespace Awaken.TG.Main.Utility.TokenTexts {
    /// <summary>
    /// Defines the method of obtaining text from token.
    /// </summary>
    public class TokenType : RichEnum {
        public delegate string ValueMethod(TokenText token, string input, ICharacter owner, object payload);

        public delegate void ProcessToken(ref string text, List<object> children);
        
        // === Properties
        public ValueMethod Value { get; }

        public ProcessToken TokenPreProcessing { get; }
        public ProcessToken TokenPostProcessing { get; }

        [UnityEngine.Scripting.Preserve]
        // === Constructors
        public static readonly TokenType
            PlainText = new(nameof(PlainText), GetPlainText),
            HeroStats = new(nameof(HeroStats), GetHeroStat),
            SkillVariable = new(nameof(SkillVariable), GetVariable),
            SkillMetadata = new(nameof(SkillMetadata), GetSkillMetadata),
            ItemStats = new(nameof(ItemStats), GetItemStats),
            ItemSkills = new(nameof(ItemSkills), GetItemSkills),
            SkillCooldown = new(nameof(SkillCooldown), GetCooldown),
            SkillCost = new(nameof(SkillCost), GetCost),
            SkillDescription = new(nameof(SkillDescription), GetDescription),
            TooltipTitle = new(nameof(TooltipTitle), GetPlainText),
            TooltipMainText = new(nameof(TooltipMainText), GetPlainText),
            TooltipText = new(nameof(TooltipText), GetPlainText),
            TooltipTextOutOfFight = new(nameof(TooltipTextOutOfFight), GetPlainTextOutOfFight),
            ChainTooltip = new(nameof(ChainTooltip), GetChainText),
            RpgStatDescription = new(nameof(RpgStatDescription), GetRpgStatDescription),
            LocalizedText = new(nameof(LocalizedText), GetTranslation, null, FormatText);

        TokenType(string enumName, ValueMethod value) : base(enumName) {
            Value = value;
            TokenPreProcessing = FormatText;
            TokenPostProcessing = FormatSprite;
        }

        TokenType(string enumName, ValueMethod value, ProcessToken preProcessor, ProcessToken postProcessor) :
            base(enumName) {
            Value = value;
            TokenPreProcessing = preProcessor;
            TokenPostProcessing = postProcessor;
        }
        
        public string GetValue(TokenText token, ICharacter owner, object payload, List<object> children) {
            string inputValue = token.InputValue;
            TokenPreProcessing?.Invoke(ref inputValue, children);
            string result = Value(token, inputValue, owner, payload);
            TokenPostProcessing?.Invoke(ref result, children);
            return result;
        }

        // === Implementations

        // -- plain
        static string GetPlainText(TokenText token, string input, ICharacter c, object _) {
            return input;
        }
        
        static string GetPlainTextOutOfFight(TokenText token, string input, ICharacter c, object _) => input;

        // -- chain
        static string GetChainText(TokenText token, string input, ICharacter c, object payload) {
            return "";
        }
        
        // --- Hero rpg stat
        static string GetRpgStatDescription(TokenText token, string text, ICharacter owner, object payload) {
            if (payload is Item { StatsRequirements: not null } item) {
                ItemStatsRequirements statsRequirements = item.StatsRequirements;
                HeroRPGStats rpgStats = Hero.Current.HeroRPGStats;
                PrepareRequirementsDescription(rpgStats.Strength, statsRequirements.StrengthRequired, ref text);
                PrepareRequirementsDescription(rpgStats.Dexterity, statsRequirements.DexterityRequired, ref text);
                PrepareRequirementsDescription(rpgStats.Spirituality, statsRequirements.SpiritualityRequired, ref text);
                PrepareRequirementsDescription(rpgStats.Perception, statsRequirements.PerceptionRequired, ref text);
                PrepareRequirementsDescription(rpgStats.Endurance, statsRequirements.EnduranceRequired, ref text);
                PrepareRequirementsDescription(rpgStats.Practicality, statsRequirements.PracticalityRequired, ref text);
            }
            
            return text;
        }

        static void PrepareRequirementsDescription(Stat targetStat, Stat requiredStat, ref string text) {
            if (requiredStat <= 0) {
                return;
            }
            
            string requiredText = $"{requiredStat.ModifiedInt} {targetStat.Type.DisplayName}\n";
            Color color = targetStat >= requiredStat ? ARColor.MainGreen : ARColor.MainRed;

            if (string.IsNullOrEmpty(text)) {
                text += $"{LocTerms.Requires.Translate()} {requiredText}".ColoredText(color);;
            } else {
                text += $"{LocTerms.Requires.Translate().ColoredText(ARColor.Transparent)} {requiredText.ColoredText(color)}";
            }
        }
        
        // -- skill variable
        static string GetVariable(TokenText token, string input, ICharacter owner, object payload) {
            if (payload is ITextVariablesContainer variableContainer) {
                int index = token.AdditionalInt;
                float? floatVariable = variableContainer.GetVariable(input, index, owner);
                if (floatVariable != null) {
                    var formattedValue = floatVariable.Value.ToString(token.FormatSpecifier, LocalizationHelper.SelectedCulture);
                    formattedValue = formattedValue.Replace(' ', LocTerms.NonBreakingSpace);
                    return formattedValue;
                }
                
                StatType statType = variableContainer.GetEnum(input, index);
                return statType?.DisplayName ?? string.Empty;
            }

            return string.Empty;
        }
        
        // -- skill metadata
        static string GetSkillMetadata(TokenText token, string input, ICharacter owner, object payload) {
            
            return input switch {
                "BuffDuration" => GetBuffDuration(payload)?.ToString(token.FormatSpecifier, LocalizationHelper.SelectedCulture).Replace(' ', LocTerms.NonBreakingSpace),
                _ => null,
            } ?? string.Empty;

            static float? GetBuffDuration(object payload) {
                if (payload is Item item) {
                    if (item.TryGetElement(out ItemBuffApplier buffApplier)) {
                        return buffApplier.Duration;
                    }
                }
                return null;
            }
        }

        // -- item stats
        static string GetItemStats(TokenText token, string input, ICharacter owner, object payload) {
            if (payload is Item item) {
                return ItemUtils.DisplayWeaponStats(owner, item);
            }

            return "";
        }

        static string GetItemSkills(TokenText token, string input, ICharacter owner, object payload) {
            string result = "";
            if (payload is Item item) {
                foreach (var skill in item.ActiveSkills) {
                    string desc = skill.DescriptionFor(owner);
                    if (!string.IsNullOrWhiteSpace(desc)) {
                        result += "\n" + desc;
                    }
                }
            }

            return result.ColoredText(Color.gray).PercentSizeText(85);
        }

        // -- hero stats
        static string GetHeroStat(TokenText token, string text, ICharacter owner, object payload) {
            text = text.ToLower();
            text = Regex.Replace(text, "<.*?>", String.Empty);

            string valueText = Regex.Match(text, @"\d+").Value;
            float.TryParse(valueText, out var value);

            string result = "";
            if (text.Contains("damage")) {
                result = $"{value.ToString("P0").Replace(' ', LocTerms.NonBreakingSpace)} {ItemStatType.DamageValue.DisplayName}";
            }
            return result.FormatSprite();
        }

        // -- skill things
        static string GetCooldown(TokenText token, string text, ICharacter owner, object payload) {
            return payload switch {
                Skill skill => $"<align=center>{skill.Cooldown.GeneralDescription}</align>\n".ColoredText(Color.blue, 0.6f),
                _ => "",
            };
        }

        static string GetCost(TokenText token, string text, ICharacter owner, object payload) {
            return payload switch {
                Skill skill => $"<align=center>{LocTerms.Cost.Translate()}: {skill.Cost}</align>",
                _ => "",
            };
        }

        static string GetDescription(TokenText token, string text, ICharacter owner, object payload) {
            return payload switch {
                Skill skill => skill.DescriptionFor(owner),
                _ => "",
            };
        }

        static string GetTranslation(TokenText token, string text, ICharacter owner, object payload) {
            return LocalizationHelper.Translate(text);
        }

        static void FormatText(ref string text, List<object> children) {
            text = RichTextUtil.SmartFormat(text, children);
        }

        static void FormatSprite(ref string text, List<object> children) {
            text = text.FormatSprite().Trim();
        }
    }
}