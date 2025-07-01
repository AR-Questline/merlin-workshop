using System;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Utility.VSDatums;
using UnityEngine;

namespace Awaken.TG.Main.VisualGraphUtils.Datums {
    public class VSDatums : MonoBehaviour {
        [SerializeField] SkillDatum[] datums = Array.Empty<SkillDatum>();
        
        public VSDatumValue GetDatum(string name, in VSDatumType type) {
            foreach (var datum in datums) {
                if (datum.name == name && datum.type.Equals(type)) {
                    return datum.value;
                }
            }
            return default;
        }
    }
}