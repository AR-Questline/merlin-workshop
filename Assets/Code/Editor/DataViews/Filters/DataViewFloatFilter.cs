using System;
using Awaken.TG.Editor.DataViews.Structure;
using Awaken.TG.Editor.Helpers;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.DataViews.Filters {
    [Serializable]
    public class DataViewFloatFilter : DataViewFilter {
        [SerializeField] Operator @operator;
        [SerializeField] float operand1;
        [SerializeField] float operand2;
        
        public override bool Match(DataViewValue value) {
            return @operator switch {
                Operator.Less => value.floatValue < operand1,
                Operator.LessOrEqual => value.floatValue <= operand1,
                Operator.Equal => value.floatValue == operand1,
                Operator.GreaterOrEqual => value.floatValue >= operand1,
                Operator.Greater => value.floatValue > operand1,
                Operator.InRange => value.floatValue >= operand1 && value.floatValue <= operand2,
                Operator.OutOfRange => value.floatValue < operand1 || value.floatValue > operand2,
                _ => false,
            };
        }

        public override float DrawHeight() {
            return EditorGUIUtility.singleLineHeight * 2; // operator + operands
        }

        public override void Draw(Rect rect, ref bool changed) {
            var rects = new PropertyDrawerRects(rect);
            var newOperator = (Operator)EditorGUI.EnumPopup(rects.AllocateLine(), @operator);
            if (newOperator != @operator) {
                @operator = newOperator;
                changed = true;
            }
            var operandRect = rects.AllocateLine();

            if (@operator is Operator.InRange or Operator.OutOfRange) {
                var operandRects = new PropertyDrawerRects(operandRect);
                var newOperand1 = EditorGUI.FloatField(operandRects.AllocateLeftNormalized(0.5f), operand1);
                if (!newOperand1.Equals(operand1)) {
                    operand1 = newOperand1;
                    changed = true;
                }
                var newOperand2 = EditorGUI.FloatField((Rect)operandRects, operand2);
                if (!newOperand2.Equals(operand2)) {
                    operand2 = newOperand2;
                    changed = true;
                }
            } else {
                var newOperand1 = EditorGUI.FloatField(operandRect, operand1);
                if (!newOperand1.Equals(operand1)) {
                    operand1 = newOperand1;
                    changed = true;
                }
                operand2 = 0;
            }
        }

        enum Operator : byte {
            Less,
            LessOrEqual,
            Equal,
            GreaterOrEqual,
            Greater,
            InRange,
            OutOfRange,
        }
    }
}