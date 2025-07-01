using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Skills;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Skills.Passives;
using Awaken.TG.Main.Utility.TokenTexts;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Tooltips;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Statuses {
    /// <summary>
    /// Status if wrapper for skills, there are two main skills kind, Activation and Effect
    /// Activation - skill which decides if effects should be apply
    /// Effect - skill which produce outcome
    /// </summary>
    public partial class Status : Element<CharacterStatuses>, INamed, ITextVariablesContainer, ISkillOwner {
        public override ushort TypeForSerialization => SavedModels.Status;

        // === State

        [Saved] public StatusTemplate Template { get; private set; }
        [Saved] public StatusSourceInfo SourceInfo { get; private set; }
        [Saved(0)] int _stackLevel;

        TokenText _descriptionToken;
        SkillVariablesOverride _variableOverride;

        // === Getters
        
        public bool HasDuration => HasElement<IDuration>();
        public float? TimeLeftSeconds => TimeDuration?.TimeLeft;
        [UnityEngine.Scripting.Preserve]  public float? TimeLeftNormalized => TimeDuration?.TimeLeftNormalized;

        [CanBeNull]
        public Skill Skill => TryGetElement<Skill>();
        public float? GetVariable(string id, int index = 0, ICharacter owner = null) => Skill?.GetVariable(id, owner ?? Character);
        public StatType GetEnum(string id, int index = 0) => Skill?.GetRichEnum(id);
        
        public StatusType Type => Template.StatusType;
        public string DisplayName => Template?.displayName;
        public string DebugName => Template?.DebugName ?? "Template null";

        [UnityEngine.Scripting.Preserve] public IEnumerable<Keyword> Keywords => 
            Template.Keywords.Concat(Skill?.Keywords ?? Enumerable.Empty<Keyword>());
        public ShareableSpriteReference Icon => Template.iconReference;
        public bool HiddenOnUI => Template.hiddenOnUI;
        public bool HiddenOnHUD => HiddenOnUI || Template.hiddenOnHUD;
        public int StackLevel => _stackLevel;
        public virtual float EffectModifier => 1;

        public string Description => SourceInfo != null ? SourceInfo.SourceDescription : Template.description;
        TokenText DescriptionToken => _descriptionToken ??= new TokenText(Description);
        public string StatusDescription => DescriptionToken.GetValue(Character, this);

        public ICharacter Character => ParentModel.ParentModel;
        public StatusDuration DurationWrapper => TryGetElement<StatusDuration>();
        TimeDuration TimeDuration => DurationWrapper?.Duration as TimeDuration;

        // === Initialization
        
        [JsonConstructor, UnityEngine.Scripting.Preserve] Status() { }

        public Status(StatusTemplate template, StatusSourceInfo sourceInfo, SkillVariablesOverride variableOverride = null) {
            Template = template;
            SourceInfo = sourceInfo;
            _variableOverride = variableOverride;
        }

        // === Events

        public new static class Events {
            public static readonly Event<Status, int> StatusStackChanged = new(nameof(StatusStackChanged));
        }
        
        // === Lifetime

        protected override void OnInitialize() {
            SkillInitialization.Initialize(this, (Template.skill, _variableOverride).Yield(), SkillState.Learned);
            _variableOverride = null;
        }

        protected override void OnRestore() {
            SkillInitialization.CustomRestore(this, Template.skill.Yield(), SkillState.Learned);
        }

        protected override void OnFullyInitialized() {
            ParentModel.ListenTo(Model.Events.AfterChanged, TriggerChange, this);
            World.EventSystem.ListenTo(EventSelector.AnySource, Hero.Events.BeforeHeroRested, this, OnHeroRested);
        }

        protected override bool OnSave() {
            return !Template.notSaved;
        }

        void OnHeroRested(int gameTimeInMinutes) {
            Skill?.TryGetElement<PassiveStatModifier>()?.RestoreStatWhenResting(gameTimeInMinutes);
            TimeDuration?.ReduceTimeWhenResting(gameTimeInMinutes);
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel.Trigger(CharacterStatuses.Events.VanishedStatus, this);
        }

        // === Duration Operation

        public void Renew(IDuration duration) {
            if (duration == null) return;

            if (HasDuration) {
                DurationWrapper.Renew(duration);
            } else {
                AttachDuration(duration);
            }
        }

        public void Prolong(IDuration duration) {
            if (duration == null) return;
            
            if (HasDuration) {
                DurationWrapper.Prolong(duration);
            } else {
                AttachDuration(duration);
            }
        }

        public void AttachDuration(IDuration duration) {
            StatusDuration durationEle;
            if (Template.AddType == StatusAddType.Stack && Template.TimerShouldDeStackInsteadOfCancelingEffect) {
                durationEle = StackableStatusDuration.Create(this, duration);
            } else {
                durationEle = duration as StatusDuration ?? StatusDuration.Create(this, duration);
            }
            AddElement(durationEle);
        }
        
        // === Modification
        public void IncreaseStack() {
            _stackLevel++;
            this.Trigger(Events.StatusStackChanged, _stackLevel);
        }
        
        public void IncreaseStacks(int stacks) {
            SetStacksTo(_stackLevel + stacks);
        }

        public void SetStacksTo(int stacks) {
            _stackLevel = stacks;
            this.Trigger(Events.StatusStackChanged, _stackLevel);
        }

        public void ConsumeStack() {
            _stackLevel--;
            if (_stackLevel < 0) _stackLevel = 0;
            this.Trigger(Events.StatusStackChanged, _stackLevel);
        }

        // === Utils
        [UnityEngine.Scripting.Preserve]
        public TooltipConstructor TooltipText {
            get {
                TooltipConstructor constructor = new();
                constructor.WithTitle(DisplayName);
                StringBuilder sb = new("");
                if (HasDuration) {
                    sb.Append($"\n<size=75%><align=center>{LocTerms.Duration.Translate()}: {DurationWrapper.DisplayText}<align=center></size>");
                }

                sb.Append("<line-height=150%>\n</line-height>");
                sb.Append(StatusDescription);
                constructor.WithMainText(sb.ToString());
                return constructor;
            }
        }
    }
}
