using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    public abstract class BaseTrackerAttachment : MonoBehaviour, IAttachmentSpec {
        const string FulfillmentGroup = "Fulfillment";
        
        [FoldoutGroup("Setup"), LabelText("Display Pattern"), InfoBox("$PredefinedDisplayPatternDescription"), RichEnumExtends(typeof(TrackerDisplayPattern)), HideIf(nameof(HasCustomDisplayPattern))]
        public RichEnumReference predefinedDisplayPattern = new(TrackerDisplayPattern.None);
        [FoldoutGroup("Setup"), InfoBox("$DisplayPatternDescription", visibleIfMemberName: nameof(HasCustomDisplayPattern)), Toggle(nameof(OptionalLocString.toggled))] 
        public OptionalLocString customDisplayPattern;
        [FoldoutGroup("Setup")] 
        public bool startDisplayPatternInNewLine = true;

        [TitleGroup(FulfillmentGroup), ShowIf(nameof(HasAnyOtherTracker)), Tooltip("Choose if you want to require all other trackers to be completed or if this tracker should be independent.")]
        public OtherTrackersBehaviour whenOtherTrackersPresent = OtherTrackersBehaviour.RequireAll;
        [TitleGroup(FulfillmentGroup), LabelText("Required Objective State"), Tooltip("Tracker can be updated only when Objective is in this state")]
        public ObjectiveStateFlag requiredObjectiveState = ObjectiveStateFlag.All;
        [TitleGroup(FulfillmentGroup), LabelText("Should Affect"), Tooltip("Choose if you want to affect the quest or the objective. Choosing Quest will affect active objectives automatically with the same state.")]
        public TaskType taskType = TaskType.Objective;
        [TitleGroup(FulfillmentGroup), HideIf(nameof(TaskTypeIsOtherObjectives)), Tooltip("State for quest and objective")]
        public TaskState targetState = TaskState.Completed;
        [TitleGroup(FulfillmentGroup), ShowIf(nameof(TaskTypeIsOtherObjectives)), ListDrawerSettings(ShowIndexLabels = false, ShowPaging = false, ShowFoldout = false)]
        [LabelText("On Fulfilled - Change Objectives", Icon = SdfIconType.Check, IconColor = ARColor.EditorMediumGreen)]
        public List<ObjectiveChange> objectiveChangesOnFulfilled = new();
        [TitleGroup(FulfillmentGroup), ShowIf(nameof(TaskTypeIsOtherObjectives)), ListDrawerSettings(ShowIndexLabels = false, ShowPaging = false, ShowFoldout = false)]
        [LabelText("On Lose Fulfillment - Change Objectives", Icon = SdfIconType.X, IconColor = ARColor.EditorLightRed)]
        public List<ObjectiveChange> objectiveChangesOnLoseFulfillment = new();
        [TitleGroup(FulfillmentGroup), Tags(TagsCategory.Flag), Tooltip("Maps tracker state to the flag. If tracker is fulfilled the flag is set to true.")]
        public string flagToMap;
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        protected virtual string DisplayPatternDescription => "Write a pattern in which quest objective will be displayed. Known variables:";

        string PredefinedDisplayPatternDescription => $"Current pattern: {(string.IsNullOrEmpty(DisplayPattern?.LocalizedPattern) ? "None" : DisplayPattern.LocalizedPattern)}";
        
        public TrackerDisplayPattern DisplayPattern => predefinedDisplayPattern.EnumAs<TrackerDisplayPattern>();
        bool HasCustomDisplayPattern => customDisplayPattern.toggled;
        bool TaskTypeIsOtherObjectives => taskType == TaskType.OtherObjectives;

        public abstract Element SpawnElement();
        public abstract bool IsMine(Element element);
        
        bool HasAnyOtherTracker => GetComponents<BaseTrackerAttachment>().Length > 1;
    }

    public enum OtherTrackersBehaviour {
        RequireAll = 0,
        Independent = 1,
    }
    
    public enum TaskType : byte {
        Objective = 0,
        Quest = 1,
        OtherObjectives = 2,
    }
    
    public enum TaskState : byte {
        Completed = 0,
        Failed = 1,
    }
}