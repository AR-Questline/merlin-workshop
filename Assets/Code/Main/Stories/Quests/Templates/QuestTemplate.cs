using System.Collections.Generic;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Stories.Quests.UI;
using Awaken.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.Templates {
    public class QuestTemplate : QuestTemplateBase {
        const string ObjectivesGroup = "Objectives";
        
        // === Unique values
        // -- Implementation 
        [SerializeField, PropertyOrder(1)]
        QuestType questType = QuestType.Main;
        [SerializeField, PropertyOrder(1)]
        QuestCategory questCategory = QuestCategory.Default;

        [SerializeField, TitleGroup(ObjectivesGroup, order: 3), Tooltip("If true, objectives will be automatically completed when quest is completed.")]
        bool autoCompleteLeftObjectives;
        [SerializeField, TitleGroup(ObjectivesGroup), Tooltip("Objectives that should be automatically started when quest is started.")]
        List<ObjectiveSpec> autoRunObjectives = new();
        [SerializeField, TitleGroup(ObjectivesGroup), Tooltip("Should quest be automatically completed when chosen objectives are completed?")]
        bool autoCompletion;
        [SerializeField, ShowIf(nameof(autoCompletion)), TitleGroup(ObjectivesGroup), Tooltip("When these objectives are completed, quest will complete automatically.")]
        [LabelText("On Objectives Completed -> Complete Quest", Icon = SdfIconType.Check, IconColor = ARColor.EditorMediumGreen)]
        List<ObjectiveSpec> autoCompleteAfter = new();
        
        // === QuestTemplateBase
        public override QuestType TypeOfQuest => questType;
        public override bool AutoCompleteLeftObjectives => autoCompleteLeftObjectives;
        public override IEnumerable<ObjectiveSpecBase> AutoRunObjectives => autoRunObjectives;
        public override bool AutoCompletion => autoCompletion;
        public override IEnumerable<ObjectiveSpecBase> AutoCompleteAfter => autoCompleteAfter;
        public QuestCategory QuestCategory => questCategory;
    }
}