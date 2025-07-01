using System;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Tooltips {
    [Serializable]
    public class TooltipPosition {
        [SerializeField] Transform anchor;
        [SerializeField] Alignment alignment;

        [UnityEngine.Scripting.Preserve] public Transform Anchor => anchor;
        [UnityEngine.Scripting.Preserve] public Alignment ContentAlignment => alignment;

        public TooltipPositionCache Position {
            get {
                var position = Vector3.zero;
                if (anchor) {
                    position = anchor.position;
                }
                
                return new TooltipPositionCache(position, alignment.Pivot());
            }
        }
        
        public TooltipPosition(Transform anchor, Alignment alignment) {
            this.anchor = anchor;
            this.alignment = alignment;
        }
    }
}