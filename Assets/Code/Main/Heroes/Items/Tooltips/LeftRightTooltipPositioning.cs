using System;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Main.Heroes.Items.Tooltips {
    [Serializable]
    public struct LeftRightTooltipPositioning {
        [SerializeField, HideLabel, InlineProperty] TooltipPositioning positioning;
        [SerializeField] Side preferredSide;
        [SerializeField, CanBeNull] HorizontalOrVerticalLayoutGroup layout;

        TooltipPosition _leftPosition;
        TooltipPosition _rightPosition;

        public bool IsValid => _leftPosition != null && _rightPosition != null;
        
        public void SetPosition(TooltipPosition left, TooltipPosition right) {
            _leftPosition = left;
            _rightPosition = right;
        }

        public void RefreshPosition() {
            if (preferredSide == Side.Left) {
                CorrectLayoutGroup(true);
                positioning.UpdatePosition(_leftPosition, out _, out var horizontalMove);
                if (horizontalMove == TooltipPositioning.HorizontalMove.Right) {
                    CorrectLayoutGroup(false);
                    positioning.UpdatePosition(_rightPosition, out _, out horizontalMove);
                    if (horizontalMove == TooltipPositioning.HorizontalMove.Left) {
                        positioning.UpdatePosition(_leftPosition);
                    }
                }
            } else {
                CorrectLayoutGroup(false);
                positioning.UpdatePosition(_rightPosition, out _, out var horizontalMove);
                if (horizontalMove == TooltipPositioning.HorizontalMove.Left) {
                    CorrectLayoutGroup(true);
                    positioning.UpdatePosition(_leftPosition, out _, out horizontalMove);
                    if (horizontalMove == TooltipPositioning.HorizontalMove.Right) {
                        positioning.UpdatePosition(_rightPosition);
                    }
                }
            }
        }
        
        void CorrectLayoutGroup(bool reverse) {
            if (layout != null) {
                layout.reverseArrangement = reverse;
            }
        }

        public TooltipPositionCache CachePosition() => positioning.CachePosition();
        public void SetPosition(in TooltipPositionCache cache) => positioning.SetPosition(cache);

        enum Side : byte { Left, [UnityEngine.Scripting.Preserve] Right }
    }
}