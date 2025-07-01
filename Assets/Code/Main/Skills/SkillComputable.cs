using System;

namespace Awaken.TG.Main.Skills {
    public class SkillComputable {
        public string ID { [UnityEngine.Scripting.Preserve] get; }
        public Func<float> ValueFunc { get; }
        
        public SkillComputable(string id, Func<float> valueFunc) {
            ID = id;
            ValueFunc = valueFunc;
        }
    }
}