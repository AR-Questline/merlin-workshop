using Awaken.Utility;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.General.NewThings;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Stories.Quests.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Stories.Quests {
    /// <summary>
    /// This model represents a known Quest.
    /// Known quests are the one displayed in Quest Log.
    /// </summary>
    public sealed partial class Quest : Model, IModelNewThing, INamed {
        public override ushort TypeForSerialization => SavedModels.Quest;

        public override Domain DefaultDomain => Domain.Gameplay;

        // === Properties
        [Saved] public QuestTemplateBase Template { get; private set; }
        [Saved] public bool DisplayedByPlayer { get; set; }
        [Saved(false)] public bool IsTracked { get; private set; }
        [Saved] AttachmentTracker _tracker;
        
        public float ExperiencePoints { get; private set; }

        public string DisplayName => Template.displayName;
        public string DebugName => Template?.DebugName ?? "Missing Template";
        public string Description => Template.description;
        [UnityEngine.Scripting.Preserve] public int TargetLevel => ActiveObjectives.Any() ? ActiveObjectives.Max(o => o.TargetLevel) : Template.targetLvl;
        public bool ShowQuestMarkers => Template.showQuestMarkers;
        public ModelsSet<Objective> Objectives => Elements<Objective>();
        public IEnumerable<Objective> ActiveObjectives => Objectives.Where(o => o.State == ObjectiveState.Active);
        public IEnumerable<Objective> ActiveObjectivesWithMarkers => ActiveObjectives.Where(o => o.AnyMarkerVisible);
        public QuestState State => Services.Get<GameplayMemory>().Context(this).Get("state", QuestState.NotTaken);
        protected override bool OnSave() => Template != null;
        public QuestType QuestType => Template.TypeOfQuest;
        public bool CanBeTracked => QuestType is QuestType.Main or QuestType.Side or QuestType.Misc;
        public bool ShowNotificationTrackPrompt => CanBeTracked;
        public bool VisibleInQuestLog => CanBeTracked || QuestType == QuestType.Misc;
        public string NewThingId => Template?.GUID;
        public bool DiscardAfterMarkedAsSeen => false;

        public override string ContextID => QuestUtils.ContextID(this);

        // === Constructors
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        Quest() { }

        public Quest(QuestTemplateBase template) {
            Template = template;
        }

        // === Initialization
        protected override void OnInitialize() {
            Init();
            _tracker = new AttachmentTracker();
            _tracker.SetOwner(this);
            using var attachmentGroups = Template.GetAttachmentGroups();
            _tracker.Initialize(attachmentGroups.value);
            this.ListenTo(Events.AfterFullyInitialized, SetActive, this);
        }

        protected override void OnAfterDeserialize() {
            _tracker.SetOwner(this);
        }

        protected override void OnPreRestore() {
            using var attachmentGroups = Template.GetAttachmentGroups();
            _tracker.PreRestore(attachmentGroups.value);
            base.OnPreRestore();
        }

        protected override void OnRestore() {
            Init();
        }

        void Init() {
            ExperiencePoints = QuestUtils.CalculateXp(Template.targetLvl, Template.xpGainRange, Template.experiencePoints);
            this.ListenTo(QuestUtils.Events.ObjectiveChanged, () => QuestUtils.TryAutocomplete(this), this);
        }

        void SetActive() {
            QuestUtils.SetQuestState(this, QuestState.Active);
            this.Trigger(QuestUtils.Events.ActiveObjectivesEstablished, this);
        }

        // === Operations
        public void ToggleTracking(bool isTracked) {
            IsTracked = isTracked;
            foreach (var objective in Elements<Objective>()) {
                objective.TrackingChanged();
            }
        }
    }
}