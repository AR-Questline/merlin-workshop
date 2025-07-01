using System.Collections.Generic;
using System.Linq;
using Animancer;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Awaken.TG.Main.Utility.Animations.ARAnimator {
    [CreateAssetMenu(menuName = "TG/Animancer/StateToAnimationMapping", order = 0)]
    public class ARStateToAnimationMapping : ScriptableObject {
        [SerializeField, Searchable, PropertyOrder(999), Title("Entries")] public List<ARStateToAnimationMappingEntry> entries = new();

        public IEnumerable<ITransition> GetAnimancerNodes(NpcStateType npcStateType) {
            return AnimancerUtils.GetAnimancerNodes(npcStateType, entries);
        }
        
#if UNITY_EDITOR      
        // === Helpers
        [Button, HorizontalGroup("Helpers")]
        void Sort() {
            entries = entries.OrderBy(e => (int)e.npcStateType).ToList();
        }
        
        [Button, HorizontalGroup("Helpers")]
        void ResampleRootRotationDeltas() {
            foreach (var entry in entries) {
                foreach (var clipTransition in entry.clipTransitions) {
                    clipTransition.SampleRootRotationDelta();
                }
            }
        }
        
        [Button, HorizontalGroup("Helpers")]
        void FindNullClipsAndPrint() {
            foreach (var entry in entries) {
                if (entry.IsMixerType) {
                    if (!entry.mixerTransition.IsValid) {
                        Log.Critical?.Error($"Invalid mixer transition found in {entry.npcStateType} in '{name}'", this);
                    }
                    continue;
                }
                foreach (var clipTransition in entry.clipTransitions) {
                    if (clipTransition.Clip == null) {
                        Log.Critical?.Error($"Null clip found in {entry.npcStateType} in '{name}'", this);
                    }
                }
            }
        }
        
        [Button, Title("Draw Style"), HorizontalGroup("Drawing Style")]
        void DrawSimplified() {
            EditorPrefs.SetBool("UseSimplifiedTransitionDrawer", true);
        }

        [Button, Title(""), HorizontalGroup("Drawing Style")]
        void DrawFull() {
            EditorPrefs.SetBool("UseSimplifiedTransitionDrawer", false);
        }
#endif
    }
}