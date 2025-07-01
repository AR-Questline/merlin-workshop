using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Skills {
    [Serializable]
    public partial class SkillReference {
        public ushort TypeForSerialization => SavedTypes.SkillReference;

        [Saved, SerializeField] 
        [TemplateType(typeof(SkillGraph))]
        public TemplateReference skillGraphRef;
        
        [Saved] public List<SkillVariable> variables = new();
        [Saved] public List<SkillRichEnum> enums = new();
        [Saved] public List<SkillDatum> datums = new();
        [Saved] public List<SkillTemplate> templates = new();
        [Saved] public List<SkillAssetReference> assetReferences = new();

        public SkillGraph SkillGraph(object debugTarget) => skillGraphRef.Get<SkillGraph>(debugTarget);
        public bool IsSet => skillGraphRef.IsSet;

        public Skill CreateSkill() {
            var skill = new Skill(SkillGraph(null));
            skill.AssignVariables(this);
            return skill;
        }
        
        public SkillReference Copy() {
            return new SkillReference {
                skillGraphRef = skillGraphRef, 
                variables = variables.Select(v => v.Copy()).ToList(), 
                enums = enums.Select(e => e.Copy()).ToList(),
                datums = datums.Select(d => d.Copy()).ToList(),
                templates = templates.Select(e => e.Copy()).ToList(),
                assetReferences = assetReferences.Select(e => e.Copy()).ToList()
            };
        }

        [Obsolete]
        public void SetVariable(string name, float value) {
            variables.First(v => v.name.Equals(name)).value = value;
        }

        [Obsolete]
        public void SetEnum(string name, RichEnumReference value) {
            enums.First(v => v.name.Equals(name)).enumReference = value;
        }

        [Obsolete]
        public float? GetVariable(string name) {
            return variables.FirstOrDefault(v => v.name.Equals(name))?.value;
        }

        [Obsolete]
        public StatType GetEnum(string name) {
            return enums.FirstOrDefault(v => v.name.Equals(name))?.enumReference.EnumAs<StatType>();
        }
        
        // === Equality Members

        protected bool Equals(SkillReference other) {
            return Equals(skillGraphRef, other.skillGraphRef) && 
                   Equals(variables, other.variables) && 
                   Equals(enums, other.enums) && 
                   Equals(datums, other.datums) &&
                   Equals(templates, other.templates) &&
                   Equals(assetReferences, other.assetReferences);
        }
        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SkillReference) obj);
        }
        public override int GetHashCode() {
            unchecked {
                int hashCode = (skillGraphRef != null ? skillGraphRef.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (variables != null ? variables.GetSequenceHashCode() : 0);
                hashCode = (hashCode * 397) ^ (enums != null ? enums.GetSequenceHashCode() : 0);
                hashCode = (hashCode * 397) ^ (datums != null ? datums.GetSequenceHashCode() : 0);
                hashCode = (hashCode * 397) ^ (templates != null ? templates.GetSequenceHashCode() : 0);
                hashCode = (hashCode * 397) ^ (assetReferences != null ? assetReferences.GetSequenceHashCode() : 0);
                return hashCode;
            }
        }
        
        public static bool operator ==(SkillReference left, SkillReference right) {
            return Equals(left, right);
        }
        public static bool operator !=(SkillReference left, SkillReference right) {
            return !Equals(left, right);
        }

        public static int Compare(SkillReference x, SkillReference y) {
            if (x == null) {
                return y == null ? 0 : 1;
            }
            if (y == null) {
                return -1;
            }
            
            int result = x.GetHashCode() - y.GetHashCode();
            if (result != 0) return result;
            if (x == y) return 0;

            result = string.Compare(x.skillGraphRef.GUID, y.skillGraphRef.GUID, StringComparison.InvariantCulture);
            if (result != 0) return result;

            result = x.variables.GetHashCode() - y.variables.GetHashCode();
            if (result != 0) return result;

            result = x.enums.GetHashCode() - y.enums.GetHashCode();
            if (result != 0) return result;

            result = x.datums.GetHashCode() - y.datums.GetHashCode();
            if (result != 0) return result;
            
            result = x.templates.GetHashCode() - y.templates.GetHashCode();
            if (result != 0) return result;
            
            result = x.assetReferences.GetHashCode() - y.assetReferences.GetHashCode();
            if (result != 0) return result;
                
            Log.Important?.Warning("SkillReference Comparison returned falsely equality");
            return 0;
        }
    }
}