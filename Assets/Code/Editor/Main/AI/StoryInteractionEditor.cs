using Awaken.TG.Main.AI.Idle.Interactions;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.Editor.Main.AI {
    [CustomEditor(typeof(StoryInteraction))]
    public class StoryInteractionEditor : OdinEditor {

        void OnSceneGUI() {
            OnSceneGUI(target as StoryInteraction);
        }
        

        void OnSceneGUI(StoryInteraction storyInteraction) {
            EditorGUI.BeginChangeCheck();
            StoryInteraction.EDITOR_Accessor accessor;
        
            var previousZTest = Handles.zTest;
            var previousColor = Handles.color;

            float newRange = accessor.Range(ref storyInteraction);
            DrawRangeHandle(storyInteraction.transform.position, ref newRange, CompareFunction.Greater, Color.gray);
            DrawRangeHandle(storyInteraction.transform.position, ref newRange, CompareFunction.Less, Color.white);

            Handles.zTest = previousZTest;
            Handles.color = previousColor;
        
            if (EditorGUI.EndChangeCheck() && !Application.isPlaying) {
                Undo.RecordObject(storyInteraction, "Change idleBehaviour range");
                accessor.Range(ref storyInteraction) = newRange;
            }
        }

        void DrawRangeHandle(Vector3 position, ref float range, CompareFunction zTest, Color color) {
            Handles.zTest = zTest;
            Handles.color = color;
            range = Handles.RadiusHandle(Quaternion.identity, position, range);
        }
    }
}