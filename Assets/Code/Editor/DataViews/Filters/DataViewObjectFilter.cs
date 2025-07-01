using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.DataViews.Structure;
using Awaken.TG.Editor.Helpers;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.DataViews.Filters {
    [Serializable]
    public class DataViewObjectFilter : DataViewFilter {
        [SerializeField] Operator @operator;
        [SerializeField] List<Object> operands = new();
        
        public override bool Match(DataViewValue value) {
            return @operator switch {
                Operator.IsOneOf => operands.Contains(value.objectReferenceValue),
                Operator.IsNot => !operands.Contains(value.objectReferenceValue),
                _ => false,
            };
        }

        public override float DrawHeight() {
            return EditorGUIUtility.singleLineHeight * (1 + operands.Count + 1); // operator + operands + add button
        }

        public override void Draw(Rect rect, ref bool changed) {
            var rects = new PropertyDrawerRects(rect);
            var newOperator = (Operator)EditorGUI.EnumPopup(rects.AllocateLine(), @operator);
            if (newOperator != @operator) {
                @operator = newOperator;
                changed = true;
            }
            for (int i = 0; i < operands.Count; i++) {
                var enumRect = rects.AllocateLine();
                var enumRects = new PropertyDrawerRects(enumRect);
                var operand = operands[i];
                var newOperand = EditorGUI.ObjectField(enumRects.AllocateWithRest(80), operand, typeof(Object), false);
                if (!newOperand == operand) {
                    operands[i] = newOperand;
                    changed = true;
                }
                if (GUI.Button((Rect)enumRects, "X")) {
                    operands.RemoveAt(i);
                    i--;
                    changed = true;
                }
            }
            if (GUI.Button(rects.AllocateLine(), "Add")) {
                operands.Add(default);
                changed = true;
            }
        }

        enum Operator : byte {
            IsOneOf,
            IsNot
        }
    }
}