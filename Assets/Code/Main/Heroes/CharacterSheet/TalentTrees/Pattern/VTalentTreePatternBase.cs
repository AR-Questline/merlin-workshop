using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.GameObjects;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Pattern {
    public abstract class VTalentTreePatternBase : View<TalentTreePatternElement> {
        [SerializeField] ARButton firstSelectedSubtree;

        public List<TalentTreeNode> TalentNodes => GetSubTrees().SelectMany(subTree => subTree.Segments.SelectMany(treeSegment => treeSegment.treeNodes)).ToList();
        public ARButton FirstSelectedSubtree => firstSelectedSubtree;
        
        List<TalentSubTreeBase> TalentTree => GetSubTrees();
        protected abstract List<TalentSubTreeBase> GetSubTrees();
        
#if UNITY_EDITOR
        [FoldoutGroup("Editor"), SerializeField] Sprite lineSprite;
        [FoldoutGroup("Editor"), SerializeField] float lineThickness = 8f;
        
        bool IsUsedOnlyOnce(TemplateReference talentReference) {
            return TalentTree.SelectMany(subTree => subTree.Segments.SelectMany(segment => segment.treeNodes)).Count(node => node.IsUsed(talentReference)) <= 1;
        }
        
        [Button]
        void RedrawLines() {
            DEBUG_HideLines();
            
            foreach (TalentTreeNode node in TalentNodes) {
                if (node.HasParent == false) continue;
                Transform parent = node.OverrideParent != null ? node.OverrideParent : TalentNodes.Find(n => n.Talent == node.Parent).UISlot;
                
                // UILineRenderer lineRenderer = new GameObject("line").AddComponent<UILineRenderer>();
                // lineRenderer.raycastTarget = false;
                // lineRenderer.LineThickness = lineThickness;
                // lineRenderer.color = ARColor.MainGrey;
                // lineRenderer.sprite = lineSprite;
                // lineRenderer.Points = new Vector2[] {
                //     node.UISlot.position,
                //     parent.position
                // };
                //
                // lineRenderer.transform.SetParent(node.UISlot);
            }
        }
        
        [Button]
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
#if UNITY_EDITOR
        [SerializeField] string designerNote;
#endif
        [SerializeField] ButtonConfig buttonConfig;
        [SerializeField] List<TalentTreeSegment> treeSegment;
        
        public List<TalentTreeSegment> Segments => treeSegment;
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
            foreach (var segment in treeSegment) {
                foreach (var node in segment.treeNodes) {
                    node.UISlot.gameObject.SetActiveOptimized(active);
                }
            }
            
            ButtonConfig.TrySetActiveOptimized(active);
        }
    }
    
    /// <summary>
    /// A segment of a talent tree. Each segment is a row of talents. Used for inspector organization.
    /// </summary>
    [Serializable]
    public struct TalentTreeSegment {
#if UNITY_EDITOR
        [SerializeField] string designerNote;
#endif
        [FormerlySerializedAs("talentTreeSegment")] 
        [SerializeField, TableList(ShowIndexLabels = true)] public List<TalentTreeNode> treeNodes;
    }
    
    [Serializable]
    public struct TalentTreeNode {
        [SerializeField, TableColumnWidth(120), TemplateType(typeof(TalentTemplate))]
        TemplateReference talentReference;
        [SerializeField, TableColumnWidth(80)]
        Transform uiSlot;
        
        [SerializeField, TableColumnWidth(60, Resizable = false)] 
        bool isRoot;
        [SerializeField, TableColumnWidth(120), TemplateType(typeof(TalentTemplate)), HideIf(nameof(isRoot))]
        TemplateReference parentTalent;
        [SerializeField, TableColumnWidth(80), CanBeNull, ShowIf(nameof(isRoot))]
        Transform overrideParent;
        
        public TalentTemplate Talent => talentReference.Get<TalentTemplate>();
        public TalentTemplate Parent => isRoot ? null : parentTalent.Get<TalentTemplate>();
        public bool HasParent => !isRoot || overrideParent != null;
        public Transform OverrideParent => overrideParent;
        public Transform UISlot => uiSlot;
        
        public bool IsUsed(TemplateReference talentReference) => talentReference != null && talentReference.IsSet && this.talentReference.IsSet && talentReference == this.talentReference;
        bool IsNotSame => talentReference != parentTalent;
    }
}
