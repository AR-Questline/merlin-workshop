using Awaken.Utility;
using System;
using Awaken.TG.Utility.Attributes;

namespace Awaken.TG.Main.Skills {
    [Serializable]
    public partial class SkillVariablesOverride {
        public ushort TypeForSerialization => SavedTypes.SkillVariablesOverride;

        [Saved] SkillVariable[] _overrideVariables;
        [Saved] SkillRichEnum[] _overrideEnums;
        [Saved] SkillDatum[] _overrideDatums;
        [Saved] SkillAssetReference[] _assetReferences;
        [Saved] SkillTemplate[] _templates;

        public SkillVariablesOverride(SkillVariable[] overrideVariables = null, SkillRichEnum[] overrideEnums = null, SkillDatum[] overrideDatums = null, SkillAssetReference[] assetReferences = null, SkillTemplate[] templates = null) {
            _overrideVariables = overrideVariables ?? Array.Empty<SkillVariable>();
            _overrideEnums = overrideEnums ?? Array.Empty<SkillRichEnum>();
            _overrideDatums = overrideDatums ?? Array.Empty<SkillDatum>();
            _assetReferences = assetReferences ?? Array.Empty<SkillAssetReference>();
            _templates = templates ?? Array.Empty<SkillTemplate>();
        }

        public void Apply(Skill skill) {
            if (_overrideVariables != null) {
                foreach (var oVariable in _overrideVariables) {
                    skill.OverrideVariable(oVariable.name, oVariable.value);
                }
            }
            if (_overrideEnums != null) {
                foreach (var oEnum in _overrideEnums) {
                    skill.OverrideRichEnum(oEnum.name, oEnum.Value);
                }
            }
            if (_overrideDatums != null) {
                foreach (var oDatum in _overrideDatums) {
                    skill.OverrideDatum(oDatum.name, oDatum.type, oDatum.value);
                }
            }
            if (_assetReferences != null) {
                foreach (var oAssetRef in _assetReferences) {
                    skill.OverrideAssetReference(oAssetRef.name, oAssetRef.assetReference);
                }
            }
            if (_templates != null) {
                foreach (var oTemplate in _templates) {
                    skill.OverrideTemplate(oTemplate.name, oTemplate.templateReference);
                }
            }
        }
    }
}