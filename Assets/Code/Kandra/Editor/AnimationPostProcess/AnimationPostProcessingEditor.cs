using System.Collections;
using System.Collections.Generic;
using Awaken.Kandra.AnimationPostProcess;
using Awaken.Utility.UI;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Awaken.Kandra.Editor.AnimationPostProcess {
    [CustomEditor(typeof(AnimationPostProcessing))]
    public class AnimationPostProcessingEditor : UnityEditor.Editor {
        bool _debugFoldout;

        bool _additionalEntriesFoldout;
        ImguiTable<AnimationPostProcessing.Entry> _additionalEntriesList;
        Vector2 _additionalEntriesScroll;

        bool _dataFoldout;
        DataWrapper _dataWrapper;
        ImguiTable<DataWrapper.Data> _dataList;
        Vector2 _dataScroll;

        bool _batchDataFoldout;
        ImguiTable<int> _batchDataList;
        Vector2 _batchDataScroll;

        void OnEnable() {
            _additionalEntriesList = new ImguiTable<AnimationPostProcessing.Entry>(
                (_, _) => true,
                EditorGUIUtility.singleLineHeight,
                ImguiTable<AnimationPostProcessing.Entry>.ColumnDefinition.Create("Preset", Width.Flexible(0.5f), DrawPreset, e => e.preset.name),
                ImguiTable<AnimationPostProcessing.Entry>.ColumnDefinition.Create("Weight", Width.Flexible(0.5f), DrawWight, e => e.weight)
                );
            _additionalEntriesList.ShowHeader = false;
            _additionalEntriesList.ShowToolbar = false;
            _additionalEntriesList.ShowFooter = false;
            _additionalEntriesList.Margin = EditorGUIUtility.standardVerticalSpacing / 2f;

            _dataWrapper = new DataWrapper();
            _dataList = new ImguiTable<DataWrapper.Data>(
                (_, _) => true,
                EditorGUIUtility.singleLineHeight,
                ImguiTable<DataWrapper.Data>.ColumnDefinition.Create("Transform", Width.Flexible(0.2f), DrawDataTransform, i => i.transform.name),
                ImguiTable<DataWrapper.Data>.ColumnDefinition.Create("Position", Width.Flexible(0.4f), DrawDataPosition, i => i.position.x),
                ImguiTable<DataWrapper.Data>.ColumnDefinition.Create("Scale", Width.Flexible(0.4f), DrawDataScale, i => i.scale.y)
            );
            _dataList.ShowHeader = false;
            _dataList.ShowToolbar = false;
            _dataList.ShowFooter = false;
            _dataList.Margin = EditorGUIUtility.standardVerticalSpacing / 2f;

            _batchDataList = new ImguiTable<int>(
                (_, _) => true,
                EditorGUIUtility.singleLineHeight,
                ImguiTable<int>.ColumnDefinition.Create("Value", Width.Flexible(1), DrawBatchStartIndex, i => i)
            );
            _batchDataList.ShowHeader = false;
            _batchDataList.ShowToolbar = false;
            _batchDataList.ShowFooter = false;
            _batchDataList.Margin = EditorGUIUtility.standardVerticalSpacing / 2f;
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            var pp = (AnimationPostProcessing)target;

            _debugFoldout = EditorGUILayout.Foldout(_debugFoldout, "Debug", true);
            if (!_debugFoldout) {
                return;
            }

            var elementSize = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var maxHeight = 5 * elementSize;
            var width = EditorGUILayout.GetControlRect(false, 0).width - 16; // Minus scroll size

            ++EditorGUI.indentLevel;

            _additionalEntriesFoldout = EditorGUILayout.Foldout(_additionalEntriesFoldout, "Additional Entries", true);
            if (_additionalEntriesFoldout) {
                var additionalEntries = AnimationPostProcessing.EditorAccess.AdditionalEntries(pp);
                var height = math.min(maxHeight, elementSize * additionalEntries.Length);
                _additionalEntriesScroll = EditorGUILayout.BeginScrollView(_additionalEntriesScroll, GUILayout.Height(height));
                EditorGUI.BeginDisabledGroup(true);
                _additionalEntriesList.Draw(additionalEntries, height, _additionalEntriesScroll.y, width);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndScrollView();
            }

            _dataFoldout = EditorGUILayout.Foldout(_dataFoldout, "Data", true);
            if (_dataFoldout) {
                _dataWrapper.transforms = pp.transforms;
                _dataWrapper.positions = pp.positions;
                _dataWrapper.scales = pp.scales;

                var height = math.min(maxHeight, elementSize * _dataWrapper.Count);
                _dataScroll = EditorGUILayout.BeginScrollView(_dataScroll, GUILayout.MaxHeight(height));
                EditorGUI.BeginDisabledGroup(true);
                _dataList.Draw(_dataWrapper, maxHeight, _dataScroll.y, width);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndScrollView();
            }

            _batchDataFoldout = EditorGUILayout.Foldout(_batchDataFoldout, "Batch Start Index", true);
            if (_batchDataFoldout) {
                var height = math.min(maxHeight, elementSize * pp.batchStartIndex.Length);
                _batchDataScroll = EditorGUILayout.BeginScrollView(_batchDataScroll, GUILayout.MaxHeight(height));
                EditorGUI.BeginDisabledGroup(true);
                _batchDataList.Draw(pp.batchStartIndex, maxHeight, _batchDataScroll.y, width);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndScrollView();
            }

            --EditorGUI.indentLevel;
        }

        static void DrawPreset(in Rect rect, AnimationPostProcessing.Entry element) {
            EditorGUI.ObjectField(rect, element.preset, typeof(AnimationPostProcessingPreset), false);
        }

        static void DrawWight(in Rect rect, AnimationPostProcessing.Entry element) {
            EditorGUI.Slider(rect, element.weight, -1, 1);
        }

        static void DrawDataTransform(in Rect rect, DataWrapper.Data element) {
            EditorGUI.ObjectField(rect, element.transform, typeof(Transform), true);
        }

        static void DrawDataPosition(in Rect rect, DataWrapper.Data element) {
            EditorGUI.Vector3Field(rect, GUIContent.none, element.position);
        }

        static void DrawDataScale(in Rect rect, DataWrapper.Data element) {
            EditorGUI.Vector3Field(rect, GUIContent.none, element.scale);
        }

        static void DrawBatchStartIndex(in Rect rect, int value) {
            EditorGUI.IntField(rect, value);
        }

        class DataWrapper : IReadOnlyList<DataWrapper.Data> {
            public Transform[] transforms;
            public Vector3[] positions;
            public Vector3[] scales;

            public IEnumerator<Data> GetEnumerator() {
                throw new System.NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }

            public int Count => transforms.Length;

            public Data this[int index] => new(transforms[index], positions[index], scales[index]);

            public struct Data {
                public Transform transform;
                public Vector3 position;
                public Vector3 scale;

                public Data(Transform transform, Vector3 position, Vector3 scale) {
                    this.transform = transform;
                    this.position = position;
                    this.scale = scale;
                }
            }
        }
    }
}