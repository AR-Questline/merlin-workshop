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

        public SkillVariablesOverride(SkillVariable[] overrideVariables = null, SkillRichEnum[] overrideEnums = null, SkillDatum[] overrideDatums = null) {
            _overrideVariables = overrideVariables ?? Array.Empty<SkillVariable>();
            _overrideEnums = overrideEnums ?? Array.Empty<SkillRichEnum>();
            _overrideDatums = overrideDatums ?? Array.Empty<SkillDatum>();
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
        }
    }
}