using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Maths;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Pattern {
    public class VTalentTreePattern : VTalentTreePatternBase {
        [SerializeField] CanvasGroup slotCanvasGroup;
        [SerializeField] List<SkillTalentSubTree> subTrees;

        public List<SkillTalentSubTree> SkillTalentTree => subTrees;
        protected override List<TalentSubTreeBase> GetSubTrees() => subTrees.Cast<TalentSubTreeBase>().ToList();
        
        void Awake() {
            SetSlotInteractions(false);
        }

        public void ShowOthersSubtrees() {
            SetSlotInteractions(false);

            foreach (var subTree in SkillTalentTree) {
                subTree.ButtonConfig.button.Interactable = true;
                subTree.ButtonConfig.button.enabled = true;
                subTree.ShowSubtree();
            }
        }

        public void HideOthersSubtree(TalentSubTreeBase target) {
            SetSlotInteractions(true);
                
            foreach (var subTree in SkillTalentTree) {
                subTree.ButtonConfig.button.Interactable = false;
                subTree.ButtonConfig.button.enabled = false;
                
                if (subTree.Equals(target)) continue;
                subTree.HideSubtree();
            }
        }
        
        public void SetSlotInteractions(bool interact) {
            slotCanvasGroup.interactable = !interact;
            slotCanvasGroup.blocksRaycasts = interact;
        }
        
#if UNITY_EDITOR
        [Button]
        void CalculateOffsetToCenter() {
            for (int index = 0; index < subTrees.Count; index++) {
                SkillTalentSubTree subTree = subTrees[index];
                IEnumerable<RectTransform> slots = subTree.Segments.SelectMany(segment => segment.treeNodes)
                    .Select(node => node.UISlot as RectTransform);
                Bounds bounds = RectTransformUtil.CalculateBoundsOfRectTransform(slots);
                var rectTransform = subTree.ButtonConfig.transform;
                subTree.SetZoomInOffset(bounds.center.XY() - rectTransform.position.XY());
                subTrees[index] = subTree;
            }
        }
#endif
    }
    
        
    [Serializable]
    public class SkillTalentSubTree : TalentSubTreeBase {
        [Title("Zoom In Settings")]
        [SerializeField] float zoomInScale;
        [SerializeField] Vector2 zoomInOffset;
        [Title("Name")]
        [SerializeField] LocString subTreeName;
        [SerializeField] TMP_Text nameLabel;
        
        public float ZoomInScale => zoomInScale;
        public Vector2 IconPosition => ButtonConfig.transform.position.XY() + zoomInOffset;
        
        public void SetupName() {
            nameLabel.text = subTreeName.ToString();
        }
        
        public void SetNameActive(bool active) {
            nameLabel.TrySetActiveOptimized(active);
        }
        
#if UNITY_EDITOR
        [Button]
        void CenterSubTreeImage() {
            IEnumerable<RectTransform> slots = Segments.SelectMany(segment => segment.treeNodes).Select(node => node.UISlot as RectTransform);
            Bounds bounds = RectTransformUtil.CalculateBoundsOfRectTransform(slots);
            
            var rectTransform = (RectTransform)ButtonConfig.transform;
            rectTransform.position = bounds.center;
        }
        
        public void SetZoomInOffset(Vector2 offset) {
            zoomInOffset = offset;
        }
#endif
    }
}
