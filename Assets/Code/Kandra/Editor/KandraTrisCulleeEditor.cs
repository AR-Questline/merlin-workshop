using UnityEditor;
using UnityEngine;

namespace Awaken.Kandra.Editor {
    [CustomEditor(typeof(KandraTrisCullee))]
    public class KandraTrisCulleeEditor : UnityEditor.Editor {
        bool _showDebug;

        bool _expandedCullers;

        KandraTrisCuller _cullTarget;
        KandraTrisCuller _uncullTarget;

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            var cullee = (KandraTrisCullee)target;
            _showDebug = EditorGUILayout.Foldout(_showDebug, "Debug", true);

            if (!_showDebug) {
                return;
            }

            ++EditorGUI.indentLevel;
            ref readonly var cullers = ref KandraTrisCullee.EditorAccess.Cullers(cullee);
            _expandedCullers = EditorGUILayout.Foldout(_expandedCullers, $"Cullers: {cullers.Count}", true);
            if (_expandedCullers) {
                ++EditorGUI.indentLevel;
                for (int i = 0; i < cullers.Count; i++) {
                    var culler = cullers[i];
                    EditorGUILayout.ObjectField($"Culler {i}:", culler, typeof(KandraTrisCuller), true);
                }
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.BeginHorizontal();
            {
                _cullTarget = (KandraTrisCuller)EditorGUILayout.ObjectField("Cull Target:", _cullTarget, typeof(KandraTrisCuller), true);

                using var disabledScope = new EditorGUI.DisabledScope(_cullTarget == null);
                if (GUILayout.Button("Cull")) {
                    _cullTarget.Cull(cullee);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                _uncullTarget = (KandraTrisCuller)EditorGUILayout.ObjectField("Uncull Target:", _uncullTarget, typeof(KandraTrisCuller), true);

                using var disabledScope = new EditorGUI.DisabledScope(_uncullTarget == null);
                if (GUILayout.Button("Uncull")) {
                    _uncullTarget.Uncull(cullee);
                }
            }
            EditorGUILayout.EndHorizontal();

            if(GUILayout.Button("Update Culled Mesh")) {
                KandraTrisCullee.EditorAccess.UpdateCulledMesh(cullee);
            }
        }
    }
}