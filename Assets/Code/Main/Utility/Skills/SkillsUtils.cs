using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Utility.TokenTexts;
using Awaken.TG.Utility;
using Awaken.Utility;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Utility.Skills {
    public static class SkillsUtils {
        public static float StatValueToValue(Stat stat, float value, ValueType valueType) {
            return valueType switch {
                ValueType.PercentOfMax => MaxValue(stat) * value * 0.01F,
                ValueType.Percent => stat * value * 0.01F,
                _ => value
            };

            static float MaxValue(Stat stat) => stat switch {
                LimitedStat limited => limited.UpperLimit - limited.LowerLimit,
                _ => stat.ModifiedValue,
            };
        }

        // [] bracelets are for upgradables
        // {} bracelets are for text icons
        // || bracelets are for hero stats
        // $$ bracelets are for computables

        static Regex KeywordRegex => TokenConverter.Keywords.Regex;

        public static TokenText ConstructDescriptionToken(Skill skill) {
            return new(BaseDescription(skill));
        }

        static string BaseDescription(Skill skill) {
            var descriptionBuilder = new StringBuilder();
            if (skill.TooltipPart is { } part) {
                descriptionBuilder.AppendLine(part);
            }

            return descriptionBuilder.ToString().TrimEnd();
        }
        
        public static TokenText ConstructTooltip(Skill skill, TokenText description) {
            var token = new TokenText();

            token.Append($"<b><align=center>{skill.DisplayName}</align></b>\n");
            
            if (skill.HasCooldown) {
                token.AddToken(TokenType.SkillCooldown);
                token.Append("<size=50%>\n</size>");
            }

            if (skill.HasCost) {
                token.AddToken(TokenType.SkillCost);
                token.Append("<size=50%>\n</size>");
            }

            token.AddToken(TokenType.SkillDescription);
            
            foreach (var keywordDescription in KeywordDescriptions(description.InputValue, skill.Keywords)) {
                token.Append("\n\n");
                token.AddToken(keywordDescription);
            }

            return token;
        }

        [UnityEngine.Scripting.Preserve]
        public static IEnumerable<string> KeywordDescriptions(string description) {
            return KeywordDescriptions(ExtractKeywords(description));
        }
        
        public static IEnumerable<string> KeywordDescriptions(string description, IEnumerable<Keyword> keywords) {
            return KeywordDescriptions(ExtractKeywords(description).Concat(keywords));
        }
        
        public static IEnumerable<string> KeywordDescriptions(IEnumerable<Keyword> keywords) {
            var displayedKeywords = new List<Keyword>();
            foreach (var keyword in keywords) {
                if (!displayedKeywords.Contains(keyword)) {
                    displayedKeywords.Add(keyword);
                    string name = keyword.Name.ToString().ColoredText(keyword.DescColor.Hex).Bold();
                    string keywordDescription = keyword.Description;
                    yield return $"{name} - {ConvertKeywords(keywordDescription)}";
                    foreach (var nestedKeyword in ExtractKeywords(keywordDescription)) {
                        if (!displayedKeywords.Contains(nestedKeyword)) {
                            displayedKeywords.Add(nestedKeyword);
                            string nestedName = nestedKeyword.Name.ToString().ColoredText(nestedKeyword.DescColor.Hex).Bold();
                            yield return $"{nestedName} - {nestedKeyword.Description}";
                        }
                    }
                }
            }
        }

        // === Converters
        public static string ConvertKeywords(string desc) {
            desc ??= "";
            string descriptionWithKeywords = KeywordRegex.Replace(desc, match => {
                // Group 1 - keyword enum, Group 2 - text to replace
                var keyword = StringToKeyword(match.Groups[1].Value);
                string text = match.Groups[2].Value;
                return text.ColoredText(keyword?.DescColor.Hex ?? ARColor.SpecialAccent.Hex);
            });
            return descriptionWithKeywords;
        }

        public static IEnumerable<Keyword> ExtractKeywords(string desc) {
            List<Keyword> keywords = new();
            desc ??= "";
            foreach (var match in KeywordRegex.Matches(desc).Where(m => m.Success)) {
                var keyword = StringToKeyword(match.Groups[1].Value);
                if (keyword != null) {
                    keywords.Add(keyword);
                }
            }
            return keywords;
        }

        public static Keyword StringToKeyword(string key) => Keyword.KeywordFor(key);
        
        // === Variable Cache
        
        public static Dictionary<string, Skill> ConstructVariableCache(IEnumerable<Skill> skills) {
            var map = new Dictionary<string, Skill>();
            foreach (Skill skill in skills) {
                foreach (string id in skill.VariableNames()) {
                    // If multiple skills contain same variable, first skill will be remembered
                    map.TryAdd(id, skill);
                }
            }
            return map;
        }
        
        [UnityEngine.Scripting.Preserve]
        public static float GetVariableValue(this ISkillUnit unit, Flow flow, string variableName) {
            var skill = unit.Skill(flow);
            return skill.SourceItem?.GetVariable(variableName, owner: skill.Owner) ?? 0.0f;
        }
    }
}