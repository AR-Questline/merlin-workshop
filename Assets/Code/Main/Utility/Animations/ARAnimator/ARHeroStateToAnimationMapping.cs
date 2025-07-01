using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Heroes.Animations;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Utility.Animations.ARAnimator {
    [CreateAssetMenu(menuName = "TG/Animancer/HeroStateToAnimationMapping", order = 0)]
    public class ARHeroStateToAnimationMapping : ScriptableObject {
        public HeroLayerType layerType;
        [SerializeField, Searchable, PropertyOrder(999), Title("Entries")] public ARHeroStateToAnimationMappingEntry[] entries = Array.Empty<ARHeroStateToAnimationMappingEntry>();

        [UnityEngine.Scripting.Preserve]
        public IEnumerable<ITransition> GetAnimancerNodes(HeroStateType heroStateType) {
            return AnimancerUtils.GetAnimancerNodes(heroStateType, this);
        }
        
        public AnimationCurve GetCustomCurve(HeroStateType heroStateType) {
            foreach (var entry in entries) {
                if (entry.heroStateType == heroStateType) {
                    return entry.customSpeedMultiplyCurve ? entry.speedMultiplyCurve : null;
                }
            }

            return null;
        }
        
        // === Helpers
        [Button]
        void Sort() {
            entries = entries.OrderBy(e => (int)e.heroStateType).ToArray();
        }

#if UNITY_EDITOR
        [Button, Title("Converters")]
        void CopyEventsFromAnimationClips() => EDITOR_CopyEventsFromAnimationClip();
        
        public void EDITOR_CopyEventsFromAnimationClip() {
            foreach (var entry in entries) {
                entry.EDITOR_Owner = this;
                entry.EDITOR_CopyEventsFromAnimationClip();
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [Button]
        void RemoveAnimationEvents() => EDITOR_RemoveAnimationEvents();
        
        public void EDITOR_RemoveAnimationEvents() {
            foreach (var entry in entries) {
                entry.EDITOR_RemoveAnimationEvents();
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        [Button, Title("Draw Style"), HorizontalGroup("Drawing Style")]
        void DrawSimplified() {
            UnityEditor.EditorPrefs.SetBool("UseSimplifiedTransitionDrawer", true);
        }

        [Button, Title(""), HorizontalGroup("Drawing Style")]
        void DrawFull() {
            UnityEditor.EditorPrefs.SetBool("UseSimplifiedTransitionDrawer", false);
        }
#endif
    }
}