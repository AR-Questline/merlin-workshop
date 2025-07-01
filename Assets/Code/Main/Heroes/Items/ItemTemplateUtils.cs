using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Buffs;
using Awaken.TG.Main.Heroes.Items.Gems;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Utility.TokenTexts;

namespace Awaken.TG.Main.Heroes.Items {
    public static class ItemTemplateUtils {
        public static List<SkillReference> GetSkillReferences(ItemTemplate template) {
            var references = new List<SkillReference>();
            if (template.TryGetComponent(out ItemEffectsSpec effects)) {
                references.AddRange(effects.Skills);
            }
            if (template.TryGetComponent(out ItemBuffApplierAttachment buffApplier)) {
                references.AddRange(buffApplier.Skills);
            }
            if (template.TryGetComponent(out GemAttachment gem)) {
                references.AddRange(gem.Skills);
            }
            return references;
        }
        
        public static string GetDebugDescription(ItemTemplate template) {
            var skillReferences = GetSkillReferences(template);
            return TokenConverter.SkillVariableRegex.Replace(template.Description, match => {
                string variableName = match.Groups[1].Value;
                string additionalString = match.Groups[2].Value;
                string format = match.Groups[3].Value;
                int.TryParse(additionalString, out var skillIndex);

                foreach (var skillRef in skillReferences) {
                    if (TryGetVariable(skillRef, variableName, format, out var value)) {
                        if (skillIndex == 0) {
                            return $"[{value}]";
                        }
                        skillIndex--;
                    }
                }
                return "[-]";
            });
            
            static bool TryGetVariable(SkillReference skillRef, string name, string format, out string value) {
                foreach (var variable in skillRef.variables) {
                    if (variable.name == name) {
                        value = variable.value.ToString(format);
                        return true;
                    }
                }
                foreach (var variable in skillRef.enums) {
                    if (variable.name == name) {
                        value = variable.enumReference.EnumAs<StatType>().DisplayName;
                        return true;
                    }
                }
                foreach (var variable in skillRef.datums) {
                    if (variable.name == name) {
                        var objectValue = variable.type.GetValue(variable.value);
                        value = objectValue switch {
                            int iValue => iValue.ToString(format),
                            float fValue => fValue.ToString(format),
                            _ => objectValue.ToString()
                        };
                        return true;
                    }
                }
                value = null;
                return false;
            }
        }
    }
}