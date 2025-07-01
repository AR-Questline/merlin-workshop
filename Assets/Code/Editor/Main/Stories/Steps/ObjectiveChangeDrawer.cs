using System.Linq;
using Awaken.TG.Editor.Main.Stories.Drawers;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Editor;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.Main.Stories.Steps {
    [CustomElementEditor(typeof(SEditorObjectiveChange))]
    public class SObjectiveChangeEditor : ElementEditor {
        protected override void OnElementGUI() {
            GUILayout.Space(5);
            GUIUtils.PushLabelWidth(190);
            DrawPropertiesExcept(nameof(SEditorObjectiveChange.newState), nameof(SEditorObjectiveChange.objectiveGuid));
            GUIUtils.PopLabelWidth();

            SEditorObjectiveChange step = Target<SEditorObjectiveChange>();

            if (step.questRef.IsSet) {
                QuestTemplate questTemplate = step.questRef.Get<QuestTemplate>();
                if (questTemplate == null) {
                    Log.Important?.Warning("Invalid template assigned");
                    return;
                }

                using var objectiveSpecs = questTemplate.ObjectiveSpecs;
                string[] possibleObjectives = objectiveSpecs.value.Select(os => os.GetName()).ToArray();

                int chosen = possibleObjectives.IndexOf(questTemplate.Editor_GetNameOfObjectiveSpec(step.objectiveGuid));
                if (chosen == -1) {
                    chosen = 0;
                    step.objectiveGuid = possibleObjectives.Length > 0 ? questTemplate.Editor_GetGuidOfObjectiveSpec(possibleObjectives[0]) : "";
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                GUIUtils.PushLabelWidth(5);
                GUIUtils.PushFieldWidth(100);
                
                chosen = EditorGUILayout.Popup(chosen, possibleObjectives);
                if (EditorGUI.EndChangeCheck()) {
                    step.objectiveGuid = questTemplate.Editor_GetGuidOfObjectiveSpec(possibleObjectives[chosen]);
                }

                EditorGUILayout.Space(3);
                DrawProperties(nameof(SEditorObjectiveChange.newState));
                
                GUIUtils.PopLabelWidth();
                GUIUtils.PopFieldWidth();
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}