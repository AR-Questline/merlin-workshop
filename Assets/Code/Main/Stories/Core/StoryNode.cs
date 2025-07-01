using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Utility.RichEnums;
using Sirenix.OdinInspector;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;
using XNode;

namespace Awaken.TG.Main.Stories.Core {
    /// <summary>
    /// Abstract base node for all nodes used in StoryGraph. Manages elements list.
    /// </summary>
    public abstract class StoryNode : Node {
        [HideInStoryGraph] 
        public bool toReview;
        [HideInStoryGraph]
        public List<NodeElement> elements = new List<NodeElement>();
        public IEnumerable<NodeElement> NodeElements => elements;

        [RichEnumExtends(typeof(NodeType)), HideInStoryGraph]
        public RichEnumReference nodeType;

        [HideInInspector, HideInStoryGraph]
        public bool changedTint;
        [HideInStoryGraph]
        public Color tint;

        public NodeType Type => nodeType.EnumAs<NodeType>() ?? NodeType.Default;
        public StoryGraph Graph => (StoryGraph) graph;

        public abstract Type GenericType { get; }
        
        public bool Folded { get; set; }

        public override object GetValue(NodePort port) {
            return this;
        }

        public override void OnJustPasted() {
            foreach (var element in NodeElements) {
                element.JustPasted = true;
            }
        }

        // === Debug
#if UNITY_EDITOR
        [FoldoutGroup("Debug"), ShowInInspector, HideInStoryGraph]
        UnityEditor.MonoScript[] ElementTypes => elements.Select(UnityEditor.MonoScript.FromScriptableObject).ToArray();
        [FoldoutGroup("Debug"), ShowInInspector, HideInStoryGraph]
        List<NodeElement> DebugElements => elements;
#endif
    }

    public class StoryNode<T> : StoryNode where T : NodeElement {
        public IEnumerable<T> Elements => NodeElements.OfType<T>();
        public override Type GenericType => typeof(T);
    }
}