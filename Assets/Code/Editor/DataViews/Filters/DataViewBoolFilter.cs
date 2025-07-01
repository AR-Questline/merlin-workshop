using System;
using Awaken.TG.Editor.DataViews.Structure;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.DataViews.Filters {
    [Serializable]
    public class DataViewBoolFilter : DataViewFilter {
        [SerializeField] Operator @operator;
        
        public override bool Match(DataViewValue value) {
            return @operator switch {
                Operator.IsTrue => value.boolValue,
                Operator.IsFalse => !value.boolValue,
                _ => false,
            };
        }

        public override float DrawHeight() {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void Draw(Rect rect, ref bool changed) {
            var newOperator = (Operator)EditorGUI.EnumPopup(rect, @operator);
            if (newOperator != @operator) {
                @operator = newOperator;
                changed = true;
            }
        }

        enum Operator : byte {
            IsTrue,
            IsFalse
        }
    }
}