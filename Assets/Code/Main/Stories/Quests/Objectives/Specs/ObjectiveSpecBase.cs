using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Heroes.Stats.StatConfig;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.Tags;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Collections;
using Awaken.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Specs {
    public abstract class ObjectiveSpecBase : MonoBehaviour, IAttachmentGroup, IAttachmentSpec {
        protected const string ExpGroup = "Exp";
        [SerializeField, HideInInspector] string guid;

        public abstract LocString Description { get; }
        public abstract OptionalLocString TrackerDescription { get; }
        public virtual int TargetLevel => GetComponentInParent<QuestTemplateBase>(true)?.targetLvl ?? 1;
        public abstract StatDefinedRange ExperienceGainRange { get; }
        public abstract float ExperiencePoints { get; }
        public abstract bool IsMarkerRelatedToStory { get; }
        public abstract bool CanBeCompletedMultipleTimes { get; }
        public abstract FlagLogic RelatedStoryFlag { get; }
        public abstract LocationReference TargetLocationReference { get; }
        public virtual SceneReference TargetScene => null;
        protected virtual MarkersCreationData[] AdditionalMarkers => Array.Empty<MarkersCreationData>();
        public virtual IEnumerable<ObjectiveChange> AutoRunAfterCompletion => Enumerable.Empty<ObjectiveChange>();
        public virtual IEnumerable<ObjectiveChange> AutoRunAfterFailure => Enumerable.Empty<ObjectiveChange>();

        [FoldoutGroup(ExpGroup), ShowInInspector, HideIf("@" + nameof(ExperienceGainRange) + "==" + nameof(StatDefinedRange) + "." + nameof(StatDefinedRange.Custom))]
        public FloatRange CalculatedExpRange =>
            QuestUtils.CalculateXpRange(TargetLevel, ExperienceGainRange, ExperiencePoints);

        public string GetName() => gameObject.name;

        public string AttachGroupId => Template.DefaultAttachmentGroupName;
        public bool StartEnabled => true;

        public IEnumerable<IAttachmentSpec> GetAttachments() {
            PooledList<IAttachmentSpec>.Get(out var attachmentSpecs);
            GetComponents<IAttachmentSpec>(attachmentSpecs);
            foreach (var attachmentSpec in attachmentSpecs.value) {
                if (attachmentSpec is not IAttachmentGroup) {
                    yield return attachmentSpec;
                }
            }
            attachmentSpecs.Release();
        }

        public Element SpawnElement() => new Objective();
        public bool IsMine(Element element) => element is Objective obj && obj.Guid == Guid;

        [ShowInInspector, ReadOnly, PropertyOrder(-1)] public string Guid => guid;

        void OnValidate() {
            //Ensure new guid when copied/pasted objective spec.
            EnsureValidGuid();
        }

        // === Possible Attachments (EDITOR)
        const string PossibleAttachmentsGroup = "Possible Attachments";
        
        static Dictionary<AttachmentCategory, PossibleAttachmentsGroup> s_possibleAttachments;
        static Dictionary<AttachmentCategory, PossibleAttachmentsGroup> PossibleAttachments => s_possibleAttachments ??= PossibleAttachmentsUtil.Get(typeof(ObjectiveSpecBase));

        [TitleGroup(PossibleAttachmentsGroup, order:999, boldTitle: false), ShowInInspector, HideReferenceObjectPicker]
        [LabelText(nameof(AttachmentCategory.Trackers), icon: SdfIconType.InfoCircleFill, IconColor = ARColor.EditorLightYellow)]
        PossibleAttachmentsGroup TrackersGroup {
            get => PossibleAttachments.TryGetValue(AttachmentCategory.Trackers, out var group) ? group.WithContext(this) : null;
            set => PossibleAttachments[AttachmentCategory.Trackers] = value;
        }
        
        [TitleGroup(PossibleAttachmentsGroup), ShowInInspector, HideReferenceObjectPicker]
        [LabelText(nameof(AttachmentCategory.Effectors), icon: SdfIconType.InfoCircleFill, IconColor = ARColor.EditorLightBrown, NicifyText = true)]
        PossibleAttachmentsGroup EffectorsGroup {
            get => PossibleAttachments.TryGetValue(AttachmentCategory.Effectors, out var group) ? group.WithContext(this) : null;
            set => PossibleAttachments[AttachmentCategory.Effectors] = value;
        }

        public Objective.MarkerData[] GetMarkersData() {
            var trackersData = new Objective.MarkerData[AdditionalMarkers.Length + 1];
            trackersData[0] = new Objective.MarkerData(IsMarkerRelatedToStory, RelatedStoryFlag, TargetLocationReference ,TargetScene);
            for(int i = 0; i < AdditionalMarkers.Length; i++) {
                trackersData[i + 1] = AdditionalMarkers[i].ToMarker();
            }
            return trackersData;
        }
        
        void EnsureValidGuid() {
            var questTemplate = GetComponentInParent<QuestTemplateBase>();
            if (questTemplate) {
                if (string.IsNullOrEmpty(guid)) { 
                    guid = System.Guid.NewGuid().ToString();
                    return;
                }
                using var objectiveSpecs = questTemplate.ObjectiveSpecs;
                if(objectiveSpecs.value.Count(o => o.guid == guid) > 1)
                {
                    guid = System.Guid.NewGuid().ToString();
                }
            }
        }

        [Serializable]
        protected struct MarkersCreationData {
            [SerializeField, Tooltip("If true, marker will be shown only if given flag is true.")] 
            bool showMarkerWhenStoryFlag;
            [SerializeField, ShowIf(nameof(showMarkerWhenStoryFlag)), Tooltip("Flag that will determine whether marker should be shown.")] 
            FlagLogic relatedStoryFlag;
            [SerializeField, Tooltip("Location that will be used as target marker for this objective.")] 
            LocationReference targetLocationReference;
            [SerializeField, Tooltip("Scene on which the marker should be found, marker is shown on Portal to that scene if we're somewhere else.")] 
            SceneReference targetScene;
            
            public Objective.MarkerData ToMarker() {
                return new Objective.MarkerData(showMarkerWhenStoryFlag, relatedStoryFlag, targetLocationReference, targetScene);
            }
        }
    }
}