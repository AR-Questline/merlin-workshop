using System.Linq;
using Awaken.TG.Editor.Main.Stories.Drawers;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEditor;

namespace Awaken.TG.Editor.Main.Stories.Steps {
    [CustomElementEditor(typeof(CEditorQuestObjective))]
    public class CQuestObjectiveEditor : ElementEditor {

        protected override void OnElementGUI() {
            DrawPropertiesExcept("objectiveName", "requiredState");

            CEditorQuestObjective step = Target<CEditorQuestObjective>();

            if (step.questRef.IsSet) {
                QuestTemplate questTemplate = step.questRef.Get<QuestTemplate>();
                if (questTemplate == null) {
                    Log.Important?.Warning($"Invalid template assigned");
                    return;
                }

                using var objectiveSpecs = questTemplate.ObjectiveSpecs;
                string[] possibleObjectives = objectiveSpecs.value.Select(os => os.GetName()).ToArray();

                int chosen = possibleObjectives.IndexOf(questTemplate.Editor_GetNameOfObjectiveSpec(step.objectiveGuid));
                if (chosen == -1) {
                    chosen = 0;
                    step.objectiveGuid = possibleObjectives.Length > 0 ? questTemplate.Editor_GetGuidOfObjectiveSpec(possibleObjectives[0]) : "";
                }

                EditorGUI.BeginChangeCheck();
                chosen = EditorGUILayout.Popup(chosen, possibleObjectives);
                if (EditorGUI.EndChangeCheck()) {
                    step.objectiveGuid = questTemplate.Editor_GetGuidOfObjectiveSpec(possibleObjectives[chosen]);
                }

                DrawProperties("requiredState");
            }
        }
    }
}
