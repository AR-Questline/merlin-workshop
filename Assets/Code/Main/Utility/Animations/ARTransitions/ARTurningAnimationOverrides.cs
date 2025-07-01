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
        [SerializeField] public ARTurningAnimationOverrideEntry[] entries;
        
        public bool ShouldOverrideFor(NpcElement npc) {
            return entries?.Any(entry => entry.IsInRange(npc)) ?? false;
        }

        public ITransition GetOverrideFor(NpcElement npc) {
            return entries?.FirstOrAny(entry => entry.IsInRange(npc)).clip ?? null;
        }
    }
}