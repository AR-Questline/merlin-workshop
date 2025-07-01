using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Utility.Animations.ARTransitions;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Awaken.TG.Main.Utility.Animations.ARAnimator {
    [Serializable]
    public class ARStateToAnimationMappingEntry {
        // === Fields
        public NpcStateType npcStateType;
        [HideIf(nameof(IsMixerType))]
        public ARClipTransition[] clipTransitions;
        [ShowIf(nameof(IsMixerType))]
        public MixerTransition2DAsset mixerTransition;

        // === Properties
        public IEnumerable<ITransition> AnimancerNodes => IsMixerType ? mixerTransition.Yield() : clipTransitions;
        public bool IsMixerType => AnimancerUtils.IsMixerType(npcStateType);
        
        // === Constructors
        public ARStateToAnimationMappingEntry() {
            npcStateType = NpcStateType.None;
        }
        
        public ARStateToAnimationMappingEntry(NpcStateType stateType) {
            npcStateType = stateType;
        }
    }
}