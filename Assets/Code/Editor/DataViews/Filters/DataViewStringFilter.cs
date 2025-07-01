using System;
using Awaken.TG.Editor.DataViews.Structure;
using Awaken.TG.Editor.Helpers;
using Awaken.Utility.Extensions;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;

namespace Awaken.TG.Editor.DataViews.Filters {
    [Serializable]
    public class DataViewStringFilter : DataViewFilter {
        static readonly GUIContent LabelPattern = new("Pattern");
        static readonly GUIContent LabelScore = new("Score");
        
        [SerializeField] Operator @operator;
        [SerializeField] string operand1;
        [SerializeField] long operand2;
        
        public override bool Match(DataViewValue value) {
            return @operator switch {
                Operator.Contains => Contains(value.stringValue),
                Operator.Fuzzy => FuzzyMatch(value.stringValue),
                _ => false,
            };
        }

        bool Contains(string value) {
            if (operand1.IsNullOrWhitespace()) {
                return true;
            }
            if (value.IsNullOrWhitespace()) {
                return false;
            }
            return value.Contains(operand1, StringComparison.OrdinalIgnoreCase);
        }
        
        bool FuzzyMatch(string value) {
            long score = 0;
            return FuzzySearch.FuzzyMatch(operand1, value, ref score) && score > operand2;
        }

        public override float DrawHeight() {
            if (@operator is Operator.Fuzzy) {
                return EditorGUIUtility.singleLineHeight * 3; // operator + 2 operands
            } else {
                return EditorGUIUtility.singleLineHeight * 2; // operator + operand
            }
        }

        public override void Draw(Rect rect, ref bool changed) {
            var rects = new PropertyDrawerRects(rect);
            var newOperator = (Operator)EditorGUI.EnumPopup(rects.AllocateLine(), @operator);
            if (newOperator != @operator) {
                @operator = newOperator;
                changed = true;
            }
            if (@operator is Operator.Fuzzy) {
                var newOperand1 = EditorGUI.TextField(rects.AllocateLine(), LabelPattern, operand1);
                if (newOperand1 != operand1) {
                    operand1 = newOperand1;
                    changed = true;
                }
                var newOperand2 = EditorGUI.LongField(rects.AllocateLine(), LabelScore, operand2);
                if (newOperand2 != operand2) {
                    operand2 = newOperand2;
                    changed = true;
                }
            } else {
                var newOperand1 = EditorGUI.TextField(rects.AllocateLine(), operand1);
                if (newOperand1 != operand1) {
                    operand1 = newOperand1;
                    changed = true;
                }
            }
        }

        enum Operator : byte {
            Contains,
            Fuzzy,
        }
    }
}