using System.Linq;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Journal {
    public class ObjectiveGuidDrawer 
        // : OdinValueDrawer<QuestObjective>
    {
        // protected override void DrawPropertyLayout(GUIContent label) {
        //     this.Property.Children[nameof(QuestObjective.questTemplate)].Draw();
        //     
        //     var value = this.ValueEntry.SmartValue;
        //     if (value.questTemplate is {IsSet: true}) {
        //         var questTemplate = value.Quest;
        //         
        //         using var objectiveSpecs = questTemplate.ObjectiveSpecs;
        //         string[] possibleObjectives = objectiveSpecs.value.Select(os => os.GetName()).ToArray();
        //
        //         int chosen = possibleObjectives.IndexOf(questTemplate.Editor_GetNameOfObjectiveSpec(value.objectiveGuid));
        //         if (chosen == -1) {
        //             chosen = 0;
        //             value.objectiveGuid = possibleObjectives.Length > 0 ? questTemplate.Editor_GetGuidOfObjectiveSpec(possibleObjectives[0]) : "";
        //             this.ValueEntry.SmartValue = value;
        //         }
        //
        //         var newChosen = SirenixEditorFields.Dropdown(chosen, possibleObjectives);
        //         if (newChosen != chosen) {
        //             value.objectiveGuid = questTemplate.Editor_GetGuidOfObjectiveSpec(possibleObjectives[newChosen]);
        //             this.ValueEntry.SmartValue = value;
        //         }
        //         
        //         GUIHelper.PushGUIEnabled(false);
        //         SirenixEditorFields.TextField("GUID", value.objectiveGuid);
        //         GUIHelper.PopGUIEnabled();
        //     }
        // }
    }
}