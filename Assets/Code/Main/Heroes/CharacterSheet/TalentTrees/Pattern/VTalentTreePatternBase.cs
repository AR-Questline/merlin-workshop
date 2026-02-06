using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Subtree;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Pattern {
    public abstract class VTalentTreePatternBase : ViewComponent<ITreePatternHost> {
        [SerializeField] ARButton firstSelectedSubtree;

        public abstract List<TalentSubTreeBase> SubTrees { get; }
        public ARButton FirstSelectedSubtree => firstSelectedSubtree;
        public TalentPatternSlot FirstSlot => _cachedSlots.FirstOrDefault();
        public HashSet<TalentPatternSlot> CachedSlots => _cachedSlots ??= new HashSet<TalentPatternSlot>(GetComponentsInChildren<TalentPatternSlot>(true));
        protected HashSet<TalentPatternSlot> _cachedSlots;
        
        protected override void OnAttach() {
            _cachedSlots = new HashSet<TalentPatternSlot>(GetComponentsInChildren<TalentPatternSlot>(true));
        }

        public Transform GetSlotForTalent(Talent talent) {
            var node = CachedSlots.FirstOrDefault(slot => slot.Talent == talent.Template);
            return node != null ? node.transform : null;
        }

#if UNITY_EDITOR
        [FoldoutGroup("Editor"), SerializeField] float lineThickness = 8f;
        
        [FoldoutGroup("Editor"), Button]
        void RedrawLines() {
            DEBUG_HideLines();
            _cachedSlots = new HashSet<TalentPatternSlot>(GetComponentsInChildren<TalentPatternSlot>(true));

            foreach (var node in _cachedSlots) {
                if (!node.HasParent) continue;
                Transform parent = node.OverrideParent != null ? node.OverrideParent : _cachedSlots.FirstOrDefault(n => n.Talent == node.Parent)?.UISlot;
                
                if (parent == null) continue;
                
                // UILineRenderer lineRenderer = new GameObject("line").AddComponent<UILineRenderer>();
                // lineRenderer.raycastTarget = false;
                // lineRenderer.LineThickness = lineThickness;
                // lineRenderer.color = ARColor.MainGrey;
                // lineRenderer.Points = new Vector2[] {
                //     node.UISlot.position,
                //     parent.position
                // };
                //
                // lineRenderer.transform.SetParent(node.UISlot);
            }
        }
        
        [FoldoutGroup("Editor"), Button]
        void DEBUG_HideLines() {
            // var lines = transform.GetComponentsInChildren<UILineRenderer>(true);
            //
            // for (int i = lines.Length - 1; i >= 0; i--) {
            //     DestroyImmediate(lines[i].gameObject);
            // }
        }
#endif
    }
    
    /// <summary>
    /// A subtree of talents. Use in skill tree UI to zoom in on a specific part of the tree.
    /// </summary>
    [Serializable]
    public class TalentSubTreeBase {
        [Title("Base Subtree")]
        [SerializeField, RichEnumExtends(typeof(TalentSubtreeType))] 
        RichEnumReference subtreeType;
        [SerializeField] ButtonConfig buttonConfig;
        [SerializeField] protected Transform slotsRoot;
        
        public TalentSubtreeType SubtreeType => subtreeType.EnumAs<TalentSubtreeType>();
        public ButtonConfig ButtonConfig => buttonConfig;

        public virtual void SetSectionState(bool enabled) {
            SetActive(enabled);
        }

        public void ShowSubtree() {
            SetActive(true);
        }
        
        public void HideSubtree() {
            SetActive(false);
        }

        void SetActive(bool active) {
            foreach (var node in slotsRoot.GetComponentsInChildren<TalentPatternSlot>(true).Where(slot => slot.SubtreeType.Equals(SubtreeType))) {
                node.TrySetActiveOptimized(active);
            }
            
            ButtonConfig.TrySetActiveOptimized(active);
        }
    }
}
