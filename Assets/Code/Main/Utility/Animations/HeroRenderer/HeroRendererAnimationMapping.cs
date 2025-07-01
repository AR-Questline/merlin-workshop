using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.HeroRenderer {
    [CreateAssetMenu(menuName = "TG/HeroRendererAnimationMapping", order = 0)]
    public class HeroRendererAnimationMapping : ScriptableObject {
        [SerializeField, PropertyOrder(999), Title("Entries")]
        public List<HeroRendererAnimationEntry> entries = new();

        public HeroRendererAnimationEntry FindFor(ILoadout loadout) {
            return entries.Find(e => e.Matches(loadout));
        }

        // === Helpers
#if UNITY_EDITOR
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