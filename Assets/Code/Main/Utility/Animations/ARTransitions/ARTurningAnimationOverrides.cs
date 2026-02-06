using System;
using System.Linq;
using Animancer;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.ARTransitions {
    [Serializable]
    public class ARTurningAnimationOverrides {
        [PropertyOrder(999), ListDrawerSettings(ShowFoldout = false, ShowIndexLabels = false), Indent(2)] 
        [SerializeField] public ARTurningAnimationOverrideEntry[] entries = Array.Empty<ARTurningAnimationOverrideEntry>();
        
        public bool ShouldOverrideFor(NpcElement npc) {
            for (int i = 0; i < entries.Length; i++) {
                if (entries[i].IsInRange(npc)) {
                    return true;
                }
            }
            return false;
        }

        public ITransition GetOverrideFor(NpcElement npc) {
            if (entries.Length == 0) {
                return null;
            }
            for (int i = 0; i < entries.Length; i++) {
                if (entries[i].IsInRange(npc)) {
                    return entries[i].clip;
                }
            }
            return entries[0].clip;
        }
    }
}