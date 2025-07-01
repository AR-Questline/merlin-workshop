using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Main.Utility.Skills;
using Awaken.TG.Main.Utility.TokenTexts;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.Tooltips;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Development.Talents {
    public class TalentTemplate : ScriptableObject, ITemplate {
        public string GUID { get; set; }

        [SerializeField, HideInInspector] TemplateMetadata metadata;
        public TemplateMetadata Metadata => metadata;
        
        string INamed.DisplayName => Name;
        string INamed.DebugName => name;

        [SerializeField, LocStringCategory(Category.Skill)] 
        LocString talentName;
        
        [RichEnumExtends(typeof(Keyword)), SerializeField] 
        List<RichEnumReference> keywords = new();

        [SerializeField, ListDrawerSettings(ShowIndexLabels = true), Space(10f)]
        Level[] levels = Array.Empty<Level>();
        
        [SerializeField] int requiredTreeLevelToUnlock;
        
        public string Name => talentName;
        public IEnumerable<Keyword> Keywords => keywords.Select(k => k.EnumAs<Keyword>()).Concat(levels.SelectMany(l => l.Keywords));
        public int MaxLevel => levels.Length;
        public int RequiredTreeLevelToUnlock => requiredTreeLevelToUnlock;

        public ref Level GetLevel(int i) {
            if (i == 0) {
                return ref Level.empty;
            } else {
                return ref levels[i-1];
            }
        }
        
        public TooltipConstructor KeywordDescription(Talent talent, int currentLevel, int nextLevel) {
            var tokenText = new TooltipConstructorTokenText();
            bool hasAny = false;

            var keywordsFromDescription = GetLevel(currentLevel).KeywordsFromDescription().Concat(GetLevel(nextLevel).KeywordsFromDescription());
            
            foreach (var keyword in SkillsUtils.KeywordDescriptions(keywordsFromDescription.Concat(Keywords))) {
                hasAny = true;
                tokenText.AddToken(new TokenText(TokenType.TooltipText, keyword));
            }

            return !hasAny ? null : tokenText.GetTooltip(Hero.Current, talent);
        }

        public string GetDebugDescription() {
            if (levels.Length == 0) {
                return "No levels";
            }

            var sb = new StringBuilder();
            var descriptionBlueprint = levels[0].GetDebugDescriptionBlueprint();
            return TokenConverter.SkillVariableRegex.Replace(descriptionBlueprint, match => {
                string variable = match.Groups[1].Value;
                string additionalString = match.Groups[2].Value;
                string formatSpecifier = match.Groups[3].Value;
                int.TryParse(additionalString, out var skillIndex);

                sb.Clear();
                AppendVariable(levels[0], variable, skillIndex, formatSpecifier, out var isNumeric);
                if (isNumeric) {
                    sb.Insert(0, '[');
                    for (int level = 1; level < levels.Length; level++) {
                        sb.Append('/');
                        AppendVariable(levels[level], variable, skillIndex, formatSpecifier, out _);
                    }
                    sb.Append(']');
                }
                return sb.ToString();
            });

            void AppendVariable(in Level level, string name, int skillIndex, string formatSpecifier, out bool isNumeric) {
                foreach (var skillRef in level.Skills) {
                    if (TryAppendVariable(skillRef, ref skillIndex, name, formatSpecifier, out var isThisNumeric)) {
                        isNumeric = isThisNumeric;
                        return;
                    }
                }
                isNumeric = false;
                sb.Append('-');
            }
            
            bool TryAppendVariable(SkillReference skillRef, ref int skillIndex, string name, string format, out bool isNumeric) {
                isNumeric = false;
                foreach (var variable in skillRef.variables) {
                    if (variable.name != name) {
                        continue;
                    }
                    if (skillIndex == 0) {
                        sb.Append(variable.value.ToString(format));
                        isNumeric = true;
                        return true;
                    }
                    skillIndex--;
                    return false;
                }
                foreach (var variable in skillRef.enums) {
                    if (variable.name != name) {
                        continue;
                    }
                    if (skillIndex == 0) {
                        sb.Append(variable.enumReference.EnumAs<StatType>().DisplayName);
                        return true;
                    }
                    skillIndex--;
                    return false;
                }
                foreach (var variable in skillRef.datums) {
                    if (variable.name != name) {
                        continue;
                    }
                    if (skillIndex == 0) {
                        var value = variable.type.GetValue(variable.value);
                        if (value is int iValue) {
                            isNumeric = true;
                            sb.Append(iValue.ToString(format));
                        } else if (value is float fValue) {
                            isNumeric = true;
                            sb.Append(fValue.ToString(format));
                        } else {
                            sb.Append(value);
                        }
                        return true;
                    }
                    skillIndex--;
                    return false;
                }
                return false;
            }
        }

        [Serializable]
        public struct Level {
            [SerializeField, LocStringCategory(Category.Skill)] LocString description;
            [SerializeField] List<SkillReference> skills;

            public static Level empty = new() { skills = new List<SkillReference>() };
            public List<SkillReference> Skills => skills;
            public IEnumerable<Keyword> Keywords => Skills.SelectMany(s => s.skillGraphRef.Get<SkillGraph>().Keywords);

            public IEnumerable<Keyword> KeywordsFromDescription() {
                return SkillsUtils.ExtractKeywords(description?.ToString());
            }
            
            public string Description(Talent talent, int level) {
                var hero = Hero.Current;
                string descriptionText = description?.ToString();
                
                if (!string.IsNullOrWhiteSpace(descriptionText)) {
                    var descriptionToken = new TokenText(description);
                    var retriever = new TalentVariablesRetriever(talent, level);
                    return descriptionToken.GetValue(hero, retriever);
                } else {
                    var builder = new StringBuilder();
                    foreach (var skillRef in skills) {
                        builder.Append(SkillReferenceUtils.Description(hero, skillRef));
                        builder.Append("\n<size=50%>\n</size>");
                    }
                    return builder.ToString();
                }
            }

            public string GetDebugDescriptionBlueprint() {
                return description;
            }
        }
    }
}