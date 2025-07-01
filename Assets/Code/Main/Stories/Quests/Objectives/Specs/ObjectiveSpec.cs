using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Stats.StatConfig;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Main.Utility.Tags;
using Awaken.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Specs {
    public class ObjectiveSpec : ObjectiveSpecBase {
        const string TextGroup = "Texts";
        const string MarkersGroup = "Markers";
        const string ObjectivesGroup = "Objectives";
        
        [SerializeField, FoldoutGroup(TextGroup), LabelText("Quest Log"), Tooltip("Used in quest log. If Tracker is empty it's also used on HUD."), LocStringCategory(Category.Quest)] 
        LocString description;
        
        [SerializeField, LabelText("Override Tracker Description"), Tooltip("By default Quest Log desc gets used in Trackers, check this to override it with either nothing or custom text.")]
        [FoldoutGroup(TextGroup), Toggle(nameof(OptionalLocString.toggled)), LocStringCategory(Category.Quest)]
        OptionalLocString trackerDescription;
        
        [SerializeField, FoldoutGroup(ExpGroup), LabelText("Change Target Level"), Tooltip("If false, quest target level will be used instead.")] 
        bool overrideTargetLevel;
        [SerializeField, FoldoutGroup(ExpGroup), ShowIf(nameof(overrideTargetLevel)), HideLabel]
        [Tooltip("Exp reward for completed objective will be calculated based on target lvl and Xp Gain Range.")] 
        int targetLevel;
        [SerializeField, FoldoutGroup(ExpGroup), Tooltip("You can find multipliers for given ranges in Common References -> Systems.")] 
        StatDefinedRange xpGainRange = StatDefinedRange.Custom;
        [FoldoutGroup(ExpGroup), ShowIf("@"+nameof(xpGainRange)+"=="+nameof(StatDefinedRange)+".Custom"), SerializeField] 
        float experiencePoints;

        [SerializeField, FoldoutGroup(MarkersGroup), Tooltip("If true, marker will be shown only if given flag is true.")] 
        bool showMarkerWhenStoryFlag;
        [SerializeField, ShowIf(nameof(showMarkerWhenStoryFlag)), FoldoutGroup(MarkersGroup), Tooltip("Flag that will determine whether marker should be shown.")] 
        FlagLogic relatedStoryFlag;
        [SerializeField, FoldoutGroup(MarkersGroup), Tooltip("Location that will be used as target marker for this objective.")] 
        LocationReference targetLocationReference = new() { targetTypes = TargetType.Tags };
        [SerializeField, FoldoutGroup(MarkersGroup), Tooltip("Scene on which the marker should be found, marker is shown on Portal to that scene if we're somewhere else.")] 
        SceneReference targetScene;
        [SerializeField, FoldoutGroup(MarkersGroup), Tooltip("Additional markers can be added to support different tags, scenes or conditions, or make a moving flow of markers.")] 
        [ListDrawerSettings(ShowIndexLabels = false, ShowPaging = false, ShowFoldout = false)]
        MarkersCreationData[] additionalMarkers = Array.Empty<MarkersCreationData>();

        [TitleGroup(ObjectivesGroup, boldTitle: false), SerializeField]
        bool canBeCompletedMultipleTimes;
        [TitleGroup(ObjectivesGroup, boldTitle: false), SerializeField, ListDrawerSettings(ShowIndexLabels = false, ShowPaging = false, ShowFoldout = false)]
        [LabelText("On Completed - Change Objectives", Icon = SdfIconType.Check, IconColor = ARColor.EditorMediumGreen)]
        List<ObjectiveChange> autoObjectiveChangesOnCompleted = new();
        [TitleGroup(ObjectivesGroup), SerializeField, ListDrawerSettings(ShowIndexLabels = false, ShowPaging = false, ShowFoldout = false)]
        [LabelText("On Failed - Change Objectives", Icon = SdfIconType.X, IconColor = ARColor.EditorLightRed)]
        List<ObjectiveChange> autoObjectiveChangesOnFailed = new();

        // === ObjectiveSpecBase
        public override LocString Description => description;
        public override OptionalLocString TrackerDescription => trackerDescription;
        public override int TargetLevel => overrideTargetLevel ? targetLevel : base.TargetLevel;
        public override StatDefinedRange ExperienceGainRange => xpGainRange;
        public override float ExperiencePoints => experiencePoints;
        public override bool IsMarkerRelatedToStory => showMarkerWhenStoryFlag;
        public override bool CanBeCompletedMultipleTimes => canBeCompletedMultipleTimes;
        public override FlagLogic RelatedStoryFlag => relatedStoryFlag;
        public override LocationReference TargetLocationReference => targetLocationReference;
        public override SceneReference TargetScene => targetScene;
        protected override MarkersCreationData[] AdditionalMarkers => additionalMarkers;
        public override IEnumerable<ObjectiveChange> AutoRunAfterCompletion => autoObjectiveChangesOnCompleted;
        public override IEnumerable<ObjectiveChange> AutoRunAfterFailure => autoObjectiveChangesOnFailed;
    }
}